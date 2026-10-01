using Tekly.DevBoard.Components;
using Tekly.DevBoard.Components.Inputs;
using UnityEngine;

namespace Tekly.DevBoard
{
	public class DevBoardAssets : ScriptableObject
	{
		public Board[] Boards;
		public ContainerWidget[] Containers;
		public ButtonWidget[] Buttons;
		public PropertyWidget[] Properties;
		public FoldoutWidget[] Foldouts;
		public DividerWidget[] Dividers;
		public TextInputWidget[] TextInputs;
		public IntInputWidget[] IntInputs;
		public FloatInputWidget[] FloatInputs;
		public ToggleWidget[] Toggles;
	}
}