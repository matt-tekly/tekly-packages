using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Tweenlines.Tracks
{
    [Serializable]
    public struct ShakeClip : IClip
    {
        public float Start;
        public float Duration;

        [Tooltip("The furthest it moves on each axis. Pixels for UI, units for 3D. Zero leaves the position alone")]
        public Vector3 Position;

        [Tooltip("The furthest it turns on each axis, in degrees. Zero leaves the rotation alone")]
        public Vector3 Rotation;

        [Tooltip("How many times a second it changes direction")]
        [Min(0.01f)] public float Frequency;

        [Tooltip("How quickly the shake dies down over the clip. 0 keeps full strength until the clip ends, 1 fades evenly, higher fades sooner")]
        [Min(0)] public float Damping;

        [Tooltip("Picks the pattern of the shake. The same seed always gives the same shake, so scrubbing shows what will play")]
        public int Seed;

        public float End => Start + Duration;

        float IClip.Start => Start;
    }

    /// <summary>
    /// Shakes a target's position, rotation or both. It is layered, so the shake is added onto whichever
    /// position and rotation a base track drives (or the pose the target had when the animation started).
    /// Outside its clips it leaves the target alone. The shake is smooth noise that only depends on the
    /// clip's seed and the time, so it can be scrubbed and looks the same every play.
    /// </summary>
    [Serializable]
    public class ShakeTrack : TweenTrack, ILayeredTrack
    {
        private const int ROTATION_AXIS = 3;

        public Transform Target {
            get => m_target;
            set => m_target = value;
        }

        public List<ShakeClip> Clips => m_clips;

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
        [SerializeField] private List<ShakeClip> m_clips = new List<ShakeClip>();

        [NonSerialized] private PositionLayer m_position;
        [NonSerialized] private ChannelSlot<Vector3> m_rotationSlot;

        private bool IsUi => m_target is RectTransform;

        public override bool Bind(GameObject gameObject)
        {
            m_target = gameObject.transform;
            return true;
        }

        public override int AddClip(float start)
        {
            m_clips.Add(new ShakeClip {
                Start = start,
                Duration = 0.5f,
                Position = IsUi ? new Vector3(20, 20, 0) : new Vector3(0.2f, 0.2f, 0),
                Frequency = 25f,
                Damping = 1f,
                Seed = UnityEngine.Random.Range(0, 10000)
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

            if (Uses(clip => clip.Position)) {
                PositionLayer.CaptureOrigins(origins, m_target);
            }

            if (Uses(clip => clip.Rotation)) {
                origins.GetOrCapture(m_target, TweenChannels.LocalRotation);
            }
        }

        public override void Prepare(TweenMixer mixer)
        {
            m_position.Clear();
            m_rotationSlot = null;

            if (!m_enabled || m_target == null || m_clips.Count == 0) {
                return;
            }

            // Only the channels a clip shakes are taken, so the others stay free for layouts and other code
            if (Uses(clip => clip.Position)) {
                m_position.Prepare(mixer, m_target);
            }

            if (Uses(clip => clip.Rotation)) {
                m_rotationSlot = mixer.Get(m_target, TweenChannels.LocalRotation);
            }
        }

        public override void Evaluate(float time)
        {
            if (!m_position.IsPrepared && m_rotationSlot == null) {
                return;
            }

            var index = ClipOrder.FindCovering(m_clips, time);

            if (index < 0) {
                return;
            }

            var clip = m_clips[index];
            var local = time - clip.Start;

            if (clip.Duration <= 0 || local >= clip.Duration) {
                return;
            }

            var cycles = local * Mathf.Max(0.01f, clip.Frequency);

            // Builds up over the first change of direction so it starts from rest, then dies down
            var strength = Mathf.Min(1, cycles);

            if (clip.Damping > 0) {
                strength *= Mathf.Pow(1 - local / clip.Duration, clip.Damping);
            }

            if (m_position.IsPrepared && clip.Position != Vector3.zero) {
                m_position.Offset(Sample(clip.Position, clip.Seed, 0, cycles) * strength);
            }

            if (m_rotationSlot != null && clip.Rotation != Vector3.zero) {
                m_rotationSlot.Offset(Sample(clip.Rotation, clip.Seed, ROTATION_AXIS, cycles) * strength);
            }
        }

        private bool Uses(Func<ShakeClip, Vector3> amount)
        {
            for (var i = 0; i < m_clips.Count; i++) {
                var clip = m_clips[i];
                if (amount(clip) != Vector3.zero) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Noise for three axes scaled by the furthest each one moves. Axes with no movement are skipped.
        /// </summary>
        private static Vector3 Sample(Vector3 amount, int seed, int firstAxis, float cycles)
        {
            var result = Vector3.zero;

            for (var i = 0; i < 3; i++) {
                if (amount[i] != 0) {
                    result[i] = amount[i] * Noise(seed, firstAxis + i, cycles);
                }
            }

            return result;
        }

        /// <summary>
        /// Smooth noise between -1 and 1: a swing to one side at each whole cycle, eased from one to the next.
        /// Each axis is shifted by a different part of a cycle so the axes don't all turn at the same moment.
        /// </summary>
        private static float Noise(int seed, int axis, float cycles)
        {
            var position = cycles + (TweenRandom.Hash(seed, axis, -1) + 1) * 0.5f;
            var index = Mathf.FloorToInt(position);
            var blend = position - index;

            blend = blend * blend * (3 - 2 * blend);

            return Mathf.LerpUnclamped(Swing(seed, axis, index), Swing(seed, axis, index + 1), blend);
        }

        /// <summary>
        /// How far a cycle swings. The side alternates every cycle so it keeps crossing the middle, which is
        /// what reads as a shake, and the size is random so it doesn't look like a steady wave.
        /// </summary>
        private static float Swing(int seed, int axis, int index)
        {
            var size = 0.4f + 0.3f * (TweenRandom.Hash(seed, axis, index) + 1);
            var startsPositive = TweenRandom.Hash(seed, axis, -2) > 0;

            return (index & 1) == 0 == startsPositive ? size : -size;
        }

#if UNITY_EDITOR
        public override void CollectDrivenProperties(List<DrivenProperty> properties)
        {
            if (m_target == null) {
                return;
            }

            if (Uses(clip => clip.Position)) {
                PositionLayer.CollectDrivenProperties(m_target, properties);
            }

            if (Uses(clip => clip.Rotation)) {
                TweenChannels.LocalRotation.CollectDrivenProperties(m_target, properties);
            }
        }

        public override string GetWarning(Tweenline owner)
        {
            for (var i = 0; i < m_clips.Count; i++) {
                if (m_clips[i].Position == Vector3.zero && m_clips[i].Rotation == Vector3.zero) {
                    return $"Clip [{i + 1}] has no Position or Rotation, so it doesn't shake";
                }
            }

            return null;
        }
#endif
    }
}
