using System;
using Tekly.Common.Utils;
using Tekly.Injectors;
using Tekly.Simulant.Core;
using Tekly.Simulant.Extensions.Systems;
using Tekly.Simulant.Systems;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TeklySample.Samples.CubeMovement
{
	[Serializable]
	public struct PrefabData : ITransient
	{
		public GameObject Prefab;
	}
	
	/// <summary>
	/// Creates GameObjects for an Entity from the attached PrefabData.
	/// </summary>
	public class PrefabSystem : ISystem, IDisposable
	{
		[Inject] private World m_world;
		
		[Inject] private DataPool<GameObjectData> m_gameObjectData;
		[Inject] private DataPool<PrefabData> m_prefabData;
		
		private Query m_prefabs;
		private Query m_gameObjects;

		private Disposables m_disposables = new Disposables();

		[Inject]
		private void Initialize()
		{
			m_prefabs = m_world.Query<PrefabData>();
			m_prefabs.ListenAdd(Create).AddTo(m_disposables);

			m_gameObjects = m_world.Query<GameObjectData>();
			m_gameObjects.ListenRemove(entity => {
				ref var gameObjectData = ref m_gameObjectData.Get(entity);
				Object.Destroy(gameObjectData.GameObject);
			}).AddTo(m_disposables);

			foreach (var entity in m_prefabs) {
				Create(entity);
			}
		}
		
		public void Dispose()
		{
			m_disposables.Dispose();
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