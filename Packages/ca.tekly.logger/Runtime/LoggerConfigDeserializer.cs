using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Tekly.Logging.LogDestinations;

namespace Tekly.Logging
{
    /// <summary>
    /// Responsible for deserializing the LoggerConfigData from XML.
    /// </summary>
    public static class LoggerConfigDeserializer
    {
        private static readonly List<XmlArrayItemAttribute> s_arrayAttributeOverrides = new List<XmlArrayItemAttribute>();

        private static bool s_initialized;
        
        /// <summary>
        /// Adds a mapping of an element name to a LogDestinationConfig derived type.
        /// This allows you to add your own custom defined Destinations to the config.
        /// </summary>
        /// <param name="elementName"></param>
        /// <typeparam name="T"></typeparam>
        public static void AddDestinationConfigMapping<T>(string elementName) where T : LogDestinationConfig
        {
            s_arrayAttributeOverrides.Add(new XmlArrayItemAttribute(elementName, typeof(T)));
        }
        
        public static LoggerConfigData Deserialize(string xml)
        {
            using var reader = new StringReader(xml);
            
            var attributes = new XmlAttributes();
            
            foreach (var arrayAttributeOverride in s_arrayAttributeOverrides) {
                attributes.XmlArrayItems.Add(arrayAttributeOverride);
            }
            
            var overrides = new XmlAttributeOverrides();
            overrides.Add(typeof(LoggerProfileData), "Destinations", attributes);
            
            var serializer = new XmlSerializer(typeof(LoggerConfigData), overrides);
            
            return (LoggerConfigData) serializer.Deserialize(reader);
        }
        
        internal static void Initialize()
        {
            if (s_initialized) {
                return;
            }
            
            AddDestinationConfigMapping<FlatFileLogConfig>("FlatFile");
            AddDestinationConfigMapping<StructuredFileLogConfig>("StructuredFile");
            AddDestinationConfigMapping<UnityLogDestinationConfig>("Unity");

            s_initialized = true;
        }

        internal static void Reset()
        {
            s_initialized = false;
            s_arrayAttributeOverrides.Clear();
        }
    }
}