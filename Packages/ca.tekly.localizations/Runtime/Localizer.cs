using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.Common.Utils;
using Tekly.Logging;

namespace Tekly.Localizations
{
    public class Localizer : Singleton<Localizer, ILocalizer>, ILocalizer
    {
        public bool IsLoading => m_banks.Any(x => x.IsLoading);
        
        public string LanguageLabel { get; set; }
        
        private readonly Dictionary<string, LocalizationString> m_strings = new Dictionary<string, LocalizationString>();

        private readonly TkLogger m_logger = TkLogger.Get<Localizer>();
        
        private readonly (string, object)[] m_emptyData = Array.Empty<(string, object)>();
        private readonly ArraysPool<object> m_objectArrayPool = new ArraysPool<object>();
        
        private readonly List<LocalizationBank> m_banks = new List<LocalizationBank>();
        public static char LocToken = '$';
        
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
                    return Localize(locString, m_emptyData);
                }
                return locString.Format;
            }

            m_logger.Error("Failed to find localization ID: [{id}]", ("id", id));
            
            return $"[{id}]";
        }

        public string Localize(string id, (string, object)[] data)
        {
            if (m_strings.TryGetValue(id, out var locString)) {
                return Localize(locString, data);
            }
            
            m_logger.Error("Failed to find localization ID: [{id}]", ("id", id));
            return $"[{id}]";
        }

        public string Localize(LocalizationString locString, (string, object)[] data)
        {
            var formattingData = m_objectArrayPool.Get(locString.Keys.Length);
            ToFormattedArray(formattingData, data, locString.Keys);
            var text = string.Format(locString.Format, formattingData);
                
            m_objectArrayPool.Return(formattingData);

            return text;
        }
        
        public void LoadBank(string key)
        {
            var bank = m_banks.FirstOrDefault(x => x.Key == key);

            if (bank != null) {
                bank.AddRef();
            } else {
                bank = new LocalizationBank(key, this);
                m_banks.Add(bank);
            }
        }
        
        public void UnloadBank(string key)
        {
            var bank = m_banks.FirstOrDefault(x => x.Key == key);

            if (bank != null) {
                bank.RemoveRef();

                if (bank.References == 0) {
                    bank.Dispose();
                    m_banks.Remove(bank);
                }
            }
        }

        private void ToFormattedArray(object[] outObjects, (string, object)[] data, string[] keys)
        {
            for (var index = 0; index < keys.Length; index++) {
                outObjects[index] = GetData(keys[index], data);
            }
        }

        private object GetData(string key, (string, object)[] data)
        {
            if (key[0] == LocToken) {
                return Localize(key);
            }
            
            foreach (var (dataKey, dataValue) in data) {
                if (dataKey == key) {
                    if (dataValue is string dataString)
                    {
                        if (dataString[0] == LocToken) {
                            return Localize(dataString);
                        }
                    }
                    return dataValue;
                }
            }
            
            m_logger.Error("Failed to find data with key: [{key}]", ("key", key));

            return $"[{key}]";
        }
    }
}