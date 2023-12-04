namespace Tekly.Sheets.Core
{
	public class PathEntry
	{
		public readonly object Key;
		public readonly bool IsFixedIndex;
		public readonly bool IsArray;

		public PathEntry(string key)
		{
			if (key == "#") {
				IsArray = true;
			} else if (int.TryParse(key, out var index)) {
				Key = index;
				IsArray = true;
				IsFixedIndex = true;
			} else {
				Key = key;
			}
		}
	}
}