// ============================================================================
// Copyright 2021 Matt King
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using Tekly.Common.Maths;
using Tekly.Common.Observables;

namespace Tekly.DataModels.Models
{
    public enum ValueType
    {
        Bool,
        Double,
        String
    }
    
    public class StringValueModel : BasicValueModel
    {
        public StringValueModel(string value) : base(value) { }
    }
    
    public class NumberValueModel : BasicValueModel
    {
        public NumberValueModel(double value) : base(value) { }
    }

    public class BoolValueModel : BasicValueModel
    {
        public BoolValueModel(bool value) : base(value) { }
    }
    
    public abstract class BasicValueModel : ModelBase, IValueModel
    {
        public readonly ValueType Type;
        
        public bool AsBool
        {
            get => m_bool;
            set {
                if (m_bool == value) {
                    return;
                }

                m_bool = value;

                NotifyChanged();
            }
        }
        
        public double AsDouble
        {
            get => m_double;
            set {
                if (MathUtils.IsApproximately(m_double, value)) {
                    return;
                }

                m_double = value;

                NotifyChanged();
            }
        }
        
        public string AsString
        {
            get => m_string;
            set {
                if (m_string == value) {
                    return;
                }

                m_string = value;

                NotifyChanged();
            }
        }

        public object AsObject
        {
            get {
                switch (Type) {
                    case ValueType.Bool:
                        return m_bool;
                    case ValueType.Double:
                        return m_double;
                    case ValueType.String:
                        return m_string;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        
        private string m_string;
        private double m_double;
        private bool m_bool;

        private List<IValueObserver<BasicValueModel>> m_observers;
        
        protected BasicValueModel(bool value)
        {
            m_bool = value;
            Type = ValueType.Bool;
        }
        
        protected BasicValueModel(double value)
        {
            m_double = value;
            Type = ValueType.Double;
        }
        
        protected BasicValueModel(string value)
        {
            m_string = value;
            Type = ValueType.String;
        }
        
        public IDisposable Subscribe(IValueObserver<BasicValueModel> observer)
        {
            if (m_observers == null) {
                m_observers = new List<IValueObserver<BasicValueModel>>();
            }
            
            m_observers.Add(observer);
            
            var unsubscriber = new Unsubscriber<IValueObserver<BasicValueModel>>(observer, m_observers);

            observer.Changed(this);

            return unsubscriber;
        }
        
        public IDisposable Subscribe(Action<BasicValueModel> observer)
        {
            return Subscribe(new ActionObserver<BasicValueModel>(observer));
        }

        private void NotifyChanged()
        {
            if (m_observers == null) {
                return;
            }
                
            foreach (var observer in m_observers) {
                observer.Changed(this);
            }
        }

        public override void ToJson(StringBuilder sb)
        {
            switch (Type) {
                case ValueType.Bool:
                    sb.Append(AsBool ? "true": "false");
                    break;
                case ValueType.Double:
                    sb.Append(AsDouble.ToString());
                    break;
                case ValueType.String:
                    sb.Append($"\"{AsString}\"");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public string ToDisplayString()
        {
            switch (Type) {
                case ValueType.Bool:
                    return AsBool ? "true" : "false";
                case ValueType.Double:
                    var doubleValue = AsDouble;
                    if (doubleValue == 0) {
                        return "0";
                    }
                    return AsDouble.ToString("##,#.##");
                case ValueType.String:
                    return AsString;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}