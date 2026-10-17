namespace Tekly.Injectors
{
	/// <summary>
	/// Injects the [Inject] members one type declares itself, not the ones it inherits. The injector for a whole
	/// object is a chain of these, one per type in its hierarchy, built by TypeDatabase.
	///
	/// By default each level is a ReflectionInjector. A type can supply its own instead: a nested class named
	/// __Injector (see TypeDatabase.GENERATED_INJECTOR) that implements this and has a parameterless constructor.
	/// Being nested, it can assign the type's private fields directly, which is what generated code does:
	/// <code>
	/// public partial class Player
	/// {
	///     [Inject] private Inventory m_inventory;
	///
	///     private sealed class __Injector : IInjector
	///     {
	///         public void Inject(object instance, InjectorContainer container)
	///         {
	///             ((Player) instance).m_inventory = container.Get&lt;Inventory&gt;();
	///         }
	///
	///         public void Clear(object instance)
	///         {
	///             ((Player) instance).m_inventory = null;
	///         }
	///     }
	/// }
	/// </code>
	/// </summary>
	public interface IInjector
	{
		/// <summary>
		/// Sets the type's [Inject] fields from container and calls its [Inject] methods.
		/// </summary>
		void Inject(object instance, InjectorContainer container);

		/// <summary>
		/// Nulls the type's [Inject] fields.
		/// </summary>
		void Clear(object instance);
	}
}
