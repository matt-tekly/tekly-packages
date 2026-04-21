using Tekly.DataModels.Models;

namespace TeklySample.Samples.Leaf
{
    public class GameSettingsModel : ObjectModel
    {
        public GameSettingsModel()
        {
            
        }
    }

    public class SettingsOptionModel : ObjectModel
    {
        
    }

    public class ToggleSettingModel : ObjectModel
    {
        public ToggleSettingModel(bool defaultValue, string name, string description)
        {
            Add("value", defaultValue);
            Add("name", name);
            Add("description", description);
        }
    }

    public class RadioGroupModel : ObjectModel
    {
        
    }

    public class RadioOptionModel : ObjectModel
    {
        public RadioOptionModel()
        {
            
        }
    }
}
