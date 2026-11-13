using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Tekly.Tweenlines.Tracks
{
    /// <summary>
    /// Enables and disables a Behaviour, Renderer or Collider.
    /// </summary>
    [Serializable]
    [MovedFrom(true, "Tekly.Common.Tweenlines.Tracks", "Tekly.Common.Runtime")]
    public class EnabledTrack : StepTrack<Component>
    {
        protected override TweenChannel<bool> Channel => TweenChannels.Enabled;

        /// <summary>
        /// Picks the first component that can be enabled, skipping Tweenlines.
        /// </summary>
        public override bool Bind(GameObject gameObject)
        {
            m_target = null;

            foreach (var component in gameObject.GetComponents<Component>()) {
                if (!(component is Tweenline) && EnabledChannel.CanToggle(component)) {
                    m_target = component;
                    return true;
                }
            }

            return false;
        }

#if UNITY_EDITOR
        public override string GetWarning(Tweenline owner)
        {
            if (m_target == null) {
                return null;
            }

            if (!EnabledChannel.CanToggle(m_target)) {
                return $"[{m_target.GetType().Name}] can't be enabled or disabled. Use a Behaviour, Renderer or Collider.";
            }

            if (m_target == owner) {
                return "Disabling the Tweenline itself stops the animation.";
            }

            return null;
        }
#endif
    }
}
