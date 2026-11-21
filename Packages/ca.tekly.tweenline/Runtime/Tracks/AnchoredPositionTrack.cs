using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Tekly.Tweenlines.Tracks
{
    /// <summary>
    /// Moves a RectTransform's anchored position, so movement stays relative to its anchors. A clip can end
    /// on another Transform, followed every frame, with To as an offset from it, and bend into an Arc. The
    /// target's world position is used, so it must be in the same canvas space: a world object can't be a
    /// target for a Screen Space Overlay canvas.
    /// </summary>
    [Serializable]
    [MovedFrom(true, "Tekly.Common.Tweenlines.Tracks", "Tekly.Common.Runtime")]
    public class AnchoredPositionTrack : Vector2Track<RectTransform>, IPositionTrack
    {
        protected override TweenChannel<Vector2> Channel => TweenChannels.AnchoredPosition;

        protected override bool TryGetTargetValue(Transform target, out Vector2 value)
        {
            var parent = m_target.parent;
            Vector2 local = parent != null ? parent.InverseTransformPoint(target.position) : target.position;

            // The gap between local and anchored position depends only on the anchors and the parent's rect,
            // so it can be read while the track is moving the target
            var anchorOffset = (Vector2)m_target.localPosition - m_target.anchoredPosition;
            value = local - anchorOffset;

            return true;
        }

        /// <summary>
        /// A quadratic bezier whose control point sits sideways from the middle, so the curve's furthest point
        /// is arc times the distance away.
        /// </summary>
        protected override Vector2 Curve(Vector2 from, Vector2 to, float t, float arc)
        {
            var delta = to - from;
            var side = new Vector2(-delta.y, delta.x);
            var control = (from + to) * 0.5f + side * (2 * arc);

            return Vector2.LerpUnclamped(Vector2.LerpUnclamped(from, control, t), Vector2.LerpUnclamped(control, to, t), t);
        }

        protected override TweenClip<Vector2> CreateClip(float start)
        {
            return new TweenClip<Vector2> {
                Start = start,
                Duration = 0.5f,
                FromMode = ClipFrom.Previous,
                Space = ClipSpace.RelativeToOrigin
            };
        }
    }
}
