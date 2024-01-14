using System;
using Tekly.Injectors;
using Tekly.Simulant.Core;
using Tekly.Simulant.Extensions.Systems;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TeklySample.Samples.CubeMovement
{
	public struct PrefabData : ITransient
	{
		public GameObject Prefab;
	}
	
	public class PrefabSystem : ISystem, IDisposable, IQueryListener
	{
		[Inject] private World m_world;
		
		[Inject] private DataPool<GameObjectData> m_gameObjectData;
		[Inject] private DataPool<PrefabData> m_prefabData;
		
		private Query m_prefabs;
		private Query m_gameObjects;

		[Inject]
		private void Initialize()
		{
			m_prefabs = m_world.Query().Include<PrefabData>().Build();
			m_prefabs.AddListener(this);
			
			m_gameObjects = m_world.Query().Include<GameObjectData>().Build();
			m_gameObjects.AddListener(this);

			foreach (var entity in m_prefabs) {
				Create(entity);
			}
		}
		
		public void Dispose()
		{
			m_prefabs.RemoveListener(this);
		}

		public void EntityAdded(int entity, Query query)
		{
			if (query == m_prefabs) {
				Create(entity);	
			}
		}

		public void EntityRemoved(int entity, Query query)
		{
			if (query == m_gameObjects) {
				ref var gameObjectData = ref m_gameObjectData.Get(entity);
				Object.Destroy(gameObjectData.GameObject);
			} 
		}
		
		private void Create(int entity)
		{
			ref var prefabData = ref m_prefabData.Get(entity);
			var instance = Object.Instantiate(prefabData.Prefab);
			
			m_gameObjectData.Add(entity, new GameObjectData {
				GameObject = instance,
				Transform =  instance.transform
			});
		}
	}
}