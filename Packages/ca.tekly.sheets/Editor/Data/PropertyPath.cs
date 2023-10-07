namespace Tekly.Sheets.Data
{
    public class PropertyPath
    {
        public readonly PathKey[] Key;
        public readonly int Index;

        public PropertyPath(string header, int index)
        {
            Key = PathKey.ParseHeader(header);
            Index = index;
        }
    }
}