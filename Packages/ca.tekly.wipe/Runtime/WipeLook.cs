using UnityEngine;

namespace Tekly.Wipes
{
    /// <summary>
    /// A wipe's look and timing as an asset, so screens share transitions such as "Iris Pop" instead of
    /// each setting one up. Wipes, Wipe Reveals, Tweenline clips and the Screen Wipe all take one.
    /// </summary>
    [CreateAssetMenu(menuName = "Tekly/Wipe Look", fileName = "WipeLook")]
    public class WipeLook : ScriptableObject
    {
        public WipeStyle Style => m_style;

        [SerializeField] private WipeStyle m_style = new WipeStyle();
    }
}
