using System.Collections.Generic;
using UnityEngine;

namespace Tekly.WebSockets
{
	public static class SetPropertyUtility
	{
		public static bool SetColor(ref Color current, Color value)
		{
			if (current.r == value.r && current.g == value.g && current.b == value.b && current.a == value.a) {
				return false;
			}

			current = value;
			return true;
		}

		public static bool SetStruct<T>(ref T currentValue, T newValue) where T : struct
		{
			if (EqualityComparer<T>.Default.Equals(currentValue, newValue)) {
				return false;
			}

			currentValue = newValue;
			return true;
		}

		public static bool SetClass<T>(ref T currentValue, T newValue) where T : class
		{
			if ((currentValue == null && newValue == null) || (currentValue != null && currentValue.Equals(newValue))) {
				return false;
			}

			currentValue = newValue;
			return true;
		}
	}
}