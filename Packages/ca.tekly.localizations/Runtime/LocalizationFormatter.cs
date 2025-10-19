using Tekly.Common.Utils;
using Tekly.Logging;

namespace Tekly.Localizations
{
	public interface ILocalizationFormatter
	{
		string Localize(LocalizationString locString, (string, object)[] data);
	}

	public class LocalizationFormatter : ILocalizationFormatter
	{
		private readonly Localizer m_localizer;
		private readonly ArraysPool<object> m_objectArrayPool = new ArraysPool<object>();
		private readonly TkLogger m_logger = TkLogger.Get<LocalizationFormatter>();

		public LocalizationFormatter(Localizer localizer)
		{
			m_localizer = localizer;
		}
		
		public string Localize(LocalizationString locString, (string, object)[] data)
		{
			if (locString.Keys == null || locString.Keys.Length == 0) {
				return locString.Format;
			}

			var formattingData = m_objectArrayPool.Get(locString.Keys.Length);
			ToFormattedArray(formattingData, data, locString.Keys);
			var text = string.Format(locString.Format, formattingData);

			m_objectArrayPool.Return(formattingData);

			return text;
		}

		private void ToFormattedArray(object[] outObjects, (string, object)[] data, string[] keys)
		{
			for (var index = 0; index < keys.Length; index++) {
				outObjects[index] = GetData(keys[index], data);
			}
		}

		private object GetData(string key, (string, object)[] data)
		{
			if (key[0] == Localizer.LocToken) {
				return m_localizer.Localize(key);
			}

			foreach (var (dataKey, dataValue) in data) {
				if (dataKey == key) {
					if (dataValue is string dataString) {
						if (dataString[0] == Localizer.LocToken) {
							return m_localizer.Localize(dataString);
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