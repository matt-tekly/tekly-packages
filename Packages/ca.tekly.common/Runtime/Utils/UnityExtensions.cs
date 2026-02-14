using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace Tekly.Common.Utils
{
    public static class UnityExtensions
    {
        /// <summary>
        /// Like GetComponentInParent but doesn't include itself
        /// </summary>
        public static T GetComponentInAncestor<T>(this Transform transform) where T : class
        {
            var parent = transform.parent;
            return parent != null ? parent.GetComponentInParent<T>() : null;
        }
        
        public static T GetComponentInDirectParent<T>(this MonoBehaviour component) where T : Component
        {
            var parent = component.transform.parent;
            return parent != null ? parent.GetComponent<T>() : null;
        }
        
        /// <summary>
        /// Like GetComponentInParent but doesn't include itself
        /// </summary>
        public static T GetComponentInAncestor<T>(this MonoBehaviour component) where T : Component
        {
            return component.transform.GetComponentInAncestor<T>();
        }
        
        /// <summary>
        /// Like GetComponentsInChildren but doesn't include itself
        /// </summary>
        public static void GetComponentsInDescendents<T>(this Component component, List<T> items) where T : Component
        {
            using (ListPool<T>.Get(out var list)) {
                for (var index = 0; index < component.transform.childCount; ++index) {
                    component.transform.GetChild(index).GetComponentsInChildren(list);
                    items.AddRange(list);  
                } 
            }
        }
        
        public static T[] GetInScene<T>() where T : Component
        {
#if UNITY_EDITOR
            var sceneObjects = new List<T>();
            var allObjects = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

            for (var index = 0; index < allObjects.Length; index++) {
                var obj = allObjects[index];
                var go = obj.gameObject;

                if (go.hideFlags == HideFlags.NotEditable || go.hideFlags == HideFlags.HideAndDontSave) {
                    continue;
                }
                
                if (UnityEditor.EditorUtility.IsPersistent(go.transform.root.gameObject)) {
                    continue;
                }

                sceneObjects.Add(obj);
            }

            return sceneObjects.ToArray();
#else
			return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
#endif
        }
        
        public static T FindResource<T>(int instanceId) where T : Object
        {
            return Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(resource => resource.GetInstanceID() == instanceId);
        }
		
        public static T FindResource<T>(string name) where T : Object
        {
            var resources = Resources.FindObjectsOfTypeAll<T>();
            return resources.FirstOrDefault(resource => resource.name == name);
        }
        
        /// <summary>
        /// If the passed in component is null then this will fill it by using GetComponent
        /// </summary>
        public static void TryFillComponent<T>(this Component target, ref T component) where T : Component
        {
            if (component == null) {
                component = target.GetComponent<T>();
            }
        }
        
        /// <summary>
        /// If the passed in component is null then this will fill it by using GetComponentInChildren
        /// </summary>
        public static void TryFillComponentInChildren<T>(this Component target, ref T component, bool includeInactive = false) where T : Component
        {
            if (component == null) {
                component = target.GetComponentInChildren<T>(includeInactive);
            }
        }
    }
}