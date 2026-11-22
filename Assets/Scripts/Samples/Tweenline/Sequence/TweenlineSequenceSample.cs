using System;
using Tekly.Tweenlines;
using UnityEngine;

namespace TeklySample.Samples.Tweenlines
{
    /// <summary>
    /// A victory screen whose length depends on the result. Each piece (title, a star, the new best badge,
    /// the buttons) is a small Tweenline with a fixed Show animation. This script decides which pieces play,
    /// how many and in what order:
    /// - Only the earned stars play, staggered by play order.
    /// - The new best badge only plays for a full set of stars.
    /// - The buttons wait for whatever came before them, however long that took.
    /// Tapping the screen hurries the whole sequence, and tapping again jumps to its end.
    /// </summary>
    public class TweenlineSequenceSample : MonoBehaviour
    {
        private const string SHOW = "Show";

        [SerializeField] private Tweenline m_title;

        [Tooltip("One per star, in the order they fill")]
        [SerializeField] private Tweenline[] m_stars;

        [Tooltip("Only shown when every star is earned")]
        [SerializeField] private Tweenline m_newBest;

        [SerializeField] private Tweenline m_buttons;

        [Tooltip("Seconds between one star starting and the next")]
        [SerializeField] [Min(0)] private float m_starStagger = 0.2f;

        [Tooltip("Stars earned by the play that starts with the scene")]
        [SerializeField] [Min(0)] private int m_earned = 3;

        private TweenSequence m_sequence;

        private void Start()
        {
	        Play(m_earned);
        }

        /// <summary>
        /// Plays the screen for a number of earned stars. Calling it part way through starts over.
        /// </summary>
        public async void Play(int earned)
        {
            // A run that is still going ends in its catch block below before this one starts
            m_sequence?.Cancel();

            // The first frame of Show is each piece's hidden pose
            m_title.Rewind(SHOW);
            m_newBest.Rewind(SHOW);
            m_buttons.Rewind(SHOW);

            for (var i = 0; i < m_stars.Length; i++) {
                var star = m_stars[i];
                star.Rewind(SHOW);
            }

            // Every play goes through the sequence, so a hurry also reaches the steps that have not started yet
            var sequence = new TweenSequence();
            m_sequence = sequence;

            try {
                await sequence.Play(m_title, SHOW);
                await sequence.PlayAll(m_stars, SHOW, earned, m_starStagger);

                if (earned >= m_stars.Length) {
                    await sequence.Play(m_newBest, SHOW);
                }

                await sequence.Play(m_buttons, SHOW);
            } catch (OperationCanceledException) {
                // Replayed, or the screen was closed part way through
            }
        }

        /// <summary>
        /// The first tap speeds up the rest of the sequence. The second jumps to its end.
        /// </summary>
        public void Hurry()
        {
            if (m_sequence == null) {
                return;
            }

            if (m_sequence.IsHurried) {
                m_sequence.Complete();
            } else {
                m_sequence.Hurry();
            }
        }
    }
}
