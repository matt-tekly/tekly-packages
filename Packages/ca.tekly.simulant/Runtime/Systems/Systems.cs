namespace Tekly.Simulant.Systems
{
	public interface ISystem { }

	public interface ITickSystem : ISystem
	{
		void Tick(float deltaTime);
	}
	
	public interface ITickLateSystem : ISystem
	{
		void TickLate(float deltaTime);
	}
	
	public interface ITickEndSystem : ISystem
	{
		void TickEnd(float deltaTime);
	}
}