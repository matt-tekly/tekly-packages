using System;
using System.Collections.Generic;
using Tekly.Simulant.Collections;
using UnityEngine.Assertions;

namespace Tekly.Simulant.Core
{
	public class QueryBuilder
	{
		public int Hash;
		
		private readonly List<int> m_includes = new List<int>(8);
		private readonly List<int> m_excludes = new List<int>(8);
		
		private readonly World m_world;

		private bool m_isBuilding;
		
		private static readonly Comparison<int> s_compareInt = (a, b) => a - b;
			
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
		
		public QueryBuilder Include<T, U, V>() where T : struct where U : struct where V : struct
		{
			Include<T>();
			Include<U>();
			Include<V>();
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
			m_includes.Sort(s_compareInt);
			m_excludes.Sort(s_compareInt);

			Hash = 17;
			
			Hash = Hash * 31 + m_includes.Count;
			Hash = Hash * 31 + m_excludes.Count;
			
			for (int i = 0, iMax = m_includes.Count; i < iMax; i++) {
				Hash = Hash * 31 + m_includes[i];
			}
			for (int i = 0, iMax = m_excludes.Count; i < iMax; i++) {
				Hash = Hash * 31 - m_excludes[i];
			}

			return m_world.FinalizeQuery(this);
		}

		public (int[] includes, int[] excludes) Compact()
		{
			var includes = m_includes.ToArray();
			var excludes = m_excludes.ToArray();
			
			Reset();
			
			return (includes, excludes);
		}

		public void Reset()
		{
			Hash = 0;
			m_isBuilding = false;
			
			m_includes.Clear();
			m_excludes.Clear();
		}
	}
}