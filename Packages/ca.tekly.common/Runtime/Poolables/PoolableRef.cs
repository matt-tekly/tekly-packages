namespace Tekly.Common.Poolables
{
	/// <summary>
	/// Holds a pooled object reference + generation snapshot.
	/// A reference is valid only if the current generation matches the captured generation.
	/// </summary>
	public readonly struct PoolableRef<T> where T : Poolable
	{
		public T Value => IsValid ? m_value : null;
		public bool IsValid => m_value != null && m_value.Generation == m_generation;
		
		private const uint INVALID_GENERATION = uint.MaxValue;

		private readonly uint m_generation;
		private readonly T m_value;

		public PoolableRef(T value)
		{
			m_value = value;
			m_generation = value != null ? value.Generation : INVALID_GENERATION;
		}

		public bool TryGet(out T value)
		{
			if (IsValid) {
				value = m_value;
				return true;
			}

			value = null;
			return false;
		}

		public static implicit operator PoolableRef<T>(T value)
		{
			return new PoolableRef<T>(value);
		}

		public static implicit operator T(PoolableRef<T> value)
		{
			return value.Value;
		}
	}
}
