using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Tweenlines
{
    [Serializable]
    public struct StepClip
    {
        public float Start;
        public bool Value;
    }

    /// <summary>
    /// Lets the timeline window draw step clips without knowing the track's target type.
    /// </summary>
    public interface IStepTrack
    {
        bool GetClipValue(int index);
    }

    /// <summary>
    /// A base track whose clips are instant switches. Each clip sets a value at its start and holds it until
    /// the next clip. Before the first clip the track holds the value the target had when the animation started.
    /// </summary>
    [Serializable]
    public abstract class StepTrack<TTarget> : TweenTrack, IStepTrack, IChannelSource<bool> where TTarget : Object
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

        protected abstract TweenChannel<bool> Channel { get; }

        [SerializeField] protected TTarget m_target;
        [SerializeField] private List<StepClip> m_clips = new List<StepClip>();

        [NonSerialized] private int[] m_order;
        [NonSerialized] private ChannelSlot<bool> m_slot;

        public bool GetClipValue(int index)
        {
            return m_clips[index].Value;
        }

        /// <summary>
        /// Adds a switch that flips the value the track has at that time.
        /// </summary>
        public override int AddClip(float start)
        {
            var current = m_target != null && Channel.Read(m_target);
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

        public override void CaptureOrigins(TweenOrigins origins)
        {
            if (m_target != null) {
                origins.GetOrCapture(m_target, Channel);
            }
        }

        public override void Prepare(TweenMixer mixer)
        {
            m_slot = null;

            if (!m_enabled || m_target == null || m_clips.Count == 0) {
                return;
            }

            m_slot = mixer.Get(m_target, Channel);
            m_slot.SetSource(this);

            if (m_order == null || m_order.Length != m_clips.Count) {
                m_order = new int[m_clips.Count];
            }

            ClipOrder.Sort(m_clips, m_order, clip => clip.Start);
        }

        public override void Evaluate(float time)
        {
            if (m_slot != null && TrySample(time, out var value)) {
                m_slot.SetBase(value);
            }
        }

        public bool TrySample(float time, out bool value)
        {
            value = false;

            if (m_slot == null || m_target == null || m_order.Length != m_clips.Count) {
                return false;
            }

            value = m_slot.Start;

            for (var i = 0; i < m_order.Length; i++) {
                var clip = m_clips[m_order[i]];

                if (clip.Start > time) {
                    break;
                }

                value = clip.Value;
            }

            return true;
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
}
