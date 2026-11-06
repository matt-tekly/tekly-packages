namespace TeklySample.Samples.Stagecraft
{
	/// <summary>
	/// A service shared by every actor through the stage's injector. Registered once on the container; injected
	/// into each pooled instance when it is created, not on every spawn.
	/// </summary>
	public class SampleStats
	{
		public int Blasts;
		public int Kills;
	}
}
