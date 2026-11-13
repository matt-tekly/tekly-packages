using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Common.Tweenlines
{
    [Serializable]
    public struct StepClip
    {
        public float Start;
        public bool Value;
    }

    /// <summary>
    /// A track whose clips are instant switches. Each clip sets a value at its start and holds it until
    /// the next clip. Before the first clip the track holds the target's rest value.
    /// </summary>
    public interface IStepTrack
    {
        bool GetClipValue(int index);
    }

    [Serializable]
    public abstract class StepTrack<TTarget> : TweenTrack, IStepTrack where TTarget : Object
    {
        public TTarget Target {
            get => m_target;
            set => m_target = value;
        }

        public List<StepClip> Clips => m_clips;

        public override Object TargetObject => m_target;
        public override Type TargetType => typeof(TTarget);
        public override int ClipCount => m_clips.Count;

        public override float End {
            get {
                var end = 0f;
                foreach (var clip in m_clips) {
                    end = Mathf.Max(end, clip.Start);
                }

                return end;
            }
        }

        protected abstract string Channel { get; }

        [SerializeField] protected TTarget m_target;
        [SerializeField] private List<StepClip> m_clips = new List<StepClip>();

        [NonSerialized] private int[] m_order;
        [NonSerialized] private bool m_origin;
        [NonSerialized] private bool m_prepared;

        protected abstract bool Read();
        protected abstract void Write(bool value);

        public bool GetClipValue(int index)
        {
            return m_clips[index].Value;
        }

        /// <summary>
        /// Adds a switch that flips the value the track has at that time.
        /// </summary>
        public override int AddClip(float start)
        {
            var current = m_target != null && Read();
            var latest = float.MinValue;

            foreach (var clip in m_clips) {
                if (clip.Start <= start && clip.Start >= latest) {
                    latest = clip.Start;
                    current = clip.Value;
                }
            }

            m_clips.Add(new StepClip { Start = start, Value = !current });
            return m_clips.Count - 1;
        }

        public override void GetClipRange(int index, out float start, out float duration)
        {
            start = m_clips[index].Start;
            duration = 0;
        }

        public override void CaptureOrigin(TweenOrigins origins)
        {
            if (m_target != null && !origins.Contains(m_target, Channel)) {
                origins.Set(m_target, Channel, Read());
            }
        }

        public override void Prepare(TweenOrigins origins, bool readCurrent)
        {
            m_prepared = false;

            if (m_target == null || m_clips.Count == 0) {
                return;
            }

            CaptureOrigin(origins);
            origins.TryGet(m_target, Channel, out m_origin);

            var count = m_clips.Count;
            if (m_order == null || m_order.Length != count) {
                m_order = new int[count];
            }

            for (var i = 0; i < count; i++) {
                m_order[i] = i;
            }

            for (var i = 1; i < count; i++) {
                var index = m_order[i];
                var start = m_clips[index].Start;
                var j = i - 1;

                while (j >= 0 && m_clips[m_order[j]].Start > start) {
                    m_order[j + 1] = m_order[j];
                    j--;
                }

                m_order[j + 1] = index;
            }

            m_prepared = true;
        }

        public override void Evaluate(float time)
        {
            if (!m_enabled || !m_prepared || m_target == null || m_order.Length != m_clips.Count) {
                return;
            }

            var value = m_origin;

            for (var i = 0; i < m_order.Length; i++) {
                var clip = m_clips[m_order[i]];

                if (clip.Start > time) {
                    break;
                }

                value = clip.Value;
            }

            // Only touch the target when the value changes since SetActive and enabled have side effects
            if (Read() != value) {
                Write(value);
            }
        }
    }
}
