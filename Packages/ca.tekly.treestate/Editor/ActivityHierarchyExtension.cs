// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using Tekly.EditorUtils.Gui;
using Tekly.TreeState.StandardActivities;
using UnityEditor;
using UnityEngine;

namespace Tekly.TreeState
{
	[InitializeOnLoad]
	public static class ActivityHierarchyExtension
	{
		private static readonly Color s_inactiveColor = new Color(1f, 1f, 1f, 0.4f);
		
		static ActivityHierarchyExtension()
		{
			EditorApplication.hierarchyWindowItemOnGUI += OnDrawHierarchyItem;
		}

		private static void OnDrawHierarchyItem(int instanceId, Rect selectionRect)
		{
			var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;

			if (go == null) {
				return;
			}
			
			var activity = go.GetComponent<TreeActivity>();

			if (activity == null) {
				return;
			}
			
			if (Application.isPlaying) {
				var color = activity.Mode == ActivityMode.Inactive ? s_inactiveColor : GUI.color;
				
				using (EditorGuiExt.ColorBlock(color)) {
					var content = EditorGUIUtility.ObjectContent(activity, typeof(TreeActivity));

					var imageRect = selectionRect;
					imageRect.xMin = selectionRect.xMax - 75;
					imageRect.yMin += 1;
					imageRect.width = 14;
					imageRect.height = 14;
				
					GUI.DrawTexture(imageRect, content.image);

					DrawWarning(imageRect, activity);
				
					var labelRect = selectionRect;
					labelRect.xMin = selectionRect.xMax - 59;
					labelRect.yMin -= 1;
			
					GUI.Label(labelRect, activity.Mode.ToString());
				}
			} else {
				
				var imageRect = selectionRect;
				imageRect.xMin = selectionRect.xMax - 2;
				imageRect.yMin += 1;
				imageRect.width = 14;
				imageRect.height = 14;
				
				var content = EditorGUIUtility.ObjectContent(activity, typeof(TreeActivity));
				
				GUI.DrawTexture(imageRect, content.image);

				DrawInjectionContainer(imageRect, activity);
				DrawWarning(imageRect, activity);
			}
		}
		
		private static Rect DrawInjectionContainer(Rect imageRect, TreeActivity activity)
		{
			var injectorContainerState = activity.GetComponent<InjectorContainerState>();
			if (injectorContainerState != null) {
				imageRect.xMin -= 16;
				imageRect.width = 16;
				imageRect.height = 16;
				
				var content = EditorGUIUtility.ObjectContent(injectorContainerState, typeof(InjectorContainerState));
				GUI.DrawTexture(imageRect, content.image);
			}
			
			return imageRect;
		}

		private static void DrawWarning(Rect imageRect, TreeActivity activity)
		{
			if (activity is TreeStateMachine machine && machine.DefaultState == null) {
				var warningRect = imageRect;

				warningRect.xMin -= 16;
				warningRect.width = 16;
				warningRect.height = 16;
					
				var warningContent = EditorGUIUtility.IconContent("Warning");
					
				GUI.DrawTexture(warningRect, warningContent.image);
			}
		}
	}
}