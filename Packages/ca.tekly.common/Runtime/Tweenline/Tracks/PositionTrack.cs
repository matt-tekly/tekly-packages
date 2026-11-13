using System;
using UnityEngine;

namespace Tekly.Common.Tweenlines.Tracks
{
    /// <summary>
    /// Moves a Transform's local position. Use AnchoredPositionTrack for UI.
    /// </summary>
    [Serializable]
    public class PositionTrack : Vector3Track<Transform>
    {
        protected override string Channel => "localPosition";

        protected override Vector3 Read() => m_target.localPosition;
        protected override void Write(Vector3 value) => m_target.localPosition = value;

        protected override TweenClip<Vector3> CreateClip(float start)
        {
            return new TweenClip<Vector3> {
                Start = start,
                Duration = 0.5f,
                FromMode = ClipFrom.Previous,
                Space = ClipSpace.RelativeToOrigin
            };
        }

#if UNITY_EDITOR
        public override void CollectDrivenProperties(System.Collections.Generic.List<DrivenProperty> properties)
        {
            if (m_target == null) {
                return;
            }

            properties.Add(new DrivenProperty(m_target, "m_LocalPosition.x"));
            properties.Add(new DrivenProperty(m_target, "m_LocalPosition.y"));
            properties.Add(new DrivenProperty(m_target, "m_LocalPosition.z"));

            if (m_target is RectTransform) {
                properties.Add(new DrivenProperty(m_target, "m_AnchoredPosition.x"));
                properties.Add(new DrivenProperty(m_target, "m_AnchoredPosition.y"));
            }
        }
#endif
    }
}
