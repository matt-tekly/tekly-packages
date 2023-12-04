using System.Collections.Generic;
using Tekly.Sheets.Core;
using Tekly.Sheets.Dynamics;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tekly.Sheets.Excel
{
	[CreateAssetMenu(menuName = "Tekly/Sheets/Json Processor Excel")]
	public class ExcelJsonProcessor : ExcelSheetProcessor
	{
		public override void Process(AssetImportContext ctx, Dictionary<string, SheetResult> sheets)
		{
			foreach (var (sheetName, sheetResult) in sheets) {
				if (sheetResult.Type != SheetType.Objects) {
					Debug.LogWarning($"Trying to Process Sheet [{sheetName}] as Objects but is [{sheetResult.Type}]");
					continue;
				}
				
				foreach (var kvp in sheetResult.Dynamic) {
					var dynamic = (Dynamic) kvp.Value;
					var id = dynamic[sheetResult.Key].ToString();

					var textAsset = new TextAsset(dynamic.ToJson());
					textAsset.name = id;
					ctx.AddObjectToAsset(id, textAsset);
				}
			}
		}
	}
}