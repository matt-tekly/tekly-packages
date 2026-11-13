using System;
using UnityEngine;

namespace Tekly.Tweenlines.Tracks
{
    [Serializable]
    public abstract class Vector3Track<TTarget> : ClipTrack<TTarget, Vector3> where TTarget : Component
    {
        protected override Vector3 Lerp(Vector3 a, Vector3 b, float t) => Vector3.LerpUnclamped(a, b, t);
        protected override Vector3 Add(Vector3 origin, Vector3 offset) => origin + offset;
    }

    [Serializable]
    public abstract class Vector2Track<TTarget> : ClipTrack<TTarget, Vector2> where TTarget : Component
    {
        protected override Vector2 Lerp(Vector2 a, Vector2 b, float t) => Vector2.LerpUnclamped(a, b, t);
        protected override Vector2 Add(Vector2 origin, Vector2 offset) => origin + offset;
    }
}
