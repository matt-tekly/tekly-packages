using System.Collections.Generic;
using Tekly.Sheets.Data;
using Tekly.Sheets.Processing;
using TeklySample.Game.Generators;
using TeklySample.Game.Items;
using TeklySample.Game.Worlds;
using UnityEditor;
using UnityEngine;

namespace TeklySample.Editor
{
	[CreateAssetMenu]
	public class IdleSheetProcessor : AssetSheetProcessor
	{
		[SerializeField] private Object m_directory;
		[SerializeField] private string m_assetIdKey = "Id";
		
		protected override void Process(Dictionary<string, DataObject> sheetMap)
		{
			var outDir = AssetDatabase.GetAssetPath(m_directory);
			var idKey = new PathKey(m_assetIdKey);
			
			ProcessSheet<GeneratorBalance>("Generators", sheetMap["Generators"], idKey, outDir);
			ProcessSheet<ItemBalance>("Items", sheetMap["Items"], idKey, outDir);
			ProcessSheet<WorldBalance>("Worlds", sheetMap["Worlds"], idKey, outDir);
		}
	}
}