using Tekly.Common.Utils;
using Tekly.DataModels.Models;
using Tekly.Localizations;
using TeklySample.Game.Items;
using TeklySample.Game.Worlds.BuyMultipliers;

namespace TeklySample.Game.Generators
{
    public class ItemCountModel : ObjectModel
    {
        public double Count
        {
            set => m_count.Value = value;
            get => m_count.Value;
        }
        
        private readonly SpriteValueModel m_iconModel = new SpriteValueModel();
        private readonly NumberValueModel m_count = new NumberValueModel(0);

        public ItemCountModel(ItemBalance itemBalance)
        {
            Add("icon", m_iconModel);
            Add("count", m_count);
        }
    }
    
    public class GeneratorModel : ObjectModel, ITickable
    {
        private readonly Generator m_generator;
        private readonly GeneratorManager m_generatorManager;
        private readonly BuyMultiplier m_buyMultiplier;

        private readonly StringValueModel m_name = new StringValueModel("");
        private readonly NumberValueModel m_progress = new NumberValueModel(0);
        private readonly NumberValueModel m_timeRemaining = new NumberValueModel(0);
        private readonly NumberValueModel m_count = new NumberValueModel(0);
        private readonly ButtonModel m_runButton = new ButtonModel();
        private readonly SpriteValueModel m_iconModel = new SpriteValueModel();
        
        private readonly NumberValueModel m_affordableCount = new NumberValueModel(0);
        private readonly ButtonModel m_buyButton = new ButtonModel();
        
        private readonly StringValueModel m_itemId = new StringValueModel("");
        private readonly ItemCountModel m_generation;

        private readonly Disposables m_disposables = new Disposables();
        
        public GeneratorModel(Generator generator, GeneratorManager generatorManager, BuyMultiplier buyMultiplier)
        {
            m_generator = generator;
            m_generatorManager = generatorManager;
            m_buyMultiplier = buyMultiplier;

            m_generation = new ItemCountModel(generator.GenerationItem);

            Add("name", m_name);
            Add("progress", m_progress);
            Add("count", m_count);
            Add("run", m_runButton);
            Add("buy", m_buyButton);
            Add("timeremaining", m_timeRemaining);
            Add("itemid", m_itemId);
            Add("icon", m_iconModel);
            Add("affordable", m_affordableCount);
            Add("generation", m_generation);

            m_name.Value = Localizer.Instance.Localize(generator.Balance.Item.NameId);
            m_itemId.Value = m_generator.InventoryItem.ItemId;
            m_iconModel.Value = m_generator.Balance.Item.Icon;

            m_runButton.Activated.Subscribe(RunButtonOnActivation).AddTo(m_disposables);
            m_buyButton.Activated.Subscribe(BuyButtonOnActivation).AddTo(m_disposables);
        }

        protected override void OnDispose()
        {
            m_disposables.Dispose();
        }
        
        protected override void OnTick()
        {
            m_progress.Value = m_generator.RatioComplete;
            m_runButton.Interactable.Value = m_generator.Count > 0 && m_generator.State == GeneratorState.Idle;
            m_timeRemaining.Value = m_generator.TimeRemaining;
            m_count.Value = m_generator.Count;
            m_generation.Count = m_generator.GenerationCount;

            var affordable = m_buyMultiplier.CalculateAffordableCount(m_generator);

            if (affordable <= 0) {
                m_affordableCount.Value = 1;
                m_buyButton.Interactable.Value = false;
            } else {
                m_affordableCount.Value = affordable;
                m_buyButton.Interactable.Value = true;
            }
        }
        
        private void RunButtonOnActivation(ButtonModel _)
        {
            m_generator.Run();
        }
        
        private void BuyButtonOnActivation(ButtonModel _)
        {
            m_generatorManager.Buy(m_generator, m_affordableCount.Value);
        }
    }
}
