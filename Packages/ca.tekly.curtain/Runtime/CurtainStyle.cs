using System;
using System.Collections.Generic;
using Tekly.Tweenlines;
using UnityEngine;

namespace Tekly.Curtains
{
    public enum CurtainShape
    {
        /// <summary>A straight wipe across the rect at an angle, or a split that opens from its center line</summary>
        Diagonal,

        /// <summary>A circle, or a diamond, that grows from a center</summary>
        Iris,

        /// <summary>A greyscale ramp texture: dark parts go first</summary>
        Gradient
    }

    /// <summary>
    /// Something with a curtain's progress, such as a Curtain, for the Tweenline channel to drive.
    /// </summary>
    public interface ICurtainProgress
    {
        /// <summary>
        /// How far the curtain has travelled. At 0 it hasn't arrived, at 1 it covers everything and at 2 it
        /// has passed through. From 0 to 1 the bands arrive in order and the last one ends on top. From 1 to 2
        /// the last band leaves first, moving on the way it came in.
        ///
        /// Transition in with 1 to 2, out with 0 to 1. A load screen covers with 0 to 1, loads, then reveals
        /// with 1 to 2. Going from 1 back to 0 rewinds the way it came.
        /// </summary>
        float Progress { get; set; }
    }

    public static class CurtainProgress
    {
        /// <summary>
        /// Progress where the curtain covers everything.
        /// </summary>
        public const float COVERED = 1f;

        /// <summary>
        /// Progress where the curtain has passed through and left.
        /// </summary>
        public const float PASSED = 2f;

        public static float Clamp(float progress)
        {
            return Mathf.Clamp(progress, 0f, PASSED);
        }

        /// <summary>
        /// A new curtain clip: 1 to 2, revealing what is behind as the curtain passes through.
        /// </summary>
        public static TweenClip<float> CreateClip(float start)
        {
            return new TweenClip<float> {
                Start = start,
                Duration = 0.6f,
                FromMode = ClipFrom.Value,
                Space = ClipSpace.Absolute,
                From = COVERED,
                To = PASSED
            };
        }
    }

    /// <summary>
    /// How a curtain looks and moves: its shape, its colour bands and their timing. A curtain reads it every
    /// time it refreshes, so call the owner's Refresh after changing it.
    /// </summary>
    [Serializable]
    public class CurtainStyle
    {
        public CurtainShape Shape;

        [Tooltip("Run the field backwards: a wipe starts from the other side, a split closes from the edges in, an iris closes from the outside in")]
        public bool Invert;

        [Tooltip("Diagonal: the direction the wipe travels in degrees. 0 is up and angles turn clockwise")]
        public float Angle = 45;

        [Tooltip("Diagonal: split from a line through the center instead of wiping")]
        public bool Split;

        [Tooltip("Iris: where it grows from, from (0, 0) at the bottom left to (1, 1) at the top right")]
        public Vector2 Center = new Vector2(0.5f, 0.5f);

        [Tooltip("Iris: grow as a diamond instead of a circle")]
        public bool Diamond;

        [Tooltip("Gradient: the ramp. Dark parts go first")]
        public CurtainGradient Gradient;

        [Tooltip("Width of each band's edge as a fraction of the whole sweep. 0 is a crisp, antialiased edge")]
        [Range(0, 0.5f)] public float Softness;

        [Tooltip("Colour bands, first to last. A later band replaces the earlier ones, so a clear band opens a hole")]
        public List<Color> Bands = new List<Color> { Color.black };

        [Tooltip("How far apart the bands start, as a fraction of the progress")]
        [Range(0, 1)] public float Stagger = 0.15f;

        [Tooltip("The ease each band moves with over its own part of the progress")]
        public TweenEase Ease = new TweenEase(EaseType.Linear, EaseDirection.Out);
    }
}
