using System;
using System.Collections.Generic;

namespace Tekly.DevBoard
{
	/// <summary>
	/// A value a widget shows and edits, read through a getter and written through a setter. The binding is the
	/// value itself, shared by every widget showing it; what each widget last showed is kept in its own
	/// <see cref="BindingWatch{T}"/>.
	/// </summary>
	public sealed class Binding<T>
	{
		private static readonly IEqualityComparer<T> s_comparer = EqualityComparer<T>.Default;

		private readonly Func<T> m_get;
		private readonly Action<T> m_set;
		private readonly Func<T, T, bool> m_isSame;

		/// <summary>
		/// True when there's no setter.
		/// </summary>
		public bool IsReadOnly => m_set == null;

		/// <summary>
		/// set is null for a read-only value. isSame decides whether a new value counts as a change from the last
		/// one shown; null uses the default equality for T.
		/// </summary>
		public Binding(Func<T> get, Action<T> set = null, Func<T, T, bool> isSame = null)
		{
			m_get = get ?? throw new ArgumentNullException(nameof(get));
			m_set = set;
			m_isSame = isSame ?? s_comparer.Equals;
		}

		/// <summary>
		/// Reads the value. Throws if the getter does.
		/// </summary>
		public T Read()
		{
			return m_get();
		}

		/// <summary>
		/// Writes the value. Does nothing when read-only.
		/// </summary>
		public void Write(T value)
		{
			m_set?.Invoke(value);
		}

		public bool IsSame(T last, T value)
		{
			return m_isSame(last, value);
		}

		/// <summary>
		/// The same value seen as another type, e.g. an int shown in a number input over doubles. from converts
		/// written values back; the result is read-only if this binding is or from is null.
		/// </summary>
		public Binding<TOut> Convert<TOut>(Func<T, TOut> to, Func<TOut, T> from, Func<TOut, TOut, bool> isSame = null)
		{
			Action<TOut> set = m_set != null && from != null ? value => m_set(from(value)) : null;
			return new Binding<TOut>(() => to(m_get()), set, isSame);
		}
	}

	/// <summary>
	/// What one widget last showed from a binding, so it only redraws when the value changes. Keep it in a
	/// field, not a property or readonly field: it's a mutable struct.
	/// </summary>
	public struct BindingWatch<T>
	{
		private T m_value;
		private bool m_hasValue;

		public bool HasValue => m_hasValue;

		/// <summary>
		/// The value last shown. Only meaningful while HasValue.
		/// </summary>
		public T Value => m_value;

		/// <summary>
		/// Reads binding and returns true when the value should be shown: nothing has been shown yet, or it's
		/// changed from the last value shown. That value is then recorded as shown. Throws if the getter does.
		///
		/// Compares against the last value shown, not the last one read, so slow drift still adds up to a change.
		/// </summary>
		public bool Poll(Binding<T> binding, out T value)
		{
			value = binding.Read();

			if (m_hasValue && binding.IsSame(m_value, value)) {
				return false;
			}

			m_value = value;
			m_hasValue = true;

			return true;
		}

		/// <summary>
		/// Makes the next Poll return true even if the value hasn't changed, e.g. after the user edited the widget
		/// (the setter may have rejected or clamped the edit) or the getter threw.
		/// </summary>
		public void Invalidate()
		{
			m_value = default;
			m_hasValue = false;
		}
	}

	/// <summary>
	/// Common isSame comparisons for bindings.
	/// </summary>
	public static class BindingCompare
	{
		/// <summary>
		/// Floats within epsilon of each other count as the same, so small changes don't redraw.
		/// </summary>
		public static Func<float, float, bool> Within(float epsilon)
		{
			return (last, value) => {
				// The == also covers equal infinities, whose difference is NaN
				if (last == value || (float.IsNaN(last) && float.IsNaN(value))) {
					return true;
				}

				return Math.Abs(value - last) <= epsilon;
			};
		}
	}
}
