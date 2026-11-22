using System;
using System.Collections.Generic;
using Tekly.Tweenlines;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Wipes
{
    public enum WipeMode
    {
        /// <summary>Raises the coverage</summary>
        Cover,

        /// <summary>Lowers the coverage</summary>
        Uncover
    }

    [Serializable]
    public struct WipeClip : IClip
    {
        public float Start;
        public float Duration;
        public TweenEase Ease;

        [Tooltip("Cover raises the coverage. Uncover lowers it, carrying on through or going back the way it came as the look's Exit says")]
        public WipeMode Mode;

        [Tooltip("How far the clip goes. 1 covers or uncovers all the way. Less stops part way, like an iris that holds on a small hole before it closes")]
        [Range(0f, 1f)] public float Amount;

        [Tooltip("The look this clip draws with. Empty uses the target's own")]
        public WipeLook Look;

        public float End => Start + Duration;

        float IClip.Start => Start;
    }

    /// <summary>
    /// A wipe's coverage.
    /// </summary>
    public sealed class WipeCoverageChannel : FloatChannel
    {
        public static readonly WipeCoverageChannel Instance = new WipeCoverageChannel();

        public override string Name => "wipeCoverage";
        public override float Read(Object target) => ((IWipe) target).Coverage;
        public override void Write(Object target, float value) => ((IWipe) target).Coverage = value;

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
        {
            properties.Add(new DrivenProperty(target, "m_coverage"));
        }
#endif
    }

    /// <summary>
    /// Whether a wipe is leaving: uncovering in the order it covered.
    /// </summary>
    public sealed class WipeLeavingChannel : BoolChannel
    {
        public static readonly WipeLeavingChannel Instance = new WipeLeavingChannel();

        public override string Name => "wipeLeaving";
        public override bool Read(Object target) => ((IWipe) target).Leaving;
        public override void Write(Object target, bool value) => ((IWipe) target).Leaving = value;

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
        {
            properties.Add(new DrivenProperty(target, "m_leaving"));
        }
#endif
    }

    /// <summary>
    /// A base track of Cover and Uncover clips. A clip has no from and to numbers: a Cover raises the
    /// coverage to its amount and an Uncover lowers it by its amount, each starting where the clip before
    /// it ended. The first clip starts from nothing covered for a Cover and everything covered for an
    /// Uncover, whatever the target was left at, so scrubbing always gives the same picture.
    ///
    /// The clip's ease only moves the edge. An ease that overshoots, like Back or Elastic, is held at 0
    /// and 1, so it bounces a part way hold but never turns a cover into an uncover.
    /// </summary>
    [Serializable]
    public abstract class WipeTrackBase<TTarget> : TweenTrack where TTarget : Component, IWipe
    {
        public TTarget Target {
            get => m_target;
            set => m_target = value;
        }

        public List<WipeClip> Clips => m_clips;

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

        [SerializeField] protected TTarget m_target;
        [SerializeField] private List<WipeClip> m_clips = new List<WipeClip>();

        [NonSerialized] private ChannelSlot<float> m_coverageSlot;
        [NonSerialized] private ChannelSlot<bool> m_leavingSlot;
        [NonSerialized] private int[] m_order;
        [NonSerialized] private float[] m_from;
        [NonSerialized] private float[] m_to;
        [NonSerialized] private bool[] m_leaves;

        public override bool Bind(GameObject gameObject)
        {
            m_target = gameObject.GetComponent<TTarget>();
            return m_target != null;
        }

        public override int AddClip(float start)
        {
            // Cover for the first clip and uncover after that, the usual order
            var cover = m_clips.Count == 0;

            m_clips.Add(new WipeClip {
                Start = start,
                Duration = cover ? 0.45f : 0.4f,
                Ease = new TweenEase(EaseType.Cubic, cover ? EaseDirection.Out : EaseDirection.In),
                Mode = cover ? WipeMode.Cover : WipeMode.Uncover,
                Amount = 1f
            });

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
                origins.GetOrCapture(m_target, WipeCoverageChannel.Instance);
                origins.GetOrCapture(m_target, WipeLeavingChannel.Instance);
            }
        }

        public override void Prepare(TweenMixer mixer)
        {
            m_coverageSlot = null;
            m_leavingSlot = null;

            if (!m_enabled || m_target == null || m_clips.Count == 0) {
                return;
            }

            m_coverageSlot = mixer.Get(m_target, WipeCoverageChannel.Instance);
            m_leavingSlot = mixer.Get(m_target, WipeLeavingChannel.Instance);

            var count = m_clips.Count;

            if (m_order == null || m_order.Length != count) {
                m_order = new int[count];
                m_from = new float[count];
                m_to = new float[count];
                m_leaves = new bool[count];
            }

            ClipOrder.Sort(m_clips, m_order);

            var previous = m_clips[m_order[0]].Mode == WipeMode.Cover ? 0f : 1f;

            for (var i = 0; i < count; i++) {
                var index = m_order[i];
                var clip = m_clips[index];
                var cover = clip.Mode == WipeMode.Cover;

                m_from[index] = previous;
                m_to[index] = Mathf.Clamp01(cover ? previous + clip.Amount : previous - clip.Amount);
                m_leaves[index] = !cover && m_target.StyleFor(clip.Look).ExitContinues;

                previous = m_to[index];
            }
        }

        public override void Evaluate(float time)
        {
            if (m_coverageSlot == null || m_target == null || m_order.Length != m_clips.Count) {
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

            // Before the first clip the track holds that clip's start
            if (active < 0) {
                var first = m_order[0];

                m_target.SetTrackState(m_clips[first].Look, false);
                m_coverageSlot.SetBase(m_from[first]);
                m_leavingSlot.SetBase(false);
                return;
            }

            var clip = m_clips[active];
            var ratio = clip.Duration > 0 ? Mathf.Clamp01((time - clip.Start) / clip.Duration) : 1f;
            var coverage = Mathf.Clamp01(Mathf.LerpUnclamped(m_from[active], m_to[active], clip.Ease.Evaluate(ratio)));

            m_target.SetTrackState(clip.Look, ratio < 1f);
            m_coverageSlot.SetBase(coverage);

            // With nothing covered the wipe is ready to cover again from the start
            m_leavingSlot.SetBase(m_leaves[active] && coverage > 0f);
        }

#if UNITY_EDITOR
        public override void CollectDrivenProperties(List<DrivenProperty> properties)
        {
            if (m_target != null) {
                WipeCoverageChannel.Instance.CollectDrivenProperties(m_target, properties);
                WipeLeavingChannel.Instance.CollectDrivenProperties(m_target, properties);
            }
        }

        public override string GetWarning(Tweenline owner)
        {
            if (m_target == null) {
                return null;
            }

            var warning = TargetWarning();

            if (warning != null) {
                return warning;
            }

            for (var i = 0; i < m_clips.Count; i++) {
                var clip = m_clips[i];
                var styleWarning = m_target.StyleFor(clip.Look).GetWarning();

                if (styleWarning != null) {
                    return $"Clip [{i + 1}]: {styleWarning}";
                }

                if (clip.Amount <= 0f) {
                    return $"Clip [{i + 1}] has an Amount of 0, so it doesn't move";
                }
            }

            return null;
        }

        /// <summary>
        /// What is wrong with the target's setup, or null.
        /// </summary>
        protected virtual string TargetWarning()
        {
            return null;
        }
#endif
    }

    /// <summary>
    /// Covers and uncovers a Wipe.
    /// </summary>
    [Serializable]
    public class WipeTrack : WipeTrackBase<Wipe> { }

    /// <summary>
    /// Wipes a Wipe Reveal's canvas in and out. Cover brings the content in and Uncover takes it out.
    /// </summary>
    [Serializable]
    public class WipeRevealTrack : WipeTrackBase<WipeReveal>
    {
#if UNITY_EDITOR
        protected override string TargetWarning()
        {
            return m_target.GetWarning();
        }
#endif
    }
}
