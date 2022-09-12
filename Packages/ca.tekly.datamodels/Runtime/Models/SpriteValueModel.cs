using System;
using System.Text;
using UnityEngine;

namespace Tekly.DataModels.Models
{
    public class SpriteValueModel : ValueModel<Sprite>
    {
        public override void ToJson(StringBuilder sb)
        {
            sb.Append(Value != null ? Value.name : "[null]");
        }

        public override string ToDisplayString()
        {
            return Value != null ? Value.name : "[null]";
        }

        public override int CompareTo(IValueModel valueModel)
        {
            throw new Exception($"Trying to compare [{GetType().Name}] to [{valueModel.GetType().Name}]");
        }
    }
}