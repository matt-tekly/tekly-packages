using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Common.Tweenlines
{
    [Serializable]
    public abstract class TweenTrack
    {
        public bool Enabled {
            get => m_enabled;
            set => m_enabled = value;
        }

        public abstract float End { get; }
        public abstract Object TargetObject { get; }
        public abstract Type TargetType { get; }
        public abstract int ClipCount { get; }

        [SerializeField] protected bool m_enabled = true;

        /// <summary>
        /// Resolves clip values before playback and gets the mixer slots the track drives.
        /// </summary>
        public abstract void Prepare(TweenMixer mixer);

        /// <summary>
        /// Sets, offsets or multiplies the track's slots for the given time. Only depends on time so it can
        /// be scrubbed. The mixer writes the results.
        /// </summary>
        public abstract void Evaluate(float time);

        /// <summary>
        /// Stores the current value of every channel the track drives as its origin, if none is stored yet.
        /// </summary>
        public abstract void CaptureOrigins(TweenOrigins origins);

        /// <summary>
        /// Points the track at the matching component on the GameObject. Returns false when there is none.
        /// </summary>
        public abstract bool Bind(GameObject gameObject);

        public abstract int AddClip(float start);

        public abstract void GetClipRange(int index, out float start, out float duration);

#if UNITY_EDITOR
        public abstract void CollectDrivenProperties(System.Collections.Generic.List<DrivenProperty> properties);

        /// <summary>
        /// Shown in the timeline when the track is set up in a way that will not work.
        /// </summary>
        public virtual string GetWarning(Tweenline owner)
        {
            return null;
        }
#endif
    }
}
