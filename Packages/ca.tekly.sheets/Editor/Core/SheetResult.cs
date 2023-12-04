using Tekly.Sheets.Dynamics;

namespace Tekly.Sheets.Core
{
	public enum SheetType
	{
		Objects,
		KeyValues,
	}
	
	public class SheetResult
	{
		public SheetType Type;
		public object Key;
		public Dynamic Dynamic;
		public string Name;
	}
}