using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Tekly.Tweenlines.Tracks
{
    /// <summary>
    /// Activates and deactivates a GameObject. The target should not be the Tweenline's own GameObject or
    /// one of its parents, since deactivating it stops the Tweenline from updating.
    /// </summary>
    [Serializable]
    [MovedFrom(true, "Tekly.Common.Tweenlines.Tracks", "Tekly.Common.Runtime")]
    public class ActiveTrack : StepTrack<GameObject>
    {
        protected override TweenChannel<bool> Channel => TweenChannels.Active;

        public override bool Bind(GameObject gameObject)
        {
            m_target = gameObject;
            return true;
        }

#if UNITY_EDITOR
        public override string GetWarning(Tweenline owner)
        {
            if (m_target != null && owner != null && owner.transform.IsChildOf(m_target.transform)) {
                return "Deactivating the Tweenline's own GameObject or a parent stops the animation. Target a child instead.";
            }

            return null;
        }
#endif
    }
}
