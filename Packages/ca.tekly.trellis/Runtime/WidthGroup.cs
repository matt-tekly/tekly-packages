using System.Collections.Generic;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tekly.Trellis
{
	/// <summary>
	/// Lines up widths across separate layouts, like the label column of a form where every row is its
	/// own FlowLayout. LayoutItems below this object with the same Width Group name report the widest
	/// min and preferred width among them, so they all size the same.
	///
	/// - Scope: only items under this object join; the nearest enabled WidthGroup above an item wins.
	/// - Several names can live under one group, e.g. "label" and "unit" columns.
	/// - Inactive, disabled and ignored items don't count, so hiding a row can shrink the column.
	/// - Max Width caps the shared width; an item's own min still wins.
	/// - Widths only. Flexible and heights stay each item's own.
	/// </summary>
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	[AddComponentMenu("Layout/Trellis/Width Group")]
	public class WidthGroup : UIBehaviour
	{
		[Tooltip("Cap on the shared width, so one long label can't push every input over")]
		[SerializeField] private OptionalFloat m_maxWidth;

		private readonly Dictionary<string, List<LayoutItem>> m_members = new Dictionary<string, List<LayoutItem>>();

		public OptionalFloat MaxWidth {
			get => m_maxWidth;
			set {
				if (SetPropertyUtility.SetStruct(ref m_maxWidth, value)) {
					MarkAllDirty();
				}
			}
		}

		/// <summary>
		/// Names in use, with their members. For inspectors and debugging.
		/// </summary>
		public IReadOnlyDictionary<string, List<LayoutItem>> Members => m_members;

		/// <summary>
		/// The shared width for a name: the widest min and preferred among its counted members, combined
		/// with the asking item's own measure and capped by Max Width.
		/// </summary>
		public void Share(string key, LayoutMeasure own, out float min, out float preferred)
		{
			var groupMin = own.Min;
			var groupPreferred = own.Preferred;

			if (key != null && m_members.TryGetValue(key, out var members)) {
				foreach (var member in members) {
					if (member == null || !member.isActiveAndEnabled || member.IgnoreLayout) {
						continue;
					}

					var measure = member.MeasureOwn(0);
					groupMin = Mathf.Max(groupMin, measure.Min);
					groupPreferred = Mathf.Max(groupPreferred, measure.Preferred);
				}
			}

			var cap = m_maxWidth.IsSet ? Mathf.Max(0f, m_maxWidth.Value) : float.PositiveInfinity;
			SharedSize.Combine(own.Min, groupMin, groupPreferred, cap, out min, out preferred);
		}

		internal void Add(LayoutItem item, string key)
		{
			if (!m_members.TryGetValue(key, out var members)) {
				members = new List<LayoutItem>();
				m_members[key] = members;
			}

			if (!members.Contains(item)) {
				members.Add(item);
			}

			MarkDirty(key);
		}

		internal void Remove(LayoutItem item, string key)
		{
			if (key == null || !m_members.TryGetValue(key, out var members)) {
				return;
			}

			members.Remove(item);

			if (members.Count == 0) {
				m_members.Remove(key);
			} else {
				MarkDirty(key);
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			RefreshDescendants();
		}

		protected override void OnDisable()
		{
			// isActiveAndEnabled is already false, so items move to an outer group or none
			RefreshDescendants();
			m_members.Clear();
			base.OnDisable();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			MarkAllDirty();
		}
#endif

		private void RefreshDescendants()
		{
			foreach (var item in GetComponentsInChildren<LayoutItem>(true)) {
				item.RefreshWidthGroup();
			}
		}

		/// <summary>
		/// Everyone sharing a name changes width together, so all of them need a rebuild.
		/// </summary>
		private void MarkDirty(string key)
		{
			if (!m_members.TryGetValue(key, out var members)) {
				return;
			}

			foreach (var member in members) {
				if (member != null) {
					member.SetDirty();
				}
			}
		}

		private void MarkAllDirty()
		{
			foreach (var key in m_members.Keys) {
				MarkDirty(key);
			}
		}
	}
}
