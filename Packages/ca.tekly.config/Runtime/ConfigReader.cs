using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Config
{
    public interface IConfigReader
    {

        bool Load();
        bool Get(string key, bool defaultValue);
        int Get(string key, int defaultValue);
        float Get(string key, float defaultValue);
        double Get(string key, double defaultValue);
        string Get(string key, string defaultValue);
    }


    public class ConfigReader : IConfigReader
    {
        public bool Load()
        {
            return false;
        }

        public bool Get(string key, bool defaultValue)
        {
            return false;
        }

        public int Get(string key, int defaultValue)
        {
            return 0;
        }

        public float Get(string key, float defaultValue)
        {
            return 0.0f;
        }

        public double Get(string key, double defaultValue)
        {
            return 0.0;
        }

        public string Get(string key, string defaultValue)
        {
            return "";
        }
    }
}
