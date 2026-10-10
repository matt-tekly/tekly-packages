using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Common.Ui.Fancy
{
	public static class FancyRectUtility
	{
		private const string REPLACE_MENU = "CONTEXT/Image/Replace with Fancy Rect";

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

		[MenuItem(REPLACE_MENU, true)]
		private static bool ValidateReplaceWithFancyRect(MenuCommand command)
		{
			return command.context is Image;
		}

		/// <summary>
		/// Swaps an Image for a FancyRect, keeping color, raycast and masking settings, and turning a filled
		/// Image into the matching reveal. The sprite and material are intentionally not copied: a custom UI
		/// material would not render the shape, and a rounded-rect sprite would be drawn inside the new shape.
		/// </summary>
		[MenuItem(REPLACE_MENU)]
		public static void ReplaceWithFancyRect(MenuCommand command)
		{
			var image = command.context as Image;
			if (image == null) {
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

			Undo.SetCurrentGroupName("Replace with Fancy Rect");
			var group = Undo.GetCurrentGroup();

			Undo.DestroyObjectImmediate(image);

			if (image != null) {
				// Another component depends on the Image and blocked the removal.
				Debug.LogError($"[FancyRect] Could not remove the Image on '{go.name}'.", go);
				return;
			}

			var fancyRect = Undo.AddComponent<FancyRect>(go);
			fancyRect.color = imageColor;
			fancyRect.raycastTarget = raycastTarget;
			fancyRect.raycastPadding = raycastPadding;
			fancyRect.maskable = maskable;

			if (isFilled) {
				fancyRect.RevealMethod = ToRevealMethod(fillMethod);
				fancyRect.RevealOrigin = ToRevealOrigin(fillMethod, fillOrigin);
				fancyRect.RevealAmount = fillAmount;
				fancyRect.RevealClockwise = fillClockwise;
			}

			EnableShaderChannels(fancyRect.canvas);
			Undo.CollapseUndoOperations(group);
		}

		private static RevealMethod ToRevealMethod(Image.FillMethod fillMethod)
		{
			switch (fillMethod) {
				case Image.FillMethod.Horizontal:
					return RevealMethod.Horizontal;
				case Image.FillMethod.Vertical:
					return RevealMethod.Vertical;
				default:
					// Radial90 and Radial180 have no direct equivalent, so they become a full radial reveal
					return RevealMethod.Radial;
			}
		}

		private static RevealOrigin ToRevealOrigin(Image.FillMethod fillMethod, int fillOrigin)
		{
			switch (fillMethod) {
				case Image.FillMethod.Horizontal:
					return fillOrigin == (int) Image.OriginHorizontal.Right ? RevealOrigin.Right : RevealOrigin.Left;
				case Image.FillMethod.Vertical:
					return fillOrigin == (int) Image.OriginVertical.Top ? RevealOrigin.Top : RevealOrigin.Bottom;
				case Image.FillMethod.Radial360:
					switch ((Image.Origin360) fillOrigin) {
						case Image.Origin360.Right:
							return RevealOrigin.Right;
						case Image.Origin360.Top:
							return RevealOrigin.Top;
						case Image.Origin360.Left:
							return RevealOrigin.Left;
						default:
							return RevealOrigin.Bottom;
					}
				default:
					return RevealOrigin.Bottom;
			}
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
