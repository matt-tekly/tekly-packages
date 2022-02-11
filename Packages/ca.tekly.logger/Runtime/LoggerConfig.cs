using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace Tekly.Logging
{
    [Serializable]
    public class LoggerLevelPair
    {
        [XmlAttribute] 
        public string Logger;
        
        [XmlAttribute] 
        public TkLogLevel Level;
    }
    
    [Serializable]
    public class LoggerProfile
    {
        [XmlAttribute] 
        public string Name;

        [XmlElement] 
        public List<LoggerLevelPair> Levels = new List<LoggerLevelPair>();
    }

    [Serializable]
    public class LoggerConfig
    {
        public LoggerProfile DefaultProfile = new LoggerProfile();

        public static LoggerConfig DeserializeXml(string xml)
        {
            using TextReader reader = new StringReader(xml);
            XmlSerializer deserializer = new XmlSerializer(typeof(LoggerConfig));
            return (LoggerConfig) deserializer.Deserialize(reader);
        }
    }
}