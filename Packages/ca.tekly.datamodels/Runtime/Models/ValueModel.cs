using System.Text;
using Tekly.Common.Observables;
using Tekly.Logging;

namespace Tekly.DataModels.Models
{
    public abstract class ValueModel<T> : ObservableValue<T>, IValueModel
    {
        private bool m_isDisposed;
        
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

        public virtual string ToDisplayString()
        {
            return "[UNIMPLEMENTED]";
        }
    }
}