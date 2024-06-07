using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Config
{
    public interface IConfigParser
    {
        bool Parse(string config);

        Dictionary<string, string> ToDictionary();
    }
    
    public class ConfigParser : IConfigParser
    {
        private Dictionary<string, string> m_config;
        private RuntimePlatform m_platform;

        private string m_platformString;


        public ConfigParser(RuntimePlatform platform)
        {
            m_config = new Dictionary<string, string>();
            m_platform = platform;
            m_platformString = PlatformToTokenString();
        }

        public bool Parse(string configString)
        {
            if (string.IsNullOrEmpty(configString)) {
                return false;
            }

            var lines = configString.Split('\n');

            foreach (var line in lines) {
                ParseLine(line);
            }

            return true;
        }

        public Dictionary<string, string> ToDictionary()
        {
            return m_config;
        }

        private void ParseLine(string line)
        {
            // given a line like this: "boolValue:#editor#true\n"
            var parts = line.Split('#');

            if (parts.Length != 3) {
                return;
            }

            var key = parts[0];
            var platform = parts[1];
            var value = parts[2];

            if (platform == m_platformString) {
                m_config[key] = value;
            }
        }

        private string PlatformToTokenString()
        {
            var editorPlatforms = new List<RuntimePlatform>() {
                RuntimePlatform.WindowsEditor,
                RuntimePlatform.OSXEditor,
                RuntimePlatform.LinuxEditor
            };

            if (m_platform == RuntimePlatform.Android) {
                return "android";
            }

            if (editorPlatforms.Contains(m_platform)) {
                return "editor";
            }

            if (m_platform == RuntimePlatform.IPhonePlayer) {
                return "ios";
            }

            throw new InvalidOperationException("Invalid Platform: " + m_platform);
        }
    }
}