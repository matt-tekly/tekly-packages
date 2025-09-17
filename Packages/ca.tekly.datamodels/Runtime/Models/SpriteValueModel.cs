using System;
using System.Text;
using UnityEngine;

namespace Tekly.DataModels.Models
{
    public class SpriteValueModel : ValueModel<Sprite>
    {
        public override bool IsTruthy => Value != null;
        public SpriteValueModel(Sprite sprite) : base(sprite) { }
        public SpriteValueModel() { }
            
        public override void ToJson(StringBuilder sb)
        {
            sb.Append(Value != null ? Value.name : "[null]");
        }

        protected override string OnToDisplayString()
        {
            return Value != null ? Value.name : "[null]";
        }

        public override int CompareTo(IValueModel valueModel)
        {
            throw new Exception($"Trying to compare [{GetType().Name}] to [{valueModel.GetType().Name}]");
        }
        
        public override void SetOverrideValue(string value)
        {
            Debug.LogWarning("Can't override SpriteValueModel");
        }
    }
}