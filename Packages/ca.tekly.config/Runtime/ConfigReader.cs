using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Tekly.Config
{
    public class ConfigReader : IConfigReader
    {
        private Dictionary<string, string> m_config;

        public bool Load(IDictionary<string, string> config)
        {
            if (config == null) {
                return false;
            }
            
            m_config = new Dictionary<string, string>(config);

            return true;
        }

        public bool Get(string key, bool defaultValue)
        {
            if (bool.TryParse(m_config.GetValueOrDefault(key), out bool value)) {
                return value;
            }

            return defaultValue;
        }

        public int Get(string key, int defaultValue)
        {
            if (int.TryParse(m_config.GetValueOrDefault(key), out int value)) {
                return value;
            }

            return defaultValue;
        }

        public float Get(string key, float defaultValue)
        {
            if (float.TryParse(m_config.GetValueOrDefault(key), out float value)) {
                return value;
            }

            return defaultValue;
        }

        public double Get(string key, double defaultValue)
        {
            if (double.TryParse(m_config.GetValueOrDefault(key), out double value)) {
                return value;
            }

            return defaultValue;
        }

        public string Get(string key, string defaultValue)
        {
            if (m_config.TryGetValue(key, out string value)) {
                return value;
            }

            return defaultValue;
        }
    }
}
