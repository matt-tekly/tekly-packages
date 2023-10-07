using System.Collections.Generic;
using Tekly.Sheets.Core;
using UnityEngine;

namespace Tekly.Sheets.Processing
{
	public abstract class GoogleSheetProcessor : ScriptableObject
	{
		public abstract void Process(GoogleSheetObject googleSheetObject, IList<Sheet> sheets);
	}
}