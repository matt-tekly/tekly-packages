using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Tweenlines.Tracks
{
    /// <summary>
    /// The position channel a layered track offsets. It layers onto whichever position channel a base track
    /// drives, preferring anchored position for UI, or onto the position the target had when the animation
    /// started when there is no base track.
    /// </summary>
    internal struct PositionLayer
    {
        public bool IsPrepared => m_anchoredSlot != null || m_positionSlot != null;

        private ChannelSlot<Vector2> m_anchoredSlot;
        private ChannelSlot<Vector3> m_positionSlot;

        public static void CaptureOrigins(TweenOrigins origins, Transform target)
        {
            if (target is RectTransform) {
                origins.GetOrCapture(target, TweenChannels.AnchoredPosition);
            }

            origins.GetOrCapture(target, TweenChannels.LocalPosition);
        }

        public void Clear()
        {
            m_anchoredSlot = null;
            m_positionSlot = null;
        }

        public void Prepare(TweenMixer mixer, Transform target)
        {
            Clear();

            if (target is RectTransform && !mixer.HasSource(target, TweenChannels.LocalPosition)) {
                m_anchoredSlot = mixer.Get(target, TweenChannels.AnchoredPosition);
            } else {
                m_positionSlot = mixer.Get(target, TweenChannels.LocalPosition);
            }
        }

        public void Offset(Vector3 offset)
        {
            if (m_anchoredSlot != null) {
                m_anchoredSlot.Offset(offset);
            } else {
                m_positionSlot?.Offset(offset);
            }
        }

        /// <summary>
        /// The target's base position at a time in its parent's space, before any layered offsets.
        /// </summary>
        public Vector3 BaseAt(Transform target, float time)
        {
            if (m_anchoredSlot != null) {
                // Anchored and local positions differ by a constant, so convert through the current pair
                var anchored = m_anchoredSlot.SampleBase(time);
                var rectTransform = (RectTransform) target;
                return target.localPosition + (Vector3) (anchored - rectTransform.anchoredPosition);
            }

            return m_positionSlot.SampleBase(time);
        }

#if UNITY_EDITOR
        public static void CollectDrivenProperties(Transform target, List<DrivenProperty> properties)
        {
            TweenChannels.LocalPosition.CollectDrivenProperties(target, properties);

            if (target is RectTransform) {
                TweenChannels.AnchoredPosition.CollectDrivenProperties(target, properties);
            }
        }
#endif
    }
}
