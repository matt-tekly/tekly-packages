using System.Collections.Generic;
using Tekly.Common.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Trellis
{
	/// <summary>
	/// Converts Unity layout components to Trellis, with undo:
	/// - Horizontal/Vertical Layout Group → FlowLayout, keeping padding, spacing, alignment and reverse
	/// - LayoutElement → LayoutItem settings (min, preferred, flexible, ignore layout, priority)
	/// - ContentSizeFitter on a converted layout → its Fit settings
	/// - Children of a converted layout get a LayoutItem so they keep being laid out
	///
	/// Unity settings without an exact equivalent are approximated and logged with the object as context:
	/// Trellis always sizes its children, Child Force Expand becomes Flexible 1 or Space Evenly, and Child Scale
	/// is ignored. Components that come from a prefab can only be converted inside that prefab.
	/// </summary>
	public static class TrellisConverter
	{
		private const string GROUP_MENU = "Convert to Trellis Flow Layout";
		private const string HIERARCHY_MENU = "GameObject/Trellis/Convert Layouts in Hierarchy";

		private sealed class Report
		{
			public int Layouts;
			public int Items;
			public readonly List<(Object Context, string Message)> Notes = new List<(Object, string)>();

			public void Note(Object context, string message)
			{
				Notes.Add((context, message));
			}

			public void Log()
			{
				foreach (var note in Notes) {
					Debug.LogWarning($"Trellis convert [{note.Context.name}]: {note.Message}", note.Context);
				}

				Debug.Log($"Trellis convert: {Layouts} layouts, {Items} layout items, {Notes.Count} notes");
			}
		}

		[MenuItem("CONTEXT/HorizontalLayoutGroup/" + GROUP_MENU)]
		[MenuItem("CONTEXT/VerticalLayoutGroup/" + GROUP_MENU)]
		private static void ConvertGroupMenu(MenuCommand command)
		{
			Run("Convert to Trellis Flow Layout", report => ConvertGroup((HorizontalOrVerticalLayoutGroup) command.context, report));
		}

		[MenuItem("CONTEXT/LayoutElement/Convert to Trellis Layout Item")]
		private static void ConvertElementMenu(MenuCommand command)
		{
			Run("Convert to Trellis Layout Item", report => ConvertElement((LayoutElement) command.context, report));
		}

		/// <summary>
		/// Every Horizontal/Vertical Layout Group on and below the selected objects, plus their children.
		/// From the Hierarchy's context menu Unity calls this once per selected object.
		/// </summary>
		[MenuItem(HIERARCHY_MENU, false, 49)]
		private static void ConvertHierarchyMenu(MenuCommand command)
		{
			var roots = command.context is GameObject root ? new[] { root } : Selection.gameObjects;

			Run("Convert Layouts to Trellis", report => {
				foreach (var gameObject in roots) {
					ConvertHierarchy(gameObject, report);
				}
			});
		}

		[MenuItem(HIERARCHY_MENU, true)]
		private static bool ValidateConvertHierarchyMenu()
		{
			return Selection.gameObjects.Length > 0;
		}

		private static void Run(string undoName, System.Action<Report> convert)
		{
			Undo.IncrementCurrentGroup();
			Undo.SetCurrentGroupName(undoName);
			var undoGroup = Undo.GetCurrentGroup();

			var report = new Report();
			convert(report);

			Undo.CollapseUndoOperations(undoGroup);
			report.Log();
		}

		private static void ConvertHierarchy(GameObject root, Report report)
		{
			// Parents first: each conversion gives the children their LayoutItem, and a child's own group
			// keeps that item's settings when it's converted next
			var groups = root.GetComponentsInChildren<HorizontalOrVerticalLayoutGroup>(true);

			foreach (var group in groups) {
				if (group != null) {
					ConvertGroup(group, report);
				}
			}
		}

		private static void ConvertGroup(HorizontalOrVerticalLayoutGroup group, Report report)
		{
			var gameObject = group.gameObject;
			var existingItem = gameObject.GetComponent<LayoutItem>();
			var element = gameObject.GetComponent<LayoutElement>();
			var fitter = gameObject.GetComponent<ContentSizeFitter>();

			if (existingItem is LayoutContainer) {
				report.Note(gameObject, "already has a Trellis layout, skipped");
				return;
			}

			if (!CanRemove(group, report) || !CanRemove(existingItem, report) || !CanRemove(element, report) || !CanRemove(fitter, report)) {
				return;
			}

			var settings = new GroupSettings(group);
			var itemJson = existingItem != null ? JsonUtility.ToJson(existingItem) : null;

			Undo.DestroyObjectImmediate(group);

			// LayoutItem is DisallowMultipleComponent, so the old item has to go before the layout arrives
			if (existingItem != null) {
				Undo.DestroyObjectImmediate(existingItem);
			}

			var layout = Undo.AddComponent<FlowLayout>(gameObject);

			if (itemJson != null) {
				JsonUtility.FromJsonOverwrite(itemJson, layout);
			}

			var serialized = new SerializedObject(layout);
			settings.Apply(serialized, gameObject, report);

			if (element != null) {
				CopyElement(element, serialized);
				Undo.DestroyObjectImmediate(element);
			}

			if (fitter != null) {
				serialized.FindProperty("m_fitWidth").enumValueIndex = ToFitMode(fitter.horizontalFit);
				serialized.FindProperty("m_fitHeight").enumValueIndex = ToFitMode(fitter.verticalFit);
				Undo.DestroyObjectImmediate(fitter);
			}

			serialized.ApplyModifiedProperties();
			report.Layouts++;

			ConvertChildren(layout, settings, report);
		}

		private static void ConvertChildren(FlowLayout layout, GroupSettings settings, Report report)
		{
			foreach (Transform child in layout.transform) {
				if (!(child is RectTransform)) {
					continue;
				}

				var item = child.GetComponent<LayoutItem>();

				if (item == null) {
					var element = child.GetComponent<LayoutElement>();
					item = element != null ? ConvertElement(element, report) : AddItem(child.gameObject, report);
				}

				if (item == null) {
					continue;
				}

				if (settings.ExpandsChildren) {
					SetFlexibleIfUnset(item, settings.MainAxis);
				}

				// A child with its own Unity group keeps working as an item; ConvertHierarchy converts it next
				var childFitter = child.GetComponent<ContentSizeFitter>();
				if (childFitter != null && !(item is LayoutContainer) && child.GetComponent<HorizontalOrVerticalLayoutGroup>() == null) {
					report.Note(child.gameObject, "has a ContentSizeFitter, which fights the parent layout. Remove it");
				}
			}
		}

		private static LayoutItem ConvertElement(LayoutElement element, Report report)
		{
			var gameObject = element.gameObject;

			if (gameObject.GetComponent<LayoutItem>() != null) {
				report.Note(gameObject, "already has a LayoutItem, the LayoutElement was left alone");
				return null;
			}

			if (!CanRemove(element, report)) {
				return null;
			}

			var item = Undo.AddComponent<LayoutItem>(gameObject);
			var serialized = new SerializedObject(item);

			CopyElement(element, serialized);
			serialized.ApplyModifiedProperties();

			Undo.DestroyObjectImmediate(element);
			report.Items++;

			return item;
		}

		private static LayoutItem AddItem(GameObject gameObject, Report report)
		{
			report.Items++;
			return Undo.AddComponent<LayoutItem>(gameObject);
		}

		private static void CopyElement(LayoutElement element, SerializedObject target)
		{
			SetOptional(target, "m_minWidth", element.minWidth);
			SetOptional(target, "m_minHeight", element.minHeight);
			SetOptional(target, "m_preferredWidth", element.preferredWidth);
			SetOptional(target, "m_preferredHeight", element.preferredHeight);
			SetOptional(target, "m_flexibleWidth", element.flexibleWidth);
			SetOptional(target, "m_flexibleHeight", element.flexibleHeight);

			target.FindProperty("m_ignoreLayout").boolValue = element.ignoreLayout;
			target.FindProperty("m_layoutPriority").intValue = element.layoutPriority;
		}

		private static void SetFlexibleIfUnset(LayoutItem item, int axis)
		{
			var serialized = new SerializedObject(item);
			var flexible = serialized.FindProperty(axis == 0 ? "m_flexibleWidth" : "m_flexibleHeight");

			if (!flexible.FindPropertyRelative("IsSet").boolValue) {
				flexible.FindPropertyRelative("IsSet").boolValue = true;
				flexible.FindPropertyRelative("Value").floatValue = 1f;
				serialized.ApplyModifiedProperties();
			}
		}

		/// <summary>
		/// LayoutElement uses negative values for "not set".
		/// </summary>
		private static void SetOptional(SerializedObject target, string name, float value)
		{
			if (value < 0f) {
				return;
			}

			var property = target.FindProperty(name);
			property.FindPropertyRelative("IsSet").boolValue = true;
			property.FindPropertyRelative("Value").floatValue = value;
		}

		private static bool CanRemove(Component component, Report report)
		{
			if (component == null) {
				return true;
			}

			if (PrefabUtility.IsPartOfPrefabInstance(component) && !PrefabUtility.IsAddedComponentOverride(component)) {
				report.Note(component.gameObject, $"its {component.GetType().Name} comes from a prefab. Open the prefab to convert it");
				return false;
			}

			return true;
		}

		private static int ToFitMode(ContentSizeFitter.FitMode mode)
		{
			switch (mode) {
				case ContentSizeFitter.FitMode.MinSize:
					return (int) FitMode.Min;
				case ContentSizeFitter.FitMode.PreferredSize:
					return (int) FitMode.Preferred;
				default:
					return (int) FitMode.None;
			}
		}

		/// <summary>
		/// A Unity group's settings, read before it's removed, and how they map onto a FlowLayout.
		/// </summary>
		private readonly struct GroupSettings
		{
			public readonly int MainAxis;

			private readonly bool m_isHorizontal;
			private readonly RectOffset m_padding;
			private readonly float m_spacing;
			private readonly TextAnchor m_alignment;
			private readonly bool m_reverse;
			private readonly bool m_controlMain;
			private readonly bool m_controlCross;
			private readonly bool m_expandMain;
			private readonly bool m_expandCross;
			private readonly bool m_scales;

			/// <summary>
			/// Force Expand with Control Child Size: Unity gives every child a share of spare room, which in
			/// Trellis means making them flexible.
			/// </summary>
			public bool ExpandsChildren => m_expandMain && m_controlMain;

			public GroupSettings(HorizontalOrVerticalLayoutGroup group)
			{
				m_isHorizontal = group is HorizontalLayoutGroup;
				MainAxis = m_isHorizontal ? 0 : 1;

				m_padding = new RectOffset(group.padding.left, group.padding.right, group.padding.top, group.padding.bottom);
				m_spacing = group.spacing;
				m_alignment = group.childAlignment;
				m_reverse = group.reverseArrangement;

				m_controlMain = m_isHorizontal ? group.childControlWidth : group.childControlHeight;
				m_controlCross = m_isHorizontal ? group.childControlHeight : group.childControlWidth;
				m_expandMain = m_isHorizontal ? group.childForceExpandWidth : group.childForceExpandHeight;
				m_expandCross = m_isHorizontal ? group.childForceExpandHeight : group.childForceExpandWidth;
				m_scales = group.childScaleWidth || group.childScaleHeight;
			}

			public void Apply(SerializedObject layout, Object context, Report report)
			{
				layout.FindProperty("m_axis").enumValueIndex = (int) (m_isHorizontal ? LayoutAxis.Horizontal : LayoutAxis.Vertical);
				layout.FindProperty("m_reverse").boolValue = m_reverse;
				layout.FindProperty("m_spacing").floatValue = m_spacing;

				var padding = layout.FindProperty("m_padding");
				padding.FindPropertyRelative("Left").floatValue = m_padding.left;
				padding.FindPropertyRelative("Right").floatValue = m_padding.right;
				padding.FindPropertyRelative("Top").floatValue = m_padding.top;
				padding.FindPropertyRelative("Bottom").floatValue = m_padding.bottom;

				// TextAnchor runs Upper Left, Upper Center, Upper Right, Middle Left, ...: 0 start, 1 center, 2 end
				var row = (int) m_alignment / 3;
				var column = (int) m_alignment % 3;
				var mainPosition = m_isHorizontal ? column : row;
				var crossPosition = m_isHorizontal ? row : column;

				var alignment = (LayoutAlignment) mainPosition;

				if (m_expandMain && !m_controlMain) {
					alignment = LayoutAlignment.SpaceEvenly;
					report.Note(context, "Child Force Expand without Control Child Size spread the children out; now Space Evenly");
				}

				layout.FindProperty("m_alignment").enumValueIndex = (int) alignment;

				// CrossAlignment is Stretch, Start, Center, End
				var cross = m_controlCross && m_expandCross ? CrossAlignment.Stretch : (CrossAlignment) (crossPosition + 1);
				layout.FindProperty("m_crossAlignment").enumValueIndex = (int) cross;

				if (!m_controlMain || !m_controlCross) {
					report.Note(context, "Control Child Size was off, but Trellis always sizes children: they now get their preferred size");
				}

				if (ExpandsChildren) {
					report.Note(context, "Child Force Expand along the axis: children without a Flexible size were given Flexible 1");
				}

				if (m_scales) {
					report.Note(context, "Child Scale has no Trellis equivalent and was dropped");
				}
			}
		}
	}
}
