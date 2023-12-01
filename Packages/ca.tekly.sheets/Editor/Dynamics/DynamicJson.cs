using System.Globalization;
using Newtonsoft.Json;
using Tekly.Sheets.Data;

namespace Tekly.Sheets.Dynamics
{
	public static class DynamicJson
	{
		public static string ToJson(this Dynamic dyn)
        {
            var sb = new StringBlock();

            ToJson(dyn, sb);

            return sb.ToString();
        }

        public static void ToJson(this Dynamic dyn, StringBlock sb)
        {
            if (dyn.Type == DynamicType.Array) {
	            sb.InBracket();
	            var end = dyn.Count;
	            var index = 0;
	            
	            foreach (var kvp in dyn) {
		            ToJson(kvp.Value, sb);
		            if (index < end - 1) {
			            sb.AppendLine(",");
		            }

		            index++;
	            }
	            sb.OutBracket();
            } else {
	            
	            sb.InBracket();
	            var end = dyn.Count;
	            var index = 0;
	            
	            foreach (var kvp in dyn) {
		            sb.Append($"\"{kvp.Key}\": ");
		            ToJson(kvp.Value, sb);
		            if (index < end - 1) {
			            sb.AppendLine(",");
		            }

		            index++;
	            }
	            sb.OutBracket();
            }
        }

        public static void ToJson(this object obj, StringBlock sb)
        {
            if (obj is DataObject dataObject) {
                dataObject.ToJson(sb);
            } else if (obj is double doubleValue) {
                sb.Append(doubleValue.ToString(CultureInfo.InvariantCulture));
            } else if (obj is float floatValue) {
                sb.Append(floatValue.ToString(CultureInfo.InvariantCulture));
            } else if (obj is long longValue) {
                sb.Append(longValue.ToString());
            } else if (obj is int intValue) {
                sb.Append(intValue.ToString());
            } else if (obj is bool boolValue) {
                sb.Append(boolValue ? "true" : "false");
            } else if (obj is string str) {
                // This will properly escape the string for JSON
                sb.Append(JsonConvert.ToString(str.Trim()));
            } else {
                sb.Append($"\"{obj}\"");
            }
        }
	}
}