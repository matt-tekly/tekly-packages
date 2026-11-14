using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using Object = UnityEngine.Object;

namespace Tekly.Tweenlines.Tracks
{
    [Serializable]
    public struct BounceClip
    {
        public float Start;
        public float Duration;

        [Tooltip("Starts in the air and falls in, like being dropped. The fall counts as the first hop")]
        public bool Drop;

        [Tooltip("Where a drop starts. Height drops from Height along Direction and Off Screen drops from just outside the canvas or camera view along Direction")]
        public BounceDropFrom DropFrom;

        [Tooltip("What counts as the screen when Drop From is Off Screen. A RectTransform or Canvas for UI, like a masked viewport, or a Camera for 3D. Empty uses the root canvas or Main Camera")]
        public Component OffScreenArea;

        [Tooltip("Height of the first hop, or the height it drops from")]
        public float Height;

        [Tooltip("Number of arcs including the fall when dropping. 1 drops and lands with no rebound")]
        [Min(1)] public int Hops;

        [Tooltip("Each hop's height relative to the previous one")]
        [Range(0, 1)] public float Decay;

        [Tooltip("Stretch along the bounce direction while in the air")]
        public float Stretch;

        [Tooltip("Squash on each impact, scaled by how hard it lands")]
        public float Squash;

        [Tooltip("Seconds each impact squash lasts")]
        [Min(0)] public float ImpactTime;

        [Tooltip("Squash before the first hop. 0 for none. Lasts ImpactTime. Ignored when dropping")]
        public float Anticipation;

        public float End => Start + Duration;
    }

    public enum BounceDropFrom
    {
        Height,
        OffScreen
    }

    public enum BouncePreserve
    {
        /// <summary>The other planar axis compensates, for UI and 2D</summary>
        Area,

        /// <summary>Both other axes compensate</summary>
        Volume,

        /// <summary>Only the bounce axis changes</summary>
        None
    }

    /// <summary>
    /// Hops a target with matching squash and stretch: stretched in the air, squashed on each impact. It is
    /// layered, so the hop offsets whichever position channel a base track drives (or the position the
    /// target had when the animation started) and the squash multiplies the scale channel. Outside its clips
    /// it leaves the target alone, and every clip ends back on the base values.
    /// </summary>
    [Serializable]
    [MovedFrom(true, "Tekly.Common.Tweenlines.Tracks", "Tekly.Common.Runtime")]
    public class BounceTrack : TweenTrack, ILayeredTrack
    {
        private const int MAX_HOPS = 32;

        public Transform Target {
            get => m_target;
            set => m_target = value;
        }

        public List<BounceClip> Clips => m_clips;

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

        [Tooltip("Direction of the hops in local space. The squash axis is its largest component")]
        [SerializeField] private Vector3 m_direction = Vector3.up;

        [SerializeField] private BouncePreserve m_preserve = BouncePreserve.Area;
        [SerializeField] private List<BounceClip> m_clips = new List<BounceClip>();

        [NonSerialized] private ChannelSlot<Vector2> m_anchoredSlot;
        [NonSerialized] private ChannelSlot<Vector3> m_positionSlot;
        [NonSerialized] private ChannelSlot<Vector3> m_scaleSlot;

        // Per clip drop offset from the landing position, worked out when the animation starts. Zero uses Height
        [NonSerialized] private Vector3[] m_dropVectors;

        /// <summary>
        /// UI heights are in pixels, 3D heights in units, so new clips pick defaults to match.
        /// </summary>
        private bool IsUi => m_target is RectTransform;

        private Vector3 Direction => m_direction.sqrMagnitude > 0 ? m_direction.normalized : Vector3.up;

        public override bool Bind(GameObject gameObject)
        {
            m_target = gameObject.transform;

            // UI squashes flat, 3D objects keep their volume
            m_preserve = IsUi ? BouncePreserve.Area : BouncePreserve.Volume;
            return true;
        }

        public override int AddClip(float start)
        {
            m_clips.Add(new BounceClip {
                Start = start,
                Duration = 1f,
                Height = IsUi ? 100f : 1f,
                Hops = 3,
                Decay = 0.45f,
                Stretch = 0.15f,
                Squash = 0.25f,
                ImpactTime = 0.08f,
                Anticipation = 0f
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
            m_scaleSlot = null;

            if (!m_enabled || m_target == null || m_clips.Count == 0) {
                return;
            }

            // Layer onto whichever position channel a base track drives, preferring anchored position for UI
            if (IsUi && !mixer.HasSource(m_target, TweenChannels.LocalPosition)) {
                m_anchoredSlot = mixer.Get(m_target, TweenChannels.AnchoredPosition);
            } else {
                m_positionSlot = mixer.Get(m_target, TweenChannels.LocalPosition);
            }

            m_scaleSlot = mixer.Get(m_target, TweenChannels.LocalScale);

            ResolveDrops(mixer);
        }

        public override void Evaluate(float time)
        {
            if (m_scaleSlot == null) {
                return;
            }

            var index = FindClip(time);

            if (index < 0) {
                return;
            }

            var clip = m_clips[index];
            var direction = Direction;

            // Off screen drops fall along the resolved offset instead of using Height
            if (m_dropVectors[index] != Vector3.zero) {
                var vector = m_dropVectors[index];
                clip.Height = vector.magnitude;
                direction = vector / clip.Height;
            }

            Sample(clip, time - clip.Start, out var height, out var stretch);

            var offset = direction * height;

            if (m_anchoredSlot != null) {
                m_anchoredSlot.Offset(offset);
            } else {
                m_positionSlot.Offset(offset);
            }

            m_scaleSlot.Multiply(ApplyStretch(Vector3.one, stretch, direction));
        }

        /// <summary>
        /// Finds the clip that covers the time, or -1 when none does.
        /// </summary>
        private int FindClip(float time)
        {
            var result = -1;
            var latest = float.MinValue;

            for (var i = 0; i < m_clips.Count; i++) {
                if (m_clips[i].Start <= time && m_clips[i].Start >= latest) {
                    latest = m_clips[i].Start;
                    result = i;
                }
            }

            if (result >= 0) {
                return time <= m_clips[result].End ? result : -1;
            }

            // Before a drop starts, hold its starting height so it doesn't show at rest first
            var earliest = float.MaxValue;

            for (var i = 0; i < m_clips.Count; i++) {
                if (m_clips[i].Start < earliest) {
                    earliest = m_clips[i].Start;
                    result = i;
                }
            }

            return result >= 0 && m_clips[result].Drop ? result : -1;
        }

        private void ResolveDrops(TweenMixer mixer)
        {
            if (m_dropVectors == null || m_dropVectors.Length != m_clips.Count) {
                m_dropVectors = new Vector3[m_clips.Count];
            }

            for (var i = 0; i < m_clips.Count; i++) {
                var clip = m_clips[i];
                m_dropVectors[i] = Vector3.zero;

                if (!clip.Drop || clip.DropFrom == BounceDropFrom.Height) {
                    continue;
                }

                // Measure the pose it has at the top of the drop, which is the start of the clip
                var position = OffScreen.PositionAt(m_target, m_anchoredSlot, m_positionSlot, clip.Start);
                var rotation = OffScreen.RotationAt(mixer, m_target, clip.Start);
                var scale = m_scaleSlot.SampleBase(clip.Start);

                m_dropVectors[i] = OffScreen.Vector(m_target, clip.OffScreenArea, Direction, position, rotation, scale);
            }
        }

        /// <summary>
        /// The clip is laid out as an optional anticipation squash, then each hop's air time followed by an
        /// impact squash. Air time scales with the square root of hop height, like real gravity.
        /// </summary>
        private static void Sample(BounceClip clip, float local, out float height, out float stretch)
        {
            height = 0;
            stretch = 0;

            if (local <= 0) {
                height = clip.Drop ? clip.Height : 0;
                return;
            }

            if (clip.Duration <= 0 || local >= clip.Duration) {
                return;
            }

            var hops = Mathf.Clamp(clip.Hops, 1, MAX_HOPS);
            var anticipationTime = clip.Anticipation != 0 && !clip.Drop ? clip.ImpactTime : 0;
            var airTotal = Mathf.Max(0, clip.Duration - anticipationTime - hops * clip.ImpactTime);
            var impactTime = clip.ImpactTime;

            // Squeeze impacts when the clip is too short for them
            if (airTotal <= 0) {
                var scale = clip.Duration / (anticipationTime + hops * clip.ImpactTime);
                anticipationTime *= scale;
                impactTime *= scale;
            }

            if (local < anticipationTime) {
                stretch = -clip.Anticipation * Mathf.Sin(Mathf.PI * local / anticipationTime);
                return;
            }

            local -= anticipationTime;

            // A drop's first arc is only the falling half, so it takes half the time of a full hop
            var rootSum = 0f;
            var hopHeight = 1f;
            for (var i = 0; i < hops; i++) {
                rootSum += Mathf.Sqrt(hopHeight) * ArcPortion(clip, i);
                hopHeight *= clip.Decay;
            }

            hopHeight = 1f;

            for (var i = 0; i < hops; i++) {
                var strength = Mathf.Sqrt(hopHeight);
                var portion = ArcPortion(clip, i);
                var air = rootSum > 0 ? airTotal * strength * portion / rootSum : 0;

                if (local < air) {
                    var u = 1 - portion + portion * local / air;
                    height = clip.Height * hopHeight * 4 * u * (1 - u);

                    if (IsFall(clip, i)) {
                        // Stretch builds as the drop speeds up and peaks at impact
                        var fall = (u - 0.5f) * 2;
                        stretch = clip.Stretch * strength * fall * fall;
                    } else {
                        var sin = Mathf.Sin(2 * Mathf.PI * u);
                        stretch = clip.Stretch * strength * sin * sin;
                    }

                    return;
                }

                local -= air;

                if (local < impactTime) {
                    var v = local / impactTime;
                    stretch = -clip.Squash * strength * Mathf.Sin(Mathf.PI * v);

                    // A drop arrives fully stretched, so the stretch snaps into the squash instead of popping
                    if (IsFall(clip, i)) {
                        var release = 1 - v;
                        stretch += clip.Stretch * strength * release * release * release;
                    }

                    return;
                }

                local -= impactTime;
                hopHeight *= clip.Decay;
            }
        }

        private static bool IsFall(BounceClip clip, int arc)
        {
            return clip.Drop && arc == 0;
        }

        private static float ArcPortion(BounceClip clip, int arc)
        {
            return IsFall(clip, arc) ? 0.5f : 1f;
        }

        private Vector3 ApplyStretch(Vector3 scale, float stretch, Vector3 direction)
        {
            if (stretch == 0) {
                return scale;
            }

            var axis = LargestAxis(direction);
            var factor = Mathf.Max(0.01f, 1 + stretch);

            scale[axis] *= factor;

            switch (m_preserve) {
                case BouncePreserve.Area when axis != 2:
                    scale[axis == 0 ? 1 : 0] /= factor;
                    break;
                case BouncePreserve.Area:
                case BouncePreserve.Volume:
                    var compensate = 1 / Mathf.Sqrt(factor);

                    for (var i = 0; i < 3; i++) {
                        if (i != axis) {
                            scale[i] *= compensate;
                        }
                    }

                    break;
            }

            return scale;
        }

        private static int LargestAxis(Vector3 direction)
        {
            var x = Mathf.Abs(direction.x);
            var y = Mathf.Abs(direction.y);
            var z = Mathf.Abs(direction.z);

            if (x >= y && x >= z) {
                return 0;
            }

            return y >= z ? 1 : 2;
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

            TweenChannels.LocalScale.CollectDrivenProperties(m_target, properties);
        }

        public override string GetWarning(Tweenline owner)
        {
            if (m_target == null) {
                return null;
            }

            for (var i = 0; i < m_clips.Count; i++) {
                var clip = m_clips[i];

                if (!clip.Drop) {
                    continue;
                }

                if (clip.DropFrom == BounceDropFrom.OffScreen) {
                    var areaWarning = OffScreen.GetAreaWarning(m_target, clip.OffScreenArea);

                    if (areaWarning != null) {
                        return $"Clip [{i + 1}] {areaWarning}";
                    }

                    if (IsUi && OffScreen.AreaRect(m_target, clip.OffScreenArea) == null) {
                        return $"Clip [{i + 1}] drops from off screen but the target isn't under a Canvas, so it uses Height";
                    }

                    if (!IsUi && OffScreen.AreaCamera(clip.OffScreenArea) == null) {
                        return $"Clip [{i + 1}] drops from off screen but there is no Main Camera, so it uses Height";
                    }
                }
            }

            return null;
        }

#endif
    }
}
