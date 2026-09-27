using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements.Animators
{
	public enum LeafColorBlend
	{
		/// Replaces the color from earlier layers.
		Override,
		/// Multiplies the color from earlier layers (e.g. half alpha while disabled).
		Multiply,
	}

	/// <summary>
	/// Colors used while all of <see cref="Flags"/> are set.
	/// </summary>
	[Serializable]
	public class LeafColorLayer
	{
		public LeafElementFlags Flags => m_flags;

		[Tooltip("The layer is used while all of these flags are set")]
		[SerializeField] private LeafElementFlags m_flags;
		[SerializeField] private LeafColorBlend m_blend;
		[SerializeField] private LeafColorBlock m_colors = LeafColorBlock.Default;

		public bool Matches(LeafElementState state) => state.HasAll(m_flags);

		public Color Apply(Color color, LeafElementMode mode)
		{
			var layerColor = m_colors.GetColor(mode);
			return m_blend == LeafColorBlend.Multiply ? color * layerColor : layerColor;
		}
	}

	/// <summary>
	/// Drives the CanvasRenderer color of one Graphic. The color is tracked here rather than read back
	/// from the renderer, so fades always start from the right color, keep running while the Graphic is
	/// hidden, and a renderer reset (scene save, undo) can be repaired.
	/// </summary>
	[Serializable]
	public class LeafColorTarget
	{
		public Graphic Target => m_target;

		[SerializeField] private Graphic m_target;

		[Tooltip("Base colors, used when no layer overrides them")]
		[SerializeField] private LeafColorBlock m_colors = LeafColorBlock.Default;

		[Tooltip("Applied in order on top of the base colors when their flags match. Put Disabled last")]
		[SerializeField] private LeafColorLayer[] m_layers = Array.Empty<LeafColorLayer>();

		[SerializeField, Min(0f)] private float m_fadeDuration = 0.1f;

		[NonSerialized] private bool m_hasColor;
		[NonSerialized] private bool m_isFading;
		[NonSerialized] private Color m_current;
		[NonSerialized] private Color m_from;
		[NonSerialized] private Color m_goal;
		[NonSerialized] private float m_elapsed;

		public bool IsFading => m_isFading;

		public Color Evaluate(LeafElementState state)
		{
			var color = m_colors.GetColor(state.Mode);

			for (var i = 0; i < m_layers.Length; i++) {
				var layer = m_layers[i];
				if (layer != null && layer.Matches(state)) {
					color = layer.Apply(color, state.Mode);
				}
			}

			return color;
		}

		public void SetState(LeafElementState state, bool instant)
		{
			var goal = Evaluate(state);

			if (instant || !m_hasColor || m_fadeDuration <= 0f) {
				m_current = goal;
				m_goal = goal;
				m_isFading = false;
				m_hasColor = true;
				Write();
				return;
			}

			if (goal == m_goal) {
				return;
			}

			m_from = m_current;
			m_goal = goal;
			m_elapsed = 0f;
			m_isFading = true;
		}

		public void Tick(float deltaTime)
		{
			if (!m_isFading) {
				return;
			}

			m_elapsed += deltaTime;
			var t = Mathf.Clamp01(m_elapsed / m_fadeDuration);
			m_current = Color.LerpUnclamped(m_from, m_goal, t);

			if (t >= 1f) {
				m_current = m_goal;
				m_isFading = false;
			}

			Write();
		}

		/// <summary>
		/// Rewrites the tracked color if something else changed the renderer's color.
		/// </summary>
		public void Repair()
		{
			if (!m_hasColor || m_target == null || !m_target.isActiveAndEnabled) {
				return;
			}

			if (m_target.canvasRenderer.GetColor() != m_current) {
				Write();
			}
		}

		private void Write()
		{
			if (m_target != null) {
				m_target.canvasRenderer.SetColor(m_current);
			}
		}
	}

	/// <summary>
	/// Shows a GameObject only while all of <see cref="m_flags"/> are set.
	/// </summary>
	[Serializable]
	public class LeafActiveTarget
	{
		[SerializeField] private GameObject m_target;

		[Tooltip("The target is only active while all of these flags are set. Nothing means always active")]
		[SerializeField] private LeafElementFlags m_flags;

		/// <summary>
		/// Returns true if this showed an object that was hidden.
		/// </summary>
		public bool SetState(LeafElementState state)
		{
			if (m_target == null) {
				return false;
			}

			var wasActive = m_target.activeSelf;
			var isActive = state.HasAll(m_flags);
			m_target.SetActive(isActive);

			return isActive && !wasActive;
		}
	}

	/// <summary>
	/// Tints graphics by state, like Unity's ColorTint transition but aware of <see cref="LeafElementFlags"/>.
	/// Each Graphic gets one target: base colors per mode plus layers that apply when their flags match
	/// (e.g. Selected, On, then Disabled last). Fades are run here with unscaled time, not with
	/// Graphic.CrossFadeColor, so they don't depend on the renderer's current color or on coroutines.
	/// </summary>
	[ExecuteAlways]
	public class LeafAnimatorColors : LeafAnimator
	{
		[Tooltip("One per Graphic")]
		[SerializeField] private LeafColorTarget[] m_targets = Array.Empty<LeafColorTarget>();

		[Tooltip("GameObjects shown or hidden by state")]
		[SerializeField] private LeafActiveTarget[] m_activeTargets = Array.Empty<LeafActiveTarget>();

		protected override void OnHandleState(LeafElementState current, LeafElementState previous, bool instant)
		{
			var showedObjects = false;
			for (var i = 0; i < m_activeTargets.Length; i++) {
				var activeTarget = m_activeTargets[i];
				if (activeTarget != null && activeTarget.SetState(current)) {
					showedObjects = true;
				}
			}

#if UNITY_EDITOR
			if (m_hasPreview) {
				return;
			}
#endif

			ApplyColors(current, instant);

			// A Graphic that was just shown may not have been written while hidden
			if (showedObjects) {
				RepairTargets();
			}
		}

		private void ApplyColors(LeafElementState state, bool instant)
		{
			// No fades outside play mode: LateUpdate only runs in edit mode when something changes
			instant |= !Application.isPlaying;

			for (var i = 0; i < m_targets.Length; i++) {
				m_targets[i]?.SetState(state, instant);
			}
		}

		private void OnEnable()
		{
			RepairTargets();

#if UNITY_EDITOR
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaved += OnSceneSaved;
			UnityEditor.Undo.undoRedoPerformed += QueueEditorRefresh;
#endif
		}

		private void OnDisable()
		{
#if UNITY_EDITOR
			UnityEditor.SceneManagement.EditorSceneManager.sceneSaved -= OnSceneSaved;
			UnityEditor.Undo.undoRedoPerformed -= QueueEditorRefresh;
#endif
		}

		private void LateUpdate()
		{
			var deltaTime = Time.unscaledDeltaTime;
			for (var i = 0; i < m_targets.Length; i++) {
				m_targets[i]?.Tick(deltaTime);
			}
		}

		private void RepairTargets()
		{
			for (var i = 0; i < m_targets.Length; i++) {
				m_targets[i]?.Repair();
			}
		}

#if UNITY_EDITOR
		[NonSerialized] private bool m_hasPreview;
		[NonSerialized] private LeafElementState m_previewState;
		[NonSerialized] private bool m_refreshQueued;

		public bool EditorHasPreview => m_hasPreview;

		/// <summary>
		/// Shows the colors for <paramref name="state"/> without changing the element's real state.
		/// Only colors are previewed; active targets are left alone so nothing gets saved into the scene.
		/// </summary>
		public void EditorSetPreview(LeafElementState state)
		{
			m_hasPreview = true;
			m_previewState = state;
			ApplyColors(state, true);
			RepaintViews();
		}

		public void EditorClearPreview()
		{
			if (!m_hasPreview) {
				return;
			}

			m_hasPreview = false;
			Refresh();
			RepaintViews();
		}

		private void OnValidate()
		{
			// Renderers and SetActive can't be touched from OnValidate, so apply once the change is done
			QueueEditorRefresh();
		}

		/// <summary>
		/// Re-applies the current (or previewed) state on the next editor tick, once per tick at most.
		/// Uses delayCall rather than LateUpdate, which only runs in edit mode when Unity thinks something changed.
		/// </summary>
		private void QueueEditorRefresh()
		{
			if (m_refreshQueued) {
				return;
			}

			m_refreshQueued = true;
			UnityEditor.EditorApplication.delayCall += RunEditorRefresh;
		}

		private void RunEditorRefresh()
		{
			m_refreshQueued = false;

			// The object may have been destroyed or disabled since the refresh was queued
			if (this == null || !isActiveAndEnabled) {
				return;
			}

			Refresh();
			RepaintViews();
		}

		private void Refresh()
		{
			if (m_hasPreview) {
				ApplyColors(m_previewState, true);
			} else if (HasState) {
				OnHandleState(CurrentState, CurrentState, true);
			} else {
				ApplyColors(LeafElementState.Default, true);
			}
		}

		private void OnSceneSaved(UnityEngine.SceneManagement.Scene scene)
		{
			// Saving can reset renderer colors after this callback, so repair once the save is done
			UnityEditor.EditorApplication.delayCall += () => {
				if (this != null && isActiveAndEnabled) {
					RepairTargets();
					RepaintViews();
				}
			};
		}

		/// <summary>
		/// Renderer colors changed outside the player loop; ask the Scene and Game views to redraw.
		/// </summary>
		private static void RepaintViews()
		{
			UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
		}
#endif
	}
}
