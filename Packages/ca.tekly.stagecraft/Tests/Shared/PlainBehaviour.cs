namespace Tekly.Stagecraft.Tests
{
	/// <summary>Overrides nothing: must never enter a tick list.</summary>
	public class PlainBehaviour : ActorBehaviour
	{
		public int Spawned;

		public override void OnSpawned()
		{
			Spawned++;
		}
	}
}
