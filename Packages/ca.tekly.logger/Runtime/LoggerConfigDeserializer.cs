using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Tekly.Logging.LogDestinations;

namespace Tekly.Logging
{
    /// <summary>
    /// Responsible for deserializing the LoggerConfigData from XML.
    /// </summary>
    public class LoggerConfigDeserializer
    {
        private readonly List<XmlArrayItemAttribute> m_arrayAttributeOverrides = new List<XmlArrayItemAttribute>();
        private XmlSerializer m_serializer;

        public LoggerConfigDeserializer()
        {
            AddDestinationConfigMapping<FlatFileLogConfig>("FlatFile");
            AddDestinationConfigMapping<StructuredFileLogConfig>("StructuredFile");
            AddDestinationConfigMapping<UnityLogDestinationConfig>("Unity");
        }
        
        /// <summary>
        /// Adds a mapping of an element name to a LogDestinationConfig derived type.
        /// This allows you to add your own custom defined Destinations to the config.
        /// </summary>
        /// <param name="elementName"></param>
        /// <typeparam name="T"></typeparam>
        public void AddDestinationConfigMapping<T>(string elementName) where T : LogDestinationConfig
        {
            m_arrayAttributeOverrides.Add(new XmlArrayItemAttribute(elementName, typeof(T)));
        }
        
        public LoggerConfigData Deserialize(string xml)
        {
            using var reader = new StringReader(xml);
            
            var attributes = new XmlAttributes();
            
            foreach (var arrayAttributeOverride in m_arrayAttributeOverrides) {
                attributes.XmlArrayItems.Add(arrayAttributeOverride);
            }
            
            var overrides = new XmlAttributeOverrides();
            overrides.Add(typeof(LoggerProfileData), "Destinations", attributes);
            
            m_serializer = new XmlSerializer(typeof(LoggerConfigData), overrides);
            
            return (LoggerConfigData) m_serializer.Deserialize(reader);
        }
    }
}