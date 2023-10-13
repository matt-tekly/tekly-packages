using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using ExcelDataReader;
using Tekly.Sheets.Data;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tekly.Sheets.Excel
{
	[ScriptedImporter(9, "xlsx")]
	public class ExcelSheetImporter : ScriptedImporter
	{
		[SerializeField] private ExcelSheetProcessor m_processor;

		public override void OnImportAsset(AssetImportContext ctx)
		{
			if (m_processor == null) {
				return;
			}

			using var jsonStream = File.OpenRead(ctx.assetPath);
			using var reader = ExcelReaderFactory.CreateReader(jsonStream);

			var dataset = reader.AsDataSet();

			var sheets = new Dictionary<string, DataObject>();

			for (var i = 0; i < dataset.Tables.Count; ++i) {
				var table = dataset.Tables[i];
				sheets[table.TableName] = ParseTable(table);
			}
			
			m_processor.Process(ctx, sheets);
		}

		private static DataObject ParseTable(DataTable table)
		{
			var paths = ParseHeaderPaths(table.Rows[0]);

			var objects = new List<DataObject>();
			var currentObject = new DataObject(DataObjectType.Object);

			var index = 1;

			var rows = table.Rows;

			for (var i = 1; i < rows.Count; i++) {
				var row = rows[i].ItemArray;

				if (row.Length == 0 || IsComment(row[0])) {
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
				var currPath = path.Key.Select(v => v.IsNumber ? new PathKey(index) : v).ToArray();
				if (path.Index > row.Count - 1) {
					continue;
				}

				var value = row[path.Index];
				if (!IsBlank(value)) {
					obj.Set(currPath, value);
				}
			}
		}

		private static List<PropertyPath> ParseHeaderPaths(DataRow headers)
		{
			var paths = new List<PropertyPath>();
			for (var index = 0; index < headers.ItemArray.Length; index++) {
				var item = headers.ItemArray[index];
				var header = item as string;

				if (!string.IsNullOrWhiteSpace(header) && !header.Contains("//")) {
					paths.Add(new PropertyPath(header, index));
				}
			}

			return paths;
		}

		private static bool IsComment(object val)
		{
			if (val is string str) {
				return str.StartsWith("//");
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