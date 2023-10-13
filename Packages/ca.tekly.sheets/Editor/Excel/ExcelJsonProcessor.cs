using System.Collections.Generic;
using System.Linq;
using Tekly.Sheets.Data;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tekly.Sheets.Excel
{
	[CreateAssetMenu(menuName = "Tekly/Sheets/Json Processor Excel")]
	public class ExcelJsonProcessor : ExcelSheetProcessor
	{
		public override void Process(AssetImportContext ctx, Dictionary<string, DataObject> sheets)
		{
			foreach (var (sheetName, data) in sheets) {
				
				foreach (var value in data.Object.Values) {
					var objectValue = (DataObject) value;
					var id = objectValue.Object.Values.First().ToString();

					var textAsset = new TextAsset(objectValue.ToJson());
					textAsset.name = id;
					ctx.AddObjectToAsset(id, textAsset);
				}
			}
		}
	}
}