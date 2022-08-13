using System.Collections.Generic;
using Tekly.Common.Utils;

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
        
        public static T Random<T>(this T[] collection, NumberGenerator generator)
        {
            if (collection.Length == 1) {
                return collection[0];
            }
            
            return collection[collection.Length.FromRange(generator)];
        }
        
        public static T Random<T>(this List<T> collection)
        {
            if (collection.Count == 1) {
                return collection[0];
            }
            
            return collection[collection.Count.FromRange()];
        }
        
        public static T Random<T>(this List<T> collection, NumberGenerator generator)
        {
            if (collection.Count == 1) {
                return collection[0];
            }
            
            return collection[collection.Count.FromRange(generator)];
        }
        
        public static T Random<T>(this IList<T> collection)
        {
            return collection[collection.Count.FromRange()];
        }
        
        public static T Random<T>(this IList<T> collection, NumberGenerator generator)
        {
            return collection[collection.Count.FromRange(generator)];
        }
        
        public static int FromRange(this int number)
        {
            return UnityEngine.Random.Range(0, number);
        }
        
        public static int FromRange(this int number, NumberGenerator generator)
        {
            return generator.Range(0, number);
        }
        
        public static T Last<T>(this IList<T> collection)
        {
            return collection[collection.Count - 1];
        }
        
        public static T Last<T>(this T[] collection)
        {
            return collection[collection.Length - 1];
        }
    }
}