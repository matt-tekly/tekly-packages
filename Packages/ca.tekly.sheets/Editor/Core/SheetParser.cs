using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.Sheets.Data;
using UnityEngine;

namespace Tekly.Sheets.Core
{
    public static class SheetParser
    {
        public static DataObject ParseRows(IList<IList<object>> values, string sheetName)
        {
            try {
                if ((string)values[0][0] == "// Single Row") {
                    if ((string)values[0][1] == "#Key" && (string)values[0][2] == "Value#") {
                        return ParseKvp(values);
                    }

                    return ParseObjectSheet(values);
                }

                return ParseObjectSheet(values);
            } catch {
                Debug.LogError($"Failed to parse sheet: [{sheetName}]");
                throw;
            }
        }

        private static List<PropertyPath> ParseHeaderPaths(IList<object> headers)
        {
            var paths = new List<PropertyPath>();
            for (var index = 0; index < headers.Count; index++) {
                var header = headers[index] as string;
                if (!string.IsNullOrWhiteSpace(header) && !header.Contains("//")) {
                    paths.Add(new PropertyPath(header, index));
                }
            }

            return paths;
        }

        private static DataObject ParseObjectSheet(IList<IList<object>> sheet)
        {
            var paths = ParseHeaderPaths(sheet[0]);

            var objects = new List<DataObject>();
            var currentObject = new DataObject(DataObjectType.Object);

            var index = 1;

            for (var i = 1; i < sheet.Count; i++) {
                var row = sheet[i];

                if (row == null || row.Count == 0 || IsComment(row[0])) {
                    continue;
                }

                if (!IsBlank(row[0])) {
                    if (currentObject.Object.Count > 0) {
                        objects.Add(currentObject);
                    }

                    currentObject = new DataObject(DataObjectType.Object);
                    index = i;
                }

                ParseRow(paths, row, currentObject, i - index);
            }

            if (objects.Count == 0 || objects[objects.Count - 1] != currentObject && currentObject.Object.Count > 0) {
                objects.Add(currentObject);
            }

            var dataObject = new DataObject(DataObjectType.Array);
            for (var i = 0; i < objects.Count; i++) {
                dataObject.Set(i, objects[i]);
            }

            return dataObject;
        }

        private static void ParseRow(List<PropertyPath> paths, IList<object> row, DataObject obj, int index)
        {
            foreach (var path in paths) {
                var currPath = path.Key.Select(v => v.IsArray && !v.IsFixedIndex ? new PathKey(index) : v).ToArray();
                if (path.Index > row.Count - 1) {
                    continue;
                }

                var value = row[path.Index];
                if (!IsBlank(value)) {
                    obj.Set(currPath, GetValue(value));
                }
            }
        }

        private static DataObject ParseKvp(IList<IList<object>> sheet)
        {
            var obj = new DataObject(DataObjectType.Object);

            for (var i = 1; i < sheet.Count; i++) {
                var row = sheet[i];
                var value = row[2];

                obj.Set(row[1] as string, value);
            }

            return obj;
        }

        private static object GetValue(object val)
        {
            return val;
        }

        public static bool IsComment(object val)
        {
            if (val is string str) {
                return str.StartsWith("//") || str.StartsWith("__") || str.StartsWith("$");
            }

            return false;
        }

        private static bool IsBlank(object val)
        {
            if (val == null || val is DBNull) {
                return true;
            }

            if (val is bool || val is double) {
                return false;
            }

            if (val is string str) {
                return string.IsNullOrWhiteSpace(str);
            }

            return false;
        }
    }
}