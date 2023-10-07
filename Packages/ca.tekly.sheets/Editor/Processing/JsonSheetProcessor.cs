using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tekly.Sheets.Core;
using Tekly.Sheets.Data;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Sheets.Processing
{
	public enum JsonSheetExportFormat
	{
		OneFilePerSheet,
		OneFilePerObject
	}
	
	/// <summary>
	/// Converts each Sheet into a JSON file named after the Sheet's name.
	/// </summary>
	[CreateAssetMenu(menuName = "Tekly/Sheets/Json Processor")]
	public class JsonSheetProcessor : GoogleSheetProcessor
	{
		[SerializeField] private Object m_directory;
		[SerializeField] private JsonSheetExportFormat m_jsonFormat;
		
		[Tooltip("For One File Per Sheet this the name of the array that all the objects will be under")]
		[SerializeField] private string m_rootArrayKey = "Data";
		
		public override void Process(GoogleSheetObject googleSheetObject, IList<Sheet> sheets)
		{
			if (m_directory == null) {
				Debug.LogError("JsonSheetProcessor has null output directory");
				return;
			}

			switch (m_jsonFormat) {
				case JsonSheetExportFormat.OneFilePerSheet:
					ExportAsOneFilePerSheet(sheets);
					break;
				case JsonSheetExportFormat.OneFilePerObject:
					ExportAsOneFilePerObject(sheets);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
		}

		private void ExportAsOneFilePerSheet(IList<Sheet> sheets)
		{
			var outDir = AssetDatabase.GetAssetPath(m_directory);
			
			foreach (var s in sheets) {
				var dataObject = SheetParser.ParseSheet(s);
				var container = new DataObject(DataObjectType.Object);
				container.Set(m_rootArrayKey, dataObject);

				var json = container.ToJson();

				var fileName = $"{s.Name}.json";
				File.WriteAllText(Path.Combine(outDir, fileName), json);
			}
		}
		
		private void ExportAsOneFilePerObject(IList<Sheet> sheets)
		{
			var outDir = AssetDatabase.GetAssetPath(m_directory);
			
			foreach (var sheet in sheets) {
				var sheetDir = Path.Combine(outDir, sheet.Name);
				Directory.CreateDirectory(sheetDir);
				
				var dataObject = SheetParser.ParseSheet(sheet);

				foreach (var (key, value) in dataObject.Object) {
					var objectValue = (DataObject) value;
					var id = objectValue.Object.Values.First().ToString();

					var json = objectValue.ToJson();
					var fileName = $"{id}.json";
					File.WriteAllText(Path.Combine(sheetDir, fileName), json);
				}
			}
		}
	}
}