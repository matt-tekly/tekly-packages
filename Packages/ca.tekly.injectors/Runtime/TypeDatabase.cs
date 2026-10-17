using System;
using System.Collections.Generic;
using System.Reflection;
using Tekly.Common.Utils;

namespace Tekly.Injectors
{
	/// <summary>
	/// The Injector for each type, built on first use. An object's injector is a chain of one IInjector per type
	/// in its hierarchy, base first: a type's own nested __Injector when it has one, else a ReflectionInjector
	/// over its declared [Inject] members. The levels are shared between types that share a base.
	/// </summary>
	public class TypeDatabase : Singleton<TypeDatabase>
	{
		/// <summary>
		/// The name of the nested class a type can declare to inject its own members without reflection. It
		/// implements IInjector and has a parameterless constructor. Only the declaring type's members are its
		/// business: base and derived types have levels of their own.
		/// </summary>
		public const string GENERATED_INJECTOR = "__Injector";

		private readonly Dictionary<Type, Injector> m_injectors = new Dictionary<Type, Injector>();
		private readonly Dictionary<Type, IInjector> m_levels = new Dictionary<Type, IInjector>();

		public Injector GetInjector(Type type)
		{
			if (!m_injectors.TryGetValue(type, out var injector)) {
				injector = new Injector(type, GetLevels(type));
				m_injectors.Add(type, injector);
			}

			return injector;
		}

		/// <summary>
		/// One IInjector per type from the root base down to type, leaving out reflection levels with nothing to
		/// inject.
		/// </summary>
		private List<IInjector> GetLevels(Type type)
		{
			var levels = new List<IInjector>();

			for (var current = type; current != null && current != typeof(object); current = current.BaseType) {
				var level = GetLevel(current);

				if (level != null) {
					levels.Add(level);
				}
			}

			levels.Reverse();
			return levels;
		}

		/// <summary>
		/// The IInjector for just type's declared members, or null when reflection finds none.
		/// </summary>
		private IInjector GetLevel(Type type)
		{
			if (m_levels.TryGetValue(type, out var level)) {
				return level;
			}

			level = CreateGeneratedLevel(type);

			if (level == null) {
				var reflection = new ReflectionInjector(type);
				level = reflection.IsEmpty ? null : reflection;
			}

			m_levels.Add(type, level);
			return level;
		}

		/// <summary>
		/// An instance of type's own nested __Injector, or null when it has none. Only the type's own declaration
		/// counts, so a base type's isn't run twice.
		/// </summary>
		private static IInjector CreateGeneratedLevel(Type type)
		{
			const BindingFlags FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
			var nested = type.GetNestedType(GENERATED_INJECTOR, FLAGS);

			if (nested == null) {
				return null;
			}

			if (!typeof(IInjector).IsAssignableFrom(nested)) {
				throw new InvalidOperationException($"{type.Name}.{GENERATED_INJECTOR} must implement {nameof(IInjector)}");
			}

			return (IInjector) Activator.CreateInstance(nested, true);
		}
	}
}
