namespace Tekly.Localizations
{
    public class LocalizationString
    {
        public readonly string Id;
        public readonly string Format;
        public readonly string[] Keys;

        public LocalizationString(string id, string format, string[] keys)
        {
            Id = id;
            Format = format;
            Keys = keys;
        }
    }
}