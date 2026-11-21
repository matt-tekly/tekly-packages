using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Tekly.Tweenlines
{
    /// <summary>
    /// Ties together plays on several Tweenlines that code runs one after another, like the steps of a
    /// victory screen, so they can be hurried, completed or cancelled as one. Only plays started through the
    /// sequence belong to it, wherever their Tweenlines are in the hierarchy. A sequence is used for one run:
    /// create a new one each time, since a cancelled sequence stays cancelled.
    /// </summary>
    public class TweenSequence
    {
        /// <summary>Matches the default Hurry Speed of a Tweenline</summary>
        public const float DEFAULT_HURRY_SPEED = 4f;

        /// <summary>Whether Hurry or Complete was called, for waits of your own that should shorten</summary>
        public bool IsHurried => m_hurried;

        /// <summary>Whether Complete was called, so every later play finishes straight away</summary>
        public bool IsCompleting => m_completing;

        /// <summary>Whether Cancel was called or the token given to the sequence was cancelled</summary>
        public bool IsCancelled => m_cancelled || m_cancellationToken.IsCancellationRequested;

        private readonly List<Entry> m_active = new List<Entry>();
        private readonly CancellationToken m_cancellationToken;
        private bool m_hurried;
        private bool m_completing;
        private bool m_cancelled;

        /// <summary>
        /// Pass the destroyCancellationToken of the MonoBehaviour driving the sequence, so the sequence counts
        /// as cancelled once that is destroyed: a Delay ends and later plays throw.
        /// </summary>
        public TweenSequence(CancellationToken cancellationToken = default)
        {
            m_cancellationToken = cancellationToken;
        }

        /// <summary>
        /// Plays an animation as part of the sequence and completes when it finishes. Throws
        /// OperationCanceledException when the play is interrupted or the sequence was cancelled, see
        /// Tweenline.PlayAsync.
        /// </summary>
        public Awaitable Play(Tweenline tweenline, string animationName)
        {
            ThrowIfCancelled();

            if (!tweenline.TryFind(animationName, out var animation)) {
                return Tweenline.CompletedAwaitable();
            }

            var awaitable = tweenline.PlayAsync(animation, false, tweenline.StaggerDelay());
            Started(tweenline);

            return awaitable;
        }

        /// <summary>
        /// Plays an animation on the first count Tweenlines as part of the sequence, see
        /// Tweenline.PlayAllAsync.
        /// </summary>
        public Awaitable PlayAll(IReadOnlyList<Tweenline> tweenlines, string animationName, int count, float stagger = 0)
        {
            ThrowIfCancelled();

            return Tweenline.PlayAllAsync(tweenlines, animationName, count, stagger, this);
        }

        /// <summary>
        /// Waits as part of the sequence, like a banner holding before it leaves. The wait runs hurrySpeed
        /// times faster once the sequence is hurried and ends straight away once it is completing. Throws
        /// OperationCanceledException when the sequence is cancelled.
        /// </summary>
        public async Awaitable Delay(float seconds, float hurrySpeed = DEFAULT_HURRY_SPEED)
        {
            ThrowIfCancelled();

            var remaining = seconds;

            while (remaining > 0 && !m_completing) {
                await Awaitable.NextFrameAsync(m_cancellationToken);
                ThrowIfCancelled();

                remaining -= Time.deltaTime * (m_hurried ? hurrySpeed : 1f);
            }
        }

        /// <summary>
        /// Hurries what the sequence is playing, and every play started through it from now on starts hurried.
        /// </summary>
        public void Hurry()
        {
            m_hurried = true;

            foreach (var entry in m_active) {
                if (entry.IsActive) {
                    entry.Tweenline.Hurry();
                }
            }
        }

        /// <summary>
        /// Completes what the sequence is playing, and every play started through it from now on completes
        /// straight away, so the code driving the sequence runs through to its end.
        /// </summary>
        public void Complete()
        {
            m_hurried = true;
            m_completing = true;

            // Completing a play resumes the code awaiting it, which starts the next play through Started.
            // Those complete there and are never added, so this only drains what was already playing
            while (TakeActive(out var tweenline)) {
                tweenline.Complete();
            }
        }

        /// <summary>
        /// Stops what the sequence is playing, which cancels the awaits on it, and makes every later play
        /// through it throw OperationCanceledException.
        /// </summary>
        public void Cancel()
        {
            m_cancelled = true;

            while (TakeActive(out var tweenline)) {
                tweenline.Stop();
            }
        }

        /// <summary>
        /// Takes on a play that was just started, applying the sequence's hurry or complete to it.
        /// </summary>
        internal void Started(Tweenline tweenline)
        {
            for (var i = m_active.Count - 1; i >= 0; i--) {
                if (!m_active[i].IsActive) {
                    m_active.RemoveAt(i);
                }
            }

            if (m_completing) {
                tweenline.Complete();
                return;
            }

            if (m_hurried) {
                tweenline.Hurry();
            }

            m_active.Add(new Entry { Tweenline = tweenline, PlayId = tweenline.PlayId });
        }

        /// <summary>
        /// Removes the most recent play that is still running. False when there is none left.
        /// </summary>
        private bool TakeActive(out Tweenline tweenline)
        {
            while (m_active.Count > 0) {
                var entry = m_active[m_active.Count - 1];
                m_active.RemoveAt(m_active.Count - 1);

                if (entry.IsActive) {
                    tweenline = entry.Tweenline;
                    return true;
                }
            }

            tweenline = null;
            return false;
        }

        private void ThrowIfCancelled()
        {
            if (IsCancelled) {
                throw new OperationCanceledException();
            }
        }

        private struct Entry
        {
            public Tweenline Tweenline;
            public int PlayId;

            /// <summary>Whether the play the sequence started is still the one running on the Tweenline</summary>
            public bool IsActive => Tweenline != null && Tweenline.IsPlaying && Tweenline.PlayId == PlayId;
        }
    }
}
