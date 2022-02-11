// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;

namespace Tekly.DataModels.Models
{
    public class ModelKey
    {
        public readonly string[] Keys;
        public readonly bool IsRelative;

        private ModelKey(string[] keys, bool isRelative)
        {
            Keys = keys;
            IsRelative = isRelative;
        }
        
        public static ModelKey Parse(string key)
        {
            if (string.IsNullOrEmpty(key)) {
                return null;
            }
            
            if (s_cache.TryGetValue(key, out var modelKey)) {
                return modelKey;
            }

            string[] keys = key.Split('.');

            bool isRelative = key[0] == '*';
            if (isRelative) {
                var oldKeys = keys;
                keys = new string[keys.Length - 1];
                
                Array.Copy(oldKeys, 1, keys, 0, keys.Length);
            }

            modelKey = new ModelKey(keys, isRelative);
            s_cache.Add(key, modelKey);

            return modelKey;
        }

        public static string StripRelativePrefix(string key)
        {
            if (key[0] != '*') {
                return key;
            }

            return key.Substring(2);
        }

        private static Dictionary<string, ModelKey> s_cache = new Dictionary<string, ModelKey>();
    }
}