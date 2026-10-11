using UnityEngine;
using UnityEngine.UI;

namespace Tekly.DevBoard.Components
{
	public enum LayoutAxis
	{
		Horizontal,
		Vertical
	}

	[AddComponentMenu("Layout/Linear Layout Group")]
	public class LinearLayoutGroup : HorizontalOrVerticalLayoutGroup
	{
		[SerializeField] private LayoutAxis m_axis = LayoutAxis.Horizontal;

		public LayoutAxis Axis {
			get => m_axis;
			set => SetProperty(ref m_axis, value);
		}

		private bool IsVertical => m_axis == LayoutAxis.Vertical;

		public override void CalculateLayoutInputHorizontal()
		{
			base.CalculateLayoutInputHorizontal();
			CalcAlongAxis(0, IsVertical);
		}

		public override void CalculateLayoutInputVertical()
		{
			CalcAlongAxis(1, IsVertical);
		}

		public override void SetLayoutHorizontal()
		{
			SetChildrenAlongAxis(0, IsVertical);
		}

		public override void SetLayoutVertical()
		{
			SetChildrenAlongAxis(1, IsVertical);
		}
	}
}