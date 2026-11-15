using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using UnityEngine;

namespace Tekly.Curtains
{
    /// <summary>
    /// Draws shared curtains. A SharedCurtainTrack tells the stage a content canvas's look and progress, and
    /// the stage follows the progress:
    /// - At 0 and 2 the content is hidden.
    /// - In between a slot's camera renders the content into a screen sized texture, and the slot's overlay
    ///   canvas draws the curtain with that texture as its last band, at the content's sort order.
    /// - At exactly 1 the content renders normally.
    ///
    /// Slots are made on first use and reused, so a game that reveals one canvas at a time keeps one camera
    /// and one overlay canvas. They live under a hidden object that survives scene loads, and idle slots are
    /// disabled, so they cost nothing per frame. The stage only exists in play mode.
    /// </summary>
    public class CurtainStage : Singleton<CurtainStage>, IDisposable
    {
        private enum ContentState
        {
            None,
            Hidden,
            Live,
            Shown
        }

        private sealed class Entry
        {
            public CurtainLook Look;
            public float Progress = CurtainProgress.COVERED;
            public ContentState State;
            public CurtainSlot Slot;
        }

        // Far from anything else, and apart from each other, so each camera only sees its own canvas.
        private static readonly Vector3 s_origin = new Vector3(0, -100000, 0);
        private const float SLOT_SPACING = 1000f;

        private readonly Dictionary<Canvas, Entry> m_entries = new Dictionary<Canvas, Entry>();
        private readonly List<CurtainSlot> m_slots = new List<CurtainSlot>();
        private readonly List<Canvas> m_stale = new List<Canvas>();
        private GameObject m_root;

        /// <summary>
        /// Sets the look a content canvas is revealed with.
        /// </summary>
        public void SetLook(Canvas content, CurtainLook look)
        {
            var entry = GetEntry(content);
            entry.Look = look;
            entry.Slot?.Draw(look != null ? look.Style : null, entry.Progress);
        }

        /// <summary>
        /// A content canvas's progress. A canvas the stage hasn't seen is shown, so it reads as 1.
        /// </summary>
        public float GetProgress(Canvas content)
        {
            return m_entries.TryGetValue(content, out var entry) ? entry.Progress : CurtainProgress.COVERED;
        }

        /// <summary>
        /// Moves a content canvas's curtain: hides, captures or shows the content, then redraws.
        /// </summary>
        public void SetProgress(Canvas content, float progress)
        {
            var entry = GetEntry(content);
            entry.Progress = CurtainProgress.Clamp(progress);

            var state = StateFor(entry.Progress);

            if (state != entry.State) {
                ReleaseSlot(entry);
                entry.State = state;

                switch (state) {
                    case ContentState.Hidden:
                        content.enabled = false;
                        break;
                    case ContentState.Shown:
                        content.enabled = true;
                        break;
                    case ContentState.Live:
                        entry.Slot = AcquireSlot(content);
                        break;
                }
            }

            entry.Slot?.Draw(entry.Look != null ? entry.Look.Style : null, entry.Progress);
        }

        /// <summary>
        /// Forgets a content canvas, giving it back its own rendering if it is mid curtain.
        /// </summary>
        public void Forget(Canvas content)
        {
            if (m_entries.TryGetValue(content, out var entry)) {
                ReleaseSlot(entry);
                m_entries.Remove(content);
            }
        }

        public void Dispose()
        {
            foreach (var slot in m_slots) {
                slot.End();
            }

            m_slots.Clear();
            m_entries.Clear();

            if (m_root != null) {
                CurtainGraphic.DestroySafely(m_root);
                m_root = null;
            }
        }

        private static ContentState StateFor(float progress)
        {
            if (progress <= 0f || progress >= CurtainProgress.PASSED) {
                return ContentState.Hidden;
            }

            return Mathf.Approximately(progress, CurtainProgress.COVERED) ? ContentState.Shown : ContentState.Live;
        }

        private Entry GetEntry(Canvas content)
        {
            if (m_entries.TryGetValue(content, out var entry)) {
                return entry;
            }

            ForgetDestroyed();

            entry = new Entry();
            m_entries.Add(content, entry);
            return entry;
        }

        /// <summary>
        /// Drops canvases destroyed along with their scenes, freeing any slot they held.
        /// </summary>
        private void ForgetDestroyed()
        {
            m_stale.Clear();

            foreach (var pair in m_entries) {
                if (pair.Key == null) {
                    m_stale.Add(pair.Key);
                }
            }

            foreach (var content in m_stale) {
                ReleaseSlot(m_entries[content]);
                m_entries.Remove(content);
            }
        }

        private CurtainSlot AcquireSlot(Canvas content)
        {
            if (!content.isRootCanvas || content.renderMode == RenderMode.WorldSpace) {
                Debug.LogError($"A shared curtain can only reveal a screen space root canvas, not [{content.name}]", content);
                content.enabled = true;
                return null;
            }

            if (m_root == null) {
                m_root = new GameObject("Curtain Stage") {
                    hideFlags = HideFlags.HideAndDontSave
                };

                UnityEngine.Object.DontDestroyOnLoad(m_root);
                m_slots.Clear();
            }

            CurtainSlot slot = null;

            foreach (var candidate in m_slots) {
                if (!candidate.InUse) {
                    slot = candidate;
                    break;
                }
            }

            if (slot == null) {
                var position = s_origin + new Vector3(m_slots.Count * SLOT_SPACING, 0, 0);
                slot = new CurtainSlot(m_root.transform, position);
                m_slots.Add(slot);
            }

            slot.Begin(content);
            return slot;
        }

        private static void ReleaseSlot(Entry entry)
        {
            if (entry.Slot != null) {
                entry.Slot.End();
                entry.Slot = null;
            }
        }
    }

    /// <summary>
    /// A camera, a texture and an overlay canvas the stage lends to a moving shared curtain.
    /// </summary>
    internal sealed class CurtainSlot
    {
        public bool InUse => m_target != null;

        private readonly Camera m_camera;
        private readonly Canvas m_canvas;
        private readonly CurtainStageGraphic m_graphic;

        private RenderTexture m_texture;
        private Canvas m_target;
        private RenderMode m_renderMode;
        private Camera m_worldCamera;
        private float m_planeDistance;

        public CurtainSlot(Transform parent, Vector3 position)
        {
            var cameraObject = new GameObject("Curtain Camera") {
                hideFlags = HideFlags.HideAndDontSave
            };

            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.position = position;

            m_camera = cameraObject.AddComponent<Camera>();
            m_camera.enabled = false;
            m_camera.orthographic = true;
            m_camera.nearClipPlane = 0.1f;
            m_camera.farClipPlane = 10f;
            m_camera.clearFlags = CameraClearFlags.SolidColor;
            m_camera.backgroundColor = Color.clear;
            m_camera.allowHDR = false;
            m_camera.allowMSAA = false;

            // Renders before the cameras that show the scene.
            m_camera.depth = -100;

            var canvasObject = new GameObject("Curtain Canvas", typeof(RectTransform), typeof(Canvas)) {
                hideFlags = HideFlags.HideAndDontSave
            };

            canvasObject.transform.SetParent(parent, false);

            m_canvas = canvasObject.GetComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.enabled = false;

            var graphicObject = new GameObject("Curtain", typeof(RectTransform), typeof(CanvasRenderer)) {
                hideFlags = HideFlags.HideAndDontSave
            };

            var rect = (RectTransform) graphicObject.transform;
            rect.SetParent(canvasObject.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            m_graphic = graphicObject.AddComponent<CurtainStageGraphic>();
            m_graphic.maskable = false;
        }

        public void Draw(CurtainStyle style, float progress)
        {
            m_graphic.Draw(style, progress, m_texture);
        }

        public void Begin(Canvas target)
        {
            m_target = target;

            // URP's render graph refuses a camera target with no depth buffer. The canvas never reads depth,
            // so it is the smallest one and memoryless, which costs no memory on tile-based mobile GPUs.
            var descriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.ARGB32, 16) {
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear,
                useMipMap = false,
                msaaSamples = 1,
                memoryless = RenderTextureMemoryless.Depth
            };

            m_texture = RenderTexture.GetTemporary(descriptor);

            m_camera.cullingMask = 1 << target.gameObject.layer;
            m_camera.targetTexture = m_texture;
            m_camera.enabled = true;

            m_renderMode = target.renderMode;
            m_worldCamera = target.worldCamera;
            m_planeDistance = target.planeDistance;

            target.renderMode = RenderMode.ScreenSpaceCamera;
            target.worldCamera = m_camera;
            target.planeDistance = 1f;
            target.enabled = true;

            // Drawn where the content would be, so there is one sort order to think about.
            m_canvas.sortingLayerID = target.sortingLayerID;
            m_canvas.sortingOrder = target.sortingOrder;
            m_canvas.enabled = true;
        }

        public void End()
        {
            if (m_target != null) {
                m_target.renderMode = m_renderMode;
                m_target.worldCamera = m_worldCamera;
                m_target.planeDistance = m_planeDistance;
                m_target = null;
            }

            if (m_camera != null) {
                m_camera.enabled = false;
                m_camera.targetTexture = null;
            }

            if (m_canvas != null) {
                m_canvas.enabled = false;
            }

            if (m_graphic != null) {
                m_graphic.Draw(null, 0, null);
            }

            if (m_texture != null) {
                RenderTexture.ReleaseTemporary(m_texture);
                m_texture = null;
            }
        }
    }
}
