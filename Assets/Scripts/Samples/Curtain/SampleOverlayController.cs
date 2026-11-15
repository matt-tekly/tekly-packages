using UnityEngine;

namespace TeklySample.Samples.Curtain
{
    /// <summary>
    /// Shows one full-screen overlay at a time. A newly shown overlay goes on top of the current one, and
    /// once its Show finishes it covers the screen, so the one under it is closed without playing its Hide.
    /// </summary>
    public class SampleOverlayController : MonoBehaviour
    {
        [Tooltip("Sort order of the first overlay. Each overlay shown on top of another goes one higher")]
        [SerializeField] private int m_baseOrder = 20;

        private SampleOverlay m_current;
        private SampleOverlay m_covered;
        private int m_depth;

        public void Show(SampleOverlay overlay)
        {
            if (overlay == m_current) {
                return;
            }

            CloseCovered();

            m_covered = m_current;
            m_current = overlay;

            if (m_covered != null) {
                overlay.Shown += OnShown;
            }

            overlay.Show(m_baseOrder + m_depth++);
        }

        public void HideCurrent()
        {
            if (m_current == null) {
                return;
            }

            CloseCovered();

            m_current.Hide();
            m_current = null;
            m_depth = 0;
        }

        private void OnShown(SampleOverlay overlay)
        {
            overlay.Shown -= OnShown;
            CloseCovered();
        }

        /// <summary>
        /// Closes the overlay the current one is covering, if any, and stops waiting for the current one.
        /// </summary>
        private void CloseCovered()
        {
            if (m_current != null) {
                m_current.Shown -= OnShown;
            }

            if (m_covered != null) {
                m_covered.Close();
                m_covered = null;
            }
        }
    }
}
