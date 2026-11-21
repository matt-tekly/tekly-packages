using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.Tweenlines
{
    /// <summary>
    /// Updates the Tweenlines that are playing, so idle ones cost nothing per frame. It is created the first
    /// time something plays and lives on a DontDestroyOnLoad object, which Unity destroys when play mode
    /// ends, so each play session starts with a fresh list even without a domain reload.
    /// </summary>
    [AddComponentMenu("")]
    internal sealed class TweenlineRunner : MonoBehaviour
    {
        private static TweenlineRunner s_instance;

        private readonly List<Tweenline> m_tweenlines = new List<Tweenline>();
        private bool m_ticking;
        private bool m_hasRemoved;

        public static void Add(Tweenline tweenline)
        {
            if (s_instance == null) {
                var gameObject = new GameObject(nameof(TweenlineRunner));
                DontDestroyOnLoad(gameObject);
                s_instance = gameObject.AddComponent<TweenlineRunner>();
            }

            tweenline.RunnerIndex = s_instance.m_tweenlines.Count;
            s_instance.m_tweenlines.Add(tweenline);
        }

        public static void Remove(Tweenline tweenline)
        {
            var index = tweenline.RunnerIndex;
            tweenline.RunnerIndex = -1;

            if (s_instance == null || index < 0) {
                return;
            }

            // Cleared now and compacted later, so a Tweenline that stops another one mid tick doesn't shift
            // the list under the loop
            s_instance.m_tweenlines[index] = null;
            s_instance.m_hasRemoved = true;

            if (!s_instance.m_ticking) {
                s_instance.Compact();
            }
        }

        private void Update()
        {
            m_ticking = true;

            // Plays started during the loop are added past the count and first tick next frame, after Begin
            // has already shown their start pose
            var count = m_tweenlines.Count;

            for (var i = 0; i < count; i++) {
                var tweenline = m_tweenlines[i];

                // Destroyed Tweenlines remove themselves in OnDisable, so a reference check is enough
                if (ReferenceEquals(tweenline, null)) {
                    continue;
                }

                // A throwing marker listener only breaks its own Tweenline, as it did with one Update each
                try {
                    tweenline.Tick();
                } catch (Exception exception) {
                    Debug.LogException(exception, tweenline);
                }
            }

            m_ticking = false;

            if (m_hasRemoved) {
                Compact();
            }
        }

        private void Compact()
        {
            var write = 0;

            for (var read = 0; read < m_tweenlines.Count; read++) {
                var tweenline = m_tweenlines[read];

                if (ReferenceEquals(tweenline, null)) {
                    continue;
                }

                tweenline.RunnerIndex = write;
                m_tweenlines[write++] = tweenline;
            }

            m_tweenlines.RemoveRange(write, m_tweenlines.Count - write);
            m_hasRemoved = false;
        }

        private void OnDestroy()
        {
            foreach (var tweenline in m_tweenlines) {
                if (!ReferenceEquals(tweenline, null)) {
                    tweenline.RunnerIndex = -1;
                }
            }

            m_tweenlines.Clear();

            if (s_instance == this) {
                s_instance = null;
            }
        }
    }
}
