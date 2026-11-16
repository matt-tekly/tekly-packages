using UnityEngine;

namespace Tekly.Wipes
{
    /// <summary>
    /// Where a wipe is centered, for an iris that closes on a character. A Wipe or Wipe Reveal that
    /// references one uses it instead of its look's center, and follows it while a transition is part way.
    ///
    /// Set the target from code before covering:
    ///     focus.Target = player.transform;
    /// </summary>
    public class WipeFocus : MonoBehaviour
    {
        /// <summary>
        /// What the wipe centers on. A RectTransform is followed on its canvas and anything else through
        /// the camera. Null uses the point.
        /// </summary>
        public Transform Target {
            get => m_target;
            set => m_target = value;
        }

        /// <summary>
        /// The camera a target that isn't UI is seen through. Null uses the main camera.
        /// </summary>
        public Camera Camera {
            get => m_camera;
            set => m_camera = value;
        }

        /// <summary>
        /// The center when there is no target, from (0, 0) at the bottom left of the screen to (1, 1) at
        /// the top right.
        /// </summary>
        public Vector2 Point {
            get => m_point;
            set => m_point = value;
        }

        /// <summary>
        /// True when the focus can move from frame to frame.
        /// </summary>
        public bool Moves => m_target != null;

        [Tooltip("What the wipe centers on. Empty uses the point")]
        [SerializeField] private Transform m_target;

        [Tooltip("The camera a target that isn't UI is seen through. Empty uses the main camera")]
        [SerializeField] private Camera m_camera;

        [Tooltip("The center when there is no target, from (0, 0) at the bottom left of the screen to (1, 1) at the top right")]
        [SerializeField] private Vector2 m_point = new Vector2(0.5f, 0.5f);

        /// <summary>
        /// Centers on a world or UI object.
        /// </summary>
        public void SetTarget(Transform target, Camera camera = null)
        {
            m_target = target;
            m_camera = camera;
        }

        /// <summary>
        /// Centers on a fixed point in screen pixels.
        /// </summary>
        public void SetScreenPoint(Vector2 screenPoint)
        {
            m_target = null;
            m_point = new Vector2(screenPoint.x / Mathf.Max(Screen.width, 1), screenPoint.y / Mathf.Max(Screen.height, 1));
        }

        /// <summary>
        /// The focus in screen pixels. False when a target needs a camera and there is none.
        /// </summary>
        public bool TryGetScreenPoint(out Vector2 screenPoint)
        {
            if (m_target == null) {
                screenPoint = new Vector2(m_point.x * Screen.width, m_point.y * Screen.height);
                return true;
            }

            if (m_target is RectTransform) {
                var canvas = m_target.GetComponentInParent<Canvas>();
                var root = canvas != null ? canvas.rootCanvas : null;
                var canvasCamera = root != null && root.renderMode != RenderMode.ScreenSpaceOverlay ? root.worldCamera : null;

                if (canvasCamera != null && canvasCamera.targetTexture != null) {
                    // A canvas a Wipe Reveal is rendering to a texture. The camera's pixels are the
                    // texture's, which may be smaller than the screen it stands for.
                    var viewport = canvasCamera.WorldToViewportPoint(m_target.position);
                    screenPoint = new Vector2(viewport.x * Screen.width, viewport.y * Screen.height);
                } else {
                    screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, m_target.position);
                }

                return true;
            }

            var worldCamera = m_camera != null ? m_camera : Camera.main;

            if (worldCamera == null) {
                screenPoint = default;
                return false;
            }

            screenPoint = worldCamera.WorldToScreenPoint(m_target.position);
            return true;
        }
    }
}
