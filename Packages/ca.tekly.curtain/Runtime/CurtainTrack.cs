using System;
using System.Collections.Generic;
using Tekly.Tweenlines;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Curtains
{
    /// <summary>
    /// A Curtain's progress.
    /// </summary>
    public sealed class CurtainProgressChannel : FloatChannel
    {
        public static readonly CurtainProgressChannel Instance = new CurtainProgressChannel();

        public override string Name => "curtainProgress";
        public override float Read(Object target) => ((ICurtainProgress) target).Progress;
        public override void Write(Object target, float value) => ((ICurtainProgress) target).Progress = value;

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
        {
            properties.Add(new DrivenProperty(target, "m_progress"));
        }
#endif
    }

    /// <summary>
    /// The progress of a content canvas's shared curtain, kept by the CurtainStage. Outside play mode there
    /// is no stage, so it reads as shown and writes do nothing.
    /// </summary>
    public sealed class SharedCurtainChannel : FloatChannel
    {
        public static readonly SharedCurtainChannel Instance = new SharedCurtainChannel();

        public override string Name => "sharedCurtainProgress";

        public override float Read(Object target)
        {
            var stage = CurtainStage.Instance;
            return stage != null ? stage.GetProgress((Canvas) target) : CurtainProgress.COVERED;
        }

        public override void Write(Object target, float value)
        {
            CurtainStage.Instance?.SetProgress((Canvas) target, value);
        }

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties) { }
#endif
    }

    /// <summary>
    /// Moves a Curtain's progress. A new clip runs it from 1 to 2, revealing what is behind as the curtain
    /// passes through. Use 0 to 1 to cover, and 1 back to 0 to rewind the way it came.
    /// </summary>
    [Serializable]
    public class CurtainTrack : ClipTrack<Curtain, float>
    {
        protected override TweenChannel<float> Channel => CurtainProgressChannel.Instance;

        protected override float Lerp(float a, float b, float t) => Mathf.LerpUnclamped(a, b, t);
        protected override float Add(float origin, float offset) => origin + offset;
        protected override TweenClip<float> CreateClip(float start) => CurtainProgress.CreateClip(start);
    }

    /// <summary>
    /// Reveals a content canvas through a shared curtain look, drawn by the CurtainStage, so an overlay only
    /// holds its content and its Tweenline. The content keeps running its own materials and animations
    /// while it moves, because it renders live into a texture.
    ///
    /// Clips work like a Curtain's: 0 to 1 brings the content in, 1 to 2 carries it out the far side and
    /// 1 back to 0 rewinds. At 0 and 2 the content is hidden, and at exactly 1 it renders normally. A new
    /// clip runs 0 to 1, since revealing content is the common case.
    ///
    /// The target must be a screen space root canvas, so the root of an overlay can't be a canvas. Effects
    /// that read the screen, such as a grab pass, don't work while the content moves, and materials need to
    /// write sensible alpha for the edges to blend. There is nothing to preview outside play mode.
    /// </summary>
    [Serializable]
    public class SharedCurtainTrack : ClipTrack<Canvas, float>
    {
        public CurtainLook Look {
            get => m_look;
            set => m_look = value;
        }

        protected override TweenChannel<float> Channel => SharedCurtainChannel.Instance;

        [SerializeField] private CurtainLook m_look;

        public override void Prepare(TweenMixer mixer)
        {
            base.Prepare(mixer);

            if (m_enabled && m_target != null) {
                CurtainStage.Instance?.SetLook(m_target, m_look);
            }
        }

        protected override float Lerp(float a, float b, float t) => Mathf.LerpUnclamped(a, b, t);
        protected override float Add(float origin, float offset) => origin + offset;

        protected override TweenClip<float> CreateClip(float start)
        {
            var clip = CurtainProgress.CreateClip(start);
            clip.From = 0;
            clip.To = CurtainProgress.COVERED;
            return clip;
        }

#if UNITY_EDITOR
        public override string GetWarning(Tweenline owner)
        {
            if (m_look == null) {
                return "Needs a Curtain Look";
            }

            if (m_target != null && (!m_target.isRootCanvas || m_target.renderMode == RenderMode.WorldSpace)) {
                return "The target must be a screen space root canvas";
            }

            return null;
        }
#endif
    }
}
