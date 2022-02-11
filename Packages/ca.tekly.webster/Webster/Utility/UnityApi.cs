//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Tekly.Webster.Utility
{
	public static class UnityApi
	{
		public static void SetComponentEnabled(Component component, bool enabled)
		{
			if (component is Behaviour behaviour) {
				behaviour.enabled = enabled;
			} else if (component is Renderer renderer) {
				renderer.enabled = enabled;
			}
		}

		public static Component FindComponent(int instanceId)
		{
			return Resources
				.FindObjectsOfTypeAll<Component>()
				.FirstOrDefault(component => component.GetInstanceID() == instanceId && IsNotPrefab(component.gameObject));
		}

		public static GameObject FindGameObjectByInstanceId(int instanceId)
		{
			return Resources
				.FindObjectsOfTypeAll<GameObject>()
				.FirstOrDefault(go => go.GetInstanceID() == instanceId && IsNotPrefab(go));
		}

		public static bool IsNotPrefab(GameObject go)
		{
			return !IsPrefab(go);
		}
		
		public static T FindResource<T>(int instanceId) where T : Object
		{
			return Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(resource => resource.GetInstanceID() == instanceId);
		}
		
		public static T FindResource<T>(string name) where T : Object
		{
			return Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(resource => resource.name == name);
		}

		public static bool IsPrefab(GameObject go)
		{
#if UNITY_EDITOR
#if UNITY_2018_3_OR_NEWER
			return PrefabUtility.IsPartOfPrefabAsset(go);
#else
            var prefabType = UnityEditor.PrefabUtility.GetPrefabType(go);
            return prefabType == UnityEditor.PrefabType.Prefab || prefabType == UnityEditor.PrefabType.ModelPrefab;
#endif
#else
            return false;
#endif
		}
	}
}