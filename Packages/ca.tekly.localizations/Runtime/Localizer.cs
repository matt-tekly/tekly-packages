using System;
using System.Collections.Generic;
using Tekly.Logging;

namespace Tekly.Localizations
{
    public class Localizer
    {
        private LocalizationData m_localizationData;
        private readonly Dictionary<string, LocalizationString> m_strings = new Dictionary<string, LocalizationString>();

        private readonly TkLogger m_logger = TkLogger.Get<Localizer>();

        public static readonly Localizer Instance = new Localizer();

        private readonly (string, object)[] m_emptyData = Array.Empty<(string, object)>();
        
        public void Clear()
        {
            m_strings.Clear();
        }
        
        public void AddData(LocalizationData localizationData)
        {
            foreach (var dataString in localizationData.Strings) {
                LocalizationStringifier.Stringify(dataString.Format, out var outFormat, out var outKeys);
                m_strings[dataString.Id] = new LocalizationString(dataString.Id, outFormat, outKeys);
            }
        }

        public string Localize(string id)
        {
            if (m_strings.TryGetValue(id, out var locString)) {
                if (locString.Keys != null && locString.Keys.Length > 0) {
                    var formatData = ToFormattedArray(m_emptyData, locString.Keys);
                    return string.Format(locString.Format, formatData);
                }
                return locString.Format;
            }

            m_logger.Error("Failed to find localization ID: [{id}]", ("id", id));
            
            return $"[{id}]";
        }

        public string Localize(string id, (string, object)[] data)
        {
            if (m_strings.TryGetValue(id, out var locString)) {
                var formatData = ToFormattedArray(data, locString.Keys);
                return string.Format(locString.Format, formatData);
            }
            
            m_logger.Error("Failed to find LocalizationString: [{id}]", ("id", id));
            return $"[{id}]";
        }

        private object[] ToFormattedArray((string, object)[] data, string[] keys)
        {
            var objects = new object[keys.Length];

            for (var index = 0; index < keys.Length; index++) {
                var key = keys[index];
                objects[index] = GetData(key, data);
            }

            return objects;
        }

        private object GetData(string key, (string, object)[] data)
        {
            if (key[0] == '$') {
                return Localize(key);
            }
            
            foreach (var (dataKey, dataValue) in data) {
                if (dataKey == key) {
                    return dataValue;
                }
            }
            
            m_logger.Error("Failed to find key: [{key}]", ("key", key));

            return $"[{key}]";
        }
    }
}