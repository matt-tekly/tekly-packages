using Tekly.EditorUtils.Gui;
using UnityEditor;
using UnityEngine;

namespace Tekly.TwoD.Cells
{
	[CustomPreview(typeof(CellSprite))]
	public class CellSpritePreview : ObjectPreview
	{
		public override bool HasPreviewGUI() => true;

		public override void OnPreviewGUI(Rect containerRect, GUIStyle background)
		{
			var sprite = (target as CellSprite).Icon;
			EditorGuiExt.DrawSprite(containerRect, sprite);
		}
	}
}