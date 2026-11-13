using System;
using System.Collections.Generic;
using Tekly.Tweenlines.Tracks;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.Tweenlines
{
    /// <summary>
    /// Plays named timeline animations made of tracks of clips. Only one animation plays at a time.
    /// </summary>
    public class Tweenline : MonoBehaviour
    {
        public event Action<Tweenline, TweenAnimation> Completed;

        public bool IsPlaying => m_playing;
        public float Time => m_time;
        public TweenAnimation Current => m_current;
        public List<TweenAnimation> Animations => m_animations;
        public TweenOrigins Origins => m_origins ??= new TweenOrigins();

        public float Speed {
            get => m_speed;
            set => m_speed = Mathf.Max(0, value);
        }

        [Tooltip("Animation to play when enabled. Leave empty to not play anything")]
        [SerializeField] private string m_playOnEnable;
        [SerializeField] private bool m_useUnscaledTime;
        [SerializeField] [Min(0)] private float m_speed = 1;

        [Tooltip("Start delay in seconds multiplied by the sibling index")]
        [SerializeField] [Min(0)] private float m_stagger;

        [SerializeField] private List<TweenAnimation> m_animations = new List<TweenAnimation>();
        [SerializeField] private UnityEvent<string> m_completed = new UnityEvent<string>();

        private TweenOrigins m_origins;
        private TweenAnimation m_current;
        private float m_time;
        private float m_duration;
        private float m_delay;
        private bool m_startMarkersPending;
        private float m_direction = 1;
        private bool m_playing;
        private int m_playId;

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(m_playOnEnable)) {
                Play(m_playOnEnable);
            }
        }

        private void Update()
        {
            if (!m_playing) {
                return;
            }

            var deltaTime = (m_useUnscaledTime ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime) * m_speed;

            if (m_delay > 0) {
                m_delay -= deltaTime;

                if (m_delay > 0) {
                    return;
                }

                deltaTime = -m_delay;
                m_delay = 0;
            }

            if (m_startMarkersPending && !FireStartMarkers()) {
                return;
            }

            Advance(deltaTime);
        }

        public bool Has(string animationName)
        {
            return Find(animationName) != null;
        }

        public TweenAnimation Find(string animationName)
        {
            foreach (var animation in m_animations) {
                if (animation.Name == animationName) {
                    return animation;
                }
            }

            return null;
        }

        public void Play(string animationName)
        {
            Play(animationName, false);
        }

        public void PlayReverse(string animationName)
        {
            Play(animationName, true);
        }

        public void Play(int index)
        {
            if (index < 0 || index >= m_animations.Count) {
                Debug.LogWarning($"Tweenline [{name}] has no animation at index [{index}]", this);
                return;
            }

            Play(m_animations[index], false);
        }

        public void Play(TweenAnimation animation, bool reverse)
        {
            m_playId++;
            m_current = animation;
            m_duration = animation.Duration;
            m_direction = reverse ? -1 : 1;
            m_time = reverse ? m_duration : 0;
            m_delay = m_stagger * transform.GetSiblingIndex();
            m_playing = true;

            // Reversing plays back towards the start, so it keeps the start values of the last forward play
            // instead of reading the end pose as the start
            animation.Prepare(Origins, !reverse);

            // The start pose shows straight away, but markers wait for the stagger delay
            animation.Evaluate(m_time);
            m_startMarkersPending = true;

            if (m_delay > 0 || !FireStartMarkers()) {
                return;
            }

            if (m_duration <= 0) {
                Finish();
            }
        }

        /// <summary>
        /// Stops playback, leaving every property where it is.
        /// </summary>
        public void Stop()
        {
            m_playId++;
            m_playing = false;
            m_startMarkersPending = false;
        }

        /// <summary>
        /// Jumps the playing animation to its end, fires the remaining markers and completes it.
        /// </summary>
        public void Complete()
        {
            if (!m_playing) {
                return;
            }

            if (m_startMarkersPending && !FireStartMarkers()) {
                return;
            }

            var playId = m_playId;
            var edge = m_direction > 0 ? m_duration : 0;

            m_current.FireMarkers(m_time, edge);

            if (playId != m_playId) {
                return;
            }

            m_time = edge;
            m_current.Evaluate(m_time);
            Finish();
        }

        /// <summary>
        /// Forgets the stored rest values and captures them again from the current state.
        /// </summary>
        public void CaptureOrigins()
        {
            Origins.Clear();

            foreach (var animation in m_animations) {
                animation.CaptureOrigins(Origins);
            }
        }

        /// <summary>
        /// Resolves an animation's clip values without playing it. Used by the editor preview.
        /// </summary>
        public void Prepare(TweenAnimation animation, bool readCurrent)
        {
            animation.Prepare(Origins, readCurrent);

            if (animation == m_current) {
                m_duration = animation.Duration;
            }
        }

        /// <summary>
        /// Samples an animation at a time without firing markers. Call Prepare first.
        /// </summary>
        public void Evaluate(TweenAnimation animation, float time)
        {
            animation.Evaluate(time);
        }

        private void Play(string animationName, bool reverse)
        {
            var animation = Find(animationName);

            if (animation == null) {
                Debug.LogWarning($"Tweenline [{name}] has no animation named [{animationName}]", this);
                return;
            }

            Play(animation, reverse);
        }

        /// <summary>
        /// Fires markers at the starting time once the stagger delay has passed. False when a marker started
        /// another animation or stopped this one.
        /// </summary>
        private bool FireStartMarkers()
        {
            var playId = m_playId;

            m_startMarkersPending = false;
            m_current.FireMarkers(m_time, m_time, true);

            return playId == m_playId;
        }

        private void Advance(float deltaTime)
        {
            var playId = m_playId;
            var duration = m_duration;
            var previous = m_time;
            var finished = false;

            m_time += deltaTime * m_direction;

            var reachedEdge = m_direction > 0 ? m_time >= duration : m_time <= 0;

            if (reachedEdge) {
                var edge = m_direction > 0 ? duration : 0;
                var overshoot = duration > 0 ? Mathf.Abs(m_time - edge) % duration : 0;

                m_current.FireMarkers(previous, edge);

                if (playId != m_playId) {
                    return;
                }

                var wrap = duration > 0 ? m_current.Wrap : TweenWrap.Once;

                switch (wrap) {
                    case TweenWrap.Once:
                        m_time = edge;
                        finished = true;
                        break;
                    case TweenWrap.Loop:
                        var start = m_direction > 0 ? 0 : duration;
                        m_time = start + overshoot * m_direction;
                        m_current.FireMarkers(start, m_time, true);
                        break;
                    case TweenWrap.PingPong:
                        m_direction = -m_direction;
                        m_time = edge + overshoot * m_direction;
                        m_current.FireMarkers(edge, m_time);
                        break;
                }
            } else {
                m_current.FireMarkers(previous, m_time);
            }

            if (playId != m_playId) {
                return;
            }

            m_current.Evaluate(m_time);

            if (finished) {
                Finish();
            }
        }

        private void Finish()
        {
            m_playing = false;

            var animation = m_current;
            Completed?.Invoke(this, animation);
            m_completed?.Invoke(animation.Name);
        }

        private void Reset()
        {
            var animation = new TweenAnimation("Show");
            TweenTrack track = GetComponent<RectTransform>() != null ? new AnchoredPositionTrack() : new PositionTrack();

            track.Bind(gameObject);
            track.AddClip(0);
            animation.Tracks.Add(track);

            m_animations = new List<TweenAnimation> { animation };
        }

        private void OnValidate()
        {
            m_completed ??= new UnityEvent<string>();
        }
    }
}
