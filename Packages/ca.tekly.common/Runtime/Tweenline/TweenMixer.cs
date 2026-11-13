using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Common.Tweenlines
{
    /// <summary>
    /// Combines what every track in an animation does to each target and channel, then writes each channel
    /// once. Base tracks set a value, layered tracks offset or multiply it. A channel nothing touched this
    /// evaluation is left alone, so layouts and other code can move it.
    /// </summary>
    public sealed class TweenMixer
    {
        private readonly Dictionary<(Object, TweenChannel), ChannelSlot> m_slots = new Dictionary<(Object, TweenChannel), ChannelSlot>();
        private readonly List<ChannelSlot> m_active = new List<ChannelSlot>();

        private TweenOrigins m_origins;
        private bool m_readCurrent;

        /// <summary>
        /// The slot for a target and channel in the animation being prepared. Tracks call this from Prepare.
        /// </summary>
        public ChannelSlot<T> Get<T>(Object target, TweenChannel<T> channel)
        {
            var key = (target, (TweenChannel) channel);

            if (!m_slots.TryGetValue(key, out var slot)) {
                slot = new ChannelSlot<T>(target, channel);
                m_slots[key] = slot;
            }

            var typed = (ChannelSlot<T>) slot;

            if (!typed.Active) {
                typed.Active = true;
                typed.Prepare(m_origins, m_readCurrent);
                m_active.Add(typed);
            }

            return typed;
        }

        /// <summary>
        /// Whether a base track drives the channel in the animation being prepared. Layered tracks are
        /// prepared after base tracks, so they can use this to pick which channel to layer onto.
        /// </summary>
        public bool HasSource<T>(Object target, TweenChannel<T> channel)
        {
            return m_slots.TryGetValue((target, channel), out var slot) && slot.Active && ((ChannelSlot<T>) slot).HasSource;
        }

        internal void BeginPrepare(TweenOrigins origins, bool readCurrent)
        {
            m_origins = origins;
            m_readCurrent = readCurrent;

            foreach (var slot in m_active) {
                slot.Active = false;
            }

            m_active.Clear();
        }

        internal void BeginEvaluate()
        {
            foreach (var slot in m_active) {
                slot.BeginEvaluate();
            }
        }

        internal void Flush()
        {
            foreach (var slot in m_active) {
                slot.Flush();
            }
        }
    }

    public abstract class ChannelSlot
    {
        internal bool Active;

        internal abstract void BeginEvaluate();
        internal abstract void Flush();
    }

    /// <summary>
    /// One target and channel inside an animation: its origin, its value when the animation started, the
    /// base track driving it and what layered tracks did to it this evaluation.
    /// </summary>
    public sealed class ChannelSlot<T> : ChannelSlot
    {
        public Object Target { get; }
        public TweenChannel<T> Channel { get; }

        /// <summary>The shared rest value</summary>
        public T Origin { get; private set; }

        /// <summary>The value when the animation started playing, or the origin when it hasn't been read</summary>
        public T Start { get; private set; }

        public bool HasSource => m_source != null;

        private IChannelSource<T> m_source;
        private bool m_hasStart;
        private T m_base;
        private bool m_hasBase;
        private T m_multiplier;
        private T m_offset;
        private bool m_layered;
        private bool m_layeredLastEvaluate;

        public ChannelSlot(Object target, TweenChannel<T> channel)
        {
            Target = target;
            Channel = channel;
        }

        /// <summary>
        /// Marks a base track as the source of the channel's value, so layered tracks can sample it.
        /// </summary>
        public void SetSource(IChannelSource<T> source)
        {
            m_source = source;
        }

        /// <summary>
        /// The base value at a time, from the base track or the start value when there is none.
        /// </summary>
        public T SampleBase(float time)
        {
            if (m_source != null && m_source.TrySample(time, out var value)) {
                return value;
            }

            return Start;
        }

        public void SetBase(T value)
        {
            m_base = value;
            m_hasBase = true;
        }

        public void Multiply(T value)
        {
            m_multiplier = Channel.Multiply(m_multiplier, value);
            m_layered = true;
        }

        public void Offset(T value)
        {
            m_offset = Channel.Add(m_offset, value);
            m_layered = true;
        }

        internal void Prepare(TweenOrigins origins, bool readCurrent)
        {
            m_source = null;
            m_layeredLastEvaluate = false;
            Origin = origins.GetOrCapture(Target, Channel);

            if (readCurrent || !m_hasStart) {
                Start = readCurrent ? Channel.Read(Target) : Origin;
                m_hasStart = true;
            }
        }

        internal override void BeginEvaluate()
        {
            m_hasBase = false;
            m_multiplier = Channel.One;
            m_offset = Channel.Zero;
            m_layered = false;
        }

        internal override void Flush()
        {
            // A layer that stopped last evaluation still writes once so the target lands on its base value
            var touched = m_hasBase || m_layered || m_layeredLastEvaluate;
            m_layeredLastEvaluate = m_layered;

            if (!touched || Target == null) {
                return;
            }

            var value = Channel.Add(Channel.Multiply(m_hasBase ? m_base : Start, m_multiplier), m_offset);

            if (!Channel.Same(Channel.Read(Target), value)) {
                Channel.Write(Target, value);
            }
        }
    }
}
