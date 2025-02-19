using System;
using System.Collections.Generic;
using System.IO;
using Tekly.Sheets.Core;
using Tekly.Sheets.Dynamics;
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
				var sheetResult = SheetParser.ParseRows(s.Values, s.Name);
				var container = new Dynamic(DynamicType.Object);
				container[m_rootArrayKey] = sheetResult.Dynamic;

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
				
				var result = SheetParser.ParseRows(sheet.Values, sheet.Name);

				if (result.Type != SheetType.Objects) {
					Debug.LogWarning($"Trying to Process Sheet [{sheet.Name}] as Objects but is [{result.Type}]");
					continue;
				}
				
				foreach (var kvp in result.Dynamic) {
					var objectValue = (Dynamic) kvp.Value;
					var id = objectValue[result.Key];

					var json = objectValue.ToJson();
					var fileName = $"{id}.json";
					File.WriteAllText(Path.Combine(sheetDir, fileName), json);
				}
			}
		}
	}
}