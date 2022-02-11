// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Common.Utils
{
    public static class UnityExtensions
    {
        /// <summary>
        /// Like GetComponentInParent but doesn't include itself
        /// </summary>
        public static T GetComponentInAncestor<T>(this Transform transform) where T : Component
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
            for (var index = 0; index < component.transform.childCount; ++index) {
                component.transform.GetChild(index).GetComponentsInChildren(items);
            }
        }
    }
}