using UnityEngine;

namespace Tekly.Stagecraft
{
	/// <summary>
	/// The default time source: ticks a stage from Unity's Update and FixedUpdate. Replace it with your own driver
	/// (a netcode tick, a test loop) by calling Stage.Tick / Stage.FixedTick yourself.
	/// </summary>
	public sealed class StageRunner : MonoBehaviour
	{
		public Stage Stage { get; private set; }

		public static StageRunner Create(Stage stage, string name = null)
		{
			var go = new GameObject(name ?? $"[StageRunner] {stage.Name}");
			var runner = go.AddComponent<StageRunner>();
			runner.Stage = stage;
			return runner;
		}

		private void Update()
		{
			if (Stage != null && !Stage.IsDisposed) {
				Stage.Tick(Time.deltaTime);
			}
		}

		private void FixedUpdate()
		{
			if (Stage != null && !Stage.IsDisposed) {
				Stage.FixedTick(Time.fixedDeltaTime);
			}
		}
	}
}
