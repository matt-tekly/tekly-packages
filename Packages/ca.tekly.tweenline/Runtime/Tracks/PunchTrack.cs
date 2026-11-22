using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Tweenlines.Tracks
{
    [Serializable]
    public struct PunchClip : IClip
    {
        public float Start;
        public float Duration;

        [Tooltip("How much bigger the kick makes it on each axis, as a fraction of its scale. 0.25 is 25 percent bigger and a negative value shrinks it. Zero leaves the scale alone")]
        public Vector3 Scale;

        [Tooltip("How far the kick turns it on each axis, in degrees. Zero leaves the rotation alone")]
        public Vector3 Rotation;

        [Tooltip("How far the kick moves it on each axis. Pixels for UI, units for 3D. Zero leaves the position alone")]
        public Vector3 Position;

        [Tooltip("Size of each swing back relative to the one before it. Higher keeps wobbling for longer, so it needs a longer clip. 0 kicks out and settles without swinging back past the start")]
        [Range(0, PunchTrack.MAX_FALLOFF)] public float Falloff;

        public float End => Start + Duration;

        float IClip.Start => Start;
    }

    /// <summary>
    /// Kicks a target's scale, rotation or position and lets it spring back. It is layered, so the kick
    /// multiplies the scale and is added onto the rotation and position that base tracks drive (or the pose
    /// the target had when the animation started). Every clip starts and ends on the base values, and
    /// outside its clips it leaves the target alone.
    /// </summary>
    [Serializable]
    public class PunchTrack : TweenTrack, ILayeredTrack
    {
        public const float MAX_FALLOFF = 0.9f;

        private const float MIN_FALLOFF = 0.001f;

        // The swings have shrunk to 1 percent of the kick when the clip ends. Stored as -ln(0.01)
        private const float END_DECAY = 4.60517f;

        public Transform Target {
            get => m_target;
            set => m_target = value;
        }

        public List<PunchClip> Clips => m_clips;

        public override Object TargetObject => m_target;
        public override Type TargetType => typeof(Transform);
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

        [SerializeField] private Transform m_target;
        [SerializeField] private List<PunchClip> m_clips = new List<PunchClip>();

        [NonSerialized] private PositionLayer m_position;
        [NonSerialized] private ChannelSlot<Vector3> m_rotationSlot;
        [NonSerialized] private ChannelSlot<Vector3> m_scaleSlot;

        private bool IsUi => m_target is RectTransform;

        public override bool Bind(GameObject gameObject)
        {
            m_target = gameObject.transform;
            return true;
        }

        public override int AddClip(float start)
        {
            // UI is flat, so its depth is left alone
            m_clips.Add(new PunchClip {
                Start = start,
                Duration = 0.4f,
                Scale = IsUi ? new Vector3(0.25f, 0.25f, 0) : new Vector3(0.25f, 0.25f, 0.25f),
                Falloff = 0.35f
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

            GetUsedChannels(out var usesPosition, out var usesRotation, out var usesScale);

            if (usesPosition) {
                PositionLayer.CaptureOrigins(origins, m_target);
            }

            if (usesRotation) {
                origins.GetOrCapture(m_target, TweenChannels.LocalRotation);
            }

            if (usesScale) {
                origins.GetOrCapture(m_target, TweenChannels.LocalScale);
            }
        }

        public override void Prepare(TweenMixer mixer)
        {
            m_position.Clear();
            m_rotationSlot = null;
            m_scaleSlot = null;

            if (!m_enabled || m_target == null || m_clips.Count == 0) {
                return;
            }

            // Only the channels a clip kicks are taken, so the others stay free for layouts and other code
            GetUsedChannels(out var usesPosition, out var usesRotation, out var usesScale);

            if (usesPosition) {
                m_position.Prepare(mixer, m_target);
            }

            if (usesRotation) {
                m_rotationSlot = mixer.Get(m_target, TweenChannels.LocalRotation);
            }

            if (usesScale) {
                m_scaleSlot = mixer.Get(m_target, TweenChannels.LocalScale);
            }
        }

        public override void Evaluate(float time)
        {
            if (!m_position.IsPrepared && m_rotationSlot == null && m_scaleSlot == null) {
                return;
            }

            var index = ClipOrder.FindCovering(m_clips, time);

            if (index < 0) {
                return;
            }

            var clip = m_clips[index];

            if (clip.Duration <= 0) {
                return;
            }

            var kick = Kick((time - clip.Start) / clip.Duration, clip.Falloff);

            if (kick == 0) {
                return;
            }

            if (m_position.IsPrepared && clip.Position != Vector3.zero) {
                m_position.Offset(clip.Position * kick);
            }

            if (m_rotationSlot != null && clip.Rotation != Vector3.zero) {
                m_rotationSlot.Offset(clip.Rotation * kick);
            }

            if (m_scaleSlot != null && clip.Scale != Vector3.zero) {
                m_scaleSlot.Multiply(Vector3.one + clip.Scale * kick);
            }
        }

        /// <summary>
        /// Which channels any clip kicks, so the track only takes those.
        /// </summary>
        private void GetUsedChannels(out bool position, out bool rotation, out bool scale)
        {
            position = false;
            rotation = false;
            scale = false;

            var count = m_clips.Count;

            for (var i = 0; i < count; i++) {
                var clip = m_clips[i];
                position |= clip.Position != Vector3.zero;
                rotation |= clip.Rotation != Vector3.zero;
                scale |= clip.Scale != Vector3.zero;
            }
        }

        /// <summary>
        /// How far out the kick is for a progress from 0 to 1, where 1 is the full kick and negative values
        /// are swings back past the start. It is a spring that was struck: a sine inside a fading envelope
        /// that starts at 0, reaches 1 early, and has each swing after that the falloff times the one before.
        /// The swing rate is picked so the swings have just died away as the clip ends, so the motion fills
        /// the clip however long it is.
        /// </summary>
        private static float Kick(float progress, float falloff)
        {
            if (progress <= 0 || progress >= 1) {
                return 0;
            }

            var decay = -Mathf.Log(Mathf.Clamp(falloff, MIN_FALLOFF, MAX_FALLOFF)) / Mathf.PI;

            // The fade pulls the first peak earlier than a quarter turn. Dividing by its height makes it 1
            var peakPhase = Mathf.Atan(1 / decay);
            var peak = Mathf.Exp(-decay * peakPhase) * Mathf.Sin(peakPhase);

            // How far the swing has turned by the end of the clip, which is when it has died away
            var phase = (peakPhase + END_DECAY / decay) * progress;

            // Eases out what swing is left over the back half of the clip, so it ends exactly on the base
            var squared = progress * progress;
            var landing = 1 - squared * squared;

            return Mathf.Exp(-decay * phase) * Mathf.Sin(phase) / peak * landing;
        }

#if UNITY_EDITOR
        public override void CollectDrivenProperties(List<DrivenProperty> properties)
        {
            if (m_target == null) {
                return;
            }

            GetUsedChannels(out var usesPosition, out var usesRotation, out var usesScale);

            if (usesPosition) {
                PositionLayer.CollectDrivenProperties(m_target, properties);
            }

            if (usesRotation) {
                TweenChannels.LocalRotation.CollectDrivenProperties(m_target, properties);
            }

            if (usesScale) {
                TweenChannels.LocalScale.CollectDrivenProperties(m_target, properties);
            }
        }

        public override string GetWarning(Tweenline owner)
        {
            for (var i = 0; i < m_clips.Count; i++) {
                var clip = m_clips[i];

                if (clip.Scale == Vector3.zero && clip.Rotation == Vector3.zero && clip.Position == Vector3.zero) {
                    return $"Clip [{i + 1}] has no Scale, Rotation or Position, so it doesn't kick";
                }
            }

            return null;
        }
#endif
    }
}
