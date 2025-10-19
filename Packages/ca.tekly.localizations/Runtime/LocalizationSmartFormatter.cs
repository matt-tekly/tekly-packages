#if TEKLY_SMARTFORMAT
using System.Linq;
using SmartFormat;
using SmartFormat.Core.Extensions;
using SmartFormat.Core.Settings;
using SmartFormat.Extensions;
using Tekly.SmartFormat;

namespace Tekly.Localizations
{
	public class LocalizationSource : Source
	{
		private readonly Localizer m_localizer;

		public LocalizationSource(Localizer localizer)
		{
			m_localizer = localizer;
		}

		public override bool TryEvaluateSelector(ISelectorInfo selectorInfo)
		{
			if (TrySetResultForNullableOperator(selectorInfo)) {
				return true;
			}

			if (selectorInfo.SelectorText[0] == Localizer.LocToken) {
				selectorInfo.Result = m_localizer.Localize(selectorInfo.SelectorText);
				return true;
			}

			return false;
		}
	}

	public class LocalizationSmartFormatter : ILocalizationFormatter
	{
		private readonly SmartFormatter m_formatter;

		public LocalizationSmartFormatter(Localizer localizer)
		{
			var settings = new SmartSettings();
			settings.Parser.AddCustomSelectorChars(new[] { '$' });
			settings.StringFormatCompatibility = false;
			settings.Parser.ErrorAction = ParseErrorAction.ThrowError;
			settings.Formatter.ErrorAction = FormatErrorAction.ThrowError;
			
			m_formatter = Smart.CreateDefaultSmartFormat(settings);
			
			m_formatter.InsertExtension(0, new LocalizationSource(localizer));
			m_formatter.InsertExtension(0, new IndefiniteArticleFormatter());
			m_formatter.AddExtensions(new TuplesSource());
		}

		public string Localize(LocalizationString locString, (string, object)[] data)
		{
			//var dictionary = data.ToDictionary(x => x.Item1, x => x.Item2);
			return m_formatter.Format(locString.Format, data);
		}
	}
}
#endif