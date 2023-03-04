using System;
using UnityEngine;

namespace Tekly.Common.Registrys
{
	public abstract class RegisterableRef<T> : ScriptableObject
	{
		[NonSerialized] private T m_value;
		[NonSerialized] private bool m_initialized;
		
		public abstract IRegistry<T> Registry { get; }
		
		public T Value {
			get {
				if (m_value != null) {
					return m_value;
				}

				if (!Registry.TryGet(name, out m_value) && !m_initialized) {
					Debug.LogError($"Accessing RegisterableRef before it is initialized [{name}]");
					return default;
				}

				m_initialized = true;
				return m_value;
			}
		}
		
		public void Initialize(T value)
		{
			if (Registry.TryGet(name, out m_value)) {
				Debug.LogError($"Initializing RegisterableRef twice [{name}]");
			} else {
				m_value = value;
				Registry.Register(name, m_value);
			}

			m_initialized = true;
		}

		public void Clear()
		{
			Registry.Remove(name);
			m_initialized = false;
			m_value = default;
		}
	}
}