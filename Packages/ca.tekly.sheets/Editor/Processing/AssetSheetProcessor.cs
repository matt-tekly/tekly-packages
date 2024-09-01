using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tekly.EditorUtils.Assets;
using Tekly.Sheets.Core;
using Tekly.Sheets.Dynamics;
using UnityEditor;
using UnityEngine;

namespace Tekly.Sheets.Processing
{
	/// <summary>
	/// Subclass this to create ScriptableObjects from your sheets. Look at IdleSheetProcessor in the Tekly packages
	/// project for an example.
	/// </summary>
	public abstract class AssetSheetProcessor : GoogleSheetProcessor
	{
		protected delegate void ProcessAssetDelegate<in T>(T asset, Dynamic dynamic);
		
		public override void Process(GoogleSheetObject googleSheetObject, IList<Sheet> sheets)
		{
			var sheetMap = sheets.ToDictionary(s => s.Name, s => SheetParser.ParseRows(s.Values, s.Name));
			Process(sheetMap);
		}

		protected abstract void Process(Dictionary<string, SheetResult> sheetMap);
		
		protected void ProcessSheet<T>(SheetResult sheetResult, string outputDir, ProcessAssetDelegate<T> processor = null)
			where T : ScriptableObject
		{
			if (sheetResult.Type != SheetType.Objects) {
				Debug.LogWarning($"Trying to Process Sheet [{sheetResult.Name}] as Objects but is [{sheetResult.Type}]");
				return;
			}
			
			var assetDir = Path.Combine(outputDir, sheetResult.Name);
			Directory.CreateDirectory(assetDir);
			
			foreach (var kvp in sheetResult.Dynamic) {
				var dyn = kvp.Value as Dynamic;

				var objectId = dyn[sheetResult.Key].ToString();
				var objectPath = Path.Combine(assetDir, objectId + ".asset");

				var asset = AssetDatabaseExt.LoadOrCreate<T>(objectPath);
				DynamicExt.PopulateObject(dyn, asset);
				processor?.Invoke(asset, dyn);
				
				EditorUtility.SetDirty(asset);
			}
			
			AssetDatabase.SaveAssets();
		}
	}
}