using System.Collections.Generic;
using Tekly.Sheets.Core;
using Tekly.Sheets.Dynamics;
using Tekly.Sheets.Excel;
using Tekly.Sheets.Processing;
using Tekly.Webster;
using TeklySample.Game.Generators;
using TeklySample.Game.Items;
using TeklySample.Game.Worlds;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace TeklySample.Editor
{
	[CreateAssetMenu]
	public class IdleExcelProcessor : ExcelSheetProcessor
	{
		public override void Process(AssetImportContext ctx, Dictionary<string, SheetResult> sheets)
		{
			Frameline.BeginEvent("Start", "IdleExcelProcessor");
			
			ProcessSheet<ItemBalance>(ctx, sheets["Items"]);
			ProcessSheet<GeneratorBalance>(ctx, sheets["Generators"]);
			ProcessSheet<WorldBalance>(ctx, sheets["Worlds"]);
			
			Frameline.EndEvent("Start", "IdleExcelProcessor");
		}

		private void ProcessSheet<T>(AssetImportContext ctx, SheetResult sheetResult) where T: ScriptableObject
		{
			Frameline.BeginEvent($"Process: [{typeof(T).Name}]", "IdleExcelProcessor");
			
			if (sheetResult.Type != SheetType.Objects) {
				Debug.LogWarning($"Trying to Process Sheet [{sheetResult.Name}] as Objects but is [{sheetResult.Type}]");
				return;
			}
			
			foreach (var kvp in sheetResult.Dynamic) {
				var dyn = kvp.Value as Dynamic;
				
				var objectId = dyn[sheetResult.Key].ToString();

				var asset = CreateInstance<T>();
				asset.name = objectId;
				
				DynamicExt.PopulateObject(dyn, asset);
				ctx.AddObjectToAsset(objectId, asset);
			}
			
			Frameline.EndEvent($"Process: [{typeof(T).Name}]", "IdleExcelProcessor");
		}
	}
}