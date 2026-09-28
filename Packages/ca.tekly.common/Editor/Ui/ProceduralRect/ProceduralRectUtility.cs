using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui.ProceduralRect
{
	public static class ProceduralRectUtility
	{
		private const string REPLACE_MENU = "CONTEXT/Image/Replace with Procedural Rect";

		[MenuItem("GameObject/UI (Canvas)/Procedural Rect", false, 2000)]
		public static void AddProceduralRect(MenuCommand command)
		{
			var parent = command.context as GameObject;
			if (parent == null) {
				parent = Selection.activeGameObject;
			}

			if (parent == null || parent.GetComponentInParent<Canvas>(true) == null) {
				var canvas = FindOrCreateCanvas();
				if (canvas == null) {
					Debug.LogError("[ProceduralRect] Could not find or create a Canvas.");
					return;
				}

				parent = canvas.gameObject;
			}

			var go = new GameObject("Procedural Rect", typeof(RectTransform));
			go.layer = LayerMask.NameToLayer("UI");
			GameObjectUtility.SetParentAndAlign(go, parent);
			Undo.RegisterCreatedObjectUndo(go, "Create Procedural Rect");

			go.AddComponent<ProceduralRectImage>();
			EnableShaderChannels(parent.GetComponentInParent<Canvas>(true));

			Selection.activeGameObject = go;
		}

		/// <summary>
		/// Finds a Canvas in the current stage (so this works in Prefab Mode), creating one if needed.
		/// </summary>
		private static Canvas FindOrCreateCanvas()
		{
			var stage = StageUtility.GetCurrentStageHandle();
			var canvas = stage.FindComponentOfType<Canvas>();

			if (canvas != null) {
				return canvas;
			}

			Selection.activeGameObject = null;
			EditorApplication.ExecuteMenuItem("GameObject/UI (Canvas)/Canvas");

			var created = Selection.activeGameObject;
			canvas = created != null ? created.GetComponent<Canvas>() : null;

			return canvas != null ? canvas : stage.FindComponentOfType<Canvas>();
		}

		private static void EnableShaderChannels(Canvas canvas)
		{
			if (canvas == null) {
				return;
			}

			var root = canvas.rootCanvas;
			Undo.RecordObject(root, "Enable Procedural Rect Shader Channels");
			root.additionalShaderChannels |= ProceduralRectImage.NEEDED_SHADER_CHANNELS;

			if (canvas != root) {
				Undo.RecordObject(canvas, "Enable Procedural Rect Shader Channels");
				canvas.additionalShaderChannels |= ProceduralRectImage.NEEDED_SHADER_CHANNELS;
			}
		}

		[MenuItem(REPLACE_MENU, true)]
		private static bool ValidateReplaceWithProceduralRect(MenuCommand command)
		{
			return command.context is Image && !(command.context is ProceduralRectImage);
		}

		/// <summary>
		/// Swaps an Image for a ProceduralRectImage, keeping color, raycast, masking and fill settings.
		/// The sprite and material are intentionally not copied: a custom UI material would not render the
		/// procedural shape, and a rounded-rect sprite would be drawn inside the new shape.
		/// </summary>
		[MenuItem(REPLACE_MENU)]
		public static void ReplaceWithProceduralRect(MenuCommand command)
		{
			var image = command.context as Image;
			if (image == null || image is ProceduralRectImage) {
				return;
			}

			var go = image.gameObject;

			var imageColor = image.color;
			var raycastTarget = image.raycastTarget;
			var raycastPadding = image.raycastPadding;
			var maskable = image.maskable;
			var isFilled = image.type == Image.Type.Filled;
			var fillMethod = image.fillMethod;
			var fillOrigin = image.fillOrigin;
			var fillAmount = image.fillAmount;
			var fillClockwise = image.fillClockwise;

			Undo.SetCurrentGroupName("Replace with Procedural Rect");
			var group = Undo.GetCurrentGroup();

			Undo.DestroyObjectImmediate(image);

			if (image != null) {
				// Another component depends on the Image and blocked the removal.
				Debug.LogError($"[ProceduralRect] Could not remove the Image on '{go.name}'.", go);
				return;
			}

			var proceduralRect = Undo.AddComponent<ProceduralRectImage>(go);
			proceduralRect.color = imageColor;
			proceduralRect.raycastTarget = raycastTarget;
			proceduralRect.raycastPadding = raycastPadding;
			proceduralRect.maskable = maskable;

			if (isFilled) {
				proceduralRect.type = Image.Type.Filled;
				proceduralRect.fillMethod = fillMethod;
				proceduralRect.fillOrigin = fillOrigin;
				proceduralRect.fillAmount = fillAmount;
				proceduralRect.fillClockwise = fillClockwise;
			}

			EnableShaderChannels(proceduralRect.canvas);
			Undo.CollapseUndoOperations(group);
		}
	}
}
