using System;
using UnityEngine;

namespace Tekly.Common.Tweenlines.Tracks
{
    /// <summary>
    /// Moves a RectTransform's anchored position, so movement stays relative to its anchors.
    /// </summary>
    [Serializable]
    public class AnchoredPositionTrack : Vector2Track<RectTransform>
    {
        protected override string Channel => "anchoredPosition";

        protected override Vector2 Read() => m_target.anchoredPosition;
        protected override void Write(Vector2 value) => m_target.anchoredPosition = value;

        protected override TweenClip<Vector2> CreateClip(float start)
        {
            return new TweenClip<Vector2> {
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

            properties.Add(new DrivenProperty(m_target, "m_AnchoredPosition.x"));
            properties.Add(new DrivenProperty(m_target, "m_AnchoredPosition.y"));
            properties.Add(new DrivenProperty(m_target, "m_LocalPosition.x"));
            properties.Add(new DrivenProperty(m_target, "m_LocalPosition.y"));
        }
#endif
    }
}
