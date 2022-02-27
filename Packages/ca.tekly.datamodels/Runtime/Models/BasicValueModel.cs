// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Text;
using UnityEngine;

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

        protected override bool ValueEquals(string value)
        {
            return Value == value;
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
            return Value == 0 ? "0" : Value.ToString("##,#.##");
        }
        
        protected override bool ValueEquals(double value)
        {
            return Math.Abs(Value - value) < double.Epsilon;
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

        protected override bool ValueEquals(bool value)
        {
            return Value == value;
        }
    }
}