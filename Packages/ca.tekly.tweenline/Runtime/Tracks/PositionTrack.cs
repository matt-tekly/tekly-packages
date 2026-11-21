using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Tekly.Tweenlines.Tracks
{
    /// <summary>
    /// Moves a Transform's local position. Use AnchoredPositionTrack for UI. A clip can end on another
    /// Transform, followed every frame, with To as a local offset from it, and bend into an Arc.
    /// </summary>
    [Serializable]
    [MovedFrom(true, "Tekly.Common.Tweenlines.Tracks", "Tekly.Common.Runtime")]
    public class PositionTrack : Vector3Track<Transform>, IPositionTrack
    {
        protected override TweenChannel<Vector3> Channel => TweenChannels.LocalPosition;

        protected override bool TryGetTargetValue(Transform target, out Vector3 value)
        {
            var parent = m_target.parent;
            value = parent != null ? parent.InverseTransformPoint(target.position) : target.position;

            return true;
        }

        /// <summary>
        /// A quadratic bezier whose control point sits sideways from the middle in the XY plane, so the curve's
        /// furthest point is arc times the distance away. Travel along Z bulges up.
        /// </summary>
        protected override Vector3 Curve(Vector3 from, Vector3 to, float t, float arc)
        {
            var delta = to - from;
            var side = Vector3.Cross(Vector3.forward, delta);
            side = side.sqrMagnitude > 1e-8f ? side.normalized : Vector3.up;

            var control = (from + to) * 0.5f + side * (2 * arc * delta.magnitude);

            return Vector3.LerpUnclamped(Vector3.LerpUnclamped(from, control, t), Vector3.LerpUnclamped(control, to, t), t);
        }

        protected override TweenClip<Vector3> CreateClip(float start)
        {
            return new TweenClip<Vector3> {
                Start = start,
                Duration = 0.5f,
                FromMode = ClipFrom.Previous,
                Space = ClipSpace.RelativeToOrigin
            };
        }
    }
}
