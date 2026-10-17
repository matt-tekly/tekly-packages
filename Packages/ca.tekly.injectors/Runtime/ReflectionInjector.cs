using System;
using System.Collections.Generic;
using System.Reflection;

namespace Tekly.Injectors
{
	/// <summary>
	/// The IInjector a type gets when it has no __Injector of its own: finds the type's declared [Inject] fields
	/// and methods once, and sets and calls them through reflection.
	/// </summary>
	public sealed class ReflectionInjector : IInjector
	{
		/// <summary>
		/// True when the type declares nothing to inject, so the level can be left out of the chain.
		/// </summary>
		public bool IsEmpty => m_fields.Count == 0 && m_methods.Count == 0;

		private const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

		private readonly List<InjectableField> m_fields;
		private readonly List<InjectableMethod> m_methods;

		public ReflectionInjector(Type type)
		{
			m_fields = GetInjectableFields(type);
			m_methods = GetInjectableMethods(type);
		}

		public void Inject(object instance, InjectorContainer container)
		{
			foreach (var field in m_fields) {
				field.Inject(instance, container);
			}

			foreach (var method in m_methods) {
				method.Inject(instance, container);
			}
		}

		public void Clear(object instance)
		{
			foreach (var field in m_fields) {
				field.Clear(instance);
			}
		}

		/// <summary>
		/// Whether type itself declares any [Inject] field or method. Doesn't look at base types.
		/// </summary>
		public static bool DeclaresInjectables(Type type)
		{
			foreach (var field in type.GetFields(FLAGS)) {
				if (field.IsDefined(typeof(InjectAttribute), false)) {
					return true;
				}
			}

			foreach (var method in type.GetMethods(FLAGS)) {
				if (method.IsDefined(typeof(InjectAttribute), false)) {
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// The [Inject] fields type declares itself.
		/// </summary>
		public static List<InjectableField> GetInjectableFields(Type type)
		{
			var injectableFields = new List<InjectableField>();

			foreach (var fieldInfo in type.GetFields(FLAGS)) {
				var injectAttribute = fieldInfo.GetCustomAttribute<InjectAttribute>();

				if (injectAttribute != null) {
					injectableFields.Add(new InjectableField(injectAttribute, fieldInfo));
				}
			}

			return injectableFields;
		}

		/// <summary>
		/// The [Inject] methods type declares itself.
		/// </summary>
		public static List<InjectableMethod> GetInjectableMethods(Type type)
		{
			var injectableMethods = new List<InjectableMethod>();

			foreach (var method in type.GetMethods(FLAGS)) {
				if (method.IsDefined(typeof(InjectAttribute), false)) {
					injectableMethods.Add(new InjectableMethod(method));
				}
			}

			return injectableMethods;
		}
	}
}
