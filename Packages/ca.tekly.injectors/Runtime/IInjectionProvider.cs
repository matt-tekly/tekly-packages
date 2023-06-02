namespace Tekly.Injectors
{
	public interface IInjectionProvider
	{
		void Provide(InjectorContainer container);
	}
}