// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Text;

namespace Tekly.DataModels.Models
{
    public abstract class BasicValueModel<T> : ValueModel<T> where T : IEquatable<T>
    {
        public BasicValueModel(T value) : base(value) { }
    }
    
    public class StringValueModel : BasicValueModel<string>
    {
        public StringValueModel(string value) : base(value) { }
        
        public override void ToJson(StringBuilder sb)
        {
            sb.Append($"\"{Value}\"");
        }

        public override string ToDisplayString()
        {
            return Value;
        }
    }
    
    public class NumberValueModel : BasicValueModel<double>
    {
        public NumberValueModel(double value) : base(value) { }
        
        public override void ToJson(StringBuilder sb)
        {
            sb.Append(Value.ToString());
        }

        public override string ToDisplayString()
        {
            if (Value == 0) {
                return "0";
            }

            if (Value < 1_000_000_000) {
                return Value.ToString("##,#.##");
            }

            return Value.ToString("e2");
        }
    }

    public class BoolValueModel : BasicValueModel<bool>
    {
        public BoolValueModel(bool value) : base(value) { }
        
        public override void ToJson(StringBuilder sb)
        {
            sb.Append(Value ? "true": "false");
        }

        public override string ToDisplayString()
        {
            return Value ? "true" : "false";
        }
    }
}