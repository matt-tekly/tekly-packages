using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Tweenlines
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
    /// A track that offsets or multiplies channels instead of setting them. Layered tracks are prepared and
    /// evaluated after every base track in the animation, so they can see which channels have a base track.
    /// </summary>
    public interface ILayeredTrack { }

    /// <summary>
    /// A base track that can report its value at any time, so layered tracks can work out where the target
    /// will be, like where a drop lands.
    /// </summary>
    public interface IChannelSource<T>
    {
        bool TrySample(float time, out T value);
    }

    /// <summary>
    /// The rest values of every target and channel driven by a Tweenline. Animations driving the same
    /// channel share one origin so interrupting an animation never captures a half animated value.
    /// </summary>
    public class TweenOrigins
    {
        private readonly Dictionary<(Object, TweenChannel), object> m_records = new Dictionary<(Object, TweenChannel), object>();

        public void Clear()
        {
            m_records.Clear();
        }

        /// <summary>
        /// The stored origin, capturing the current value first if there is none.
        /// </summary>
        public T GetOrCapture<T>(Object target, TweenChannel<T> channel)
        {
            return GetRecord(target, channel).Origin;
        }

        /// <summary>
        /// The shared record for a target and channel, capturing its origin first if there is none.
        /// </summary>
        internal ChannelRecord<T> GetRecord<T>(Object target, TweenChannel<T> channel)
        {
            if (m_records.TryGetValue((target, channel), out var record)) {
                return (ChannelRecord<T>) record;
            }

            var created = new ChannelRecord<T>(channel.Read(target));
            m_records[(target, channel)] = created;
            return created;
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

    /// <summary>
    /// What every animation on a Tweenline knows about one target and channel: its origin and what was last
    /// written to it. The last write lets the next animation tell its own leftover offsets apart from moves
    /// made by other code.
    /// </summary>
    internal sealed class ChannelRecord<T>
    {
        public readonly T Origin;

        public bool HasWritten;

        /// <summary>The value last written, including layered offsets and multipliers</summary>
        public T LastWritten;

        /// <summary>The base value under the last write, without layered offsets and multipliers</summary>
        public T LastBase;

        public ChannelRecord(T origin)
        {
            Origin = origin;
        }
    }
}
