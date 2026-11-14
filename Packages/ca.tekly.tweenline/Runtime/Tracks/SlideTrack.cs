using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace Tekly.Tweenlines.Tracks
{
    [Serializable]
    public struct SlideClip
    {
        public float Start;
        public float Duration;
        public TweenEase Ease;

        [Tooltip("In slides from off screen to the resting position. Out slides from the resting position to off screen")]
        public SlideMode Mode;

        [Tooltip("The side of the screen it slides in from or out to")]
        public SlideDirection Direction;

        [Tooltip("Direction in the parent's space when Direction is Custom")]
        public Vector3 CustomDirection;

        [Tooltip("What counts as the screen. A RectTransform or Canvas for UI, like a masked viewport, or a Camera for 3D. Empty uses the root canvas or Main Camera")]
        public Component OffScreenArea;

        public float End => Start + Duration;
    }

    public enum SlideMode
    {
        In,
        Out
    }

    public enum SlideDirection
    {
        Left,
        Right,
        Up,
        Down,
        Custom
    }

    /// <summary>
    /// Slides a target in from off screen or out to off screen. It is layered, so the slide offsets whichever
    /// position channel a base track drives (or the position the target had when the animation started) and
    /// lands exactly on the base value. The off screen distance is worked out when the animation starts from
    /// the screen area and the target's pose while off screen, including any rotation or scale from base
    /// tracks, so it works at any resolution.
    /// </summary>
    [Serializable]
    [MovedFrom(true, "Tekly.Common.Tweenlines.Tracks", "Tekly.Common.Runtime")]
    public class SlideTrack : TweenTrack, ILayeredTrack
    {
        public Transform Target {
            get => m_target;
            set => m_target = value;
        }

        public List<SlideClip> Clips => m_clips;

        public override Object TargetObject => m_target;
        public override Type TargetType => typeof(Transform);
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

        [SerializeField] private Transform m_target;
        [SerializeField] private List<SlideClip> m_clips = new List<SlideClip>();

        [NonSerialized] private ChannelSlot<Vector2> m_anchoredSlot;
        [NonSerialized] private ChannelSlot<Vector3> m_positionSlot;

        // Per clip offset from the resting position to off screen, worked out when the animation starts
        [NonSerialized] private Vector3[] m_offScreenVectors;

        private bool IsUi => m_target is RectTransform;

        public override bool Bind(GameObject gameObject)
        {
            m_target = gameObject.transform;
            return true;
        }

        public override int AddClip(float start)
        {
            // Slide in for the first clip and out after that, the usual show then hide order
            m_clips.Add(new SlideClip {
                Start = start,
                Duration = 0.5f,
                Ease = new TweenEase(EaseType.Cubic, m_clips.Count == 0 ? EaseDirection.Out : EaseDirection.In),
                Mode = m_clips.Count == 0 ? SlideMode.In : SlideMode.Out,
                Direction = SlideDirection.Left,
                CustomDirection = Vector3.left
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
            if (m_target == null) {
                return;
            }

            if (IsUi) {
                origins.GetOrCapture(m_target, TweenChannels.AnchoredPosition);
            }

            origins.GetOrCapture(m_target, TweenChannels.LocalPosition);
            origins.GetOrCapture(m_target, TweenChannels.LocalScale);
        }

        public override void Prepare(TweenMixer mixer)
        {
            m_anchoredSlot = null;
            m_positionSlot = null;

            if (!m_enabled || m_target == null || m_clips.Count == 0) {
                return;
            }

            // Layer onto whichever position channel a base track drives, preferring anchored position for UI
            if (IsUi && !mixer.HasSource(m_target, TweenChannels.LocalPosition)) {
                m_anchoredSlot = mixer.Get(m_target, TweenChannels.AnchoredPosition);
            } else {
                m_positionSlot = mixer.Get(m_target, TweenChannels.LocalPosition);
            }

            ResolveOffScreen(mixer);
        }

        public override void Evaluate(float time)
        {
            if (m_anchoredSlot == null && m_positionSlot == null) {
                return;
            }

            var offset = SampleOffset(time);

            if (m_anchoredSlot != null) {
                m_anchoredSlot.Offset(offset);
            } else {
                m_positionSlot.Offset(offset);
            }
        }

        /// <summary>
        /// The offset inside the clip covering the time. Before the first clip it holds that clip's start
        /// offset and after a clip it holds the clip's end offset, so scrubbing always gives the same pose.
        /// </summary>
        private Vector3 SampleOffset(float time)
        {
            var index = -1;
            var latest = float.MinValue;

            for (var i = 0; i < m_clips.Count; i++) {
                if (m_clips[i].Start <= time && m_clips[i].Start >= latest) {
                    latest = m_clips[i].Start;
                    index = i;
                }
            }

            if (index < 0) {
                var earliest = float.MaxValue;

                for (var i = 0; i < m_clips.Count; i++) {
                    if (m_clips[i].Start < earliest) {
                        earliest = m_clips[i].Start;
                        index = i;
                    }
                }

                return Offset(index, 0);
            }

            var clip = m_clips[index];

            if (clip.Duration <= 0 || time >= clip.End) {
                return Offset(index, 1);
            }

            return Offset(index, clip.Ease.Evaluate((time - clip.Start) / clip.Duration));
        }

        /// <summary>
        /// A clip's offset at an eased progress, from off screen to zero sliding in and zero to off screen
        /// sliding out.
        /// </summary>
        private Vector3 Offset(int index, float progress)
        {
            var amount = m_clips[index].Mode == SlideMode.In ? 1 - progress : progress;
            return m_offScreenVectors[index] * amount;
        }

        private void ResolveOffScreen(TweenMixer mixer)
        {
            var scaleSlot = mixer.Get(m_target, TweenChannels.LocalScale);

            if (m_offScreenVectors == null || m_offScreenVectors.Length != m_clips.Count) {
                m_offScreenVectors = new Vector3[m_clips.Count];
            }

            for (var i = 0; i < m_clips.Count; i++) {
                var clip = m_clips[i];
                var direction = DirectionVector(clip);

                if (direction == Vector3.zero) {
                    m_offScreenVectors[i] = Vector3.zero;
                    continue;
                }

                // Measure the pose it has while off screen: the start of a slide in, the end of a slide out
                var time = clip.Mode == SlideMode.In ? clip.Start : clip.End;
                var position = OffScreen.PositionAt(m_target, m_anchoredSlot, m_positionSlot, time);
                var rotation = OffScreen.RotationAt(mixer, m_target, time);
                var scale = scaleSlot.SampleBase(time);

                m_offScreenVectors[i] = OffScreen.Vector(m_target, clip.OffScreenArea, direction, position, rotation, scale);
            }
        }

        private static Vector3 DirectionVector(SlideClip clip)
        {
            switch (clip.Direction) {
                case SlideDirection.Left:
                    return Vector3.left;
                case SlideDirection.Right:
                    return Vector3.right;
                case SlideDirection.Up:
                    return Vector3.up;
                case SlideDirection.Down:
                    return Vector3.down;
                default:
                    return clip.CustomDirection.sqrMagnitude > 0 ? clip.CustomDirection.normalized : Vector3.zero;
            }
        }

#if UNITY_EDITOR
        public override void CollectDrivenProperties(List<DrivenProperty> properties)
        {
            if (m_target == null) {
                return;
            }

            TweenChannels.LocalPosition.CollectDrivenProperties(m_target, properties);

            if (IsUi) {
                TweenChannels.AnchoredPosition.CollectDrivenProperties(m_target, properties);
            }
        }

        public override string GetWarning(Tweenline owner)
        {
            if (m_target == null) {
                return null;
            }

            for (var i = 0; i < m_clips.Count; i++) {
                var clip = m_clips[i];
                var areaWarning = OffScreen.GetAreaWarning(m_target, clip.OffScreenArea);

                if (areaWarning != null) {
                    return $"Clip [{i + 1}] {areaWarning}";
                }

                if (DirectionVector(clip) == Vector3.zero) {
                    return $"Clip [{i + 1}] has a zero Custom Direction, so it doesn't move";
                }

                if (IsUi && OffScreen.AreaRect(m_target, clip.OffScreenArea) == null) {
                    return $"Clip [{i + 1}] has no Off Screen Area and the target isn't under a Canvas, so it doesn't move";
                }

                if (!IsUi && OffScreen.AreaCamera(clip.OffScreenArea) == null) {
                    return $"Clip [{i + 1}] has no Off Screen Area and there is no Main Camera, so it doesn't move";
                }
            }

            return null;
        }
#endif
    }
}
