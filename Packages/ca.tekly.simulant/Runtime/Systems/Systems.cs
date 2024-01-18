namespace Tekly.Simulant.Systems
{
	public interface ISystem { }

	public interface ITickSystem : ISystem
	{
		void Tick(float deltaTime);
	}
}