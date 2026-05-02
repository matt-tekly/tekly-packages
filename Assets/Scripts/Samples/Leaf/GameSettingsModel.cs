using Tekly.Common.Maths;
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
            
            var startingVolume = Thunk.GetVolume(mixer, volumeSetting.Parameter);
            var value = AddRange("value", MathUtils.Lerp(0, 100, startingVolume), 0, 100);
            
            value.Subscribe(_ => {
                Thunk.SetVolume(mixer, volumeSetting.Parameter, value.CurrentRatio);
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
