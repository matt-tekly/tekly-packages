using Tekly.Common.Observables;

namespace Tekly.DataModels.Models
{
    public class ButtonModel : ObjectModel
    {
        public readonly BoolValueModel Interactable = new BoolValueModel(true);
        public ITriggerable<ButtonModel> Activated => m_activated;
        
        private readonly Triggerable<ButtonModel> m_activated = new Triggerable<ButtonModel>();

        public ButtonModel()
        {
            Add("interactable", Interactable);
        }

        public void Activate()
        {
            m_activated.Emit(this);
        }
    }
}