using Tekly.Leaf.Elements.Radios;
using UnityEditor;

namespace Tekly.Leaf.Elements
{
	[CustomEditor(typeof(LeafRadioOptionUnselectable), true)]
	[CanEditMultipleObjects]
	public class LeafRadioOptionUnselectableEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			LeafRadioOptionInspector.DrawIsOn(this);
			DrawDefaultInspector();
		}
	}
}
