using System;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
	public class SegmentVisibilityBinder : BasicValueBinder<double>
	{
		[SerializeField] private GameObject[] m_targets;

		protected override void BindValue(double value)
		{
			var enabledCount = (int) Math.Floor(value);

			for (var i = 0; i < enabledCount; i++) {
				m_targets[i].SetActive(true);
			}

			for (var i = enabledCount; i < m_targets.Length; i++) {
				m_targets[i].SetActive(false);
			}
		}
	}
}