using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Common.Tweenlines
{
    /// <summary>
    /// A track that drives one channel of one target through a series of clips.
    /// </summary>
    [Serializable]
    public abstract class ClipTrack<TTarget, TValue> : TweenTrack where TTarget : Component
    {
        public TTarget Target {
            get => m_target;
            set => m_target = value;
        }

        public List<TweenClip<TValue>> Clips => m_clips;

        public override Object TargetObject => m_target;
        public override Type TargetType => typeof(TTarget);
        public override int ClipCount => m_clips.Count;

        public override float End {
            get {
                var end = 0f;
                foreach (var clip in m_clips) {
                    end = Mathf.Max(end, clip.End);
                }

                return end;
            }
        }

        /// <summary>
        /// Identifies the channel for shared origins. Tracks writing the same property must use the same channel.
        /// </summary>
        protected abstract string Channel { get; }

        [SerializeField] protected TTarget m_target;
        [SerializeField] private List<TweenClip<TValue>> m_clips = new List<TweenClip<TValue>>();

        [NonSerialized] private int[] m_order;
        [NonSerialized] private TValue[] m_from;
        [NonSerialized] private TValue[] m_to;
        [NonSerialized] private TValue m_current;
        [NonSerialized] private bool m_hasCurrent;
        [NonSerialized] private bool m_prepared;

        protected abstract TValue Read();
        protected abstract void Write(TValue value);
        protected abstract TValue Lerp(TValue a, TValue b, float t);

        /// <summary>
        /// Combines an origin with a relative value.
        /// </summary>
        protected abstract TValue Add(TValue origin, TValue offset);

        protected abstract TweenClip<TValue> CreateClip(float start);

        public override bool Bind(GameObject gameObject)
        {
            m_target = gameObject.GetComponent<TTarget>();
            return m_target != null;
        }

        public override int AddClip(float start)
        {
            m_clips.Add(CreateClip(start));
            return m_clips.Count - 1;
        }

        public override void GetClipRange(int index, out float start, out float duration)
        {
            var clip = m_clips[index];
            start = clip.Start;
            duration = clip.Duration;
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
            origins.TryGet<TValue>(m_target, Channel, out var origin);

            if (readCurrent || !m_hasCurrent) {
                m_current = readCurrent ? Read() : origin;
                m_hasCurrent = true;
            }

            var count = m_clips.Count;
            if (m_order == null || m_order.Length != count) {
                m_order = new int[count];
                m_from = new TValue[count];
                m_to = new TValue[count];
            }

            SortByStart();

            var previous = origin;

            for (var i = 0; i < count; i++) {
                var index = m_order[i];
                var clip = m_clips[index];

                m_from[index] = clip.FromMode switch {
                    ClipFrom.Previous => previous,
                    ClipFrom.Current => m_current,
                    ClipFrom.Origin => origin,
                    _ => Resolve(clip.Space, origin, clip.From)
                };

                m_to[index] = Resolve(clip.Space, origin, clip.To);
                previous = m_to[index];
            }

            m_prepared = true;
        }

        public override void Evaluate(float time)
        {
            if (!m_enabled || !m_prepared || m_target == null || m_order.Length != m_clips.Count) {
                return;
            }

            var active = -1;

            for (var i = 0; i < m_order.Length; i++) {
                if (m_clips[m_order[i]].Start <= time) {
                    active = m_order[i];
                } else {
                    break;
                }
            }

            if (active < 0) {
                Write(m_from[m_order[0]]);
                return;
            }

            var clip = m_clips[active];
            var ratio = clip.Duration > 0 ? Mathf.Clamp01((time - clip.Start) / clip.Duration) : 1f;

            Write(Lerp(m_from[active], m_to[active], clip.Ease.Evaluate(ratio)));
        }

        private TValue Resolve(ClipSpace space, TValue origin, TValue value)
        {
            return space == ClipSpace.RelativeToOrigin ? Add(origin, value) : value;
        }

        private void SortByStart()
        {
            for (var i = 0; i < m_order.Length; i++) {
                m_order[i] = i;
            }

            // Insertion sort: stable, allocation free and clip counts are small
            for (var i = 1; i < m_order.Length; i++) {
                var index = m_order[i];
                var start = m_clips[index].Start;
                var j = i - 1;

                while (j >= 0 && m_clips[m_order[j]].Start > start) {
                    m_order[j + 1] = m_order[j];
                    j--;
                }

                m_order[j + 1] = index;
            }
        }
    }
}
