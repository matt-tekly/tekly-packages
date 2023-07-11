using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Common.Registrys
{
	public abstract class RegisterableValue<T> : ScriptableObject
	{
		[SerializeReference, Polymorphic] private T m_value;

		private T m_activeValue;

		public abstract IRegistry<T> Registry { get; }

		public T Value {
			get {
				if (m_activeValue != null) {
					return m_activeValue;
				}

				if (!Registry.TryGet(name, out m_activeValue)) {
					Registry.Register(name, m_value);
					m_activeValue = m_value;
				}

				return m_activeValue;
			}
		}

		public void Clear()
		{
			Registry.Remove(name);
			m_activeValue = default;
		}
	}
}