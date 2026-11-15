using UnityEngine;

namespace Tekly.Curtains
{
    /// <summary>
    /// An overlay transition: colour bands sweep over whatever is behind and leave again. It needs no camera
    /// and holds no state beyond its settings.
    ///
    /// For a load screen, cover with 0 to 1, show the loading canvas above the curtain, load, hide it, then
    /// reveal with 1 to 2. To have a canvas itself wipe in as part of the shape, use a SharedCurtainTrack.
    /// </summary>
    public class Curtain : CurtainGraphic, ICurtainProgress
    {
        public float Progress {
            get => m_progress;
            set {
                value = CurtainProgress.Clamp(value);

                if (m_progress == value) {
                    return;
                }

                m_progress = value;
                Refresh();
            }
        }

        /// <summary>
        /// The look and timing. Call Refresh after changing it.
        /// </summary>
        public CurtainStyle Style => m_style;

        protected override CurtainStyle DrawStyle => m_style;
        protected override float DrawProgress => m_progress;

        [Tooltip("0 hasn't arrived, 1 covers everything, 2 has passed through")]
        [SerializeField, Range(0, CurtainProgress.PASSED)] private float m_progress;

        [SerializeField] private CurtainStyle m_style = new CurtainStyle();
    }
}
