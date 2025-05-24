using System;
using UnityEngine;

namespace Tekly.DebugKit.Widgets
{
	public class Vector3Widget : Widget
	{
		private readonly Func<Vector3> m_getValue;
		private readonly Action<Vector3> m_setValue;

		private Vector3 Position {
			get => m_getValue();
			set => m_setValue(value);
		}
		
		public Vector3Widget(Container container, string label, Func<Vector3> getValue, Action<Vector3> setValue)
		{
			m_getValue = getValue;
			m_setValue = setValue;
			
			container.Row("dk-field-row", row =>
			{
				if (!string.IsNullOrEmpty(label)) {
					row.Label(label);
				}
				
				row.FloatField("", () => Position.x, SetVectorX);
				row.FloatField("", () => Position.y, SetVectorY);
				row.FloatField("", () => Position.z, SetVectorZ);
			});
		}
		
		private void SetVectorX(float value)
		{
			var v = m_getValue();
			v.x = value;
			m_setValue(v);
		}
		
		private void SetVectorY(float value)
		{
			var v = m_getValue();
			v.y = value;
			m_setValue(v);
		}
		
		private void SetVectorZ(float value)
		{
			var v = m_getValue();
			v.z = value;
			m_setValue(v);
		}
	}
}