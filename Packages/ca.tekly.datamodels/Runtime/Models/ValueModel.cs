using System;
using System.Collections.Generic;
using System.Text;
using Tekly.Common.Observables;

namespace Tekly.DataModels.Models
{
    public class ValueModel<T> : ModelBase, IValueModel
    {
        public T Value
        {
            get => m_value;
            set {
                if (Equals(m_value, value)) {
                    return;
                }
                
                m_value = value;
                NotifyChanged();
            }
        }
        
        private T m_value;

        private List<IValueObserver<T>> m_observers;
        
        public IDisposable Subscribe(IValueObserver<T> observer)
        {
            if (m_observers == null) {
                m_observers = new List<IValueObserver<T>>();
            }
            
            m_observers.Add(observer);
            
            var unsubscriber = new Unsubscriber<IValueObserver<T>>(observer, m_observers);

            observer.Changed(m_value);

            return unsubscriber;
        }
        
        public IDisposable Subscribe(Action<T> observer)
        {
            return Subscribe(new ActionObserver<T>(observer));
        }

        private void NotifyChanged()
        {
            if (m_observers == null) {
                return;
            }
                
            foreach (var observer in m_observers) {
                observer.Changed(m_value);
            }
        }
        
        public override void ToJson(StringBuilder sb)
        {
            sb.Append("[UNIMPLEMENTED]");
        }

        public virtual string ToDisplayString()
        {
            return "[UNIMPLEMENTED]";
        }
    }
}