using System;
using System.Collections.Generic;
using System.Reflection;
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
			m_constructor = new InjectableConstructor(type.Constructor());
		}

		public object Instantiate(InjectorContainer container)
		{
			// We need to detect cycles in instantiating objects. In the editor a stack overflow will just crash.
			if (m_isInstantiating) {
				throw new DependencyCycleException(m_type);
			}

			m_isInstantiating = true;
			var instance = m_constructor.Instantiate(container);
			Inject(instance, container);
			m_isInstantiating = false;

			return instance;
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

	public class InjectableConstructor
	{
		private readonly ConstructorInfo m_constructorInfo;
		private readonly object[] m_parameterValues;
		private readonly ParameterInfo[] m_parameterInfos;
		private readonly Type m_type;

		public InjectableConstructor(ConstructorInfo constructorInfo)
		{
			m_constructorInfo = constructorInfo;
			m_type = constructorInfo.DeclaringType;

			m_parameterInfos = constructorInfo.GetParameters();
			if (m_parameterInfos.Length == 0) {
				m_parameterValues = Array.Empty<object>();
			} else {
				m_parameterValues = new object[m_parameterInfos.Length];
			}
		}

		public object Instantiate(InjectorContainer container)
		{
			for (var index = 0; index < m_parameterInfos.Length; index++) {
				var parameterInfo = m_parameterInfos[index];
				if (parameterInfo.IsOptional) {
					container.TryGet(parameterInfo.ParameterType, out var data);
					m_parameterValues[index] = data;
				} else {
					m_parameterValues[index] = container.Get(parameterInfo.ParameterType);
				}
			}

			var instance = m_constructorInfo.Invoke(m_parameterValues);

			Array.Clear(m_parameterValues, 0, m_parameterValues.Length);
			return instance;
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
			if (m_parameterInfos.Length == 0) {
				m_parameterValues = Array.Empty<object>();
			} else {
				m_parameterValues = new object[m_parameterInfos.Length];
			}
		}

		public void Inject(object instance, InjectorContainer container)
		{
			for (var index = 0; index < m_parameterInfos.Length; index++) {
				var parameterInfo = m_parameterInfos[index];

				if (parameterInfo.IsOptional) {
					container.TryGet(parameterInfo.ParameterType, out var data);
					m_parameterValues[index] = data;
				} else {
					m_parameterValues[index] = container.Get(parameterInfo.ParameterType);
				}
			}

			m_methodInfo.Invoke(instance, m_parameterValues);

			for (var index = 0; index < m_parameterValues.Length; index++) {
				m_parameterValues[index] = null;
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
			if (m_injectAttribute.IsOptional == IsOptional.Optional) {
				if (container.TryGet(m_fieldType, out var data)) {
					m_fieldInfo.SetValue(instance, data);
				}
			} else {
				if (m_injectAttribute.Id == null) {
					var data = container.Get(m_fieldType);
					m_fieldInfo.SetValue(instance, data);
				} else {
					var data = container.Get(m_fieldType, m_injectAttribute.Id);
					m_fieldInfo.SetValue(instance, data);
				}
			}
		}

		public void Clear(object instance)
		{
			m_fieldInfo.SetValue(instance, null);
		}
	}
}
