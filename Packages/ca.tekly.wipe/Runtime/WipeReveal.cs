using System;
using UnityEngine;

namespace Tekly.Wipes
{
    /// <summary>
    /// Wipes a full screen canvas in and out through a look, with the canvas's own content appearing
    /// through the shape while it and everything behind it stay live.
    ///
    /// Coverage is how much of the screen the content covers: 0 is none and 1 is all. Cover brings the
    /// content in and Uncover takes it out, from a Wipe Reveal track or from code.
    ///
    /// What it owns, so nothing else should set these on the canvas:
    /// - Canvas.enabled, always. At coverage 0 the canvas is disabled, otherwise it is enabled.
    /// - The canvas's render mode, camera and plane distance, only while a transition is part way. The
    ///   canvas then renders through a borrowed camera into a texture, and a borrowed overlay canvas draws
    ///   that texture through the look at the content's sort order. They are put back the moment the
    ///   transition reaches full coverage, or this component is disabled or destroyed.
    ///
    /// While it is part way the content and everything under it take no input.
    ///
    /// The canvas must be a screen space root canvas: Unity can only send a root canvas to a texture.
    /// Effects in the content that read the screen, such as a blur behind UI, don't work while it moves.
    ///
    /// In edit mode nothing is borrowed and the canvas is left alone. Scrubbing a Tweenline shows the look
    /// over the content instead, with the part not revealed yet dimmed, which is enough to tune the shape
    /// and timing.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [AddComponentMenu("UI/Tekly/Wipe Reveal")]
    public class WipeReveal : MonoBehaviour, IWipe
    {
        private enum State
        {
            None,
            Hidden,
            Live,
            Shown
        }

        public float Coverage {
            get => m_coverage;
            set => Set(value, m_leaving);
        }

        public bool Leaving {
            get => m_leaving;
            set => Set(m_coverage, value);
        }

        /// <summary>
        /// The shared look. Null uses the reveal's own style.
        /// </summary>
        public WipeLook Look {
            get => m_look;
            set {
                m_look = value;
                Apply();
            }
        }

        /// <summary>
        /// The style being drawn: a track's or a Cover call's look, else the shared look, else the reveal's
        /// own.
        /// </summary>
        public WipeStyle Style => m_playLook != null ? m_playLook.Style : BaseStyle;

        public WipeFocus Focus {
            get => m_focus;
            set => m_focus = value;
        }

        /// <summary>
        /// The size of the texture the content renders into while it moves, as a fraction of the screen.
        /// </summary>
        public float TextureScale {
            get => m_textureScale;
            set => m_textureScale = Mathf.Clamp(value, MIN_TEXTURE_SCALE, 1f);
        }

        /// <summary>
        /// True while the content is rendering through a texture.
        /// </summary>
        public bool IsTransitioning => m_state == State.Live;

        /// <summary>
        /// True while a Cover or Uncover call is running.
        /// </summary>
        public bool IsPlaying => m_playback.IsPlaying;

        private WipeStyle BaseStyle => m_look != null ? m_look.Style : m_style;

        /// <summary>
        /// True while something is moving the coverage, so the content keeps rendering through its texture
        /// even if an ease has overshot to full coverage. A track that stops evaluating stops counting.
        /// </summary>
        private bool IsMoving => m_playback.IsPlaying || (m_trackMoving && Time.frameCount - m_trackFrame <= 1);

        private const float MIN_TEXTURE_SCALE = 0.25f;

        // What the part not revealed yet is dimmed with in the edit mode preview.
        private static readonly Color s_previewBackdrop = new Color(0f, 0f, 0f, 0.75f);

        [Tooltip("A shared look. Empty uses the style below")]
        [SerializeField] private WipeLook m_look;

        [SerializeField] private WipeStyle m_style = new WipeStyle();

        [Tooltip("How much of the screen the content covers: 0 is none, and the canvas is disabled. 1 is all, and the canvas renders normally")]
        [SerializeField, Range(0, 1)] private float m_coverage = 1f;

        [SerializeField, HideInInspector] private bool m_leaving;

        [Tooltip("Centers a Radial, Angular or Shape field on a target instead of the look's center")]
        [SerializeField] private WipeFocus m_focus;

        [Tooltip("The size of the texture the content renders into while it moves, as a fraction of the screen. Lower it on low end targets")]
        [SerializeField, Range(MIN_TEXTURE_SCALE, 1f)] private float m_textureScale = 1f;

        [NonSerialized] private readonly WipePlayback m_playback = new WipePlayback();
        [NonSerialized] private Canvas m_canvas;
        [NonSerialized] private State m_state;
        [NonSerialized] private WipeSlot m_slot;
        [NonSerialized] private WipeLook m_playLook;
        [NonSerialized] private bool m_trackMoving;
        [NonSerialized] private int m_trackFrame;
        [NonSerialized] private bool m_watching;

        [NonSerialized] private GameObject m_preview;
        [NonSerialized] private Wipe m_previewWipe;
        [NonSerialized] private float m_appliedCoverage = -1f;
        [NonSerialized] private bool m_appliedLeaving;

        /// <summary>
        /// Brings the content in over the look's cover duration. Completes when it renders normally.
        /// </summary>
        public Awaitable Cover(WipeLook look = null)
        {
            return Play(look, true);
        }

        /// <summary>
        /// Takes the content out over the look's uncover duration. Completes when the canvas is disabled.
        /// </summary>
        public Awaitable Uncover(WipeLook look = null)
        {
            return Play(look, false);
        }

        /// <summary>
        /// Stops a running Cover or Uncover, leaving the coverage where it is.
        /// </summary>
        public void Stop()
        {
            m_playback.Stop();
        }

        public void Set(float coverage, bool leaving)
        {
            coverage = Mathf.Clamp01(coverage);

            if (m_coverage == coverage && m_leaving == leaving && m_state != State.None) {
                // A clip or play that just finished still has to settle on normal rendering
                if (m_state == State.Live && !IsMoving) {
                    Apply();
                }

                return;
            }

            m_coverage = coverage;
            m_leaving = leaving;
            Apply();
        }

        public WipeStyle StyleFor(WipeLook look)
        {
            return look != null ? look.Style : BaseStyle;
        }

        public void SetTrackState(WipeLook look, bool moving)
        {
            m_playLook = look;
            m_trackMoving = moving;
            m_trackFrame = Time.frameCount;
        }

        /// <summary>
        /// What is wrong with the setup, or null when it works.
        /// </summary>
        public string GetWarning()
        {
            var canvas = GetComponent<Canvas>();

            if (canvas != null && (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)) {
                return "A Wipe Reveal needs a screen space root canvas. Unity can only send a root canvas to a texture";
            }

            return BaseStyle.GetWarning();
        }

        private void OnEnable()
        {
            m_canvas = GetComponent<Canvas>();
            m_state = State.None;
            Apply();
            UpdateWatching();
        }

        private void OnDisable()
        {
            m_playback.Stop();
            Release();
            DestroyPreview();

            // Without the reveal the canvas is simply shown
            if (Application.isPlaying && m_state == State.Hidden && m_canvas != null) {
                m_canvas.enabled = true;
            }

            m_state = State.None;
            m_playLook = null;
            m_trackMoving = false;
            UpdateWatching();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (isActiveAndEnabled && m_canvas != null) {
                Apply();
            }
        }
#endif

        private Awaitable Play(WipeLook look, bool cover)
        {
            if (look != null) {
                m_playLook = look;
            }

            return m_playback.Play(this, Style, cover, destroyCancellationToken);
        }

        /// <summary>
        /// Follows the coverage: hides, borrows a slot for or shows the content, then redraws.
        /// </summary>
        private void Apply()
        {
            if (!isActiveAndEnabled || m_canvas == null) {
                return;
            }

            m_appliedCoverage = m_coverage;
            m_appliedLeaving = m_leaving;

            if (!Application.isPlaying) {
                ApplyPreview();
                return;
            }

            var state = m_coverage <= 0f ? State.Hidden : m_coverage >= 1f && !IsMoving ? State.Shown : State.Live;

            if (state != m_state) {
                Release();
                m_state = state;

                switch (state) {
                    case State.Hidden:
                        m_canvas.enabled = false;
                        break;
                    case State.Shown:
                        m_canvas.enabled = true;
                        break;
                    case State.Live:
                        m_canvas.enabled = true;
                        Borrow();
                        break;
                }

                UpdateWatching();
            }

            m_slot?.Draw(Style, m_coverage, m_leaving, m_focus);
        }

        private void Borrow()
        {
            if (!m_canvas.isRootCanvas || m_canvas.renderMode == RenderMode.WorldSpace) {
                Debug.LogError($"The Wipe Reveal on [{name}] needs a screen space root canvas, so its content is shown without a transition", this);
                return;
            }

            var stage = WipeStage.Instance;

            if (stage != null) {
                m_slot = stage.Acquire(m_canvas, m_textureScale);
            }
        }

        private void Release()
        {
            if (m_slot != null) {
                m_slot.End();
                m_slot = null;
            }
        }

        /// <summary>
        /// In edit mode the canvas is left alone. While a Tweenline preview runs, the look is drawn over the
        /// content with the part not revealed yet dimmed.
        /// </summary>
        private void ApplyPreview()
        {
#if UNITY_EDITOR
            var previewing = UnityEditor.AnimationMode.InAnimationMode() && m_coverage < 1f;

            if (!previewing) {
                DestroyPreview();
                return;
            }

            if (m_preview == null) {
                m_preview = new GameObject("Wipe Reveal Preview", typeof(RectTransform), typeof(Canvas)) {
                    hideFlags = HideFlags.HideAndDontSave
                };

                var canvas = m_preview.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingLayerID = m_canvas.sortingLayerID;
                canvas.sortingOrder = m_canvas.sortingOrder + 1;

                m_previewWipe = WipeSlot.CreateWipe(m_preview.transform);
                m_previewWipe.raycastTarget = false;
            }

            m_previewWipe.Focus = m_focus;
            m_previewWipe.Set(m_coverage, m_leaving);
            m_previewWipe.Drive(Style, null, s_previewBackdrop);
#endif
        }

        private void DestroyPreview()
        {
            if (m_preview != null) {
                Wipe.DestroySafely(m_preview);
                m_preview = null;
                m_previewWipe = null;
            }
        }

        /// <summary>
        /// Watches the canvas render while there is something to pick up that no setter reports: a clip
        /// that stopped part way through an overshoot, a focus that moves, or in edit mode a previewed
        /// value that was put back behind the reveal's back.
        /// </summary>
        private void UpdateWatching()
        {
            var watch = isActiveAndEnabled && (m_state == State.Live || !Application.isPlaying);

            if (watch == m_watching) {
                return;
            }

            m_watching = watch;

            if (watch) {
                Canvas.willRenderCanvases += OnWillRenderCanvases;
            } else {
                Canvas.willRenderCanvases -= OnWillRenderCanvases;
            }
        }

        private void OnWillRenderCanvases()
        {
            if (this == null) {
                Canvas.willRenderCanvases -= OnWillRenderCanvases;
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying) {
                // Stopping a Tweenline preview restores the serialized values without calling a setter,
                // and the clip's look is no longer wanted.
                var inPreview = UnityEditor.AnimationMode.InAnimationMode();

                if (!inPreview) {
                    m_playLook = null;
                }

                if (m_coverage != m_appliedCoverage || m_leaving != m_appliedLeaving || (!inPreview && m_preview != null)) {
                    Apply();
                }

                return;
            }
#endif

            if (m_state != State.Live) {
                return;
            }

            // At full coverage with nothing moving it any more, go back to normal rendering
            if (m_coverage >= 1f && !IsMoving) {
                Apply();
            } else if (m_focus != null && m_focus.Moves) {
                m_slot?.Draw(Style, m_coverage, m_leaving, m_focus);
            }
        }
    }
}
