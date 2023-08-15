using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;
using rnd = UnityEngine.Random;
using SimonShapesModule;

public class SimonShapesScript : MonoBehaviour
{
	public KMBombInfo Info;
	public KMBombModule Module;
	public KMAudio Audio;
	public KMColorblindMode ColorblindMode;
	
	public List<Color> LightColors; //Red, Green, Blue, Yellow, Magenta, Cyan, Gray
	public List<Material> ButtonColors; //Red, Green, Blue, Yellow, Magenta, Cyan, Gray
	public List<Material> LitColors;
	
	public List<Light> Lights;
	public List<MeshRenderer> ButtonRenderers;
	public List<KMSelectable> Buttons;
	public List<AudioClip> Sounds;
	public List<TextMesh> ColorblindTextMeshes;
	
	private List<Button> _buttons;
	private List<Stage> _stages;

	private string _finalShape;
	private int _stage;
	private int _pressindex;
	private Coroutine _flashRoutine;
	private State _state;
	private bool _audioPlaying;
	private bool _colorbindMode;

	private readonly List<List<SimonShapesColor>> _stageAnswers = new List<List<SimonShapesColor>>();
	private List<List<int>> _possibleFinalShapes;
	private readonly List<List<int>> _flashes = new List<List<int>>();
	
	private int _moduleId;
	private static int _moduleIdCounter = 1;
	private bool _isSolved;
	private bool _animating;

	private bool _interactable
	{
		get
		{
			return !(_isSolved || _animating);
		}
	}

	void Start ()
	{
		_moduleId = _moduleIdCounter++;
		_colorbindMode = ColorblindMode.ColorblindModeActive;
		Module.OnActivate += Activate;
	}

	private void Activate()
	{
		_state = State.Color;
		var solution = Solver.Generate(Info);
		_stages = solution.Stages;
		_possibleFinalShapes = solution.FinalShapes;
		
		var scalar = transform.lossyScale.x;
		foreach (var l in Lights)
		{
			l.range *= scalar;
		}
		_buttons = GetButtons();
		if (_colorbindMode)
		{
			SetColorblind();
		}
		for (int i = 0; i < _buttons.Count; i++)
		{
			var j = i;
			_buttons[j].Selectable.OnInteract += delegate
			{
				HandlePress(j);
				return false;
			};
		}
		SetVisuals(_buttons);
		StartFlash(new List<List<int>>{_stages.First().Flashes});
		_flashes.Add(_stages.First().Flashes);
		for (var i = 0; i < _stages.Count; i++)
		{
			var j = i;
			_stageAnswers.Add(new List<SimonShapesColor>());
			DebugLog("Stage {0}: The reference digit is: {1}. The flashes are {2}, which means that {3} and {4} needs to be pressed for this stage.", j + 1, _stages[j].ReferenceDigit, _stages[j].Flashes.Join(), _stages[j].StageAnswer.Item1, _stages[j].StageAnswer.Item2);
			var newStage = _stages[j].StageAnswer;
			var alreadySubmitted = _stages.Where(x => x.Submitted).Select(x => x.StageAnswer);
			alreadySubmitted.ForEach(x => _stageAnswers[j].AddRange(new []{x.Item1, x.Item2}));
			_stageAnswers[j].AddRange(new []{newStage.Item1, newStage.Item2});
			_stages[j].Submitted = true;
		}
		DebugLog("---------- Stage 1 ----------");
	}

	private void HandlePress(int i)
	{
		if (!_interactable)
		{
			return;
		}
		_audioPlaying = true;
		Buttons[i].AddInteractionPunch();
		if (_state == State.Color)
		{
			if (_flashRoutine != null)
			{
				StopFlash();
			}
			DebugLog("Pressed " + _buttons[i].Color);
			if ((_stageAnswers[_stage][_pressindex] == _buttons[i].Color))
			{
				Audio.PlaySoundAtTransform(Sounds[_buttons[i].Sound].name, Buttons[i].transform);
				//Correct
				_pressindex++;
				if (_pressindex == _stageAnswers[_stage].Count)
				{
					_stage++;
					_pressindex = 0;
					if (_stage == _stages.Count)
					{
						_state = State.Shape;
						StartCoroutine(FadeToGray());
						ColorblindTextMeshes.ForEach(x => x.gameObject.SetActive(false));
						DebugLog("Time for the shape!");
						PrintPossibleShapes();
						return;
					}
					_flashes.Add(_stages[_stage].Flashes);
					StartCoroutine(ResumeFlash(_flashes));
					DebugLog("Next stage");
					DebugLog("---------- Stage {0} ----------", _stage + 1);
				}
				else
				{
					DebugLog("That is correct");
				}
			}
			else
			{
				Audio.PlaySoundAtTransform(Sounds[7].name, Buttons[i].transform);
				DebugLog("That is incorrect, expected " + _stageAnswers[_stage][_pressindex]);
				_pressindex = 0;
				Module.HandleStrike();
				StartCoroutine(ResumeFlash(_flashes));
			}

			return;
		}

		if (!_possibleFinalShapes.Any(x => x.Contains(i)))
		{
			Module.HandleStrike();
			DebugLog("You pressed {0}, that can not create a correct shape with the current configuration.", "ABC"[i % 3] + (Math.Abs(i / 3) + 1).ToString());
			Audio.PlaySoundAtTransform(Sounds[7].name, Buttons[i].transform);
			return;
		}

		_possibleFinalShapes.RemoveAll(x => !x.Contains(i));
		DebugLog("Pressed {0}, that works.", "ABC"[i % 3] + (Math.Abs(i / 3) + 1).ToString());
		_buttons[i].Selected = true;
		_buttons[i].Light.enabled = true;
		_buttons[i].Renderer.material = LitColors[6];
		if (_buttons.Count(x => x.Selected) == _possibleFinalShapes.First().Count)
		{
			DebugLog("Module solved.");
			_buttons.ForEach(x =>
			{
				x.Light.enabled = false;
				x.Renderer.material = ButtonColors[6];
			});
			StartCoroutine(Solve());
			Audio.PlaySoundAtTransform(Sounds[6].name, Buttons[i].transform);
		}
		else
		{
			Audio.PlaySoundAtTransform(Sounds[rnd.Range(0,6)].name, Buttons[i].transform);
		}
	}
	
	private void DebugLog(string message, params object[] p)
	{
		Debug.LogFormat("[Simon Shapes #{0}] {1}",_moduleId, string.Format(message, p));
	}

	private IEnumerator FlashCoroutine(List<List<int>> flashes)
	{
		foreach (var t in flashes.Last())
		{
			if (_audioPlaying)
			{
				Audio.PlaySoundAtTransform(Sounds[_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Sound].name, Buttons[t - 1].transform);
			}
			_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Light.enabled = true;
			_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Renderer.material =
				LitColors[(int)_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Color];
			yield return new WaitForSeconds(.4f);
			_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Light.enabled = false;
			_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Renderer.material =
				ButtonColors[(int)_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Color];
			yield return new WaitForSeconds(.4f);
		}
		yield return new WaitForSeconds(1.2f);
		while (true)
		{
			foreach (var t1 in flashes)
			{
				foreach (var t in t1)
				{
					if (_audioPlaying)
					{
						Audio.PlaySoundAtTransform(Sounds[_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Sound].name, Buttons[t - 1].transform);
					}
					_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Light.enabled = true;
					_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Renderer.material =
						LitColors[(int)_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Color];
					yield return new WaitForSeconds(.4f);
					_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Light.enabled = false;
					_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Renderer.material =
						ButtonColors[(int)_buttons.Where(x => x.Color != SimonShapesColor.Gray).ElementAt(t - 1).Color];
					yield return new WaitForSeconds(.4f);
				}

				if (flashes.Count != 1)
				{
					yield return new WaitForSeconds(.8f);
				}
			}
			yield return new WaitForSeconds(1.2f);
		}
	}

	private List<Button> GetButtons()
	{
		var buttons = new List<Button>();
		var colors = new List<SimonShapesColor>
		{
			SimonShapesColor.Red, SimonShapesColor.Green, SimonShapesColor.Blue, SimonShapesColor.Yellow,
			SimonShapesColor.Cyan, SimonShapesColor.Magenta, SimonShapesColor.Gray, SimonShapesColor.Gray, 
			SimonShapesColor.Gray
		}.Shuffle();
		var sounds = Enumerable.Range(0, 6).ToArray().Shuffle();
		for (int i = 0; i < 9; i++)
		{
			buttons.Add(new Button(colors[i], Lights[i], ButtonRenderers[i], Buttons[i]));
		}

		var index = 0;
		foreach (var button in buttons.Where(x => x.Color != SimonShapesColor.Gray))
		{
			button.Sound = sounds[index++];
		}

		return buttons;
	}

	private void SetVisuals(List<Button> buttons)
	{
		for (var i = 0; i < 9; ++i)
		{
			var colorIndex = (int) buttons[i].Color;
			buttons[i].Light.color = LightColors[colorIndex];
			buttons[i].Renderer.material = ButtonColors[colorIndex];
		}	
	}

	private void PrintPossibleShapes()
	{
		DebugLog("The possible shapes are:");
		for (var i = 0; i < _possibleFinalShapes.Count; i++)
		{
			if (i != 0)
			{
				DebugLog("----------------------");
			}
			var grid = new [] { new[] { ".", ".", "." }, new[] { ".", ".", "." }, new[] { ".", ".", "." } };

			foreach (var dot in _possibleFinalShapes[i])
			{
				grid[Math.Abs(dot / 3)][dot % 3] = "X";
			}

			foreach (var row in grid)
			{
				DebugLog(row.Join());
			}
		}
	}

	private IEnumerator ResumeFlash(List<List<int>> flashes)
	{
		_animating = true;
		yield return new WaitForSeconds(1.2f);
		StartFlash(flashes);
		_animating = false;
	}

	private IEnumerator FadeToGray()
	{
		_animating = true;
		var fadeAmount = 0f;
		var fadeDuration = 0.1f;
		var renderers = _buttons.Where(x => x.Color != SimonShapesColor.Gray).Select(x => x.Renderer).ToList();
		var color = new Color32(34, 34, 34, 138);
		foreach (var l in _buttons.Select(x => x.Light))
		{
			l.color = LightColors[6];
		}
		while (fadeAmount < fadeDuration)
		{
			fadeAmount += Time.deltaTime * fadeDuration;
			foreach (var rend in renderers)
			{
				rend.material.color = Color.Lerp(rend.material.color, color, fadeAmount);
			}

			yield return null;
		}
		renderers.ForEach(x => x.material = ButtonColors[6]);
		_animating = false;
	}

	private IEnumerator Solve()
	{
		_animating = true;
		var cycle = new List<int> {0, 1, 2, 5, 8, 7, 6, 3, 4};
		var renderers = _buttons.Select(x => x.Renderer).ToList();
		var lights = _buttons.Select(x => x.Light).ToList();
		foreach (var l in lights)
		{
			l.color = LightColors[1];
		}
		foreach (var t in cycle)
		{
			lights[t].enabled = true;
			_buttons[t].Renderer.material = LitColors[1];
			yield return new WaitForSeconds(.07f);
		}
		
		lights.ForEach(x => x.enabled = false);
		renderers.ForEach(x => x.material = ButtonColors[6]);
		yield return new WaitForSeconds(.3f);
		renderers.ForEach(x => x.material = LitColors[1]);
		lights.ForEach(x => x.enabled = true);
		Module.HandlePass();
		_isSolved = true;
		_animating = false;
	}

	private void StartFlash(List<List<int>> flashes)
	{
		_flashRoutine = StartCoroutine(FlashCoroutine(flashes));
	}

	private void StopFlash()
	{
		StopCoroutine(_flashRoutine);
		_flashRoutine = null;
		Lights.ForEach(x => x.enabled = false);
		_buttons.ForEach(x => x.Renderer.material = ButtonColors[(int)x.Color]);
	}

	private void SetColorblind()
	{
		for (int i = 0; i < _buttons.Count; i++)
		{
			ColorblindTextMeshes[i].gameObject.SetActive(true);
			ColorblindTextMeshes[i].text = _buttons[i].Color == SimonShapesColor.Gray ? "A" : _buttons[i].Color.ToString().First().ToString();
		}
	}
	
#pragma warning disable 414
	private const string TwitchHelpMessage =
		"Mute sound using !{0} mute. Enable colorblind mode using !{0} colorblind. Press colors using !{0} press rgb c y m, where r is red, b is blue, g is green, c is cyan, y is yellow, and m is magenta. Press squares by their position using !{0} press a1 b2 c3, where the letter is the column and the number is the row.";
#pragma restore disable 414
	
	private IEnumerator ProcessTwitchCommand(string command)
	{
		command = command.ToLowerInvariant().Trim();
		if (command.Equals("colorblind"))
		{
			if (_state == State.Color)
			{
				yield return null;
				SetColorblind();
				yield return "sendtochat Colorblind mode is now active";
			}
			yield break;
		}
		if (command.Equals("mute"))
		{
			yield return null;
			_audioPlaying = false;
			yield return "sendtochat The flashing sound is now muted";
			yield break;
		}
		var colorMatch = Constants.TPColorRegex.Match(command);
		if (colorMatch.Success)
		{
			if (_state == State.Shape)
			{
				yield return "sendtochaterror What colors am I supposed to press? All of the squares are gray! 4Head";
				yield break;
			}
			yield return null;
			var colors = colorMatch.Groups[1].ToString().Replace(" ", string.Empty).Select(x => (SimonShapesColor)"rgbymc".IndexOf(x)).ToList();
			foreach (var color in colors)
			{
				_buttons.Single(x => x.Color == color).Selectable.OnInteract();
				yield return new WaitForSeconds(.1f);
			}
			yield break;
		}

		var coorMatch = Constants.TPCoorRegex.Match(command);
		if (coorMatch.Success)
		{
			yield return null;
			foreach (var coordinate in coorMatch.Groups[1].ToString().Split(' '))
			{
				var button = (char.ToUpperInvariant(coordinate[0]) - 'A') + 3 * (coordinate[1] - '1');
				_buttons[button].Selectable.OnInteract();
				if (_buttons.Count(x => x.Selected) == _possibleFinalShapes.First().Count)
				{
					yield return "solve";
				}
				yield return new WaitForSeconds(.1f);
			}
			
		}
	}

	private IEnumerator TwitchHandleForcedSolve()
	{
		if (_state == State.Color)
		{
			foreach (var stage in _stageAnswers.TakeLast(_stages.Count - _stage))
			{
				if (_pressindex != 0)
				{
					foreach (var press in stage.TakeLast(stage.Count - _pressindex))
					{
						_buttons.Single(x => x.Color == press).Selectable.OnInteract();
						yield return new WaitForSeconds(.1f);
					}
				}
				else
				{
					foreach (var press in stage)
					{
						_buttons.Single(x => x.Color == press).Selectable.OnInteract();
						yield return new WaitForSeconds(.1f);
					}
				}
				
				while (_animating)
				{
					yield return true;
				}
			}
		}
		
		var solution = _possibleFinalShapes.First();
		foreach (var press in solution)
		{
			_buttons[press].Selectable.OnInteract();
			yield return new WaitForSeconds(.1f);
		}

		while (!_isSolved)
		{
			yield return true;
		}
	}
}