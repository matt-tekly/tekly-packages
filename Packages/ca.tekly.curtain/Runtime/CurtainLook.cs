using UnityEngine;

namespace Tekly.Curtains
{
    /// <summary>
    /// A curtain's look and timing as an asset, so overlays share transitions such as "Iris Pop" instead of
    /// each embedding a curtain. SharedCurtainTracks name the look they reveal their canvas with.
    /// </summary>
    [CreateAssetMenu(menuName = "Tekly/Curtain Look", fileName = "CurtainLook")]
    public class CurtainLook : ScriptableObject
    {
        public CurtainStyle Style => m_style;

        [SerializeField] private CurtainStyle m_style = new CurtainStyle();
    }
}
