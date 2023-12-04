using System.Collections.Generic;
using Tekly.Sheets.Core;
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
		
		protected override void Process(Dictionary<string, SheetResult> sheetMap)
		{
			var outDir = AssetDatabase.GetAssetPath(m_directory);
			
			ProcessSheet<ItemBalance>(sheetMap["Items"], outDir);
			ProcessSheet<GeneratorBalance>(sheetMap["Generators"], outDir);
			ProcessSheet<WorldBalance>(sheetMap["Worlds"], outDir);
		}
	}
}