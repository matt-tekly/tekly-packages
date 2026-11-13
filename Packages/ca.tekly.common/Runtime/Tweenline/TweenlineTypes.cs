using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Common.Tweenlines
{
    public enum TweenWrap
    {
        Once,
        Loop,
        PingPong
    }

    /// <summary>
    /// Where a clip starts its interpolation from.
    /// </summary>
    public enum ClipFrom
    {
        /// <summary>The end value of the previous clip on the track, or the origin for the first clip</summary>
        Previous,

        /// <summary>The value the property has when the animation starts playing</summary>
        Current,

        /// <summary>The rest value captured for the target</summary>
        Origin,

        /// <summary>The clip's own From value</summary>
        Value
    }

    /// <summary>
    /// How a clip's explicit values are interpreted.
    /// </summary>
    public enum ClipSpace
    {
        Absolute,
        RelativeToOrigin
    }

    /// <summary>
    /// The rest values of every target and channel driven by a Tweenline. Animations driving the same
    /// channel share one origin so interrupting an animation never captures a half animated value.
    /// </summary>
    public class TweenOrigins
    {
        private readonly Dictionary<(Object, string), object> m_values = new Dictionary<(Object, string), object>();

        public void Clear()
        {
            m_values.Clear();
        }

        public bool TryGet<T>(Object target, string channel, out T value)
        {
            if (m_values.TryGetValue((target, channel), out var boxed)) {
                value = (T) boxed;
                return true;
            }

            value = default;
            return false;
        }

        public void Set<T>(Object target, string channel, T value)
        {
            m_values[(target, channel)] = value;
        }

        public bool Contains(Object target, string channel)
        {
            return m_values.ContainsKey((target, channel));
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// A serialized property a track writes to, used by the editor preview to restore values.
    /// </summary>
    public readonly struct DrivenProperty
    {
        public readonly Object Target;
        public readonly string PropertyPath;

        public DrivenProperty(Object target, string propertyPath)
        {
            Target = target;
            PropertyPath = propertyPath;
        }
    }
#endif
}
