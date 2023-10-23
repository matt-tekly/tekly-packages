using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Common.Utils
{
    public static class PrefabProtector
    {
        public static T Protect<T>(T component) where T : Component
        {
            return PrefabProtectorImpl.Instance.Protect(component);
        }

        public static GameObject Protect(GameObject gameObject)
        {
            return PrefabProtectorImpl.Instance.Protect(gameObject);
        }
    }

    internal class PrefabProtectorImpl : Singleton<PrefabProtectorImpl>
    {
        private Transform m_container;
        private readonly Dictionary<GameObject, GameObject> m_prefabs = new Dictionary<GameObject, GameObject>();

        private Transform GetContainer()
        {
            if (m_container == null) {
                var go = new GameObject("[PrefabProtector]");
                Object.DontDestroyOnLoad(go);
                m_container = go.transform;
            }

            return m_container;
        }

        public T Protect<T>(T component) where T : Component
        {
#if UNITY_EDITOR
            var gameObject = component.gameObject;

            if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(gameObject)) {
                gameObject.SetActive(false);
                return component;
            }

            if (m_prefabs.TryGetValue(gameObject, out var existingGameObject)) {
                return existingGameObject.GetComponent<T>();
            }

            var wasActive = gameObject.activeSelf;

            gameObject.SetActive(false);
            var instance = Object.Instantiate(component, GetContainer());

            m_prefabs[gameObject] = instance.gameObject;

            instance.gameObject.name = gameObject.name;
            gameObject.SetActive(wasActive);

            return instance;
#else
            component.gameObject.SetActive(false);
            return component;
#endif
        }

        public GameObject Protect(GameObject gameObject)
        {
#if UNITY_EDITOR
            if (!UnityEditor.PrefabUtility.IsPartOfAnyPrefab(gameObject)) {
                gameObject.SetActive(false);
                return gameObject;
            }

            if (m_prefabs.TryGetValue(gameObject, out var existingGameObject)) {
                return existingGameObject;
            }

            var wasActive = gameObject.activeSelf;

            gameObject.SetActive(false);
            var instance = Object.Instantiate(gameObject, GetContainer());
            m_prefabs[gameObject] = instance;

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