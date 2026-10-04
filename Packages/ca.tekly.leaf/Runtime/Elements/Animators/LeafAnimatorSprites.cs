using System;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Leaf.Elements.Animators
{
	/// <summary>
	/// Sprites per <see cref="LeafElementMode"/>. A null sprite means "not set".
	/// </summary>
	[Serializable]
	public struct LeafSpriteBlock
	{
		public Sprite Normal;
		public Sprite Highlighted;
		public Sprite Pressed;

		public readonly Sprite GetSprite(LeafElementMode mode)
		{
			return mode switch {
				LeafElementMode.Normal => Normal,
				LeafElementMode.Highlighted => Highlighted,
				LeafElementMode.Pressed => Pressed,
				_ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
			};
		}
	}

	/// <summary>
	/// Sprites used while all of <see cref="Flags"/> are set. Unset sprites leave earlier layers alone,
	/// so an On layer can swap just the Normal icon.
	/// </summary>
	[Serializable]
	public class LeafSpriteLayer
	{
		public LeafElementFlags Flags => m_flags;

		[Tooltip("The layer is used while all of these flags are set")]
		[SerializeField] private LeafElementFlags m_flags;

		[Tooltip("Unset sprites fall back to this layer's Normal sprite, then to earlier layers")]
		[SerializeField] private LeafSpriteBlock m_sprites;

		public bool Matches(LeafElementState state) => state.HasAll(m_flags);

		public Sprite Apply(Sprite sprite, LeafElementMode mode)
		{
			var layerSprite = m_sprites.GetSprite(mode);
			if (layerSprite == null) {
				layerSprite = m_sprites.Normal;
			}

			return layerSprite != null ? layerSprite : sprite;
		}
	}

	/// <summary>
	/// Swaps the sprite on one Image by state.
	/// </summary>
	[Serializable]
	public class LeafSpriteTarget
	{
		public Image Target => m_target;

		[SerializeField] private Image m_target;

		[Tooltip("Base sprites. Unset Highlighted/Pressed fall back to Normal")]
		[SerializeField] private LeafSpriteBlock m_sprites;

		[Tooltip("Applied in order on top of the base sprites when their flags match")]
		[SerializeField] private LeafSpriteLayer[] m_layers = Array.Empty<LeafSpriteLayer>();

		public Sprite Evaluate(LeafElementState state)
		{
			var sprite = m_sprites.GetSprite(state.Mode);
			if (sprite == null) {
				sprite = m_sprites.Normal;
			}

			for (var i = 0; i < m_layers.Length; i++) {
				var layer = m_layers[i];
				if (layer != null && layer.Matches(state)) {
					sprite = layer.Apply(sprite, state.Mode);
				}
			}

			return sprite;
		}

		public void SetState(LeafElementState state)
		{
			if (m_target == null) {
				return;
			}

			var sprite = Evaluate(state);

			// Nothing configured for this state; leave whatever the Image has
			if (sprite == null || m_target.sprite == sprite) {
				return;
			}

			m_target.sprite = sprite;
		}
	}

	/// <summary>
	/// Swaps Image sprites (icons, backgrounds) by state. Sprite swaps are instant.
	/// Combine with <see cref="LeafAnimatorColors"/> using a <see cref="LeafAnimatorGroup"/>.
	/// </summary>
	[ExecuteAlways]
	public class LeafAnimatorSprites : LeafAnimator
	{
		[Tooltip("One per Image")]
		[SerializeField] private LeafSpriteTarget[] m_targets = Array.Empty<LeafSpriteTarget>();

		protected override void OnHandleState(LeafElementState current, LeafElementState previous, bool instant)
		{
			ApplySprites(current);
		}

		private void ApplySprites(LeafElementState state)
		{
			for (var i = 0; i < m_targets.Length; i++) {
				m_targets[i]?.SetState(state);
			}
		}

#if UNITY_EDITOR
		[NonSerialized] private bool m_refreshQueued;

		private void OnValidate()
		{
			// Image.sprite can't be safely changed from OnValidate, so apply once the change is done
			if (m_refreshQueued) {
				return;
			}

			m_refreshQueued = true;
			UnityEditor.EditorApplication.delayCall += RunEditorRefresh;
		}

		private void RunEditorRefresh()
		{
			m_refreshQueued = false;

			if (this == null || !isActiveAndEnabled) {
				return;
			}

			ApplySprites(HasState ? CurrentState : LeafElementState.Default);
		}
#endif
	}
}
