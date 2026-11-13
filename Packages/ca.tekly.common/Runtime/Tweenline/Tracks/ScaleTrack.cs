using System;
using UnityEngine;

namespace Tekly.Common.Tweenlines.Tracks
{
    /// <summary>
    /// Scales a Transform. Relative values multiply the origin scale, so (1, 1, 1) is the rest scale.
    /// </summary>
    [Serializable]
    public class ScaleTrack : Vector3Track<Transform>
    {
        protected override string Channel => "localScale";

        protected override Vector3 Read() => m_target.localScale;
        protected override void Write(Vector3 value) => m_target.localScale = value;
        protected override Vector3 Add(Vector3 origin, Vector3 offset) => Vector3.Scale(origin, offset);

        protected override TweenClip<Vector3> CreateClip(float start)
        {
            return new TweenClip<Vector3> {
                Start = start,
                Duration = 0.5f,
                FromMode = ClipFrom.Previous,
                Space = ClipSpace.RelativeToOrigin,
                From = Vector3.one,
                To = Vector3.one
            };
        }

#if UNITY_EDITOR
        public override void CollectDrivenProperties(System.Collections.Generic.List<DrivenProperty> properties)
        {
            if (m_target == null) {
                return;
            }

            properties.Add(new DrivenProperty(m_target, "m_LocalScale.x"));
            properties.Add(new DrivenProperty(m_target, "m_LocalScale.y"));
            properties.Add(new DrivenProperty(m_target, "m_LocalScale.z"));
        }
#endif
    }
}
