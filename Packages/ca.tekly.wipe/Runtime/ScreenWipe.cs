using System;
using Tekly.Common.Utils;
using Tekly.Tweenlines;
using UnityEngine;
using UnityEngine.UI;

namespace Tekly.Wipes
{
    /// <summary>
    /// A full screen wipe that needs no scene setup, for load screens and screen swaps:
    ///
    ///     await ScreenWipe.Instance.Cover(look);
    ///     await LoadSceneAsync();
    ///     await ScreenWipe.Instance.Uncover();
    ///
    /// Cover completes at full cover, which is the moment to swap what is behind. Uncover can take a
    /// different look for a different way out. Input under the wipe is blocked while anything is covered.
    ///
    /// Its canvas and Wipe are made on first use and survive scene loads. Loading art or tips shown at full
    /// cover go on a canvas sorted above SortingOrder. The Focus centers an iris on something:
    ///
    ///     ScreenWipe.Instance.Focus.Target = player.transform;
    ///
    /// A cover can also be a prefab with a Tweenline that has Cover and Uncover animations, for clouds,
    /// doors or a mascot that cover the screen as objects instead of a shape.
    ///
    /// The Screen Wipe only exists in play mode.
    /// </summary>
    public class ScreenWipe : Singleton<ScreenWipe>, IDisposable
    {
        public const int DEFAULT_SORTING_ORDER = 30000;
        public const string COVER_ANIMATION = "Cover";
        public const string UNCOVER_ANIMATION = "Uncover";

        public Wipe Wipe {
            get {
                Create();
                return m_wipe;
            }
        }

        public WipeFocus Focus {
            get {
                Create();
                return m_focus;
            }
        }

        public Canvas Canvas {
            get {
                Create();
                return m_canvas;
            }
        }

        /// <summary>
        /// The overlay canvas's sorting order. High by default, so the wipe is above everything.
        /// </summary>
        public int SortingOrder {
            get => m_sortingOrder;
            set {
                m_sortingOrder = value;

                if (m_canvas != null) {
                    m_canvas.sortingOrder = value;
                }
            }
        }

        /// <summary>
        /// True while a wipe or a prefab cover has anything on screen.
        /// </summary>
        public bool IsCovering => m_prefabCover != null || (m_wipe != null && m_wipe.Coverage > 0f);

        private int m_sortingOrder = DEFAULT_SORTING_ORDER;
        private GameObject m_root;
        private Canvas m_canvas;
        private Wipe m_wipe;
        private WipeFocus m_focus;
        private Tweenline m_prefabCover;

        /// <summary>
        /// Covers the screen with a look. Completes at full cover. A null look uses the last one.
        /// </summary>
        public Awaitable Cover(WipeLook look = null)
        {
            Create();
            return m_wipe.Cover(look);
        }

        /// <summary>
        /// Covers the screen with a prefab: it is added under the wipe's canvas and its Tweenline's Cover
        /// animation is played. Completes when that animation does.
        /// </summary>
        public async Awaitable Cover(Tweenline prefab)
        {
            Create();
            RemovePrefabCover();

            m_prefabCover = UnityEngine.Object.Instantiate(prefab, m_root.transform, false);
            await PlayAndWait(m_prefabCover, COVER_ANIMATION);
        }

        /// <summary>
        /// Uncovers the screen. A prefab cover plays its Uncover animation and is removed. Otherwise the
        /// wipe uncovers, with the look given or the one it covered with.
        /// </summary>
        public async Awaitable Uncover(WipeLook look = null)
        {
            if (m_prefabCover != null) {
                var cover = m_prefabCover;
                await PlayAndWait(cover, UNCOVER_ANIMATION);

                if (m_prefabCover == cover) {
                    RemovePrefabCover();
                }

                return;
            }

            Create();
            await m_wipe.Uncover(look);
        }

        public void Dispose()
        {
            if (m_root != null) {
                Wipe.DestroySafely(m_root);
                m_root = null;
            }
        }

        private void Create()
        {
            if (m_root != null) {
                return;
            }

            m_root = new GameObject("Screen Wipe", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            UnityEngine.Object.DontDestroyOnLoad(m_root);

            m_canvas = m_root.GetComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = m_sortingOrder;

            var wipeObject = new GameObject("Wipe", typeof(RectTransform), typeof(CanvasRenderer));
            var rect = (RectTransform) wipeObject.transform;

            rect.SetParent(m_root.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            m_focus = wipeObject.AddComponent<WipeFocus>();

            m_wipe = wipeObject.AddComponent<Wipe>();
            m_wipe.maskable = false;
            m_wipe.Focus = m_focus;

            // The wipe has the canvas to itself, so trimming its quad costs nothing else a rebuild
            m_wipe.Trim = true;
        }

        private void RemovePrefabCover()
        {
            if (m_prefabCover != null) {
                UnityEngine.Object.Destroy(m_prefabCover.gameObject);
                m_prefabCover = null;
            }
        }

        private static async Awaitable PlayAndWait(Tweenline tweenline, string animationName)
        {
            if (!tweenline.Has(animationName)) {
                Debug.LogWarning($"The cover [{tweenline.name}] has no animation named [{animationName}]", tweenline);
                return;
            }

            tweenline.Play(animationName);

            // Watching the Tweenline instead of its Completed event also ends the wait when the cover is
            // removed or its animation is stopped, which never raise the event
            while (tweenline != null && tweenline.IsPlaying) {
                await Awaitable.NextFrameAsync();
            }
        }
    }
}
