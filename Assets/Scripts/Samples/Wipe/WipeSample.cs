using Tekly.Tweenlines;
using Tekly.Wipes;
using UnityEngine;
using UnityEngine.UI;

namespace TeklySample.Samples.Wipe
{
    /// <summary>
    /// Drives the wipe sample scene from its buttons:
    /// - A load screen: a loading canvas above everything else wipes in through its Wipe Reveal, shows
    ///   progress while the level behind it changes, then wipes out with a different look.
    /// - An iris that closes on a moving object through the Screen Wipe's focus.
    /// - Two full screen overlays that wipe in and out over the live scene from their Tweenlines.
    /// - A billboard that wipes from an advert to live UI and back.
    /// </summary>
    public class WipeSample : MonoBehaviour
    {
        private const string SHOW = "Show";
        private const string HIDE = "Hide";

        [Tooltip("The loading screen: a root canvas sorted above the game and the overlays, with a Wipe Reveal")]
        [SerializeField] private WipeReveal m_loading;

        [Tooltip("The loading bar's fill, stretched by its right anchor")]
        [SerializeField] private RectTransform m_loadingBar;

        [SerializeField] private Text m_loadingPercent;

        [Tooltip("Changes while the loading screen covers, standing in for the scene that loaded")]
        [SerializeField] private Text m_levelLabel;

        [Tooltip("The look the load screen covers with")]
        [SerializeField] private WipeLook m_loadCover;

        [Tooltip("The look the load screen uncovers with")]
        [SerializeField] private WipeLook m_loadUncover;

        [Tooltip("Seconds the pretend load takes")]
        [SerializeField] private float m_loadSeconds = 1f;

        [SerializeField] private WipeLook m_iris;

        [Tooltip("What the iris closes on")]
        [SerializeField] private Transform m_irisTarget;

        [Tooltip("Overlays with a Wipe Reveal and a Tweenline that has Show and Hide animations")]
        [SerializeField] private Tweenline m_overlayA;
        [SerializeField] private Tweenline m_overlayB;

        [Tooltip("The advert's Wipe, over the billboard's live UI")]
        [SerializeField] private Tekly.Wipes.Wipe m_billboardAdvert;

        private bool m_overlayAShown;
        private bool m_overlayBShown;
        private bool m_busy;
        private int m_level = 1;
        private Tweenline m_swappedOut;

        public async void LoadScreen()
        {
            if (m_busy) {
                return;
            }

            m_busy = true;

            SetLoadProgress(0f);

            // The loading canvas wipes in over everything below it. Cover completes once it covers the
            // screen, which is the moment to swap what is behind
            await m_loading.Cover(m_loadCover);

            m_level++;
            m_levelLabel.text = $"Level {m_level}";

            var elapsed = 0f;

            while (elapsed < m_loadSeconds) {
                await Awaitable.NextFrameAsync();

                // The scene was unloaded or Play mode stopped while loading
                if (this == null) {
                    return;
                }

                elapsed += Time.unscaledDeltaTime;
                SetLoadProgress(elapsed / m_loadSeconds);
            }

            await m_loading.Uncover(m_loadUncover);

            m_busy = false;
        }

        private void SetLoadProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);

            m_loadingBar.anchorMax = new Vector2(progress, 1f);
            m_loadingPercent.text = $"{Mathf.RoundToInt(progress * 100f)}%";
        }

        public async void IrisOnTarget()
        {
            if (m_busy) {
                return;
            }

            m_busy = true;

            var screenWipe = ScreenWipe.Instance;
            screenWipe.Focus.Target = m_irisTarget;

            await screenWipe.Cover(m_iris);
            await Awaitable.WaitForSecondsAsync(m_loadSeconds);
            await screenWipe.Uncover(m_iris);

            m_busy = false;
        }

        public void ToggleOverlayA()
        {
            Toggle(m_overlayA, ref m_overlayAShown);
        }

        public void ToggleOverlayB()
        {
            Toggle(m_overlayB, ref m_overlayBShown);
        }

        /// <summary>
        /// Replaces whichever overlay is shown with the other one. The new overlay wipes in on top while
        /// the old one stays as it is, so only one transition is on screen. Once the new one covers the
        /// screen the old one is switched off without playing its Hide.
        /// </summary>
        public void SwapOverlays()
        {
            if (m_overlayAShown == m_overlayBShown) {
                if (!m_overlayAShown) {
                    ToggleOverlayA();
                }

                return;
            }

            var outgoing = m_overlayAShown ? m_overlayA : m_overlayB;
            var incoming = m_overlayAShown ? m_overlayB : m_overlayA;

            // The incoming overlay takes the higher of the two sort orders, so it draws over the outgoing one
            var outgoingCanvas = outgoing.GetComponent<Canvas>();
            var incomingCanvas = incoming.GetComponent<Canvas>();

            if (incomingCanvas.sortingOrder < outgoingCanvas.sortingOrder) {
                (incomingCanvas.sortingOrder, outgoingCanvas.sortingOrder) = (outgoingCanvas.sortingOrder, incomingCanvas.sortingOrder);
            }

            m_overlayAShown = !m_overlayAShown;
            m_overlayBShown = !m_overlayBShown;

            m_swappedOut = outgoing;
            incoming.Completed += OnSwapShown;
            incoming.Play(SHOW);
        }

        private void OnSwapShown(Tweenline incoming, TweenAnimation animation)
        {
            incoming.Completed -= OnSwapShown;

            if (m_swappedOut != null) {
                m_swappedOut.Stop();
                m_swappedOut.GetComponent<WipeReveal>().Set(0f, false);
                m_swappedOut = null;
            }
        }

        public async void ToggleBillboard()
        {
            if (m_billboardAdvert.IsPlaying) {
                return;
            }

            if (m_billboardAdvert.Coverage > 0.5f) {
                await m_billboardAdvert.Uncover();
            } else {
                await m_billboardAdvert.Cover();
            }
        }

        private static void Toggle(Tweenline overlay, ref bool shown)
        {
            shown = !shown;
            overlay.Play(shown ? SHOW : HIDE);
        }
    }
}
