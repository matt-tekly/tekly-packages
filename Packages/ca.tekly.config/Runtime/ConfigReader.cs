using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace Tekly.Config
{
    public interface IConfigReader
    {
        bool Load(IDictionary<string, string> config);
        bool Get(string key, bool defaultValue);
        int Get(string key, int defaultValue);
        float Get(string key, float defaultValue);
        double Get(string key, double defaultValue);
        string Get(string key, string defaultValue);
    }


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
            if (bool.TryParse(m_config.GetValueOrDefault(key), out bool boolValue)) {
                return boolValue;
            }

            return defaultValue;
        }

        public int Get(string key, int defaultValue)
        {
            if (m_config.TryGetValue(key, out string value)) {
                return int.Parse(value);
            }

            return defaultValue;
        }

        public float Get(string key, float defaultValue)
        {
            if (m_config.TryGetValue(key, out string value)) {
                return float.Parse(value);
            }

            return defaultValue;
        }

        public double Get(string key, double defaultValue)
        {
            if (m_config.TryGetValue(key, out string value)) {
                return double.Parse(value);
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
