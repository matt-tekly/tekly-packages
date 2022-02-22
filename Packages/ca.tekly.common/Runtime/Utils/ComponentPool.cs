using UnityEngine;

namespace Tekly.Common.Utils
{
    public class ComponentPool<T> : ObjectPoolBase<T> where T : Component
    {
        private readonly T m_component;
        private readonly Transform m_container;

        public ComponentPool(T component, Transform container)
        {
            m_component = component;
            m_container = container;
        }
        
        protected override T Allocate()
        {
            return Object.Instantiate(m_component);
        }

        protected override void Recycled(T element)
        {
            element.gameObject.SetActive(false);
            m_component.transform.SetParent(m_container);
        }
    }
}