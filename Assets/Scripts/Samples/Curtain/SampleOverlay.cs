using System;
using Tekly.Tweenlines;
using UnityEngine;

namespace TeklySample.Samples.Curtain
{
    /// <summary>
    /// An overlay the sample's controller shows and hides. Like an overlay edited as a prefab, its root
    /// starts disabled, and enabling it plays its Tweenline's Show, which is set to play on enable. Hiding
    /// plays Hide and disables the root once it finishes.
    /// </summary>
    [RequireComponent(typeof(Tweenline))]
    public class SampleOverlay : MonoBehaviour
    {
        private const string SHOW = "Show";
        private const string HIDE = "Hide";

        /// <summary>
        /// Raised when Show finishes, so the overlay now covers the screen.
        /// </summary>
        public event Action<SampleOverlay> Shown;

        [Tooltip("The content canvas. The controller sets its sort order so the newest overlay is on top")]
        [SerializeField] private Canvas m_content;

        private Tweenline m_tweenline;

        public void Show(int sortingOrder)
        {
            m_content.sortingOrder = sortingOrder;

            // A disabled overlay shows by being enabled, which plays Show on enable.
            if (gameObject.activeSelf) {
                GetTweenline().Play(SHOW);
            } else {
                gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (gameObject.activeSelf) {
                GetTweenline().Play(HIDE);
            }
        }

        /// <summary>
        /// Disables the overlay straight away, for one already covered by another full-screen overlay.
        /// </summary>
        public void Close()
        {
            GetTweenline().Stop();
            gameObject.SetActive(false);
        }

        private void Awake()
        {
            GetTweenline();
        }

        /// <summary>
        /// The Tweenline, listening for its animations to finish. Fetched on first use, since a disabled
        /// overlay hasn't run Awake yet.
        /// </summary>
        private Tweenline GetTweenline()
        {
            if (m_tweenline == null) {
                m_tweenline = GetComponent<Tweenline>();
                m_tweenline.Completed += OnCompleted;
            }

            return m_tweenline;
        }

        private void OnCompleted(Tweenline tweenline, TweenAnimation animation)
        {
            if (animation.Name == HIDE) {
                gameObject.SetActive(false);
            } else if (animation.Name == SHOW) {
                Shown?.Invoke(this);
            }
        }
    }
}
