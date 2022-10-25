using System.Collections.Generic;
using Tekly.Common.Utils;
using UnityEngine.Assertions;

namespace Tekly.Injectors
{
    public class InjectorContainerRegistry : Singleton<InjectorContainerRegistry>
    {
        private readonly Dictionary<string, InjectorContainer> m_containers = new Dictionary<string, InjectorContainer>();

        public void Register(string name, InjectorContainer container)
        {
            Assert.IsFalse(m_containers.ContainsKey(name));
            m_containers[name] = container;
        }

        public bool TryGet(string name, out InjectorContainer container)
        {
            return m_containers.TryGetValue(name, out container);
        }

        public void Remove(string name)
        {
            m_containers.Remove(name);
        }
    }
}