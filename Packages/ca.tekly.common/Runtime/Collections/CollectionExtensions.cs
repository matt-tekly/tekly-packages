using System;
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
        
        public static void Shuffle<T>(this T[] collection)
        {
            if (collection.Length == 1) {
                return;
            }
            
            var n = collection.Length;  
            while (n > 1) {
                n--;
                var k = UnityEngine.Random.Range(0, n + 1);
                (collection[k], collection[n]) = (collection[n], collection[k]);
            }
        }
        
        public static void Shuffle<T>(this T[] collection, NumberGenerator generator)
        {
            if (collection.Length == 1) {
                return;
            }
            
            var n = collection.Length;  
            while (n > 1) {
                n--;
                var k = generator.Range(0, n + 1);
                (collection[k], collection[n]) = (collection[n], collection[k]);
            }
        }
        
        public static void Shuffle<T>(this List<T> collection)
        {
            if (collection.Count == 1) {
                return;
            }
            
            var n = collection.Count;  
            while (n > 1) {
                n--;
                var k = UnityEngine.Random.Range(0, n + 1);
                (collection[k], collection[n]) = (collection[n], collection[k]);
            }
        }
        
        public static void Shuffle<T>(this List<T> collection, NumberGenerator generator)
        {
            if (collection.Count == 1) {
                return;
            }
            
            var n = collection.Count;  
            while (n > 1) {
                n--;
                var k = generator.Range(0, n + 1);
                (collection[k], collection[n]) = (collection[n], collection[k]);
            }
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

        public static bool TryGet<TK, TV, T>(this Dictionary<TK, TV> dict, TK key, out T target) where TV : class where T : class, TV
        {
            if (dict.TryGetValue(key, out var temp)) {
                target = temp as T;
                return target != null;
            }

            target = default;
            return false;
        }
        
        public static void AddSorted<T>(this List<T> list, T item) where T: IComparable<T>
        {
            if (list.Count == 0) {
                list.Add(item);
                return;
            }

            if (list[list.Count - 1].CompareTo(item) <= 0) {
                list.Add(item);
                return;
            }

            if (list[0].CompareTo(item) >= 0) {
                list.Insert(0, item);
                return;
            }

            var index = list.BinarySearch(item);
            if (index < 0) {
                index = ~index;
            }

            list.Insert(index, item);
        }
    }
}