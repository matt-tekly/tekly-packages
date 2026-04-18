using UnityEngine;

namespace Tekly.Common.Ui.ProceduralRect
{
	public static class EmptySprite
	{
		private static Sprite s_instance;

		/// <summary>
		/// Gets a cached small white sprite.
		/// </summary>
		public static Sprite Get()
		{
			if (s_instance == null) {
				var tex = Texture2D.whiteTexture;

				s_instance = Sprite.Create(
					tex,
					new Rect(0, 0, tex.width, tex.height),
					new Vector2(0.5f, 0.5f),
					100f
				);

				s_instance.name = "ProceduralRect_WhiteSprite";
				s_instance.hideFlags = HideFlags.HideAndDontSave;
			}

			return s_instance;
		}
	}
}