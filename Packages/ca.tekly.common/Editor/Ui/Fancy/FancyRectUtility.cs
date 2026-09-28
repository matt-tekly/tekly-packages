using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tekly.Common.Ui.Fancy
{
	public static class FancyRectUtility
	{
		[MenuItem("GameObject/UI (Canvas)/Fancy Rect", false, 2001)]
		public static void AddFancyRect(MenuCommand command)
		{
			var parent = command.context as GameObject;
			if (parent == null) {
				parent = Selection.activeGameObject;
			}

			if (parent == null || parent.GetComponentInParent<Canvas>(true) == null) {
				var canvas = FindOrCreateCanvas();
				if (canvas == null) {
					Debug.LogError("[FancyRect] Could not find or create a Canvas.");
					return;
				}

				parent = canvas.gameObject;
			}

			var go = new GameObject("Fancy Rect", typeof(RectTransform));
			go.layer = LayerMask.NameToLayer("UI");
			GameObjectUtility.SetParentAndAlign(go, parent);
			Undo.RegisterCreatedObjectUndo(go, "Create Fancy Rect");

			var shape = go.AddComponent<FancyRect>();
			shape.rectTransform.sizeDelta = new Vector2(240, 80);

			EnableShaderChannels(parent.GetComponentInParent<Canvas>(true));
			Selection.activeGameObject = go;
		}

		/// <summary>
		/// Turns on the TexCoord1-3 channels the shape needs, on the canvas and its root canvas, with undo.
		/// </summary>
		public static void EnableShaderChannels(Canvas canvas)
		{
			if (canvas == null) {
				return;
			}

			EnableOn(canvas);

			if (canvas.rootCanvas != canvas) {
				EnableOn(canvas.rootCanvas);
			}
		}

		private static void EnableOn(Canvas canvas)
		{
			var needed = FancyRect.NEEDED_SHADER_CHANNELS;

			if (canvas == null || (canvas.additionalShaderChannels & needed) == needed) {
				return;
			}

			Undo.RecordObject(canvas, "Enable Fancy Rect Shader Channels");
			canvas.additionalShaderChannels |= needed;
			EditorUtility.SetDirty(canvas);
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
	}
}
