using System.Collections.Generic;
using System.Data;
using System.IO;
using ExcelDataReader;
using Tekly.Sheets.Core;
using Tekly.Sheets.Data;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tekly.Sheets.Excel
{
	[ScriptedImporter(10, "xlsx")]
	public class ExcelSheetImporter : ScriptedImporter
	{
		[SerializeField] private ExcelSheetProcessor m_processor;

		public override void OnImportAsset(AssetImportContext ctx)
		{
			if (m_processor == null) {
				return;
			}

			using var fileStream = new FileStream(ctx.assetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var reader = ExcelReaderFactory.CreateReader(fileStream);

			var dataSet = reader.AsDataSet();
			var sheets = ConvertToDataObjects(dataSet);
			
			m_processor.Process(ctx, sheets);
		}

		private Dictionary<string, DataObject> ConvertToDataObjects(DataSet dataSet)
		{
			var sheets = new Dictionary<string, DataObject>();

			for (var i = 0; i < dataSet.Tables.Count; ++i) {
				var table = dataSet.Tables[i];
				
				if (table.TableName.StartsWith("//") || table.TableName.StartsWith("__")) {
					continue;
				}
				
				var rows = ConvertToObjectRows(table);
				sheets[table.TableName] = SheetParser.ParseRows(rows, table.TableName);
			}

			return sheets;
		}

		private IList<IList<object>> ConvertToObjectRows(DataTable table)
		{
			var rows = new List<IList<object>>(table.Rows.Count);
			
			foreach (DataRow tableRow in table.Rows) {
				rows.Add(tableRow.ItemArray);
			}

			return rows;
		}
	}
}