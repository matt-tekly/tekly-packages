using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.Common.Utils;
using Tekly.Logging;

namespace Tekly.Localizations
{
	public class Localizer : Singleton<Localizer, ILocalizer>, ILocalizer
	{
		public static char LocToken = '$';
		public bool IsLoading => m_banks.Any(x => x.IsLoading);

		public string LanguageLabel { get; set; }

		private readonly Dictionary<string, LocalizationString> m_strings = new Dictionary<string, LocalizationString>();

		private readonly (string, object)[] m_emptyData = Array.Empty<(string, object)>();
		private readonly List<LocalizationBank> m_banks = new List<LocalizationBank>();
		private readonly ILocalizationFormatter m_formatter;

		private readonly TkLogger m_logger = TkLogger.Get<Localizer>();

		public Localizer()
		{
#if TEKLY_SMARTFORMAT
			m_formatter = new LocalizationSmartFormatter(this);
#else
            m_formatter = new LocalizationFormatter(this);
#endif
		}

		public void Clear()
		{
			m_strings.Clear();
		}

		public void AddData(LocalizationData localizationData)
		{
			foreach (var dataString in localizationData.Strings) {
#if TEKLY_SMARTFORMAT
				m_strings[dataString.Id] = new LocalizationString(dataString.Id, dataString.Format);
#else
				LocalizationStringifier.Stringify(dataString.Format, out var outFormat, out var outKeys);
				m_strings[dataString.Id] = new LocalizationString(dataString.Id, outFormat, outKeys);
#endif
			}
		}

		public string Localize(string id)
		{
			if (m_strings.TryGetValue(id, out var locString)) {
				if (locString.Keys != null && locString.Keys.Length > 0) {
					return Format(locString, m_emptyData);
				}

				return locString.Format;
			}

			m_logger.Error("Failed to find localization ID: [{id}]", ("id", id));

			return $"[{id}]";
		}

		public string Localize(string id, (string, object)[] data)
		{
			if (m_strings.TryGetValue(id, out var locString)) {
				return Format(locString, data);
			}

			m_logger.Error("Failed to find localization ID: [{id}]", ("id", id));
			return $"[{id}]";
		}

		public string Format(LocalizationString locString, (string, object)[] data)
		{
			return m_formatter.Localize(locString, data);
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
	}
}