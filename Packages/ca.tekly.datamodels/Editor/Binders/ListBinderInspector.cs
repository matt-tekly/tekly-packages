using Tekly.DataModels.Binders.Collections;
using UnityEditor;

namespace Tekly.DataModels.Binders
{
	[CustomEditor(typeof(ListBinder), false)]
	public class ListBinderInspector : Editor
	{
		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			base.OnInspectorGUI();
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}