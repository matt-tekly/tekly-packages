namespace Tekly.Localizations
{
    public interface ILocalizer
    {
        void Clear();
        void AddData(LocalizationData localizationData);
        string Localize(string id);
        string Localize(string id, (string, object)[] data);
    }
}