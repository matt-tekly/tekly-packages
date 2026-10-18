using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Tekly.DevBoard.Commands
{
	/// <summary>
	/// A typed argument of a command.
	/// </summary>
	public sealed class CommandParameter
	{
		public string Name { get; }
		public Type Type { get; }
		public bool IsOptional { get; }
		public object DefaultValue { get; }

		// Where suggested values come from: set by the command's author, else the type's own (enums, bools)
		private Func<IEnumerable<string>> m_custom;
		private readonly Func<IEnumerable<string>> m_default;

		internal CommandParameter(ParameterInfo parameter, object target)
		{
			Name = parameter.Name;
			Type = parameter.ParameterType;
			IsOptional = parameter.IsOptional;

			if (IsOptional) {
				DefaultValue = parameter.HasDefaultValue ? parameter.DefaultValue
					: Type.IsValueType ? Activator.CreateInstance(Type) : null;
			}

			if (Type.IsEnum) {
				var names = Enum.GetNames(Type);
				m_default = () => names;
			} else if (Type == typeof(bool)) {
				var values = new[] { "true", "false" };
				m_default = () => values;
			}

			var suggest = parameter.GetCustomAttribute<SuggestAttribute>();

			if (suggest != null) {
				m_custom = FindProvider(parameter.Member.DeclaringType, suggest.MethodName, target);
			}
		}

		/// <summary>
		/// The values the console suggests for this parameter, or null if it has none. Never throws: a provider
		/// that does is logged and suggests nothing.
		/// </summary>
		public IEnumerable<string> GetSuggestions()
		{
			try {
				return m_custom?.Invoke() ?? m_default?.Invoke();
			} catch (Exception exception) {
				Debug.LogException(exception);
				return null;
			}
		}

		/// <summary>
		/// How the parameter is shown in a usage hint: "&lt;amount:int&gt;", or when optional "[count=1]", whose
		/// default shows the type, or just "[filter]" when the default is empty.
		/// </summary>
		public override string ToString()
		{
			if (!IsOptional) {
				return $"<{Name}:{CommandArguments.TypeName(Type)}>";
			}

			return DefaultValue == null || DefaultValue is string { Length: 0 }
				? $"[{Name}]"
				: $"[{Name}={CommandArguments.Format(DefaultValue)}]";
		}

		internal void SetSuggestions(Func<IEnumerable<string>> values)
		{
			m_custom = values;
		}

		/// <summary>
		/// The method named by a [Suggest], as a provider of strings: an instance method of the command's target
		/// when it has one and the type has one, else a static method on the type declaring the command.
		/// </summary>
		private Func<IEnumerable<string>> FindProvider(Type type, string methodName, object target)
		{
			if (target != null) {
				var instanceMethod = FindProviderMethod(target.GetType(), methodName, BindingFlags.Instance);

				if (instanceMethod != null) {
					return () => ToStrings((IEnumerable) instanceMethod.Invoke(target, null));
				}
			}

			var method = FindProviderMethod(type, methodName, BindingFlags.Static);

			if (method == null) {
				var on = target != null
					? $"an instance method on {target.GetType().Name} or a static method on {type?.Name}"
					: $"a static method on {type?.Name}";
				Debug.LogWarning($"[DevBoard] [Suggest(\"{methodName}\")] on '{Name}' needs {on} that takes nothing and returns an IEnumerable");
				return null;
			}

			return () => ToStrings((IEnumerable) method.Invoke(null, null));
		}

		/// <summary>
		/// The method named methodName on type or a base type, taking nothing and returning an IEnumerable, or
		/// null. Walks the base types itself so private methods of bases are found.
		/// </summary>
		private static MethodInfo FindProviderMethod(Type type, string methodName, BindingFlags kind)
		{
			var flags = kind | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

			for (; type != null && type != typeof(object); type = type.BaseType) {
				var method = type.GetMethod(methodName, flags, null, Type.EmptyTypes, null);

				if (method != null && typeof(IEnumerable).IsAssignableFrom(method.ReturnType)) {
					return method;
				}
			}

			return null;
		}

		private static IEnumerable<string> ToStrings(IEnumerable values)
		{
			if (values == null) {
				yield break;
			}

			foreach (var value in values) {
				if (value != null) {
					yield return value.ToString();
				}
			}
		}
	}

	/// <summary>
	/// A registered command: a name of one or more words, and the method it runs. Dispose it to remove it.
	/// </summary>
	public sealed class Command : IDisposable
	{
		/// <summary>
		/// The name as words separated by single spaces, e.g. "give gold".
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// What the command does, for search and hints. Null for none.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// The arguments typed after the name. A CommandContext parameter isn't one.
		/// </summary>
		public IReadOnlyList<CommandParameter> Parameters { get; }

		/// <summary>
		/// The name followed by its parameters, e.g. "give gold &lt;amount:int&gt;".
		/// </summary>
		public string Usage { get; }

		/// <summary>
		/// Just the parameters, e.g. "&lt;amount:int&gt; [count=1]". Empty for none.
		/// </summary>
		public string ArgumentsUsage => Usage.Length > Name.Length ? Usage.Substring(Name.Length + 1) : string.Empty;

		/// <summary>
		/// True when it needs arguments typed to run.
		/// </summary>
		public bool HasRequiredParameters { get; }

		/// <summary>
		/// True when the method returns something to wait on: a Task, an Awaitable or an IEnumerator coroutine.
		/// The result is what it produces, once it's done.
		/// </summary>
		public bool IsAsync => m_awaiter != null;

		/// <summary>
		/// True once disposed, and so removed from its registry.
		/// </summary>
		public bool IsDisposed { get; private set; }

		internal string[] Words { get; }

		private readonly CommandRegistry m_registry;
		private readonly MethodInfo m_method;
		private readonly object m_target;

		// Where the CommandContext goes in the method's arguments, or -1
		private readonly int m_contextIndex = -1;
		private readonly int m_argumentCount;

		// Waits on what the method returns and gives back its value. Null when the return value is the result
		private readonly Func<object, CancellationToken, Task<object>> m_awaiter;

		internal Command(CommandRegistry registry, string name, string description, MethodInfo method, object target)
		{
			m_registry = registry;
			m_method = method;
			m_target = target;
			m_awaiter = AwaiterFor(method.ReturnType);

			Words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			Name = string.Join(" ", Words);
			Description = string.IsNullOrEmpty(description) ? null : description;

			var parameters = new List<CommandParameter>();
			var methodParameters = method.GetParameters();
			m_argumentCount = methodParameters.Length;

			for (var i = 0; i < methodParameters.Length; i++) {
				if (methodParameters[i].ParameterType == typeof(CommandContext)) {
					m_contextIndex = i;
					continue;
				}

				var parameter = new CommandParameter(methodParameters[i], target);
				parameters.Add(parameter);
				HasRequiredParameters |= !parameter.IsOptional;
			}

			Parameters = parameters;

			var usage = new StringBuilder(Name);

			foreach (var parameter in parameters) {
				usage.Append(' ').Append(parameter);
			}

			Usage = usage.ToString();
		}

		/// <summary>
		/// Sets the values the console suggests for the parameter named parameter, replacing any from [Suggest]
		/// or its type. values returning null falls back to the type's own.
		/// </summary>
		public Command WithSuggestions(string parameter, Func<IEnumerable<string>> values)
		{
			foreach (var candidate in Parameters) {
				if (candidate.Name == parameter) {
					candidate.SetSuggestions(values);
					return this;
				}
			}

			Debug.LogWarning($"[DevBoard] Command '{Name}' has no parameter named '{parameter}' to suggest values for");
			return this;
		}

		/// <summary>
		/// The parameter typed at argument index, counting from 0 after the name. A last text parameter takes the
		/// rest of the line, so it's every index from its own on. Null past the end.
		/// </summary>
		public CommandParameter ParameterAt(int index)
		{
			if (index < 0 || Parameters.Count == 0) {
				return null;
			}

			if (index < Parameters.Count) {
				return Parameters[index];
			}

			var last = Parameters[Parameters.Count - 1];
			return last.Type == typeof(string) ? last : null;
		}

		public void Dispose()
		{
			if (IsDisposed) {
				return;
			}

			IsDisposed = true;
			m_registry?.Remove(this);
		}

		/// <summary>
		/// Calls the method with the converted arguments, one per Parameter, in order, and waits on what it returns
		/// when that's a Task, Awaitable or coroutine. The task holds the value to show as the result, or null.
		/// Exceptions from the method, now or later, fault the task.
		/// </summary>
		internal Task<object> InvokeAsync(object[] arguments, CommandContext context)
		{
			var all = new object[m_argumentCount];

			for (int i = 0, next = 0; i < all.Length; i++) {
				all[i] = i == m_contextIndex ? context : arguments[next++];
			}

			object value;

			try {
				value = m_method.Invoke(m_target, all);
			} catch (TargetInvocationException exception) {
				return Task.FromException<object>(exception.InnerException ?? exception);
			}

			if (m_awaiter == null || value == null) {
				return Task.FromResult(value);
			}

			return m_awaiter(value, context.CancellationToken);
		}

		/// <summary>
		/// How to wait on a value of returnType, or null when it's shown as it is.
		/// </summary>
		private static Func<object, CancellationToken, Task<object>> AwaiterFor(Type returnType)
		{
			if (returnType == typeof(Task)) {
				return async (value, _) => {
					await (Task) value;
					return null;
				};
			}

			if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)) {
				var result = returnType.GetProperty(nameof(Task<object>.Result));

				return async (value, _) => {
					await (Task) value;
					return result.GetValue(value);
				};
			}

			if (returnType == typeof(Awaitable)) {
				return async (value, _) => {
					await (Awaitable) value;
					return null;
				};
			}

			if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Awaitable<>)) {
				var awaitTyped = typeof(Command)
					.GetMethod(nameof(AwaitAwaitable), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod(returnType.GetGenericArguments());

				return (value, _) => (Task<object>) awaitTyped.Invoke(null, new[] { value });
			}

			if (typeof(IEnumerator).IsAssignableFrom(returnType)) {
				return async (value, cancellationToken) => {
					await CommandCoroutines.Run((IEnumerator) value, cancellationToken);
					return null;
				};
			}

			return null;
		}

		private static async Task<object> AwaitAwaitable<T>(Awaitable<T> awaitable)
		{
			return await awaitable;
		}
	}
}
