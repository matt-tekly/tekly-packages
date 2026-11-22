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
                for (var i = 0; i < m_clips.Count; i++) {
                    var clip = m_clips[i];
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

        // Which clip each clip starts from for ClipFrom.Previous, so a clip after one that ends on a target
        // starts where the target is now. -1 when it doesn't start from another clip
        [NonSerialized] private int[] m_fromClip;
        [NonSerialized] private bool[] m_followsTarget;
        [NonSerialized] private bool m_hasTargets;
        [NonSerialized] private float[] m_arcs;
        [NonSerialized] private ChannelSlot<TValue> m_slot;
        [NonSerialized] private bool m_prepared;

        protected abstract TValue Lerp(TValue a, TValue b, float t);

        /// <summary>
        /// Combines an origin with a relative value.
        /// </summary>
        protected abstract TValue Add(TValue origin, TValue offset);

        protected abstract TweenClip<TValue> CreateClip(float start);

        /// <summary>
        /// The value that puts the track's target on another Transform. Only tracks that implement
        /// IPositionTrack override it.
        /// </summary>
        protected virtual bool TryGetTargetValue(Transform target, out TValue value)
        {
            value = default;
            return false;
        }

        /// <summary>
        /// Moves along a curve that bulges sideways by arc, a fraction of the distance. Only tracks that
        /// implement IPositionTrack override it, other tracks never get an arc.
        /// </summary>
        protected virtual TValue Curve(TValue from, TValue to, float t, float arc)
        {
            return Lerp(from, to, t);
        }

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
                m_fromClip = new int[count];
                m_followsTarget = new bool[count];
                m_arcs = new float[count];
            }

            var arcs = this is IPositionTrack;
            var arcSeed = unchecked(mixer.PlaySeed * 31 + m_target.GetEntityId().GetHashCode());

            ClipOrder.Sort(m_clips, m_order);

            var previous = origin;
            var previousClip = -1;

            m_hasTargets = false;

            for (var i = 0; i < count; i++) {
                var index = m_order[i];
                var clip = m_clips[index];

                m_from[index] = clip.FromMode switch {
                    ClipFrom.Previous => previous,
                    ClipFrom.Current => m_slot.Start,
                    ClipFrom.Origin => origin,
                    _ => Resolve(clip.Space, origin, clip.From)
                };

                m_followsTarget[index] = TryResolveTarget(index, out var target);
                m_to[index] = m_followsTarget[index] ? target : Resolve(clip.Space, origin, clip.To);
                m_fromClip[index] = clip.FromMode == ClipFrom.Previous ? previousClip : -1;
                m_hasTargets |= m_followsTarget[index];
                m_arcs[index] = arcs ? clip.Arc + clip.ArcRandom * TweenRandom.Hash(arcSeed, index, 0) : 0;

                previous = m_to[index];
                previousClip = index;
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
            var from = m_from[active];
            var to = m_to[active];

            if (m_hasTargets) {
                FollowTargets(active, ref from, ref to);
            }

            var eased = clip.Ease.Evaluate(ratio);
            var arc = m_arcs[active];

            value = arc != 0 ? Curve(from, to, eased, arc) : Lerp(from, to, eased);
            return true;
        }

        /// <summary>
        /// Reads where the targets are now for a clip that ends on one, or starts from one that did. A target
        /// that has been destroyed leaves the value resolved when the animation started.
        /// </summary>
        private void FollowTargets(int index, ref TValue from, ref TValue to)
        {
            if (m_followsTarget[index] && TryResolveTarget(index, out var target)) {
                to = target;
            }

            var fromClip = m_fromClip[index];

            if (fromClip >= 0 && m_followsTarget[fromClip] && TryResolveTarget(fromClip, out target)) {
                from = target;
            }
        }

        /// <summary>
        /// Where a clip ends when it has a target: the target with To added as an offset.
        /// </summary>
        private bool TryResolveTarget(int index, out TValue value)
        {
            var clip = m_clips[index];

            if (clip.ToTarget != null && TryGetTargetValue(clip.ToTarget, out var target)) {
                value = Add(target, clip.To);
                return true;
            }

            value = default;
            return false;
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
    /// A clip's place on the timeline, so ClipOrder can read it. Clips are structs, which makes ClipOrder's
    /// generic methods compile per clip type and read these directly instead of through a delegate.
    /// </summary>
    public interface IClip
    {
        float Start { get; }
        float End { get; }
    }

    /// <summary>
    /// Orders clips by start time without allocating. Clip counts are small, so an insertion sort is used,
    /// which also keeps clips with equal starts in list order.
    /// </summary>
    public static class ClipOrder
    {
        public static void Sort<TClip>(List<TClip> clips, int[] order) where TClip : struct, IClip
        {
            var length = order.Length;

            for (var i = 0; i < length; i++) {
                order[i] = i;
            }

            for (var i = 1; i < length; i++) {
                var index = order[i];
                var value = clips[index].Start;
                var j = i - 1;

                while (j >= 0 && clips[order[j]].Start > value) {
                    order[j + 1] = order[j];
                    j--;
                }

                order[j + 1] = index;
            }
        }

        /// <summary>
        /// The clip covering a time: the one that started last, if it hasn't ended yet. -1 when there is
        /// none, so a layered track does nothing between its clips.
        /// </summary>
        public static int FindCovering<TClip>(List<TClip> clips, float time) where TClip : struct, IClip
        {
            var result = FindLatestStarted(clips, time);
            return result >= 0 && time <= clips[result].End ? result : -1;
        }

        /// <summary>
        /// The clip that started last at or before a time, whether or not it has ended. -1 before every clip.
        /// Equal starts pick the later one in the list.
        /// </summary>
        public static int FindLatestStarted<TClip>(List<TClip> clips, float time) where TClip : struct, IClip
        {
            var result = -1;
            var latest = float.MinValue;
            var count = clips.Count;

            for (var i = 0; i < count; i++) {
                var clipStart = clips[i].Start;

                if (clipStart <= time && clipStart >= latest) {
                    latest = clipStart;
                    result = i;
                }
            }

            return result;
        }

        /// <summary>
        /// The clip that starts first, or -1 when there are none. Equal starts pick the earlier one in the list.
        /// </summary>
        public static int FindEarliest<TClip>(List<TClip> clips) where TClip : struct, IClip
        {
            var result = -1;
            var earliest = float.MaxValue;
            var count = clips.Count;

            for (var i = 0; i < count; i++) {
                var clipStart = clips[i].Start;

                if (clipStart < earliest) {
                    earliest = clipStart;
                    result = i;
                }
            }

            return result;
        }
    }
}
