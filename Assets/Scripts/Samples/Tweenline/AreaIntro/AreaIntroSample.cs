using System;
using System.Collections.Generic;
using Tekly.Common.Ui.Styled;
using Tekly.Tweenlines;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TeklySample.Samples.Tweenlines
{
    /// <summary>
    /// A bold banner that introduces an area when the player walks into it. As much of it as possible is
    /// authored in one Tweenline on the banner, which has three animations:
    /// - Show: the bands sweep in and the name lands, all on one timeline.
    /// - Stamp: the new area stamp lands.
    /// - Hide: the bands sweep out, taking everything on them along.
    /// This script only does what a fixed timeline cannot:
    /// - The first visit to an area adds the stamp and a row of things found there. The row has a different
    ///   length for each area, so each find is its own small Tweenline. A return visit skips both.
    /// - The banner holds for a moment before it leaves. The hold shortens when the sequence is hurried.
    /// - Entering another area part way through cancels the banner and starts the next one.
    /// Tapping the screen hurries the banner, and tapping again jumps to its end.
    /// </summary>
    public class AreaIntroSample : MonoBehaviour
    {
        private const string SHOW = "Show";
        private const string STAMP = "Stamp";
        private const string HIDE = "Hide";

        [SerializeField] private Area[] m_areas;

        [Tooltip("Has the Show, Stamp and Hide animations for the bands, the name and the stamp")]
        [SerializeField] private Tweenline m_banner;

        [Tooltip("Tinted with the area's color")]
        [SerializeField] private StyledRect m_bandImage;

        [SerializeField] private TMP_Text m_kickerLabel;
        [SerializeField] private TMP_Text m_nameLabel;
        [SerializeField] private TMP_Text m_regionLabel;

        [Tooltip("One per find the row can show. An area uses as many as it has finds")]
        [SerializeField] private Tweenline[] m_finds;

        [Tooltip("Distance between the centers of two finds in the row")]
        [SerializeField] private float m_findSpacing = 330f;

        [Tooltip("Seconds between one find starting and the next")]
        [SerializeField] [Min(0)] private float m_findStagger = 0.08f;

        [Tooltip("Seconds the banner stays up once everything is in")]
        [SerializeField] [Min(0)] private float m_holdSeconds = 2f;

        [Tooltip("How much faster the hold runs once the sequence is hurried")]
        [SerializeField] [Min(1)] private float m_holdHurrySpeed = 4f;

        private readonly HashSet<int> m_visited = new HashSet<int>();
        private TweenSequence m_sequence;

        private void Start()
        {
            // The banner is laid out in view in the scene, so it is hidden until an area is entered
            Rewind();
        }

        /// <summary>
        /// Plays the banner for an area. The first call for an area is its first visit.
        /// </summary>
        public async void Enter(int index)
        {
            var area = m_areas[index];
            var firstVisit = m_visited.Add(index);
            var findCount = firstVisit ? Mathf.Min(area.Finds.Length, m_finds.Length) : 0;

            // A banner that is still up ends in its catch block below before this one starts
            m_sequence?.Cancel();

            Fill(area, firstVisit, findCount);
            Rewind();

            var sequence = new TweenSequence(destroyCancellationToken);
            m_sequence = sequence;

            try {
                await sequence.Play(m_banner, SHOW);

                if (firstVisit) {
                    await sequence.Play(m_banner, STAMP);
                    await sequence.PlayAll(m_finds, SHOW, findCount, m_findStagger);
                }

                await sequence.Delay(m_holdSeconds, m_holdHurrySpeed);
                await sequence.Play(m_banner, HIDE);
            } catch (OperationCanceledException) {
                // Another area was entered, or the scene closed part way through
            }
        }

        /// <summary>
        /// Makes every area a first visit again.
        /// </summary>
        public void ForgetVisits()
        {
            m_visited.Clear();
        }

        /// <summary>
        /// The first tap speeds up the rest of the banner. The second jumps to its end.
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

        /// <summary>
        /// Puts every piece on the first frame of its animation, which is its hidden pose. Stamp and Show
        /// drive different things, so the banner is rewound once for each.
        /// </summary>
        private void Rewind()
        {
            m_banner.Rewind(STAMP);
            m_banner.Rewind(SHOW);

            foreach (var find in m_finds) {
                find.Rewind(SHOW);
            }
        }

        private void Fill(Area area, bool firstVisit, int findCount)
        {
            m_bandImage.color = area.Color;
            m_kickerLabel.text = firstVisit ? "Now entering" : "Welcome back to";
            m_nameLabel.text = area.Name;
            m_regionLabel.text = area.Region;

            for (var i = 0; i < findCount; i++) {
                var find = (RectTransform) m_finds[i].transform;

                // The row is centered however many finds it has
                find.anchoredPosition = new Vector2((i - (findCount - 1) * 0.5f) * m_findSpacing, find.anchoredPosition.y);
                find.GetComponentInChildren<TMP_Text>(true).text = area.Finds[i];
            }
        }

        [Serializable]
        private struct Area
        {
            public string Name;

            [Tooltip("Shown under the name")]
            public string Region;

            public Color Color;

            [Tooltip("Things found on the first visit. The row shows as many as there are")]
            public string[] Finds;
        }
    }
}
