using System;
using UnityEngine;

namespace Tekly.Common.Tweenlines.Tracks
{
    /// <summary>
    /// Enables and disables a Behaviour, Renderer or Collider.
    /// </summary>
    [Serializable]
    public class EnabledTrack : StepTrack<Component>
    {
        protected override string Channel => "enabled";

        protected override bool Read()
        {
            return m_target switch {
                Behaviour behaviour => behaviour.enabled,
                Renderer renderer => renderer.enabled,
                Collider collider => collider.enabled,
                _ => true
            };
        }

        protected override void Write(bool value)
        {
            switch (m_target) {
                case Behaviour behaviour:
                    behaviour.enabled = value;
                    break;
                case Renderer renderer:
                    renderer.enabled = value;
                    break;
                case Collider collider:
                    collider.enabled = value;
                    break;
            }
        }

        /// <summary>
        /// Picks the first component that can be enabled, skipping Tweenlines.
        /// </summary>
        public override bool Bind(GameObject gameObject)
        {
            m_target = null;

            foreach (var component in gameObject.GetComponents<Component>()) {
                if (component is Tweenline) {
                    continue;
                }

                if (CanToggle(component)) {
                    m_target = component;
                    return true;
                }
            }

            return false;
        }

        public static bool CanToggle(Component component)
        {
            return component is Behaviour || component is Renderer || component is Collider;
        }

#if UNITY_EDITOR
        public override void CollectDrivenProperties(System.Collections.Generic.List<DrivenProperty> properties)
        {
            if (m_target != null && CanToggle(m_target)) {
                properties.Add(new DrivenProperty(m_target, "m_Enabled"));
            }
        }

        public override string GetWarning(Tweenline owner)
        {
            if (m_target == null) {
                return null;
            }

            if (!CanToggle(m_target)) {
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
