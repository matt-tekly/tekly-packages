using Tekly.Common.Utils;
using Tekly.EditorUtils.Attributes;
using UnityEngine;

namespace TeklySample.Samples.Town
{
	public class TownWorldRunner : MonoBehaviour
	{
		public int Count = 1024;
		[Polymorphic, SerializeReference] private EntityDefinition[] m_definitions;

		private TownWorld m_townWorld;
		
		private void Awake()
		{
			m_townWorld = new TownWorld();

			for (var i = 0; i < Count; i++) {
				foreach (var definition in m_definitions) {
					m_townWorld.Create(definition);
				}	
			}
		}

		private void Update()
		{
			m_townWorld.Tick(Time.deltaTime);
		}
	}
}