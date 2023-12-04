using System.Collections.Generic;
using Tekly.Sheets.Core;
using Tekly.Sheets.Dynamics;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Tekly.Sheets.Excel
{
	public abstract class ExcelSheetProcessor : ScriptableObject
	{
		public abstract void Process(AssetImportContext ctx, Dictionary<string, SheetResult> sheets);
	}
}