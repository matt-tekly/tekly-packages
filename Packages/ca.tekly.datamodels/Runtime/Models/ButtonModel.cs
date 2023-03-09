using Tekly.Common.Observables;

namespace Tekly.DataModels.Models
{
    public class ButtonModel : ObjectModel
    {
        public bool IsInteractable {
            get => m_interactable.Value;
            set => m_interactable.Value = value;
        }
        
        public ITriggerable<ButtonModel> Activated => m_activated;
        public ITriggerable<bool> Interactable => m_interactable;
        
        private readonly Triggerable<ButtonModel> m_activated = new Triggerable<ButtonModel>();
        private readonly BoolValueModel m_interactable = new BoolValueModel(true);

        public ButtonModel()
        {
            Add("interactable", m_interactable);
        }

        public void Activate()
        {
            m_activated.Emit(this);
        }
    }
}