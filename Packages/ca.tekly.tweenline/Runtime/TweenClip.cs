using System;
using UnityEngine;

namespace Tekly.Tweenlines
{
    [Serializable]
    public struct TweenClip<TValue> : IClip
    {
        public float Start;
        public float Duration;
        public TweenEase Ease;
        public ClipFrom FromMode;
        public ClipSpace Space;
        public TValue From;
        public TValue To;

        /// <summary>
        /// Where the clip ends, followed every frame, with To as an offset from it. Only used by tracks that
        /// implement IPositionTrack, like the position tracks.
        /// </summary>
        public Transform ToTarget;

        /// <summary>
        /// Bends the path into a curve: how far its middle bulges sideways, as a fraction of the distance
        /// travelled. Positive bulges to the left of the direction of travel, so up when moving right. Only used
        /// by tracks that implement IPositionTrack.
        /// </summary>
        public float Arc;

        /// <summary>
        /// Adds a random amount from -ArcRandom to ArcRandom to Arc, picked for each play and each target, so a
        /// burst of objects heading to the same place fans out.
        /// </summary>
        public float ArcRandom;

        public float End => Start + Duration;

        float IClip.Start => Start;
    }
}
