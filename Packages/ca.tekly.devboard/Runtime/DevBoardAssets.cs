using Tekly.DevBoard.Components;
using Tekly.DevBoard.Components.Inputs;
using TMPro;
using UnityEngine;

namespace Tekly.DevBoard
{
	public class DevBoardAssets : ScriptableObject
	{
		public Board[] Boards;
		public ContainerWidget[] Containers;
		public ScrollViewWidget[] ScrollViews;
		public ButtonWidget[] Buttons;
		public PropertyWidget[] Properties;
		public FoldoutWidget[] Foldouts;
		public DividerWidget[] Dividers;
		public TextInputWidget[] TextInputs;
		public IntInputWidget[] IntInputs;
		public FloatInputWidget[] FloatInputs;
		public ToggleWidget[] Toggles;
		public TMP_FontAsset[] Fonts;
		public Material[] FontMaterials;
	}
}