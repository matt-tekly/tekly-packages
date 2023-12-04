using System.Linq;

namespace Tekly.Sheets.Core
{
    public class HeaderPath
    {
        public readonly PathEntry[] Path;
        public readonly int Column;
        public readonly bool IsInlinedArray;

        public HeaderPath(string header, int column)
        {
            IsInlinedArray = header.EndsWith("[]");
            if (IsInlinedArray) {
                header = header.Substring(0, header.Length - 2);	
            }
            
            Path = ParseHeader(header);
            Column = column;
        }

        public object[] ToDynamicPath(int index)
        {
            return Path.Select(v => v.IsArray && !v.IsFixedIndex ? index: v.Key).ToArray();
        }

        private static PathEntry[] ParseHeader(string header)
        {
            // ':' and '.' indicate an object property 
            header = header.Replace(':', '.');

            // '|' indicates an array
            // We replace '|' with '.#.' to make it easy to update the PathEntry with proper indices later
            header = header.Replace("|", ".#.").TrimEnd('.');
			
            // At this point we've ensured all separators are '.'
            return header.Split('.').Select(x => new PathEntry(x)).ToArray();
        }
    }
}