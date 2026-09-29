using Tekly.Common.Utils;
using UnityEditor;
using UnityEngine;

namespace Tekly.Trellis
{
	[CustomEditor(typeof(FlowLayout))]
	[CanEditMultipleObjects]
	public class FlowLayoutEditor : Editor
	{
		private static readonly string[] s_arrangeProperties = {
			"m_axis",
			"m_reverse",
			"m_padding",
			"m_spacing"
		};

		private static readonly string[] s_alignProperties = {
			"m_alignment",
			"m_crossAlignment"
		};

		private static readonly string[] s_wrapProperties = {
			"m_maxPerLine",
			"m_lineSpacing",
			"m_lineAlignment"
		};

		private static readonly GUIContent s_arrangeLabel = new GUIContent("Layout");
		private static readonly GUIContent s_alignLabel = new GUIContent("Alignment");
		private static readonly GUIContent s_fitLabel = new GUIContent("Fit",
			"Resize this layout to its content, replacing ContentSizeFitter");
		private static readonly GUIContent s_wrapLabel = new GUIContent("Wrap",
			"Move children onto a new line when they don't fit");
		private static readonly GUIContent s_itemLabel = new GUIContent("Item",
			"How this layout is sized and placed by its parent");

		private static bool s_itemExpanded;

		private SerializedProperty m_axis;
		private SerializedProperty m_wrap;

		private void OnEnable()
		{
			m_axis = serializedObject.FindProperty("m_axis");
			m_wrap = serializedObject.FindProperty("m_wrap");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			using (TrellisSection.Begin(s_arrangeLabel)) {
				DrawProperties(s_arrangeProperties);
			}

			using (TrellisSection.Begin(s_alignLabel)) {
				DrawProperties(s_alignProperties);
			}

			using (var wrap = TrellisSection.BeginToggle(m_wrap, s_wrapLabel)) {
				if (wrap.Expanded) {
					DrawProperties(s_wrapProperties);

					if (IsVerticalWrap()) {
						EditorGUILayout.HelpBox("Vertical wrap picks its columns from the current height. " +
							"Keep the height fixed (anchors or a parent layout), not sized to content.", MessageType.Info);
					}
				}
			}

			using (TrellisSection.Begin(s_fitLabel)) {
				TrellisEditorGui.DrawFit(serializedObject);

				if (FitsWrapDirection()) {
					EditorGUILayout.HelpBox("Fitting the wrap direction cancels wrapping: Preferred puts everything " +
						"on one line, Min puts each child on its own. Fit the other direction, or set Max Per Line " +
						"and use Preferred to fit exactly that many.", MessageType.Warning);
				}
			}

			using (var item = TrellisSection.BeginFoldout(s_itemLabel, s_itemExpanded)) {
				s_itemExpanded = item.Expanded;

				if (item.Expanded) {
					LayoutItemGui.Draw(serializedObject, true);
				}
			}

			serializedObject.ApplyModifiedProperties();

			if (targets.Length == 1) {
				TrellisEditorGui.DrawMissingItems(((Component) target).transform);
			}
		}

		private void DrawProperties(string[] names)
		{
			foreach (var name in names) {
				EditorGUILayout.PropertyField(serializedObject.FindProperty(name));
			}
		}

		private bool FitsWrapDirection()
		{
			if (targets.Length != 1) {
				return false;
			}

			var layout = (FlowLayout) target;

			if (!layout.Wrap || layout.IsLaidOutByParent) {
				return false;
			}

			var fit = layout.Axis == LayoutAxis.Horizontal ? layout.FitWidth : layout.FitHeight;

			// With a set number per line, Preferred sizes to exactly that many, which is useful
			if (layout.MaxPerLine > 0 && fit == FitMode.Preferred) {
				return false;
			}

			return fit != FitMode.None;
		}

		private bool IsVerticalWrap()
		{
			return !m_wrap.hasMultipleDifferentValues && m_wrap.boolValue
				&& !m_axis.hasMultipleDifferentValues && m_axis.enumValueIndex == (int) LayoutAxis.Vertical;
		}
	}
}
