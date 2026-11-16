using System;
using System.Collections.Generic;
using Tekly.Tweenlines;
using UnityEngine;

namespace Tekly.Wipes
{
    /// <summary>
    /// Where the wipe goes when. Every pixel gets a value from the field, and low values are covered first.
    /// </summary>
    public enum WipeField
    {
        /// <summary>A straight edge travelling at an angle, or two edges from a center line when mirrored</summary>
        Linear,

        /// <summary>An iris closing on a center: a circle, diamond, square or star</summary>
        Radial,

        /// <summary>Arms sweeping around a center: a clock wipe, a pinwheel or a swirl</summary>
        Angular,

        /// <summary>A greyscale texture, dark parts first, for ink, brush strokes and anything bespoke</summary>
        Ramp,

        /// <summary>An iris in the shape of a silhouette, like a character's head or a logo</summary>
        Shape,

        /// <summary>No shape. The whole rect fades</summary>
        Fade
    }

    public enum WipeRadialShape
    {
        Circle,
        Diamond,
        Square,
        Star
    }

    /// <summary>
    /// Repeats the wipe as elements that each start when the field reaches their center.
    /// </summary>
    public enum WipeRepeat
    {
        None,

        /// <summary>Parallel lanes of bars or blinds</summary>
        Lanes,

        /// <summary>A grid of shapes that grow</summary>
        Grid
    }

    public enum WipeLaneElement
    {
        /// <summary>A blind that grows across its lane</summary>
        Grow,

        /// <summary>A bar that slides along its lane</summary>
        Slide
    }

    public enum WipeCap
    {
        Flat,
        Round,
        Chevron
    }

    public enum WipeGridElement
    {
        Diamond,
        Circle,
        Square
    }

    public enum WipeWave
    {
        ZigZag,
        Sine
    }

    /// <summary>
    /// What Uncover does.
    /// </summary>
    public enum WipeExit
    {
        /// <summary>The wipe carries on through, uncovering in the order it covered</summary>
        Continue,

        /// <summary>The wipe goes back the way it came</summary>
        Rewind
    }

    /// <summary>
    /// How a wipe looks and moves: its field, an optional repeat with staggered elements, its edge, its
    /// colour layers and its default timing. A wipe reads it every time it refreshes, so call the owner's
    /// Refresh after changing it from code.
    /// </summary>
    [Serializable]
    public class WipeStyle
    {
        public const int MAX_LAYERS = 3;

        public WipeField Field = WipeField.Linear;

        [Tooltip("Run the field backwards: a wipe starts from the other side, an iris grows from its center instead of closing on it")]
        public bool Invert;

        [Tooltip("The direction the wipe travels in degrees. 0 is up and angles turn clockwise, so 90 travels right")]
        public float Angle = 90;

        [Tooltip("Cover from a line through the center out to both sides, like barn doors")]
        public bool Mirror;

        [Tooltip("Where the field is centered, from (0, 0) at the bottom left to (1, 1) at the top right. A Wipe Focus replaces it")]
        public Vector2 Center = new Vector2(0.5f, 0.5f);

        public WipeRadialShape RadialShape = WipeRadialShape.Circle;

        [Tooltip("How many points the star has")]
        [Range(3, 12)] public int Points = 5;

        [Tooltip("How deep the star's valleys are. 1 is a regular polygon with no valleys")]
        [Range(0.2f, 1f)] public float Inner = 0.5f;

        [Tooltip("Turns the shape, in degrees")]
        public float Rotation;

        [Tooltip("How many turns the shape makes while the wipe covers")]
        public float Spin;

        [Tooltip("Where an arm starts in degrees. 0 is up and angles turn clockwise")]
        public float StartAngle;

        [Tooltip("How many arms sweep around at once. 1 is a clock wipe")]
        [Range(1, 12)] public int Arms = 1;

        [Tooltip("How many turns an arm bends through from the center to the farthest corner. 0 is straight")]
        public float Twist;

        public bool Clockwise = true;

        [Tooltip("A greyscale texture. Dark parts are covered first, and the wipe is stretched over the values that are on screen")]
        public WipeRamp Ramp;

        [Tooltip("Fill keeps the ramp's aspect and crops what overflows. Stretch fits it to the rect")]
        public WipeRampFit RampFit = WipeRampFit.Fill;

        [Tooltip("The silhouette the iris closes through")]
        public WipeShape Shape;

        public WipeRepeat Repeat = WipeRepeat.None;

        [Tooltip("The direction the lanes run in degrees. 0 is up and angles turn clockwise")]
        public float LaneAngle = 90;

        [Tooltip("Lane width as a fraction of the rect's height")]
        [Range(0.02f, 1f)] public float LaneWidth = 0.2f;

        public WipeLaneElement LaneElement = WipeLaneElement.Slide;

        [Tooltip("The shape of a sliding bar's end")]
        public WipeCap Cap = WipeCap.Round;

        [Tooltip("Cell size as a fraction of the rect's height")]
        [Range(0.02f, 1f)] public float GridSize = 0.1f;

        [Tooltip("Turn the grid 45 degrees")]
        public bool Diagonal;

        public WipeGridElement GridElement = WipeGridElement.Diamond;

        [Tooltip("How much of the wipe is spent waiting for the field to reach each element. 0 moves every element together, 1 snaps each element in as the field reaches it")]
        [Range(0f, 1f)] public float Spread = 0.5f;

        [Tooltip("Mixes a random start into each element, so they don't follow the field evenly")]
        [Range(0f, 1f)] public float Random;

        [Tooltip("Every second lane runs the other way")]
        public bool Alternate;

        [Tooltip("Width of the edge as a fraction of the whole sweep. 0 is a crisp, antialiased edge")]
        [Range(0f, 0.5f)] public float Softness;

        [Tooltip("How far a zig zag or wave pushes the edge, as a fraction of the sweep. 0 is a straight edge")]
        [Range(0f, 0.3f)] public float WaveAmplitude;

        [Tooltip("How many zig zags or waves fit in the rect's height, or around a radial field")]
        [Range(1f, 40f)] public float WaveFrequency = 8;

        public WipeWave Wave = WipeWave.ZigZag;

        [Tooltip("The colour the wipe covers with. A Wipe with a sprite, or a Wipe Reveal's content, replaces it")]
        public Color Fill = Color.black;

        [Tooltip("Colours that lead the fill, first to last. Up to three")]
        public List<Color> Layers = new List<Color>();

        [Tooltip("How far each layer trails the one before it, as a fraction of the wipe")]
        [Range(0f, 0.3f)] public float LayerOffset = 0.08f;

        [Tooltip("Continue carries the wipe on through when uncovering. Rewind goes back the way it came")]
        public WipeExit Exit = WipeExit.Continue;

        [Tooltip("Seconds Cover takes when called from code. A Tweenline clip uses its own duration")]
        [Min(0)] public float CoverDuration = 0.45f;

        [Tooltip("The ease Cover uses when called from code. A Tweenline clip uses its own ease")]
        public TweenEase CoverEase = new TweenEase(EaseType.Cubic, EaseDirection.Out);

        [Tooltip("Seconds Uncover takes when called from code. A Tweenline clip uses its own duration")]
        [Min(0)] public float UncoverDuration = 0.4f;

        [Tooltip("The ease Uncover uses when called from code. A Tweenline clip uses its own ease")]
        public TweenEase UncoverEase = new TweenEase(EaseType.Cubic, EaseDirection.In);

        /// <summary>
        /// The field that is drawn: the style's, or a simpler one when it is missing what it needs.
        /// </summary>
        public WipeField EffectiveField {
            get {
                if (Field == WipeField.Ramp && !HasRamp) {
                    return WipeField.Linear;
                }

                if (Field == WipeField.Shape && (Shape == null || !Shape.IsBaked)) {
                    return WipeField.Radial;
                }

                return Field;
            }
        }

        /// <summary>
        /// The repeat that is drawn. A silhouette and a fade have no field for an element to sample.
        /// </summary>
        public WipeRepeat EffectiveRepeat => CanRepeat(EffectiveField) ? Repeat : WipeRepeat.None;

        /// <summary>
        /// True when Uncover carries on through. A silhouette can only go back the way it came.
        /// </summary>
        public bool ExitContinues => Exit == WipeExit.Continue && EffectiveField != WipeField.Shape;

        private bool HasRamp => Ramp != null && Ramp.Texture != null;

        public static bool CanRepeat(WipeField field)
        {
            return field != WipeField.Shape && field != WipeField.Fade;
        }

        public static bool HasCenter(WipeField field)
        {
            return field == WipeField.Radial || field == WipeField.Angular || field == WipeField.Shape;
        }

        /// <summary>
        /// What is wrong with the style, or null when it draws as set up.
        /// </summary>
        public string GetWarning()
        {
            if (Field == WipeField.Ramp && !HasRamp) {
                return "The Ramp field needs a Wipe Ramp with a texture. It draws as a Linear wipe until it has one";
            }

            if (Field == WipeField.Shape) {
                if (Shape == null) {
                    return "The Shape field needs a Wipe Shape. It draws as a circle until it has one";
                }

                if (!Shape.IsBaked) {
                    return $"The Wipe Shape [{Shape.name}] has no source texture. It draws as a circle until it has one";
                }

                if (Shape.Inscribed <= 0f) {
                    return $"The Wipe Shape [{Shape.name}] is empty at its center, so the iris can't open around it";
                }
            }

            if (Layers.Count > MAX_LAYERS) {
                return $"Only the first [{MAX_LAYERS}] layers are drawn";
            }

            return null;
        }
    }
}
