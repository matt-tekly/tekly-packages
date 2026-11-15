using UnityEngine;

namespace Tekly.Curtains
{
    /// <summary>
    /// The graphic a CurtainStage slot draws a shared curtain with: a look, a progress, and the content
    /// canvas's live render as the last band. Made by the stage, never added by hand.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class CurtainStageGraphic : CurtainGraphic
    {
        protected override CurtainStyle DrawStyle => m_style;
        protected override float DrawProgress => m_progress;
        protected override bool HasContentBand => true;
        protected override Texture ContentTexture => m_content;

        private CurtainStyle m_style;
        private float m_progress;
        private Texture m_content;

        public void Draw(CurtainStyle style, float progress, Texture content)
        {
            m_style = style;
            m_progress = progress;
            m_content = content;
            Refresh();
        }
    }
}
