using Tekly.DataModels.Models;
using Tekly.Localizations;
using TeklySample.Game.Items;

namespace TeklySample.Game.Generators
{
    public class ItemCountModel : ObjectModel
    {
        public double Count
        {
            set => m_count.AsDouble = value;
            get => m_count.AsDouble;
        }
        
        private readonly SpriteValueModel m_iconModel = new SpriteValueModel();
        private readonly NumberValueModel m_count = new NumberValueModel(0);

        public ItemCountModel(ItemBalance itemBalance)
        {
            Add("icon", m_iconModel);
            Add("count", m_count);
        }
    }
    
    public class GeneratorModel : ObjectModel
    {
        private readonly Generator m_generator;
        private readonly GeneratorManager m_generatorManager;
        
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
        
        public GeneratorModel(Generator generator, GeneratorManager generatorManager)
        {
            m_generator = generator;
            m_generatorManager = generatorManager;

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

            m_name.AsString = Localizer.Instance.Localize(generator.Balance.Item.NameId);
            m_itemId.AsString = m_generator.InventoryItem.ItemId;
            m_iconModel.Value = m_generator.Balance.Item.Icon;
            
            m_runButton.Activation += RunButtonOnActivation;
            m_buyButton.Activation += BuyButtonOnActivation;
        }

        protected override void OnDispose()
        {
            m_runButton.Activation -= RunButtonOnActivation;
            m_buyButton.Activation -= BuyButtonOnActivation;
        }
        
        protected override void OnTick()
        {
            m_progress.AsDouble = m_generator.RatioComplete;
            m_runButton.Interactable.AsBool = m_generator.Count > 0 && m_generator.State == GeneratorState.Idle;
            m_timeRemaining.AsDouble = m_generator.TimeRemaining;
            m_count.AsDouble = m_generator.Count;
            m_generation.Count = m_generator.GenerationCount;

            var affordable = m_generatorManager.CalculateAffordableCount(m_generator);

            if (affordable <= 0) {
                m_affordableCount.AsDouble = 1;
                m_buyButton.Interactable.AsBool = false;
            } else {
                m_affordableCount.AsDouble = affordable;
                m_buyButton.Interactable.AsBool = true;
            }
            
        }
        
        private void RunButtonOnActivation(ButtonModel _)
        {
            m_generator.Run();
        }
        private void BuyButtonOnActivation(ButtonModel _)
        {
            m_generatorManager.Buy(m_generator, 1);
        }
    }
}
