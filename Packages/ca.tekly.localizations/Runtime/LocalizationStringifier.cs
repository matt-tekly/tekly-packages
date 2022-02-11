using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tekly.Localizations
{
    public static class LocalizationStringifier
    {
        private static readonly Regex s_splitRegex = new("({{?.*?}}?)");
        
        public static void Stringify(string format, out string outFormat, out string[] outKeys)
        {
            var parts = s_splitRegex.Split(format)
                .Where(part => part.Length > 0)
                .Select(part => new LocPart(part))
                .ToArray();

            var keys = parts.Where(x => x.IsTemplate)
                .Select(x => x.TemplateKey)
                .Distinct()
                .ToArray();
            
            outFormat = string.Concat(parts.Select(x => Stringify(x, keys)));

            outKeys = keys.Length > 0 ? keys : null;
        }

        private static string Stringify(this LocPart locPart, string[] keys)
        {
            if (!locPart.IsTemplate) {
                return locPart.Raw;
            }

            var index = Array.IndexOf(keys, locPart.TemplateKey);

            if (locPart.TemplateFormat == null) {
                return $"{{{index}}}";
            }

            return $"{{{index}:{locPart.TemplateFormat}}}";
        }
        
        private class LocPart
        {
            public readonly bool IsTemplate;
        
            public readonly string Raw;
            public readonly string TemplateKey;
            public readonly string TemplateFormat;

            public LocPart(string part)
            {
                Raw = part;
                IsTemplate = part[0] == '{';

                if (IsTemplate) {
                    var templateContentsSplit = Raw.Substring(1, Raw.Length - 2).Split(':');
                    TemplateKey = templateContentsSplit[0];
                
                    if (templateContentsSplit.Length > 1) {
                        TemplateFormat = templateContentsSplit[1];
                    }
                }
            }
        }
    }
}