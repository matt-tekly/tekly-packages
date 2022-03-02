using UnityEngine;

namespace Tekly.Common.Utils
{
    public static class PrefabProtector
    {
        private static Transform s_container;

        private static Transform GetContainer()
        {
            if (ReferenceEquals(s_container, null)) {
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
                return component;
            }

            var wasActive = gameObject.activeSelf;
            
            gameObject.SetActive(false);
            var instance = Object.Instantiate(component, GetContainer());
            instance.gameObject.name = gameObject.name;
            gameObject.SetActive(wasActive);

            return instance;
#else
            return component;
#endif
        }
        
        public static T Protect<T>(T component, bool active) where T : Component
        {
#if UNITY_EDITOR
            var gameObject = component.gameObject;
            
            if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(gameObject)) {
                return component;
            }
            
            var wasActive = gameObject.activeSelf;
            
            gameObject.SetActive(false);
            var instance = Object.Instantiate(component, GetContainer());
            instance.gameObject.name = gameObject.name;
            gameObject.SetActive(wasActive);

            return instance;
#else
            return component;
#endif
        }
        
        public static GameObject Protect(GameObject gameObject)
        {
#if UNITY_EDITOR
            if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(gameObject)) {
                return gameObject;
            }
            
            gameObject.SetActive(false);
            var instance = Object.Instantiate(gameObject, GetContainer());
            instance.gameObject.name = gameObject.name;
            gameObject.SetActive(true);

            return instance;
#else
            return gameObject;
#endif
        }
    }
}