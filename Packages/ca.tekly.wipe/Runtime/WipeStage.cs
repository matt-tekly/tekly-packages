using System;
using System.Collections.Generic;
using Tekly.Common.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Wipes
{
    /// <summary>
    /// Lends Wipe Reveals what they need while they move: a camera that renders the content canvas into a
    /// texture, and an overlay canvas with a Wipe that draws that texture as its fill.
    ///
    /// Slots are made on first use and reused, so a game that reveals one canvas at a time keeps one camera
    /// and one overlay canvas. They live under a hidden object that survives scene loads, and idle slots
    /// are disabled, so they cost nothing per frame. The stage only exists in play mode.
    /// </summary>
    internal class WipeStage : Singleton<WipeStage>, IDisposable
    {
        // Far from anything else, and apart from each other, so each camera only sees its own canvas.
        private static readonly Vector3 s_origin = new Vector3(0, -100000, 0);
        private const float SLOT_SPACING = 1000f;

        private readonly List<WipeSlot> m_slots = new List<WipeSlot>();
        private GameObject m_root;

        /// <summary>
        /// A slot rendering the content canvas into a texture at a scale of the screen's size.
        /// </summary>
        public WipeSlot Acquire(Canvas content, float textureScale)
        {
            if (m_root == null) {
                m_root = new GameObject("Wipe Stage") {
                    hideFlags = HideFlags.HideAndDontSave
                };

                UnityEngine.Object.DontDestroyOnLoad(m_root);
                m_slots.Clear();
            }

            WipeSlot slot = null;

            foreach (var candidate in m_slots) {
                if (!candidate.InUse) {
                    slot = candidate;
                    break;
                }
            }

            if (slot == null) {
                var position = s_origin + new Vector3(m_slots.Count * SLOT_SPACING, 0, 0);
                slot = new WipeSlot(m_root.transform, position);
                m_slots.Add(slot);
            }

            slot.Begin(content, textureScale);
            return slot;
        }

        public void Dispose()
        {
            foreach (var slot in m_slots) {
                slot.End();
            }

            m_slots.Clear();

            if (m_root != null) {
                Wipe.DestroySafely(m_root);
                m_root = null;
            }
        }
    }

    /// <summary>
    /// A camera, a texture and an overlay canvas with a Wipe. While in use it owns the content canvas's
    /// render mode, camera and plane distance, and End puts them back.
    /// </summary>
    internal sealed class WipeSlot
    {
        public bool InUse => m_target != null;

        private readonly Camera m_camera;
        private readonly Canvas m_canvas;
        private readonly Wipe m_wipe;

        private RenderTexture m_texture;
        private Canvas m_target;
        private RenderMode m_renderMode;
        private Camera m_worldCamera;
        private float m_planeDistance;

        public WipeSlot(Transform parent, Vector3 position)
        {
            var cameraObject = new GameObject("Wipe Camera") {
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

            // The raycaster makes the wipe block input to the content and everything under it while it moves.
            var canvasObject = new GameObject("Wipe Canvas", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster)) {
                hideFlags = HideFlags.HideAndDontSave
            };

            canvasObject.transform.SetParent(parent, false);

            m_canvas = canvasObject.GetComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.enabled = false;

            m_wipe = CreateWipe(canvasObject.transform);
        }

        /// <summary>
        /// A stretched Wipe under a canvas, for a slot or an edit mode preview.
        /// </summary>
        public static Wipe CreateWipe(Transform canvas)
        {
            var wipeObject = new GameObject("Wipe", typeof(RectTransform), typeof(CanvasRenderer)) {
                hideFlags = HideFlags.HideAndDontSave
            };

            var rect = (RectTransform) wipeObject.transform;
            rect.SetParent(canvas, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var wipe = wipeObject.AddComponent<Wipe>();
            wipe.maskable = false;

            // The wipe has the canvas to itself, so trimming its quad costs nothing else a rebuild
            wipe.Trim = true;
            return wipe;
        }

        public void Draw(WipeStyle style, float coverage, bool leaving, WipeFocus focus)
        {
            m_wipe.Focus = focus;
            m_wipe.Set(coverage, leaving);
            m_wipe.Drive(style, m_texture, Color.clear);
        }

        public void Begin(Canvas target, float textureScale)
        {
            m_target = target;

            var width = Mathf.Max(1, Mathf.RoundToInt(Screen.width * textureScale));
            var height = Mathf.Max(1, Mathf.RoundToInt(Screen.height * textureScale));

            // URP's render graph refuses a camera target with no depth buffer. The canvas never reads depth,
            // so it is the smallest one and memoryless, which costs no memory on tile-based mobile GPUs.
            var descriptor = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 16) {
                sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear,
                useMipMap = false,
                msaaSamples = 1,
                memoryless = RenderTextureMemoryless.Depth
            };

            m_texture = RenderTexture.GetTemporary(descriptor);

            m_camera.cullingMask = 1 << target.gameObject.layer;
            m_camera.targetTexture = m_texture;

            // A camera's aspect follows the screen, not its target, and in the editor that can be a different
            // window from the Game view the texture is sized from. A mismatch stretches the content.
            m_camera.aspect = (float) width / height;
            m_camera.enabled = true;

            m_renderMode = target.renderMode;
            m_worldCamera = target.worldCamera;
            m_planeDistance = target.planeDistance;

            target.renderMode = RenderMode.ScreenSpaceCamera;
            target.worldCamera = m_camera;
            target.planeDistance = 1f;

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
            }

            m_target = null;

            if (m_camera != null) {
                m_camera.enabled = false;
                m_camera.targetTexture = null;
                m_camera.ResetAspect();
            }

            if (m_canvas != null) {
                m_canvas.enabled = false;
            }

            if (m_wipe != null) {
                m_wipe.Focus = null;
                m_wipe.Set(0f, false);
                m_wipe.Drive(null, null, Color.clear);
            }

            if (m_texture != null) {
                RenderTexture.ReleaseTemporary(m_texture);
                m_texture = null;
            }
        }
    }
}
