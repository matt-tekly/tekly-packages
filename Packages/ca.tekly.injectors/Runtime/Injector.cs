using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Tekly.Injectors.Utils;

namespace Tekly.Injectors
{
	/// <summary>
	/// Creates and injects instances of one type: its constructor, then the IInjector of each type in its
	/// hierarchy, base first. Get one from TypeDatabase.
	/// </summary>
	public class Injector
	{
		private readonly Type m_type;
		private readonly List<IInjector> m_levels;
		private readonly InjectableConstructor m_constructor;

		private bool m_isInstantiating;

		public Injector(Type type, List<IInjector> levels)
		{
			m_type = type;
			m_levels = levels;
			m_constructor = new InjectableConstructor(type, type.Constructor());
		}

		public object Instantiate(InjectorContainer container)
		{
			// We need to detect cycles in instantiating objects. In the editor a stack overflow will just crash.
			if (m_isInstantiating) {
				throw new DependencyCycleException(m_type);
			}

			m_isInstantiating = true;

			try {
				var instance = m_constructor.Instantiate(container);
				Inject(instance, container);
				return instance;
			} finally {
				m_isInstantiating = false;
			}
		}

		public void Inject(object instance, InjectorContainer container)
		{
			foreach (var level in m_levels) {
				level.Inject(instance, container);
			}
		}

		/// <summary>
		/// Nulls the [Inject] fields, most derived type first.
		/// </summary>
		public void Clear(object instance)
		{
			for (var i = m_levels.Count - 1; i >= 0; i--) {
				m_levels[i].Clear(instance);
			}
		}
	}

	/// <summary>
	/// Resolves a member's dependencies from a container, naming the member when one is missing or the member
	/// throws, so the InjectionException reads as a chain.
	/// </summary>
	internal static class InjectionErrors
	{
		/// <summary>
		/// The value for parameter, from container. A missing one is reported as needed by "{member}({parameter})".
		/// Names are only built when something fails, so the happy path allocates nothing.
		/// </summary>
		public static object Resolve(InjectorContainer container, ParameterInfo parameter, MethodBase member)
		{
			try {
				if (parameter.IsOptional) {
					container.TryGet(parameter.ParameterType, out var data);
					return data;
				}

				return container.Get(parameter.ParameterType);
			} catch (InjectionException exception) {
				exception.NeededBy($"{Name(member)}({parameter.Name})");
				throw;
			}
		}

		/// <summary>
		/// Rethrows what member threw from inside a reflection call: an InjectionException keeps its chain, with
		/// member added; anything else is wrapped so the chain still says where it happened.
		/// </summary>
		public static Exception Thrown(TargetInvocationException exception, MethodBase member)
		{
			var inner = exception.InnerException ?? exception;

			if (inner is InjectionException injectionException) {
				injectionException.NeededBy(Name(member));
				ExceptionDispatchInfo.Capture(inner).Throw();
			}

			return new InjectionException($"{Name(member)} threw {inner.GetType().Name}: {inner.Message}", inner);
		}

		/// <summary>
		/// "new Type" for a constructor, "Type.Method" otherwise.
		/// </summary>
		public static string Name(MethodBase member)
		{
			return member is ConstructorInfo ? $"new {member.DeclaringType?.Name}" : $"{member.DeclaringType?.Name}.{member.Name}";
		}

		public static string Name(FieldInfo field)
		{
			return $"{field.DeclaringType?.Name}.{field.Name}";
		}
	}

	public class InjectableConstructor
	{
		private readonly ConstructorInfo m_constructorInfo;
		private readonly object[] m_parameterValues;
		private readonly ParameterInfo[] m_parameterInfos;
		private readonly Type m_type;

		public InjectableConstructor(Type type, ConstructorInfo constructorInfo)
		{
			m_type = type;
			m_constructorInfo = constructorInfo;

			m_parameterInfos = constructorInfo != null ? constructorInfo.GetParameters() : Array.Empty<ParameterInfo>();
			m_parameterValues = m_parameterInfos.Length == 0 ? Array.Empty<object>() : new object[m_parameterInfos.Length];
		}

		public object Instantiate(InjectorContainer container)
		{
			if (m_constructorInfo == null) {
				throw new InjectionException($"{m_type.FullName} has no public constructor, so the container can't create it. Register an instance instead");
			}

			for (var index = 0; index < m_parameterInfos.Length; index++) {
				m_parameterValues[index] = InjectionErrors.Resolve(container, m_parameterInfos[index], m_constructorInfo);
			}

			try {
				return m_constructorInfo.Invoke(m_parameterValues);
			} catch (TargetInvocationException exception) {
				throw InjectionErrors.Thrown(exception, m_constructorInfo);
			} finally {
				Array.Clear(m_parameterValues, 0, m_parameterValues.Length);
			}
		}
	}

	public class InjectableMethod
	{
		private readonly object[] m_parameterValues;
		private readonly ParameterInfo[] m_parameterInfos;
		private readonly MethodInfo m_methodInfo;

		public InjectableMethod(MethodInfo methodInfo)
		{
			m_methodInfo = methodInfo;

			m_parameterInfos = methodInfo.GetParameters();
			m_parameterValues = m_parameterInfos.Length == 0 ? Array.Empty<object>() : new object[m_parameterInfos.Length];
		}

		public void Inject(object instance, InjectorContainer container)
		{
			for (var index = 0; index < m_parameterInfos.Length; index++) {
				m_parameterValues[index] = InjectionErrors.Resolve(container, m_parameterInfos[index], m_methodInfo);
			}

			try {
				m_methodInfo.Invoke(instance, m_parameterValues);
			} catch (TargetInvocationException exception) {
				throw InjectionErrors.Thrown(exception, m_methodInfo);
			} finally {
				Array.Clear(m_parameterValues, 0, m_parameterValues.Length);
			}
		}
	}

	public class InjectableField
	{
		public Type FieldType => m_fieldType;

		private readonly FieldInfo m_fieldInfo;
		private readonly Type m_fieldType;
		private readonly InjectAttribute m_injectAttribute;

		public InjectableField(InjectAttribute injectAttribute, FieldInfo fieldInfo)
		{
			m_injectAttribute = injectAttribute;
			m_fieldInfo = fieldInfo;
			m_fieldType = m_fieldInfo.FieldType;
		}

		public void Inject(object instance, InjectorContainer container)
		{
			try {
				if (m_injectAttribute.IsOptional == IsOptional.Optional) {
					if (container.TryGet(m_fieldType, out var data)) {
						m_fieldInfo.SetValue(instance, data);
					}
				} else if (m_injectAttribute.Id == null) {
					m_fieldInfo.SetValue(instance, container.Get(m_fieldType));
				} else {
					m_fieldInfo.SetValue(instance, container.Get(m_fieldType, m_injectAttribute.Id));
				}
			} catch (InjectionException exception) {
				exception.NeededBy(InjectionErrors.Name(m_fieldInfo));
				throw;
			}
		}

		public void Clear(object instance)
		{
			m_fieldInfo.SetValue(instance, null);
		}
	}
}
