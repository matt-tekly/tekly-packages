using System;
using UnityEngine;

namespace Tekly.Common.Pooling
{
	[Serializable]
	public class SerializedPool<T> : IComponentPool<T> where T : PooledComponent<T>
	{
		[SerializeField] private T m_template;
		[NonSerialized] private ComponentPool<T> m_pool;

		public void Initialize()
		{
			if (m_pool == null) {
				m_pool = new ComponentPool<T>(m_template);
			}
		}
		
		public T Get()
		{
			Initialize();
			return m_pool.Get();
		}
		
		public void Clear()
		{
			m_pool?.Clear();
			m_pool = null;
		}
	}
}