using System;
using System.Threading;
using UnityEngine;

namespace Tekly.Wipes
{
    /// <summary>
    /// Runs a Cover or Uncover called from code, for a Wipe or a Wipe Reveal. It only does work while one
    /// is running, and uses unscaled time so it plays while the game is paused.
    /// </summary>
    internal sealed class WipePlayback
    {
        public bool IsPlaying { get; private set; }

        private int m_playId;

        public void Stop()
        {
            m_playId++;
            IsPlaying = false;
        }

        /// <summary>
        /// Moves the coverage to 1 or 0 over the style's duration. Returns early when another play or a
        /// Stop takes over, or the owner is destroyed.
        /// </summary>
        public async Awaitable Play(IWipe wipe, WipeStyle style, bool cover, CancellationToken destroyed)
        {
            var playId = ++m_playId;
            var leaving = !cover && style.ExitContinues;
            var from = wipe.Coverage;
            var to = cover ? 1f : 0f;
            var duration = cover ? style.CoverDuration : style.UncoverDuration;
            var ease = cover ? style.CoverEase : style.UncoverEase;
            var elapsed = 0f;

            IsPlaying = true;

            // The first frame shows the start, so a new look or direction never flashes the old one.
            wipe.Set(from, leaving);

            try {
                while (elapsed < duration) {
                    await Awaitable.NextFrameAsync(destroyed);

                    if (playId != m_playId) {
                        return;
                    }

                    elapsed += Time.unscaledDeltaTime;
                    wipe.Set(Mathf.LerpUnclamped(from, to, ease.Evaluate(Mathf.Clamp01(elapsed / duration))), leaving);
                }
            } catch (OperationCanceledException) {
                return;
            }

            if (playId != m_playId) {
                return;
            }

            // Finished before the last write, so a Wipe Reveal settles on it. Uncovered, the wipe is ready
            // to cover again from the start.
            IsPlaying = false;
            wipe.Set(to, leaving && to > 0f);
        }
    }
}
