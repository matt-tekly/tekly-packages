using Tekly.Stagecraft;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TeklySample.Samples.Stagecraft
{
	public struct DirectorArgs
	{
		public int Wanderers;
		public int Drifters;
		public Vector2 Arena;
		public float BlastInterval;
	}

	/// <summary>
	/// The one actor that spawns the others: tops the crowds up every fixed step and drops a blast now and then.
	/// The prefabs it spawns are wired on its own prefab; only the numbers come in as spawn args.
	/// </summary>
	public class Director : ActorBehaviour, ISpawnArgs<DirectorArgs>
	{
		[SerializeField] private Wanderer m_wandererPrefab;
		[SerializeField] private Drifter m_drifterPrefab;
		[SerializeField] private Blast m_blastPrefab;

		private DirectorArgs m_args;
		private float m_nextBlast;

		public void Initialize(in DirectorArgs args)
		{
			m_args = args;
			m_nextBlast = 0f;
		}

		public override void FixedTick(float fixedDeltaTime)
		{
			// Spawn in bursts, like a wave spawner would, rather than one per step
			var wanderers = Stage.All<Wanderer>().RawCount;
			if (wanderers < m_args.Wanderers) {
				var count = Mathf.Min(200, m_args.Wanderers - wanderers);
				for (var i = 0; i < count; i++) {
					Stage.Spawn(m_wandererPrefab, RandomPose(), new WandererArgs { Speed = Random.Range(2f, 6f), Lifetime = Random.Range(2f, 8f) });
				}
			}

			var drifters = Stage.All<Drifter>().RawCount;
			if (drifters < m_args.Drifters) {
				var count = Mathf.Min(200, m_args.Drifters - drifters);
				for (var i = 0; i < count; i++) {
					var pose = RandomPose();
					Stage.Spawn(m_drifterPrefab, pose, new DrifterArgs { Target = -(Vector2) pose.position * 0.5f, Speed = Random.Range(3f, 9f), Lifetime = Random.Range(3f, 6f) });
				}
			}

			if (Stage.Time.FixedElapsed >= m_nextBlast) {
				m_nextBlast = Stage.Time.FixedElapsed + m_args.BlastInterval;
				Stage.Spawn(m_blastPrefab, RandomPose(), new BlastArgs { Lifetime = 0.25f });
			}
		}

		private Pose RandomPose()
		{
			var half = m_args.Arena * 0.5f;
			var position = new Vector3(Random.Range(-half.x, half.x), Random.Range(-half.y, half.y), 0f);
			return new Pose(position, Quaternion.identity);
		}
	}
}
