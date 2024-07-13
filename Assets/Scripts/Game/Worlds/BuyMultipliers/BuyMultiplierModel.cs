using System;
using Tekly.DataModels.Models;

namespace TeklySample.Game.Worlds.BuyMultipliers
{
    public class BuyMultiplierModel : ObjectModel, ITickable
    {
        private readonly BuyMultiplier m_buyMultiplier;
        private readonly ButtonModel m_button;
        private readonly StringValueModel m_shortText;

        private BuyMultiplierMode m_multiplierMode;
        private readonly IDisposable m_buttonDisposable;
        
        public BuyMultiplierModel(BuyMultiplier buyMultiplier)
        {
            m_buyMultiplier = buyMultiplier;
            m_button = AddButton("button");
            m_shortText = Add("shorttext", GetModeText(m_buyMultiplier.Mode));
            
            m_buttonDisposable = m_button.Activated.Subscribe(OnButtonActivation);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            m_buttonDisposable.Dispose();
        }

        private void OnButtonActivation(ButtonModel buttonModel)
        {
            m_buyMultiplier.Toggle();
        }

        protected override void OnTick()
        {
            if (m_multiplierMode != m_buyMultiplier.Mode) {
                m_shortText.Value = GetModeText(m_buyMultiplier.Mode);
                m_multiplierMode = m_buyMultiplier.Mode;
            }
        }

        private string GetModeText(BuyMultiplierMode mode)
        {
            switch (mode) {
                case BuyMultiplierMode.One:
                    return "x1";
                case BuyMultiplierMode.Percent10:
                    return "10%";
                case BuyMultiplierMode.Percent50:
                    return "50%";
                case BuyMultiplierMode.Percent100:
                    return "100%";
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }
        }
    }
}