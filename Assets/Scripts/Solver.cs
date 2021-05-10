using System.Collections.Generic;
using System.Linq;
using KModkit;
using System;
using rnd = UnityEngine.Random;

namespace SimonShapesModule
{
    public static class Solver
    {
        public static SolverCalculationResult Generate(KMBombInfo info)
        {
            var referenceDigits = GetReferenceDigits(info);
            var stageCoordinates = GenerateShape();
            var stages = new List<Stage>();

            for (var i = 0; i < stageCoordinates.Item1.Count - 1; ++i)
            {
                var shape = Constants.ShapeTable[stageCoordinates.Item1[i].Item1][stageCoordinates.Item1[i].Item2]
                    .Split(',')
                    .PickRandom();
                var startingPosition = referenceDigits[i > 2 ? 2 : i] < 6
                    ? new Pair<int, int>(rnd.Range(0, 6), referenceDigits[i > 2 ? 2 : i])
                    : new Pair<int, int>(referenceDigits[i > 2 ? 2 : i] % 6, rnd.Range(0, 6));
                var stageDigits = new List<int>();
                var currentPosition = startingPosition;
                stageDigits.Add(Constants.NumberTable[startingPosition.Item1][startingPosition.Item2]);
                foreach (var inst in shape)
                {
                    switch (inst)
                    {
                        case 'u':
                            ChangeCoordinate(ref currentPosition.Item1, -1);
                            stageDigits.Add(Constants.NumberTable[currentPosition.Item1][currentPosition.Item2]);
                            break;
                        case 'd':
                            ChangeCoordinate(ref currentPosition.Item1, 1);
                            stageDigits.Add(Constants.NumberTable[currentPosition.Item1][currentPosition.Item2]);
                            break;
                        case 'l':
                            ChangeCoordinate(ref currentPosition.Item2, -1);
                            stageDigits.Add(Constants.NumberTable[currentPosition.Item1][currentPosition.Item2]);
                            break;
                        case 'r':
                            ChangeCoordinate(ref currentPosition.Item2, 1);
                            stageDigits.Add(Constants.NumberTable[currentPosition.Item1][currentPosition.Item2]);
                            break;
                        default:
                            throw new InvalidOperationException(string.Format("Invalid symbol {0}", inst));
                    }
                }

                var colors = info.GetSerialNumberNumbers().First() % 2 == 0
                    ? new Pair<SimonShapesColor, SimonShapesColor>((SimonShapesColor) stageCoordinates.Item1[i].Item2,
                        (SimonShapesColor) stageCoordinates.Item1[i].Item1)
                    : new Pair<SimonShapesColor, SimonShapesColor>((SimonShapesColor) stageCoordinates.Item1[i].Item1,
                        (SimonShapesColor) stageCoordinates.Item1[i].Item2);

                stages.Add(new Stage(referenceDigits[i > 2 ? 2 : i], stageDigits, colors));
            }
            return new SolverCalculationResult(stages, GenerateShapeSolutions(stageCoordinates.Item2));
        }

        private static List<int> GetReferenceDigits(KMBombInfo info)
        {
            var digit3 = 0;
            foreach (var indicator in info.GetIndicators())
            {
                digit3 += char.ToUpper(indicator[0]) - 64;
            }

            return new List<int>
            {
                info.GetSerialNumberNumbers().Sum() % 12,
                (int) Math.Pow(info.GetBatteryCount() + info.GetPortCount(), 2) % 12, digit3 % 12
            };
        }

        private static Pair<List<Pair<int, int>>, string> GenerateShape()
        {
            var shapeCoordinates = new List<Pair<int, int>>();
            var symbol = new Pair<int, int>(rnd.Range(0, 6), rnd.Range(0, 6));
            var shape = Constants.ShapeTable[symbol.Item1][symbol.Item2].Split(',').First();
            var startingSquare = new Pair<int, int>(rnd.Range(0, 6), rnd.Range(0, 6));
            var currentPosition = startingSquare;
            shapeCoordinates.Add(new Pair<int, int>(startingSquare.Item1, startingSquare.Item2));
            foreach (var inst in shape)
            {
                switch (inst)
                {
                    case 'u':
                        ChangeCoordinate(ref currentPosition.Item1, -1);
                        shapeCoordinates.Add(new Pair<int, int>(currentPosition.Item1, currentPosition.Item2));
                        break;
                    case 'd':
                        ChangeCoordinate(ref currentPosition.Item1, 1);
                        shapeCoordinates.Add(new Pair<int, int>(currentPosition.Item1, currentPosition.Item2));
                        break;
                    case 'l':
                        ChangeCoordinate(ref currentPosition.Item2, -1);
                        shapeCoordinates.Add(new Pair<int, int>(currentPosition.Item1, currentPosition.Item2));
                        break;
                    case 'r':
                        ChangeCoordinate(ref currentPosition.Item2, 1);
                        shapeCoordinates.Add(new Pair<int, int>(currentPosition.Item1, currentPosition.Item2));
                        break;
                    default:
                        throw new InvalidOperationException(string.Format("Invalid symbol GenerateShape {0}", inst));
                }
            }

            shapeCoordinates.Shuffle();
            shapeCoordinates.Add(symbol);
            return new Pair<List<Pair<int, int>>, string>(shapeCoordinates, shape);
        }

        private static void ChangeCoordinate(ref int initial, int modify)
        {
            initial += modify;
            while (initial < 0)
            {
                initial += 6;
            }

            initial %= 6;
        }
        
        private static List<List<int>> GenerateShapeSolutions(string instruction)
        {
            var shapeoffsets = new Dictionary<char, int>
            {
                {'u', -4},
                {'d', 4},
                {'l', -1},
                {'r', 1}
            };
            var possible = new List<List<int>>();
            var allowed = new[] {0, 1, 2, 4, 5, 6, 8, 9, 10};
            for (int i = 0; i < 11; ++i)
            {
                if (!allowed.Contains(i))
                {
                    continue;
                }
                var failed = false;
                var current = i;
                var visited = new List<int> {i};
                var moves = new List<int> {i};
                foreach (var inst in instruction)
                {
                    var offset = shapeoffsets[inst];
                    current += offset;
                    if (!allowed.Contains(current) || visited.Contains(current))
                    {
                        failed = true;
                        break;
                    }
                    visited.Add(current);
                    moves.Add(current);
                }
                if (!failed)
                {
                    for (int j = 0; j < moves.Count; j++)
                    {
                        moves[j] -= moves[j] < 7 && moves[j] > 3 ? 1 : (moves[j] < 11 && moves[j] > 7 ? 2 : 0);
                    }
                    possible.Add(moves);
                }
            }

            return possible;
        }
    }
}