using System;
using Tekly.Simulant.Collections;
using UnityEngine.Assertions;

namespace Tekly.Simulant.Core
{
	public class QueryBuilder
	{
		public int Hash;
		
		private readonly GrowingArray<int> m_includes = new GrowingArray<int>(8);
		private readonly GrowingArray<int> m_excludes = new GrowingArray<int>(8);
		
		private readonly World m_world;

		private bool m_isBuilding;
		
		public QueryBuilder(World world)
		{
			m_world = world;
		}

		public void Start()
		{
			Assert.IsFalse(m_isBuilding);
			m_isBuilding = true;
		}
		
		public QueryBuilder Include<T>() where T : struct
		{
			var typeId = m_world.GetPool<T>().Id;
			Assert.IsFalse(m_includes.Contains(typeId));
			
			m_includes.Add(typeId);
			return this;
		}
		
		public QueryBuilder Include<T, U>() where T : struct where U : struct
		{
			Include<T>();
			Include<U>();
			return this;
		}
		
		public QueryBuilder Exclude<T>() where T : struct
		{
			var typeId = m_world.GetPool<T>().Id;
			Assert.IsFalse(m_includes.Contains(typeId));
			
			m_excludes.Add(typeId);
			return this;
		}

		public Query Build()
		{
			Array.Sort(m_includes.Data, 0, m_includes.Count);
			Array.Sort(m_excludes.Data, 0, m_excludes.Count);

			Hash = 17;
			
			Hash = Hash * 31 + m_includes.Count;
			Hash = Hash * 31 + m_excludes.Count;
			
			for (int i = 0, iMax = m_includes.Count; i < iMax; i++) {
				Hash = Hash * 31 + m_includes.Data[i];
			}
			for (int i = 0, iMax = m_excludes.Count; i < iMax; i++) {
				Hash = Hash * 31 - m_excludes.Data[i];
			}

			return m_world.FinalizeQuery(this);
		}

		public (int[] includes, int[] excludes) Compact()
		{
			var includes = m_includes.Compacted();
			var excludes = m_excludes.Compacted();
			
			Reset();
			
			return (includes, excludes);
		}

		public void Reset()
		{
			Hash = 0;
			m_isBuilding = false;
			
			m_includes.Count = 0;
			m_excludes.Count = 0;
		}
	}
}