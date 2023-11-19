using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tekly.Common.Utils;
using Tekly.Sheets.Core;
using Tekly.Sheets.Data;
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
		protected delegate void ProcessAssetDelegate<in T>(T asset, DataObject dataObject);
		
		public override void Process(GoogleSheetObject googleSheetObject, IList<Sheet> sheets)
		{
			var sheetMap = sheets.ToDictionary(x => x.Name, x => SheetParser.ParseRows(x.Values, x.Name));
			Process(sheetMap);
		}

		protected abstract void Process(Dictionary<string, DataObject> sheetMap);
		
		protected void ProcessSheet<T>(string sheetName, DataObject sheetData, PathKey idKey, string outputDir, ProcessAssetDelegate<T> processor = null)
			where T : ScriptableObject
		{
			var assetDir = Path.Combine(outputDir, sheetName);
			Directory.CreateDirectory(assetDir);
			
			foreach (var data in sheetData.Object.Values.Cast<DataObject>()) {
				var objectId = data.Object[idKey].ToString();
				var objectPath = Path.Combine(assetDir, objectId + ".asset");

				var asset = AssetDatabaseExt.LoadOrCreate<T>(objectPath);
				JsonExt.PopulateObject(data.ToJson(), asset);
				processor?.Invoke(asset, data);
				
				EditorUtility.SetDirty(asset);
			}
			
			AssetDatabase.SaveAssets();
		}
	}
}