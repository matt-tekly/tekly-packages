using System;
using System.Collections.Generic;
using System.Linq;
using Tekly.Sheets.Dynamics;
using UnityEngine;

namespace Tekly.Sheets.Core
{
    public static class SheetParser
    {
        public static SheetResult ParseRows(IList<IList<object>> rows, string sheetName)
        {
            try {
                if ((string)rows[0][0] == "// Single Row") {
                    if ((string)rows[0][1] == "#Key" && (string)rows[0][2] == "Value#") {
                        return ParseKvp(rows, sheetName);
                    }

                    return ParseObjectSheet(rows, sheetName);
                }

                return ParseObjectSheet(rows, sheetName);
            } catch {
                Debug.LogError($"Failed to parse sheet: [{sheetName}]");
                throw;
            }
        }
        
        public static Dynamic ParseAsSingle(IList<IList<object>> sheet, string sheetName)
        {
            var paths = ParseHeaderPaths(sheet[0]);
            
            var result = new SheetResult {
                Type = SheetType.Objects,
                Key = paths[0].Path[0].Key,
                Name = sheetName
            };
            
            var current = new Dynamic(DynamicType.Object);

 
            for (var i = 1; i < sheet.Count; i++) {
                var row = sheet[i];

                if (row == null || row.Count == 0 || IsComment(row[0])) {
                    continue;
                }
                
                ParseRow(paths, row, current, i);
            }

            result.Dynamic = new Dynamic(DynamicType.Array);
            
            return current;
        }

        private static List<HeaderPath> ParseHeaderPaths(IList<object> headers)
        {
            var paths = new List<HeaderPath>();
            for (var index = 0; index < headers.Count; index++) {
                var header = headers[index] as string;
                if (!string.IsNullOrWhiteSpace(header) && !header.Contains("//")) {
                    paths.Add(new HeaderPath(header, index));
                }
            }

            return paths;
        }

        private static SheetResult ParseObjectSheet(IList<IList<object>> sheet, string sheetName)
        {
            var paths = ParseHeaderPaths(sheet[0]);
            
            var result = new SheetResult {
                Type = SheetType.Objects,
                Key = paths[0].Path[0].Key,
                Name = sheetName
            };

            var objects = new List<Dynamic>();
            var current = new Dynamic(DynamicType.Object);

            var index = 1;

            for (var i = 1; i < sheet.Count; i++) {
                var row = sheet[i];

                if (row == null || row.Count == 0 || IsComment(row[0])) {
                    continue;
                }

                if (!IsBlank(row[0])) {
                    if (current.Count > 0) {
                        objects.Add(current);
                    }

                    current = new Dynamic(DynamicType.Object);
                    index = i;
                }

                ParseRow(paths, row, current, i - index);
            }

            if (objects.Count == 0 || objects[objects.Count - 1] != current && current.Count > 0) {
                objects.Add(current);
            }

            result.Dynamic = new Dynamic(DynamicType.Array);
            
            for (var i = 0; i < objects.Count; i++) {
                result.Dynamic[i] = objects[i];
            }
            
            return result;
        }

        private static void ParseRow(List<HeaderPath> paths, IList<object> row, Dynamic dyn, int index)
        {
            foreach (var path in paths) {
                if (path.Column > row.Count - 1) {
                    continue;
                }

                var value = row[path.Column];
                
                if (IsBlank(value)) {
                    continue;
                }

                var currPath = path.ToDynamicPath(index);
                dyn.Set(currPath, path.IsInlinedArray ? ParseCsv(value) : GetValue(value));
            }
        }

        private static SheetResult ParseKvp(IList<IList<object>> sheet, string sheetName)
        {
            var result = new SheetResult {
                Type = SheetType.KeyValues,
                Name = sheetName
            };

            var dynamic = new Dynamic(DynamicType.Object);

            for (var i = 1; i < sheet.Count; i++) {
                var row = sheet[i];
                dynamic[row[1]] = row[2];
            }

            result.Dynamic = dynamic;

            return result;
        }

        private static object GetValue(object value)
        {
            return value;
        }

        public static bool IsComment(object val)
        {
            if (val is string str) {
                return str.StartsWith("//");
            }

            return false;
        }
        
        public static bool IsCommentName(object val)
        {
            if (val is string str) {
                return str.StartsWith("//") || str.StartsWith("__") || str.StartsWith("$");
            }

            return false;
        }

        private static Dynamic ParseCsv(object value)
        {
            var dynamic = new Dynamic(DynamicType.Array);

            if (value is string str) {
                var values = str.Split(',');
            
                if (double.TryParse(values[0], out var val)) {
                    var numbers = values.Select(double.Parse).ToArray();

                    for (var index = 0; index < numbers.Length; index++) {
                        dynamic[index] = numbers[index];
                    }
                } else {
                    for (var index = 0; index < values.Length; index++) {
                        dynamic[index] = values[index];
                    }    
                }
            } else {
                dynamic[0] = value;
            }
           
            return dynamic;
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