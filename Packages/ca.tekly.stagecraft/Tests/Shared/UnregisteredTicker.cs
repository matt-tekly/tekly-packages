namespace Tekly.Stagecraft.Tests
{
	/// <summary>Overrides Tick but is never registered.</summary>
	public class UnregisteredTicker : ActorBehaviour
	{
		public int Ticks;

		public override void Tick(float deltaTime)
		{
			Ticks++;
		}
	}
}
