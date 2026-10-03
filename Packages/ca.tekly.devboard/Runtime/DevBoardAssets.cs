using Tekly.DevBoard.Components;
using TMPro;
using UnityEngine;

namespace Tekly.DevBoard
{
	public class DevBoardAssets : ScriptableObject
	{
		/// <summary>
		/// Widget prefabs, looked up by prefab name. The first prefab of each widget type is that type's default.
		/// </summary>
		public Widget[] Widgets;
		public TMP_FontAsset[] Fonts;
		public Material[] FontMaterials;
	}
}
