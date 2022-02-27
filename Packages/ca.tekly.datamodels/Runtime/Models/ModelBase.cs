using System.Text;
using Tekly.Logging;

namespace Tekly.DataModels.Models
{
    public abstract class ModelBase : IModel
    {
        private bool m_isDisposed;

        protected ModelBase()
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

        public virtual void ToJson(StringBuilder sb) { }

        protected virtual void OnTick() { }

        protected virtual void OnDispose() { }
    }
}