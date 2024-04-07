namespace Tekly.Localizations
{
    public interface ILocalizer
    {
        bool IsLoading { get; }
        string LanguageLabel { get; set; }
        
        void Clear();
        
        string Localize(string id);
        string Localize(string id, (string, object)[] data);
        
        void LoadBank(string key);
        void UnloadBank(string key);
    }
}