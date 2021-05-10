using UnityEngine;

namespace SimonShapesModule
{
	public class Button
	{
		public SimonShapesColor Color { get; private set; }
		public Light Light { get; private set; }
		public MeshRenderer Renderer { get; private set; }
		public KMSelectable Selectable { get; private set; }
		public int Sound { get; set; }
		public bool Selected { get; set; }
		
		public Button(SimonShapesColor color, Light light, MeshRenderer renderer, KMSelectable selectable)
		{
			Color = color;
			Light = light;
			Renderer = renderer;
			Selectable = selectable;
			Selected = false;
		}
	}
}