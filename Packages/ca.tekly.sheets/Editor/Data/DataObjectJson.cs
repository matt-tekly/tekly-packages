using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace Tekly.Sheets.Data
{
    public static class DataObjectJson
    {
        public static string ToJson(this DataObject dataObject)
        {
            StringBlock sb = new StringBlock();

            ToJson(dataObject, sb);

            return sb.ToString();
        }

        public static void ToJson(this DataObject dataObject, StringBlock sb)
        {
            if (dataObject.Type == DataObjectType.Array) {
                var keys = dataObject.Object.Keys.ToArray();
                Array.Sort(keys);

                sb.InBracket();

                var end = keys.Length;
                for (var i = 0; i < end; i++) {
                    var obj = dataObject.Object[keys[i]];
                    ToJson(obj, sb);
                    if (i < end - 1) {
                        sb.AppendLine(",");
                    }
                }

                sb.OutBracket();
            } else {
                var keys = dataObject.Object.Keys.ToArray();

                sb.InBrace();

                var end = keys.Length;
                for (var i = 0; i < end; i++) {
                    var obj = dataObject.Object[keys[i]];
                    sb.Append($"\"{keys[i]}\": ");
                    ToJson(obj, sb);
                    if (i < end - 1) {
                        sb.AppendLine(",");
                    }
                }

                sb.OutBrace();
            }
        }

        public static void ToJson(object obj, StringBlock sb)
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