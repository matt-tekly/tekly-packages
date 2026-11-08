using Tekly.DevBoard;
using Tekly.DevBoard.Nodes;
using Tekly.Injectors;
using Tekly.Stagecraft;
using Tekly.Stagecraft.Integration;
using Tekly.Stagecraft.PhysicsCore2D;
using UnityEngine;

namespace TeklySample.Samples.Stagecraft
{
	/// <summary>
	/// A few thousand actors spawning and despawning continuously. Wanderers tick per object, drifters are moved
	/// by a system, blasts despawn whatever their trigger touches through buffered physics events. The stage is
	/// driven by a StageRunner. Watch the "Stage.Tick ..." / "Stage.FixedTick ..." markers in the profiler, and
	/// the Sample/Stagecraft page in DevBoard.
	/// </summary>
	public class StagecraftSample : MonoBehaviour
	{
		[Tooltip("The scene's placed actors (the Director among them), activated once the stage exists")]
		[SerializeField] private StageScene m_stageScene;

		private readonly SampleStats m_stats = new SampleStats();
		private InjectorContainer m_container;
		private Stage m_stage;
		private PhysicsCore2DAdapter m_physics;
		private StageRunner m_runner;

		private void Start()
		{
			// Registration order is tick order
			var config = new StageConfig()
				.Register<Director>()
				.Register<Wanderer>()
				.RegisterSystem<Drifter, DriftSystem>()
				.Register<Blast>()
				.Register<ActorBody2D>();

			// Services actors and systems take with [Inject]. Injection happens once per pooled instance
			m_container = new InjectorContainer("Sample");
			m_container.Register(m_stats);

			m_stage = new Stage(config, m_container, "Sample");
			m_physics = new PhysicsCore2DAdapter(m_stage);
			m_runner = StageRunner.Create(m_stage);

			// The Director and any other placed actors join the stage here; StageScene put them to sleep in its Awake
			m_stageScene.Adopt(m_stage);
			m_stageScene.Activate();

			StageDevPage.Register(m_stage, "Sample/Stagecraft", gameObject);
			DevBoard.Page("Sample/Stagecraft", page => {
				page.Property("Blasts", () => m_stats.Blasts);
				page.Property("Kills", () => m_stats.Kills);
			}, "Blasts", 1, gameObject);
		}

		private void OnDestroy()
		{
			if (m_runner != null) {
				Destroy(m_runner.gameObject);
			}

			m_physics?.Dispose();
			m_stage?.Dispose();
			m_container?.Dispose();
		}
	}
}
