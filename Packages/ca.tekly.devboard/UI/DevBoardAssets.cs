using System;
using Tekly.DevBoard.Components;
using Tekly.DevBoard.Nodes;
using Tekly.Leaf.Elements.Animators;
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

		/// <summary>
		/// Icons for buttons, looked up by sprite name, e.g. "trash". A later DevBoardAssets can replace one by
		/// adding a sprite of the same name.
		/// </summary>
		public Sprite[] Icons;

		/// <summary>
		/// The colors of each button tone. A later DevBoardAssets can replace a tone by listing it again.
		/// </summary>
		public ButtonToneColors[] ButtonTones;
	}

	/// <summary>
	/// The colors of one button tone.
	/// </summary>
	[Serializable]
	public class ButtonToneColors
	{
		public ButtonTone Tone;

		[Tooltip("A solid button's background")]
		public LeafColorBlock Background;

		[Tooltip("A solid button's label and icon")]
		public LeafColorBlock Text;

		[Tooltip("A plain button's label and icon, which carry the tone without a background")]
		public LeafColorBlock Ink;
	}
}
