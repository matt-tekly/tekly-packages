using System;
using UnityEditor;

namespace Tekly.EditorUtils.Gui
{
	public readonly struct LabelWidthScope : IDisposable
	{
		private readonly float m_previous;

		public LabelWidthScope(float width)
		{
			m_previous = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = width;
		}

		public void Dispose()
		{
			EditorGUIUtility.labelWidth = m_previous;
		}
	}
}