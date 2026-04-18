using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui.ProceduralRect
{
	public static class ProceduralRectUtility
	{
		[MenuItem("GameObject/UI (Canvas)/Procedural Rect")]
		public static void AddProceduralRect()
		{
			var selectedGameObject = Selection.activeGameObject;
			
			if (selectedGameObject != null && selectedGameObject.GetComponentInParent<Canvas>() != null) {
				var newGo = new GameObject();
				newGo.AddComponent<ProceduralRectImage>();
				newGo.layer = LayerMask.NameToLayer("UI");
				newGo.name = "Procedural Rect";
				
				newGo.transform.SetParent(selectedGameObject.transform, false);
				Selection.activeGameObject = newGo;
				
				Undo.RegisterCreatedObjectUndo(newGo, "Create Procedural Rect");
			} else {
				if (Object.FindFirstObjectByType<Canvas>() == null) {
					EditorApplication.ExecuteMenuItem("GameObject/UI (Canvas)/Canvas");
				}

				var canvas = Object.FindFirstObjectByType<Canvas>();

				canvas.additionalShaderChannels |= ProceduralRectImage.NEEDED_SHADER_CHANNELS;

				var newGo = new GameObject();
				newGo.AddComponent<ProceduralRectImage>();
				newGo.layer = LayerMask.NameToLayer("UI");
				newGo.name = "Procedural Rect";
				
				newGo.transform.SetParent(canvas.transform, false);
				Selection.activeGameObject = newGo;
				
				Undo.RegisterCreatedObjectUndo(newGo, "Create Procedural Rect");
			}
		}
		
		[MenuItem("CONTEXT/Image/Replace with Procedural Rect")]
		public static void ReplaceWithProceduralRect(MenuCommand command)
		{
			var image = (Image) command.context;
			var gameObject = image.gameObject;
			var color = image.color;
			
			Undo.DestroyObjectImmediate(image);
			
			var proceduralRect = gameObject.AddComponent<ProceduralRectImage>();
			proceduralRect.color = color;
			
			Undo.RegisterCreatedObjectUndo(proceduralRect, "Replace with Procedural Rect");
		}
	}
}