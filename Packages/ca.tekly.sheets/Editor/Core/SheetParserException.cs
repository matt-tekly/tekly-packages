using System;

namespace Tekly.Sheets.Core
{
	public class SheetParserException : Exception
	{
		public SheetParserException(string sheetName, Exception exception) : base($"Failed to parse sheet: [{sheetName}]", exception)
		{
            
		}
	}
}