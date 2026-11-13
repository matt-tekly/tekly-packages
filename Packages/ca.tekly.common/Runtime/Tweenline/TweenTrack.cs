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
        /// Resolves clip values before playback. readCurrent reads the live value used by ClipFrom.Current.
        /// </summary>
        public abstract void Prepare(TweenOrigins origins, bool readCurrent);

        /// <summary>
        /// Writes the track's value at the given time. Only depends on time so it can be scrubbed.
        /// </summary>
        public abstract void Evaluate(float time);

        /// <summary>
        /// Stores the target's current value as its origin if no origin is stored yet.
        /// </summary>
        public abstract void CaptureOrigin(TweenOrigins origins);

        /// <summary>
        /// Points the track at the matching component on the GameObject. Returns false when there is none.
        /// </summary>
        public abstract bool Bind(GameObject gameObject);

        public abstract int AddClip(float start);

        public abstract void GetClipRange(int index, out float start, out float duration);

#if UNITY_EDITOR
        public virtual bool Previewable => true;

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
