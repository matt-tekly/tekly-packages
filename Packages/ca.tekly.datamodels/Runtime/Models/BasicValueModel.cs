// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Text;
using Tekly.Common.Observables;
using Tekly.Localizations;

namespace Tekly.DataModels.Models
{
    public abstract class BasicValueModel<T> : ValueModel<T> where T : IEquatable<T>, IComparable<T>
    {
        public BasicValueModel(T value) : base(value) { }
        
        public override int CompareTo(IValueModel valueModel)
        {
            if (valueModel is ValueModel<T> model) {
                return m_value.CompareTo(model.Value);
            }

            throw new Exception($"Trying to compare [{GetType().Name}] to [{valueModel?.GetType().Name}]");
        }
    }
    
    public class StringValueModel : BasicValueModel<string>
    {
        public bool NeedsLocalization {
            get => m_needsLocalization;
            set {
                if (m_needsLocalization == value) {
                    return;
                }
                
                m_needsLocalization = value;
                Emit(Value);
            }
        }

        public override bool IsTruthy => !string.IsNullOrEmpty(Value);
        
        private bool m_needsLocalization;
        
        public StringValueModel(string value, bool needsLocalization = false) : base(value)
        {
            m_needsLocalization = needsLocalization;
        }
        
        public override void ToJson(StringBuilder sb)
        {
            sb.Append($"\"{Value}\"");
        }

        public override void SetOverrideValue(string value)
        {
            OverrideValue = true;
            OverriddenValue = value;
        }

        protected override string OnToDisplayString()
        {
            return Value;
        }
        
        public override int CompareTo(IValueModel valueModel)
        {
            if (valueModel is StringValueModel model) {
                var myValue = m_needsLocalization ? Localizer.Instance.Localize(m_value) : m_value;
                var otherValue = model.NeedsLocalization ? Localizer.Instance.Localize(model.m_value) : model.m_value;
                
                return string.Compare(myValue, otherValue, StringComparison.CurrentCulture);
            }

            throw new Exception($"Trying to compare [{GetType().Name}] to [{valueModel?.GetType().Name}]");
        }
    }
    
    public class NumberValueModel : BasicValueModel<double>
    {
        public override bool IsTruthy => Value > 0;
        
        public NumberValueModel(double value) : base(value) { }
        
        public override void ToJson(StringBuilder sb)
        {
            sb.Append(Value.ToString());
        }

        protected override string OnToDisplayString()
        {
            if (Value == 0) {
                return "0";
            }

            if (Value < 1_000_000_000) {
                return Value.ToString("##,#.##");
            }

            return Value.ToString("e2");
        }
        
        public override void SetOverrideValue(string value)
        {
            OverrideValue = true;
            if (double.TryParse(value, out var newValue)) {
                OverriddenValue = newValue;
            }
        }
    }

    public class BoolValueModel : BasicValueModel<bool>
    {
        public override bool IsTruthy => Value;
        
        public BoolValueModel(bool value) : base(value) { }
        
        public override void ToJson(StringBuilder sb)
        {
            sb.Append(Value ? "true": "false");
        }

        protected override string OnToDisplayString()
        {
            return Value ? "true" : "false";
        }
        
        public override void SetOverrideValue(string value)
        {
            OverrideValue = true;
            if (bool.TryParse(value, out var newValue)) {
                OverriddenValue = newValue;
            }
        }
    }
}