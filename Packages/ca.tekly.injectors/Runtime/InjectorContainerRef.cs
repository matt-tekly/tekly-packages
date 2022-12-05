using System;
using Tekly.Logging;
using UnityEngine;

namespace Tekly.Injectors
{
    [CreateAssetMenu(menuName = "Tekly/Injectors/Ref")]
    public class InjectorContainerRef : ScriptableObject
    {
        [NonSerialized] private InjectorContainer m_container;
        [NonSerialized] private bool m_initialized;
        [NonSerialized] private TkLogger m_logger = TkLogger.Get<InjectorContainerRef>();
        
        public InjectorContainer Container {
            get {
                if (!m_initialized) {
                    m_logger.Error("Accessing InjectorContainerRef before it is initialized [{name}]", ("name", name));
                    return null;
                }

                return m_container;
            }
        }

        public void Initialize(InjectorContainer container)
        {
            if (InjectorContainerRegistry.Instance.TryGet(name, out m_container)) {
                m_logger.Error("Initializing InjectorContainerRef twice [{name}]", ("name", name));
            } else {
                m_container = container;
                InjectorContainerRegistry.Instance.Register(name, m_container);
            }

            m_initialized = true;
        }

        public void Clear()
        {
            InjectorContainerRegistry.Instance.Remove(name);
            m_initialized = false;
            m_container = null;
        }
    }
}