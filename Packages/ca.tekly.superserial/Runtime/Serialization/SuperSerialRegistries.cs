using System;
using System.Collections.Generic;

namespace Tekly.SuperSerial.Serialization
{
	/// <summary>
	/// Every generated SuperSerialRegistry adds its RegisterAll here at start up (RuntimeInitializeOnLoadMethod
	/// in a player, InitializeOnLoadMethod in the editor), so a new SuperSerializer can register every
	/// [SuperSerializable] assembly without being told about them.
	/// </summary>
	public static class SuperSerialRegistries
	{
		private static readonly List<Action<SuperSerializer>> s_registries = new List<Action<SuperSerializer>>();

		public static int Count {
			get {
				lock (s_registries) {
					return s_registries.Count;
				}
			}
		}

		/// <summary>
		/// Adds a registry. Adding the same method twice is harmless, which lets the editor and player start up
		/// hooks both call it.
		/// </summary>
		public static void Add(Action<SuperSerializer> registry)
		{
			if (registry == null) {
				throw new ArgumentNullException(nameof(registry));
			}

			lock (s_registries) {
				if (!s_registries.Contains(registry)) {
					s_registries.Add(registry);
				}
			}
		}

		/// <summary>
		/// Applies every registry added since the serializer last asked, and returns whether there were any.
		/// </summary>
		public static bool Apply(SuperSerializer serializer, ref int applied)
		{
			Action<SuperSerializer>[] pending;

			lock (s_registries) {
				if (applied >= s_registries.Count) {
					return false;
				}

				pending = s_registries.GetRange(applied, s_registries.Count - applied).ToArray();
				applied = s_registries.Count;
			}

			foreach (var registry in pending) {
				registry(serializer);
			}

			return true;
		}
	}
}
