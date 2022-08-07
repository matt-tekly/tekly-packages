namespace Tekly.Vibration
{
	public class VibrateStub : IVibrate
	{
		public void Pop() { }

		public void Peak() { }

		public void Negative() { }

		public bool HasVibration()
		{
			return false;
		}
	}
}