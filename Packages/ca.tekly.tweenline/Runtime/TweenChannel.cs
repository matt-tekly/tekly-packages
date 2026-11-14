using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Tweenlines
{
    /// <summary>
    /// A property tracks can drive, like a Transform's local position. Channels are shared instances in
    /// TweenChannels, so tracks driving the same property always meet in the same channel.
    /// </summary>
    public abstract class TweenChannel
    {
        public abstract string Name { get; }
    }

    /// <summary>
    /// Reads, writes and combines one value type. Base tracks set a channel's value, layered tracks add an
    /// offset or multiply it, and the result is written once per evaluation.
    /// </summary>
    public abstract class TweenChannel<T> : TweenChannel
    {
        public abstract T One { get; }
        public abstract T Zero { get; }

        public abstract T Read(Object target);
        public abstract void Write(Object target, T value);
        public abstract T Multiply(T a, T b);
        public abstract T Add(T a, T b);

        public virtual bool Same(T a, T b)
        {
            return EqualityComparer<T>.Default.Equals(a, b);
        }

#if UNITY_EDITOR
        /// <summary>
        /// The serialized properties writing this channel changes, so the editor preview can restore them.
        /// </summary>
        public abstract void CollectDrivenProperties(Object target, List<DrivenProperty> properties);
#endif
    }

    public abstract class Vector3Channel : TweenChannel<Vector3>
    {
        public override Vector3 One => Vector3.one;
        public override Vector3 Zero => Vector3.zero;
        public override Vector3 Multiply(Vector3 a, Vector3 b) => Vector3.Scale(a, b);
        public override Vector3 Add(Vector3 a, Vector3 b) => a + b;
    }

    public abstract class Vector2Channel : TweenChannel<Vector2>
    {
        public override Vector2 One => Vector2.one;
        public override Vector2 Zero => Vector2.zero;
        public override Vector2 Multiply(Vector2 a, Vector2 b) => Vector2.Scale(a, b);
        public override Vector2 Add(Vector2 a, Vector2 b) => a + b;
    }

    public abstract class FloatChannel : TweenChannel<float>
    {
        public override float One => 1;
        public override float Zero => 0;
        public override float Multiply(float a, float b) => a * b;
        public override float Add(float a, float b) => a + b;
    }

    /// <summary>
    /// On and off values. Layering does nothing to them, so the base value always wins.
    /// </summary>
    public abstract class BoolChannel : TweenChannel<bool>
    {
        public override bool One => true;
        public override bool Zero => false;
        public override bool Multiply(bool a, bool b) => a;
        public override bool Add(bool a, bool b) => a;
    }

    public sealed class LocalPositionChannel : Vector3Channel
    {
        public override string Name => "localPosition";
        public override Vector3 Read(Object target) => ((Transform) target).localPosition;
        public override void Write(Object target, Vector3 value) => ((Transform) target).localPosition = value;

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
        {
            properties.Add(new DrivenProperty(target, "m_LocalPosition.x"));
            properties.Add(new DrivenProperty(target, "m_LocalPosition.y"));
            properties.Add(new DrivenProperty(target, "m_LocalPosition.z"));

            // Writing local position on a RectTransform also changes its anchored position
            if (target is RectTransform) {
                properties.Add(new DrivenProperty(target, "m_AnchoredPosition.x"));
                properties.Add(new DrivenProperty(target, "m_AnchoredPosition.y"));
            }
        }
#endif
    }

    public sealed class AnchoredPositionChannel : Vector2Channel
    {
        public override string Name => "anchoredPosition";
        public override Vector2 Read(Object target) => ((RectTransform) target).anchoredPosition;
        public override void Write(Object target, Vector2 value) => ((RectTransform) target).anchoredPosition = value;

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
        {
            // Writing anchored position also changes local position
            properties.Add(new DrivenProperty(target, "m_AnchoredPosition.x"));
            properties.Add(new DrivenProperty(target, "m_AnchoredPosition.y"));
            properties.Add(new DrivenProperty(target, "m_LocalPosition.x"));
            properties.Add(new DrivenProperty(target, "m_LocalPosition.y"));
        }
#endif
    }

    public sealed class LocalScaleChannel : Vector3Channel
    {
        public override string Name => "localScale";
        public override Vector3 Read(Object target) => ((Transform) target).localScale;
        public override void Write(Object target, Vector3 value) => ((Transform) target).localScale = value;

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
        {
            properties.Add(new DrivenProperty(target, "m_LocalScale.x"));
            properties.Add(new DrivenProperty(target, "m_LocalScale.y"));
            properties.Add(new DrivenProperty(target, "m_LocalScale.z"));
        }
#endif
    }

    /// <summary>
    /// Local rotation as Euler degrees, so values past 360 spin and layered tracks can add degrees. Reads
    /// come back between -180 and 180, so a slight tilt left reads as -10 rather than 350.
    /// </summary>
    public sealed class LocalRotationChannel : Vector3Channel
    {
        private const float SAME_DEGREES = 0.0001f;

        public override string Name => "localRotation";

        public override Vector3 Read(Object target)
        {
            var euler = ((Transform) target).localEulerAngles;
            return new Vector3(Mathf.DeltaAngle(0, euler.x), Mathf.DeltaAngle(0, euler.y), Mathf.DeltaAngle(0, euler.z));
        }

        public override void Write(Object target, Vector3 value) => ((Transform) target).localRotation = Quaternion.Euler(value);

        /// <summary>
        /// Compares angles a whole number of turns apart as the same, so a held 370 isn't rewritten every
        /// evaluation because the transform reads it back as 10.
        /// </summary>
        public override bool Same(Vector3 a, Vector3 b)
        {
            return Mathf.Abs(Mathf.DeltaAngle(a.x, b.x)) < SAME_DEGREES
                && Mathf.Abs(Mathf.DeltaAngle(a.y, b.y)) < SAME_DEGREES
                && Mathf.Abs(Mathf.DeltaAngle(a.z, b.z)) < SAME_DEGREES;
        }

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
        {
            properties.Add(new DrivenProperty(target, "m_LocalRotation.x"));
            properties.Add(new DrivenProperty(target, "m_LocalRotation.y"));
            properties.Add(new DrivenProperty(target, "m_LocalRotation.z"));
            properties.Add(new DrivenProperty(target, "m_LocalRotation.w"));

            // The inspector shows this instead of converting the quaternion, so restore it too
            properties.Add(new DrivenProperty(target, "m_LocalEulerAnglesHint.x"));
            properties.Add(new DrivenProperty(target, "m_LocalEulerAnglesHint.y"));
            properties.Add(new DrivenProperty(target, "m_LocalEulerAnglesHint.z"));
        }
#endif
    }

    public sealed class CanvasGroupAlphaChannel : FloatChannel
    {
        public override string Name => "alpha";
        public override float Read(Object target) => ((CanvasGroup) target).alpha;
        public override void Write(Object target, float value) => ((CanvasGroup) target).alpha = value;

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
        {
            properties.Add(new DrivenProperty(target, "m_Alpha"));
        }
#endif
    }

    public sealed class ActiveChannel : BoolChannel
    {
        public override string Name => "active";
        public override bool Read(Object target) => ((GameObject) target).activeSelf;
        public override void Write(Object target, bool value) => ((GameObject) target).SetActive(value);

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
        {
            properties.Add(new DrivenProperty(target, "m_IsActive"));
        }
#endif
    }

    /// <summary>
    /// The enabled flag of a Behaviour, Renderer or Collider.
    /// </summary>
    public sealed class EnabledChannel : BoolChannel
    {
        public override string Name => "enabled";

        public static bool CanToggle(Object target)
        {
            return target is Behaviour || target is Renderer || target is Collider;
        }

        public override bool Read(Object target)
        {
            return target switch {
                Behaviour behaviour => behaviour.enabled,
                Renderer renderer => renderer.enabled,
                Collider collider => collider.enabled,
                _ => true
            };
        }

        public override void Write(Object target, bool value)
        {
            switch (target) {
                case Behaviour behaviour:
                    behaviour.enabled = value;
                    break;
                case Renderer renderer:
                    renderer.enabled = value;
                    break;
                case Collider collider:
                    collider.enabled = value;
                    break;
            }
        }

#if UNITY_EDITOR
        public override void CollectDrivenProperties(Object target, List<DrivenProperty> properties)
        {
            if (CanToggle(target)) {
                properties.Add(new DrivenProperty(target, "m_Enabled"));
            }
        }
#endif
    }

    /// <summary>
    /// The shared channel instances. Tracks must use these so origins and layering line up.
    /// </summary>
    public static class TweenChannels
    {
        public static readonly LocalPositionChannel LocalPosition = new LocalPositionChannel();
        public static readonly AnchoredPositionChannel AnchoredPosition = new AnchoredPositionChannel();
        public static readonly LocalScaleChannel LocalScale = new LocalScaleChannel();
        public static readonly LocalRotationChannel LocalRotation = new LocalRotationChannel();
        public static readonly CanvasGroupAlphaChannel CanvasGroupAlpha = new CanvasGroupAlphaChannel();
        public static readonly ActiveChannel Active = new ActiveChannel();
        public static readonly EnabledChannel Enabled = new EnabledChannel();
    }
}
