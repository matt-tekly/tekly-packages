using Tekly.Common.Utils;
using Tekly.DataModels.Models;
using Tekly.Thunk.Core;
using UnityEngine.Audio;

namespace TeklySample.Samples.Leaf
{
    public class GameSettingsModel : ObjectModel
    {
        public GameSettingsModel(AudioMixer mixer, GameSettingsVolume[] volumeSettings)
        {
            var audio = AddObject("audio");
            foreach (var gameSettingsVolume in volumeSettings) {
                audio.Add(gameSettingsVolume.Name, new VolumeOptionModel(mixer, gameSettingsVolume));
            }
        }
    }

    public class VolumeOptionModel : DisposableObjectModel
    {
        public VolumeOptionModel(AudioMixer mixer, GameSettingsVolume volumeSetting)
        {
            Add("name", volumeSetting.Name);
            var value = Add("value", Thunk.GetVolume(mixer, volumeSetting.Parameter));
            value.Subscribe(volume => {
                Thunk.SetVolume(mixer, volumeSetting.Parameter, volume);
            }).AddTo(m_disposables);
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
