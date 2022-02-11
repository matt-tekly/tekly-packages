using System;

namespace Tekly.DataModels.Models
{
    public class ButtonModel : ObjectModel
    {
        public event Action<ButtonModel> Activation;

        public readonly BoolValueModel Interactable = new BoolValueModel(true);

        public ButtonModel()
        {
            Add("interactable", Interactable);
        }

        public void Activate()
        {
            Activation?.Invoke(this);
        }
    }
}