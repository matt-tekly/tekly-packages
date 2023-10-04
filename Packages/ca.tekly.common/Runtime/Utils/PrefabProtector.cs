using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Common.Utils
{
    public static class PrefabProtector
    {
        private static Transform s_container;

        private static Dictionary<GameObject, GameObject> s_prefabs = new Dictionary<GameObject, GameObject>();

        private static Transform GetContainer()
        {
            if (s_container == null) {
                var go = new GameObject("[PrefabProtector]");
                Object.DontDestroyOnLoad(go);
                s_container = go.transform;
            }

            return s_container;
        }
        
        public static T Protect<T>(T component) where T : Component
        {
#if UNITY_EDITOR
            var gameObject = component.gameObject;
            
            if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(gameObject)) {
                gameObject.SetActive(false);
                return component;
            }

            if (s_prefabs.TryGetValue(gameObject, out var existingGameObject)) {
                return existingGameObject.GetComponent<T>();
            }
            
            var wasActive = gameObject.activeSelf;
            
            gameObject.SetActive(false);
            var instance = Object.Instantiate(component, GetContainer());

            s_prefabs[gameObject] = instance.gameObject;
            
            instance.gameObject.name = gameObject.name;
            gameObject.SetActive(wasActive);

            return instance;
#else
            component.gameObject.SetActive(false);
            return component;
#endif
        }

        public static GameObject Protect(GameObject gameObject)
        {
#if UNITY_EDITOR
            if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(gameObject)) {
                gameObject.SetActive(false);
                return gameObject;
            }

            if (s_prefabs.TryGetValue(gameObject, out var existingGameObject)) {
                return existingGameObject;
            }
            
            var wasActive = gameObject.activeSelf;
            
            gameObject.SetActive(false);
            var instance = Object.Instantiate(gameObject, GetContainer());
            s_prefabs[gameObject] = instance;
            
            instance.gameObject.name = gameObject.name;
            gameObject.SetActive(wasActive);

            return instance;
#else
            gameObject.SetActive(false);
            return gameObject;
#endif
        }
    }
}