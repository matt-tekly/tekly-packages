using UnityEditor;
using UnityEngine;

namespace Tekly.TreeState
{
	public static class TreeStateMenus
	{
		[MenuItem("GameObject/Tekly/Tree State Machine")]
		public static void CreateTreeState(MenuCommand command)
		{
			Undo.IncrementCurrentGroup();

			var root = new GameObject("Root State");
			var manager = root.AddComponent<TreeStateManager>();
			var rootMachine = Undo.AddComponent<TreeStateMachine>(root);
			
			Undo.RecordObject(rootMachine, "Set State Machine");
			manager.StateMachine = rootMachine;
			
			Undo.RegisterCreatedObjectUndo(root, "Create Tree state");
			
			var child = new GameObject("Machine");
			var childMachine = child.AddComponent<TreeStateMachine>();
			rootMachine.DefaultState = childMachine;
			
			Undo.RegisterCreatedObjectUndo(child, "Create Child State");
			Undo.SetTransformParent(child.transform, root.transform, "Create Child");
			
			var leaf = new GameObject("State");
			var leafState = leaf.AddComponent<TreeState>();
			childMachine.DefaultState = leafState;
			
			Undo.RegisterCreatedObjectUndo(leaf, "Create Leaf State");
			Undo.SetTransformParent(leaf.transform, child.transform, "Create Leaf Child");
			
			Undo.SetCurrentGroupName("Create Tree State Machine");
		}
	}
}