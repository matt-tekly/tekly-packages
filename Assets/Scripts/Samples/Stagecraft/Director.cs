using Tekly.Stagecraft;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TeklySample.Samples.Stagecraft
{
	/// <summary>
	/// The one actor that spawns the others: tops the crowds up every fixed step and drops a blast now and then.
	/// It is placed in the scene, so everything it needs is on its inspector; the StageScene activates it once the
	/// sample has built the stage.
	/// </summary>
	public class Director : ActorBehaviour
	{
		[SerializeField] private Wanderer m_wandererPrefab;
		[SerializeField] private Drifter m_drifterPrefab;
		[SerializeField] private Blast m_blastPrefab;

		[SerializeField] private int m_wanderers = 1500;
		[SerializeField] private int m_drifters = 1500;
		[SerializeField] private Vector2 m_arena = new Vector2(60f, 34f);
		[SerializeField] private float m_blastInterval = 0.5f;

		private float m_nextBlast;

		public override void OnSpawned()
		{
			m_nextBlast = 0f;
		}

		public override void FixedTick(float fixedDeltaTime)
		{
			// Spawn in bursts, like a wave spawner would, rather than one per step
			var wanderers = Stage.All<Wanderer>().RawCount;
			if (wanderers < m_wanderers) {
				var count = Mathf.Min(200, m_wanderers - wanderers);
				for (var i = 0; i < count; i++) {
					Stage.Spawn(m_wandererPrefab, RandomPose(), new WandererArgs { Speed = Random.Range(2f, 6f), Lifetime = Random.Range(2f, 8f) });
				}
			}

			var drifters = Stage.All<Drifter>().RawCount;
			if (drifters < m_drifters) {
				var count = Mathf.Min(200, m_drifters - drifters);
				for (var i = 0; i < count; i++) {
					var pose = RandomPose();
					Stage.Spawn(m_drifterPrefab, pose, new DrifterArgs { Target = -(Vector2) pose.position * 0.5f, Speed = Random.Range(3f, 9f), Lifetime = Random.Range(3f, 6f) });
				}
			}

			if (Stage.Time.FixedElapsed >= m_nextBlast) {
				m_nextBlast = Stage.Time.FixedElapsed + m_blastInterval;
				Stage.Spawn(m_blastPrefab, RandomPose(), new BlastArgs { Lifetime = 0.25f });
			}
		}

		private Pose RandomPose()
		{
			var half = m_arena * 0.5f;
			var position = new Vector3(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y), 0f);
			return new Pose(position, Quaternion.identity);
		}
	}
}
