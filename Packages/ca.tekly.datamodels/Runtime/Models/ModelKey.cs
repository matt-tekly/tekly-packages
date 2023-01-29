// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Tekly.DataModels.Models
{
    [DebuggerDisplay("Keys=[{DebuggerDisplayKeys}] IsRelative=[{IsRelative}]")]
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

            modelKey = ParseUncached(key);
            s_cache.Add(key, modelKey);

            return modelKey;
        }

        public static ModelKey ParseUncached(string key)
        {
            var keys = key.Split('.');

            var isRelative = key[0] == '*';
            if (isRelative) {
                var oldKeys = keys;
                keys = new string[keys.Length - 1];

                Array.Copy(oldKeys, 1, keys, 0, keys.Length);
            }

            return new ModelKey(keys, isRelative);
        }

        public static string StripRelativePrefix(string key)
        {
            if (key[0] != '*') {
                return key;
            }

            return key.Substring(2);
        }

        public override string ToString()
        {
            return DebuggerDisplayKeys;
        }

        private string DebuggerDisplayKeys => string.Join(".", Keys);
        private static readonly Dictionary<string, ModelKey> s_cache = new Dictionary<string, ModelKey>(2048);
    }
}