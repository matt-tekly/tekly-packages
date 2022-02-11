// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System.Collections.Generic;

namespace Tekly.Common.Collections
{
    public static class CollectionExtensions
    {
        public static T Random<T>(this T[] collection)
        {
            if (collection.Length == 1) {
                return collection[0];
            }
            
            return collection[collection.Length.FromRange()];
        }
        
        public static T Random<T>(this List<T> collection)
        {
            if (collection.Count == 1) {
                return collection[0];
            }
            
            return collection[collection.Count.FromRange()];
        }
        
        public static T Random<T>(this IList<T> collection)
        {
            return collection[collection.Count.FromRange()];
        }
        
        public static int FromRange(this int number)
        {
            return UnityEngine.Random.Range(0, number);
        }
        
        public static T Last<T>(this IList<T> collection)
        {
            return collection[collection.Count - 1];
        }
    }
}