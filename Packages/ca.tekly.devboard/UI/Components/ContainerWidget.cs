using Tekly.Trellis;
using UnityEngine;

namespace Tekly.DevBoard.Components
{
	public class ContainerWidget : Widget
	{
		/// <summary>
		/// Where child widgets go.
		/// </summary>
		public RectTransform Content => m_content;

		/// <summary>
		/// Which way children are laid out.
		/// </summary>
		public Common.Utils.LayoutAxis Axis => m_layout.Axis;

		private const float MIN_SHARED_WIDTH = 40f;

		[SerializeField] protected RectTransform m_content;
		[SerializeField] private FlowLayout m_layout;

		/// <summary>
		/// A container with no prefab: a bare vertical layout with no background, for grouping widgets.
		/// </summary>
		internal static ContainerWidget CreatePlain(Transform parent, string name, int spacing = 4)
		{
			var gameObject = new GameObject(name, typeof(RectTransform));
			var rectTransform = (RectTransform) gameObject.transform;
			rectTransform.SetParent(parent, false);

			var layout = gameObject.AddComponent<FlowLayout>();
			layout.Axis = Common.Utils.LayoutAxis.Vertical;
			layout.Spacing = spacing;
			layout.CrossAlignment = CrossAlignment.Stretch;

			var container = gameObject.AddComponent<ContainerWidget>();
			container.m_content = rectTransform;
			container.m_layout = layout;

			return container;
		}

		/// <summary>
		/// A container with no prefab laid out side by side, children aligned to the top.
		/// </summary>
		internal static ContainerWidget CreatePlainRow(Transform parent, string name = "Row")
		{
			return CreatePlain(parent, name)
				.WithAxis(Common.Utils.LayoutAxis.Horizontal)
				.WithCrossAlignment(CrossAlignment.Start);
		}

		/// <summary>
		/// Destroys every widget in this container.
		/// </summary>
		public void Clear()
		{
			for (var i = m_content.childCount - 1; i >= 0; i--) {
				var child = m_content.GetChild(i).gameObject;

				DestroyWidget(child);
			}
		}

		public ContainerWidget WithPadding(float left, float top, float right, float bottom)
		{
			return WithPadding(new Edges(left, right, top, bottom));
		}

		public ContainerWidget WithPadding(float all)
		{
			return WithPadding(Edges.All(all));
		}

		public ContainerWidget WithPadding(Edges padding)
		{
			m_layout.Padding = padding;
			return this;
		}

		public ContainerWidget WithSpacing(int spacing)
		{
			m_layout.Spacing = spacing;
			return this;
		}

		public ContainerWidget WithMaxHeight(float maxHeight)
		{
			m_layout.MaxHeight = maxHeight;
			return this;
		}
		
		/// <summary>
		/// Widgets inside with a width group share widths here instead of with the nearest scope above. Shared widths
		/// are at least MIN_SHARED_WIDTH, so a few short labels still make a tidy column.
		/// </summary>
		public ContainerWidget WithWidthGroupScope()
		{
			if (!TryGetComponent<WidthGroup>(out _)) {
				var group = gameObject.AddComponent<WidthGroup>();
				group.MinWidth = MIN_SHARED_WIDTH;
			}

			return this;
		}

		/// <summary>
		/// Which way children are laid out: Vertical stacks them, Horizontal puts them in a row.
		/// </summary>
		public ContainerWidget WithAxis(Common.Utils.LayoutAxis axis)
		{
			m_layout.Axis = axis;
			return this;
		}

		public ContainerWidget WithAlignment(LayoutAlignment layoutAlignment)
		{
			m_layout.Alignment = layoutAlignment;
			return this;
		}
		
		public ContainerWidget WithCrossAlignment(CrossAlignment crossAlignment)
		{
			m_layout.CrossAlignment = crossAlignment;
			return this;
		}

		/// <summary>
		/// How much of a parent layout's spare width this container takes. 1 fills it. Set on the container itself,
		/// not its content, which can be a child of it, e.g. in tabs and scroll views.
		/// </summary>
		public new ContainerWidget WithFlexibleWidth(float flexible = 1f)
		{
			base.WithFlexibleWidth(flexible);
			return this;
		}

		/// <summary>
		/// How much of a parent layout's spare height this container takes. 1 fills it. Set on the container itself,
		/// not its content.
		/// </summary>
		public new ContainerWidget WithFlexibleHeight(float flexible = 1f)
		{
			base.WithFlexibleHeight(flexible);
			return this;
		}

		/// <summary>
		/// The width this container asks a parent layout for, instead of the width of its content.
		/// </summary>
		public ContainerWidget WithPreferredWidth(float width)
		{
			m_layout.PreferredWidth = width;
			return this;
		}

		/// <summary>
		/// The height this container asks a parent layout for, instead of the height of its content.
		/// </summary>
		public ContainerWidget WithPreferredHeight(float height)
		{
			m_layout.PreferredHeight = height;
			return this;
		}

		public void Add(Widget widget)
		{
			widget.transform.SetParent(m_content, false);
		}

		/// <summary>
		/// Creates a widget from a registered prefab, inside this container.
		/// Every widget factory goes through here.
		/// </summary>
		public T Create<T>(WidgetPrefab<T> prefab) where T : Widget
		{
			return Instantiate(DevBoardUI.Instance.Get(prefab), m_content, false);
		}

		/// <summary>
		/// Destroys a widget straight away as far as layout and ticking are concerned: it's deactivated and
		/// unparented now, and the GameObject itself is destroyed at the end of the frame.
		/// </summary>
		internal static void DestroyWidget(GameObject widget)
		{
			if (widget == null) {
				return;
			}

			widget.SetActive(false);
			widget.transform.SetParent(null, false);
			Destroy(widget);
		}
	}

	public static class ContainerWidgetFactory
	{
		/// <summary>
		/// A container with a background, for grouping widgets.
		/// </summary>
		public static ContainerWidget Card(this ContainerWidget parent, WidgetPrefab<ContainerWidget> prefab = default)
		{
			return parent.Create(prefab.Or(WidgetPrefabs.Card));
		}

		/// <summary>
		/// Lays widgets out side by side. Without a prefab it's a bare layout, with no padding or background.
		/// </summary>
		public static ContainerWidget Row(this ContainerWidget parent, WidgetPrefab<ContainerWidget> prefab = default)
		{
			return prefab.IsSet ? parent.Create(prefab) : ContainerWidget.CreatePlainRow(parent.Content);
		}
	}
}
