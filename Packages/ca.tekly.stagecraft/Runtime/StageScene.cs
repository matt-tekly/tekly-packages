using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using Tekly.Logging;
using UnityEngine;
using UnityEngine.Pool;

namespace Tekly.Stagecraft
{
	/// <summary>
	/// The scene-placed actors of one scene. Saving the scene bakes the list (ISceneBaker), so the runtime never
	/// searches the scene. On load it runs before everything else and deactivates those actors, so none of them gets
	/// an Awake or OnEnable before the stage activates it, exactly like a pooled instance. Game code then calls
	/// <see cref="Adopt"/> to hand them to a stage and <see cref="Activate"/> to bring them alive, or activates and
	/// despawns them one by one through the stage when restoring a save, finding them by <see cref="SceneActorId"/>.
	/// Scene actors have no pool: despawning one destroys it, and a level reset is a scene reload.
	/// </summary>
	[DefaultExecutionOrder(-9000)]
	[DisallowMultipleComponent]
	public sealed class StageScene : MonoBehaviour, ISceneBaker
	{
		/// <summary>The stage the actors were adopted into, or null before <see cref="Adopt"/>.</summary>
		public Stage Stage { get; private set; }

		/// <summary>The baked actors, in hierarchy order, with their persistent ids.</summary>
		public IReadOnlyList<SceneActorEntry> Entries => m_entries ??= Array.Empty<SceneActorEntry>();

		[SerializeField] private SceneActorEntry[] m_entries;

		private readonly Dictionary<SceneActorId, Actor> m_byId = new Dictionary<SceneActorId, Actor>();
		private readonly TkLogger m_logger = TkLogger.Get<StageScene>();

		private void Awake()
		{
			// Before any other script's Awake in this scene: the actors sleep until the stage wakes them
			var entries = Entries;
			for (var i = 0; i < entries.Count; i++) {
				var actor = entries[i].Actor;
				if (actor != null) {
					actor.gameObject.SetActive(false);
				}
			}
		}

		/// <summary>
		/// Hands every baked actor to the stage: injected, OnCreated run, left Acquired and inactive. Once per scene.
		/// </summary>
		public void Adopt(Stage stage)
		{
			if (stage == null) {
				throw new ArgumentNullException(nameof(stage));
			}

			if (Stage != null) {
				m_logger.Error("StageScene [{scene}] already adopted into stage [{stage}]", ("scene", gameObject.scene.name), ("stage", Stage.Name));
				return;
			}

			Stage = stage;

			var entries = Entries;
			for (var i = 0; i < entries.Count; i++) {
				var entry = entries[i];
				if (entry.Actor == null) {
					m_logger.Warning("StageScene [{scene}]: baked actor [{id}] is missing. Save the scene to rebake.", ("scene", gameObject.scene.name), ("id", entry.Id));
					continue;
				}

				stage.Adopt(entry.Actor);
				m_byId[entry.Id] = entry.Actor;
			}
		}

		/// <summary>
		/// Activates every adopted actor that is still Acquired, at its placed pose. Actors already activated, despawned
		/// or destroyed (by a save restore, say) are skipped. Returns how many were activated.
		/// </summary>
		public int Activate(SpawnReason reason = SpawnReason.Placed)
		{
			if (Stage == null) {
				m_logger.Error("StageScene [{scene}]: Activate before Adopt", ("scene", gameObject.scene.name));
				return 0;
			}

			var activated = 0;
			foreach (var actor in m_byId.Values) {
				if (actor == null || actor.State != ActorState.Acquired) {
					continue;
				}

				var transform = actor.Transform;
				Stage.Activate(actor, new Pose(transform.position, transform.rotation), reason);
				activated++;
			}

			return activated;
		}

		/// <summary>The adopted actor with this persistent id, in any state short of destroyed.</summary>
		public bool TryGet(SceneActorId id, out Actor actor)
		{
			if (m_byId.TryGetValue(id, out actor) && actor != null) {
				return true;
			}

			actor = null;
			return false;
		}

		/// <summary>
		/// Scene save: every Actor in the scene, in hierarchy order, keeping the id of each one that was already baked
		/// and minting ids for the rest. Actors nested under another actor are not actors of their own and are skipped.
		/// </summary>
		public void Bake(SceneBake sceneBake)
		{
			var roots = sceneBake.Scene.GetRootGameObjects();

			using (ListPool<Actor>.Get(out var found))
			using (ListPool<Actor>.Get(out var scratch))
			using (ListPool<StageScene>.Get(out var stages))
			using (ListPool<StageScene>.Get(out var stageScratch)) {
				foreach (var root in roots) {
					root.GetComponentsInChildren(true, scratch);
					found.AddRange(scratch);
					root.GetComponentsInChildren(true, stageScratch);
					stages.AddRange(stageScratch);
				}

				if (stages.Count > 1) {
					m_logger.ErrorContext("Scene [{scene}] has [{count}] StageScenes; one per scene", this, ("scene", sceneBake.Scene.name), ("count", stages.Count));
				}

				var previous = Entries;
				var entries = new List<SceneActorEntry>(found.Count);

				for (var i = 0; i < found.Count; i++) {
					var actor = found[i];
					var parent = actor.transform.parent;
					if (parent != null && parent.GetComponentInParent<Actor>(true) != null) {
						m_logger.WarningContext("Actor [{actor}] is nested under another actor and is not baked", actor, ("actor", actor.name));
						continue;
					}

					entries.Add(new SceneActorEntry(actor, FindId(previous, actor)));
				}

				m_entries = entries.ToArray();
			}

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}

		private static SceneActorId FindId(IReadOnlyList<SceneActorEntry> previous, Actor actor)
		{
			for (var i = 0; i < previous.Count; i++) {
				if (ReferenceEquals(previous[i].Actor, actor) && !previous[i].Id.IsNone) {
					return previous[i].Id;
				}
			}

			return SceneActorId.New();
		}

		// For the tests, which stand in for the bake
		internal void SetEntries(SceneActorEntry[] entries)
		{
			m_entries = entries;
		}
	}

	/// <summary>One scene-placed actor as baked by its <see cref="StageScene"/>: the actor and its persistent id.</summary>
	[Serializable]
	public struct SceneActorEntry
	{
		public Actor Actor;
		public SceneActorId Id;

		public SceneActorEntry(Actor actor, SceneActorId id)
		{
			Actor = actor;
			Id = id;
		}
	}

	/// <summary>
	/// The persistent identity of a scene-placed actor: a random value minted when the scene is baked and kept
	/// across rebakes for as long as the actor exists. It lives in the <see cref="StageScene"/>'s baked list, not on
	/// the actor, so a duplicated actor gets a fresh id and the prefab stays untouched. This is what a save stores;
	/// <see cref="ActorId"/> is local to a run and never is.
	/// </summary>
	[Serializable]
	public struct SceneActorId : IEquatable<SceneActorId>
	{
		public static readonly SceneActorId None = default;

		public ulong Value => m_value;
		public bool IsNone => m_value == 0;

		[SerializeField] private ulong m_value;

		public SceneActorId(ulong value)
		{
			m_value = value;
		}

		/// <summary>A new random id. Never <see cref="None"/>.</summary>
		public static SceneActorId New()
		{
			ulong value;
			do {
				value = BitConverter.ToUInt64(Guid.NewGuid().ToByteArray(), 0);
			} while (value == 0);

			return new SceneActorId(value);
		}

		public bool Equals(SceneActorId other)
		{
			return m_value == other.m_value;
		}

		public override bool Equals(object obj)
		{
			return obj is SceneActorId other && Equals(other);
		}

		public override int GetHashCode()
		{
			return m_value.GetHashCode();
		}

		public static bool operator ==(SceneActorId a, SceneActorId b)
		{
			return a.m_value == b.m_value;
		}

		public static bool operator !=(SceneActorId a, SceneActorId b)
		{
			return a.m_value != b.m_value;
		}

		public override string ToString()
		{
			return m_value.ToString("x16");
		}
	}
}
