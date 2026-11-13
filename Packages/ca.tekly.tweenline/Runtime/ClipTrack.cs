using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Tweenlines
{
    /// <summary>
    /// A base track that sets one channel of one target through a series of from/to clips.
    /// </summary>
    [Serializable]
    public abstract class ClipTrack<TTarget, TValue> : TweenTrack, IChannelSource<TValue> where TTarget : Component
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

        protected abstract TweenChannel<TValue> Channel { get; }

        [SerializeField] protected TTarget m_target;
        [SerializeField] private List<TweenClip<TValue>> m_clips = new List<TweenClip<TValue>>();

        [NonSerialized] private int[] m_order;
        [NonSerialized] private TValue[] m_from;
        [NonSerialized] private TValue[] m_to;
        [NonSerialized] private ChannelSlot<TValue> m_slot;
        [NonSerialized] private bool m_prepared;

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

        public override void CaptureOrigins(TweenOrigins origins)
        {
            if (m_target != null) {
                origins.GetOrCapture(m_target, Channel);
            }
        }

        public override void Prepare(TweenMixer mixer)
        {
            m_prepared = false;
            m_slot = null;

            if (!m_enabled || m_target == null || m_clips.Count == 0) {
                return;
            }

            m_slot = mixer.Get(m_target, Channel);
            m_slot.SetSource(this);

            var origin = m_slot.Origin;
            var count = m_clips.Count;

            if (m_order == null || m_order.Length != count) {
                m_order = new int[count];
                m_from = new TValue[count];
                m_to = new TValue[count];
            }

            ClipOrder.Sort(m_clips, m_order, clip => clip.Start);

            var previous = origin;

            for (var i = 0; i < count; i++) {
                var index = m_order[i];
                var clip = m_clips[index];

                m_from[index] = clip.FromMode switch {
                    ClipFrom.Previous => previous,
                    ClipFrom.Current => m_slot.Start,
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
            if (m_slot != null && TrySample(time, out var value)) {
                m_slot.SetBase(value);
            }
        }

        /// <summary>
        /// Computes the track's value at a time without writing it. False when the track isn't prepared.
        /// </summary>
        public bool TrySample(float time, out TValue value)
        {
            value = default;

            if (!m_prepared || m_target == null || m_order.Length != m_clips.Count) {
                return false;
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
                value = m_from[m_order[0]];
                return true;
            }

            var clip = m_clips[active];
            var ratio = clip.Duration > 0 ? Mathf.Clamp01((time - clip.Start) / clip.Duration) : 1f;

            value = Lerp(m_from[active], m_to[active], clip.Ease.Evaluate(ratio));
            return true;
        }

        private TValue Resolve(ClipSpace space, TValue origin, TValue value)
        {
            return space == ClipSpace.RelativeToOrigin ? Add(origin, value) : value;
        }

#if UNITY_EDITOR
        public override void CollectDrivenProperties(List<DrivenProperty> properties)
        {
            if (m_target != null) {
                Channel.CollectDrivenProperties(m_target, properties);
            }
        }
#endif
    }

    /// <summary>
    /// Orders clips by start time without allocating. Clip counts are small, so an insertion sort is used,
    /// which also keeps clips with equal starts in list order.
    /// </summary>
    public static class ClipOrder
    {
        public static void Sort<TClip>(List<TClip> clips, int[] order, Func<TClip, float> start)
        {
            for (var i = 0; i < order.Length; i++) {
                order[i] = i;
            }

            for (var i = 1; i < order.Length; i++) {
                var index = order[i];
                var value = start(clips[index]);
                var j = i - 1;

                while (j >= 0 && start(clips[order[j]]) > value) {
                    order[j + 1] = order[j];
                    j--;
                }

                order[j + 1] = index;
            }
        }
    }
}
