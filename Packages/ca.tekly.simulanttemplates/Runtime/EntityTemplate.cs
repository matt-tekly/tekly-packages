using Tekly.Balance;
using Tekly.Simulant.Core;
using Tekly.SuperSerial.Serialization;
using Tekly.SuperSerial.Streams;

namespace Tekly.Simulant.Templates
{
    public struct TemplateInstance : ISuperSerialize
    {
        public string Template;
		
        public void Write(TokenOutputStream output, SuperSerializer superSerializer)
        {
            output.Write(Template);
        }

        public void Read(TokenInputStream input, SuperSerializer superSerializer)
        {
            Template = input.ReadString();
        }
    }
	
    public abstract class EntityTemplate : BalanceObject
    {
        /// <summary>
        /// Constructs an instance of this template in the given world. It adds a TemplateInstance to the Entity
        /// which will be serialized/deserialized. This will allow the Template to Populate the entity again on
        /// deserialization.
        /// </summary>
        public int Construct(World world)
        {
            var entity = world.Create();
            
            OnConstruct(world, entity);
            
            world.Add(entity, new TemplateInstance {
                Template = name
            });
			
            return entity;
        }

        /// <summary>
        /// Called when the EntityTemplate is first constructed
        /// </summary>
        protected virtual void OnConstruct(World world, int entity)
        {
            
        }

        /// <summary>
        /// Called when the TemplateSystem finds an Entity with an EntityTemplate.
        /// </summary>
        public void Populate(World world, int entity)
        {
            OnPopulate(world, entity);
        }

        /// <summary>
        /// Called when the TemplateSystem finds an Entity with an EntityTemplate.
        /// Implement this to add other components to the Entity.
        /// </summary>
        protected abstract void OnPopulate(World world, int entity);
    }
}
