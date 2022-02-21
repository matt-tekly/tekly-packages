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
    }
}