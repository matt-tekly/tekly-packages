using System;
using System.Text;
using Tekly.Common.Observables;
using Tekly.Logging;

namespace Tekly.DataModels.Models
{
    public interface IOverridableValue : IValueModel
    {
        /// <summary>
        /// Whether Value is the overridden value. Turning it off goes back to the real value and notifies subscribers.
        /// </summary>
        bool OverrideValue { get; set; }    
        
        /// <summary>
        /// Overrides the value with one parsed from text, e.g. typed into a debug tool. Text that can't be parsed
        /// leaves the value as it was.
        /// </summary>
        void SetOverrideValue(string value);
    }
    
    public abstract class ValueModel<T> : ObservableValue<T>, IOverridableValue, IComparable<ValueModel<T>>
    {
        private bool m_isDisposed;
        private bool m_overrideValue;
        private T m_overriddenValue;

        protected T OverriddenValue {
            get => m_overriddenValue;
            set {
                m_overriddenValue = value;
                Emit(m_overriddenValue);
            }
        }
        
        private string m_displayString;
        
        public override T Value
        {
            get => OverrideValue ? OverriddenValue : m_value;
            set {
                if (s_defaultEqualityComparer.Equals(m_value, value)) {
                    return;
                }

                m_value = value;
                Emit(Value);
            }
        }
        public abstract bool IsTruthy { get; }
        
        public bool OverrideValue {
            get => m_overrideValue;
            set {
                if (m_overrideValue == value) {
                    return;
                }

                m_overrideValue = value;
                Emit(Value);
            }
        }

        protected ValueModel(T value) : this()
        {
            m_value = value;
        }

        protected ValueModel()
        {
            ModelManager.Instance.AddModel(this);
        }

        public void Dispose()
        {
            if (m_isDisposed) {
                TkLogger.Get<ModelBase>().Error("ModelBase being disposed multiple times");
                return;
            }

            m_isDisposed = true;
            ModelManager.Instance.RemoveModel(this);
            OnDispose();
        }

        public void Tick()
        {
            OnTick();
        }
        
        protected virtual void OnTick() { }

        protected virtual void OnDispose() { }

        public virtual void ToJson(StringBuilder sb)
        {
            sb.Append("[UNIMPLEMENTED]");
        }
        
        public abstract void SetOverrideValue(string value);

        /// <summary>
        /// Overrides the value with value until OverrideValue is turned off, notifying subscribers once.
        /// </summary>
        protected void SetOverride(T value)
        {
            m_overriddenValue = value;
            m_overrideValue = true;
            Emit(value);
        }

        public string ToDisplayString()
        {
            if (m_displayString != null) {
                return m_displayString;
            }

            m_displayString = OnToDisplayString();
            return m_displayString;
        }

        protected virtual string OnToDisplayString()
        {
            return "[UNIMPLEMENTED]"; 
        }

        protected override void Emit(T value)
        {
            base.Emit(value);
            m_displayString = null;
        }

        public int Compare(IValueModel valueModel)
        {
            throw new NotImplementedException();
        }

        public abstract int CompareTo(IValueModel valueModel);
        
        
        public int CompareTo(ValueModel<T> other)
        {
            if (ReferenceEquals(this, other)) {
                return 0;
            }

            if (ReferenceEquals(null, other)) {
                return 1;
            }

            return m_isDisposed.CompareTo(other.m_isDisposed);
        }
    }
}