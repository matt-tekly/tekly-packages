using System;
using UnityEngine;

namespace Tekly.Common.Tweenlines.Tracks
{
    /// <summary>
    /// Activates and deactivates a GameObject. The target should not be the Tweenline's own GameObject or
    /// one of its parents, since deactivating it stops the Tweenline from updating.
    /// </summary>
    [Serializable]
    public class ActiveTrack : StepTrack<GameObject>
    {
        protected override string Channel => "active";

        protected override bool Read() => m_target.activeSelf;
        protected override void Write(bool value) => m_target.SetActive(value);

        public override bool Bind(GameObject gameObject)
        {
            m_target = gameObject;
            return true;
        }

#if UNITY_EDITOR
        public override void CollectDrivenProperties(System.Collections.Generic.List<DrivenProperty> properties)
        {
            if (m_target != null) {
                properties.Add(new DrivenProperty(m_target, "m_IsActive"));
            }
        }

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
