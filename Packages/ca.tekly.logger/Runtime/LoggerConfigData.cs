using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine.Scripting;

namespace Tekly.Logging
{
    [XmlRoot("LoggerConfig")]
    [Preserve]
    public class LoggerConfigData
    {
        [XmlElement("Profile")]
        public List<LoggerProfileData> Profiles = new List<LoggerProfileData>();
        
        public static LoggerConfigData DeserializeXml(string xml, XmlArrayItemAttribute[] additionalConfigs = null)
        {
            var deserializer = new LoggerConfigDeserializer();
            var config = deserializer.Deserialize(xml);
            return config;
        }
    }

    [Serializable]
    [Preserve]
    public class LoggerProfileData
    {
        [XmlAttribute] public string Name;
        [XmlAttribute] public bool Default;

        [XmlArray] public List<LogDestinationConfig> Destinations = new List<LogDestinationConfig>();
        
        [XmlArrayItem("Group")] public List<LoggerGroupData> Groups = new List<LoggerGroupData>();
        
        [XmlElement("Loggers")] public LoggersData Loggers;
    }
    
    [Serializable]
    [Preserve]
    public class LoggerGroupData
    {
        [XmlAttribute] public string Name;
        [XmlAttribute] public bool Default;
        [XmlArray] public List<string> Destinations;
    }
    
    [Serializable]
    [Preserve]
    public class LoggersData
    {
        [XmlElement] public LoggerSettingsData Default = new LoggerSettingsData();
        [XmlElement("Logger")] public List<LoggerSettingsData> Loggers = new List<LoggerSettingsData>();
    }

    [Serializable]
    [Preserve]
    public class LoggerSettingsData
    {
        [XmlAttribute] public string Logger;
        [XmlAttribute] public TkLogLevel Level = TkLogLevel.Debug;
        [XmlAttribute] public string Group;
    }

    
}