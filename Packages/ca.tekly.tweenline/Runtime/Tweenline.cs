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

        /// <summary>Whether the current play was hurried. Cleared by the next Play</summary>
        public bool IsHurried => m_hurried;
        public float Time => m_time;
        public TweenAnimation Current => m_current;
        public List<TweenAnimation> Animations => m_animations;

        /// <summary>Changes whenever a play starts or is stopped, so a play can be told apart from a later one</summary>
        internal int PlayId => m_playId;

        /// <summary>Where this sits in the TweenlineRunner's list, or -1 when it isn't being updated</summary>
        internal int RunnerIndex { get; set; } = -1;

        public TweenOrigins Origins => m_origins ??= new TweenOrigins();

        public float Speed {
            get => m_speed;
            set => m_speed = Mathf.Max(0, value);
        }

        public float HurrySpeed {
            get => m_hurrySpeed;
            set => m_hurrySpeed = Mathf.Max(1, value);
        }

        [Tooltip("Animation to play when enabled. Leave empty to not play anything")]
        [SerializeField] private string m_playOnEnable;
        [SerializeField] private bool m_useUnscaledTime;
        [SerializeField] [Min(0)] private float m_speed = 1;

        [Tooltip("Start delay in seconds multiplied by the sibling index")]
        [SerializeField] [Min(0)] private float m_stagger;

        [Tooltip("Speed multiplier Hurry applies to the current play, like when the player taps to skip")]
        [SerializeField] [Min(1)] private float m_hurrySpeed = 4;

        [SerializeField] private List<TweenAnimation> m_animations = new List<TweenAnimation>();
        [SerializeField] private UnityEvent<string> m_completed = new UnityEvent<string>();

        private static readonly List<Tweenline> s_hurryBuffer = new List<Tweenline>();
        private static bool s_hurrying;

        private TweenOrigins m_origins;
        private TweenAnimation m_current;
        private float m_time;
        private float m_duration;
        private float m_delay;
        private bool m_startMarkersPending;
        private float m_direction = 1;
        private bool m_playing;
        private bool m_hurried;
        private int m_playId;
        private AwaitableCompletionSource m_completion;
        private bool m_awaited;

        private void OnEnable()
        {
            // Picks a play left running when it was disabled back up
            UpdateRunner();

            if (!string.IsNullOrEmpty(m_playOnEnable)) {
                Play(m_playOnEnable);
            }
        }

        private void OnDisable()
        {
            UpdateRunner();

            // Ticks stop here, so a play being awaited could never finish
            CancelAwait();
        }

        /// <summary>
        /// Advances the current play by a frame. Called by TweenlineRunner while this is playing and enabled.
        /// </summary>
        internal void Tick()
        {
            if (!m_playing) {
                return;
            }

            var deltaTime = (m_useUnscaledTime ? UnityEngine.Time.unscaledDeltaTime : UnityEngine.Time.deltaTime) * m_speed;

            if (m_hurried) {
                deltaTime *= m_hurrySpeed;
            }

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

        /// <summary>
        /// Hurries every Tweenline playing under a root, including the root, for example when the player taps
        /// to skip. With completeIfHurried, ones already hurried complete instead, so a second tap finishes.
        /// </summary>
        public static void HurryAll(Component root, bool completeIfHurried = false)
        {
            if (root == null) {
                return;
            }

            // A marker fired by Complete could hurry again, so a nested call gets its own list
            var tweenlines = s_hurrying ? new List<Tweenline>() : s_hurryBuffer;
            var nested = s_hurrying;

            root.GetComponentsInChildren(tweenlines);
            s_hurrying = true;

            try {
                foreach (var tweenline in tweenlines) {
                    if (completeIfHurried && tweenline.IsHurried) {
                        tweenline.Complete();
                    } else {
                        tweenline.Hurry();
                    }
                }
            } finally {
                tweenlines.Clear();
                s_hurrying = nested;
            }
        }

        /// <summary>
        /// Plays an animation on the first count Tweenlines, each starting stagger seconds after the one before
        /// it, and completes when all of them have finished. The stagger follows play order and replaces each
        /// Tweenline's own Stagger. Inactive ones are skipped without leaving a gap. Throws
        /// OperationCanceledException as soon as any of the plays is interrupted, see PlayAsync.
        /// </summary>
        public static Awaitable PlayAllAsync(IReadOnlyList<Tweenline> tweenlines, string animationName, int count, float stagger = 0)
        {
            return PlayAllAsync(tweenlines, animationName, count, stagger, null);
        }

        internal static Awaitable PlayAllAsync(IReadOnlyList<Tweenline> tweenlines, string animationName, int count, float stagger, TweenSequence sequence)
        {
            var group = PlayGroup.Rent();
            var awaitable = group.Completion.Awaitable;
            var delay = 0f;

            count = Mathf.Min(count, tweenlines.Count);

            for (var i = 0; i < count; i++) {
                var tweenline = tweenlines[i];

                if (tweenline == null || !tweenline.isActiveAndEnabled) {
                    continue;
                }

                if (!tweenline.TryFind(animationName, out var animation)) {
                    continue;
                }

                group.Watch(tweenline.PlayAsync(animation, false, delay));
                sequence?.Started(tweenline);
                delay += stagger;
            }

            // Plays that finished straight away have already reported, so this can complete the group now
            group.Release();

            return awaitable;
        }

        /// <summary>
        /// Speeds up the current play by Hurry Speed until it ends, including any stagger delay still waiting.
        /// Markers still fire in order, unlike Complete. Looping animations are left alone since they never
        /// end. Returns true when the play was already hurried, so a caller can complete it on a second tap.
        /// </summary>
        public bool Hurry()
        {
            if (!m_playing || m_current.Wrap != TweenWrap.Once) {
                return false;
            }

            var wasHurried = m_hurried;
            m_hurried = true;

            return wasHurried;
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

        /// <summary>
        /// Plays an animation from its end back to its start. When that animation is already playing forward
        /// it turns around from the current time instead of jumping to the end, like a hover that is left
        /// part way in. Play does the same for an animation that is playing in reverse.
        /// </summary>
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
            CancelAwait();
            Begin(animation, reverse, StaggerDelay());
        }

        /// <summary>
        /// Plays an animation and completes when it finishes, including when Complete or a hurry ends it early.
        /// Throws OperationCanceledException when the play is interrupted instead: another Play, Stop, or the
        /// Tweenline being disabled or destroyed. A looping animation never finishes, so it only ever ends
        /// that way. A missing animation logs a warning and completes straight away.
        /// </summary>
        public Awaitable PlayAsync(string animationName)
        {
            if (!TryFind(animationName, out var animation)) {
                return CompletedAwaitable();
            }

            return PlayAsync(animation, false, StaggerDelay());
        }

        public Awaitable PlayAsync(TweenAnimation animation, bool reverse = false)
        {
            return PlayAsync(animation, reverse, StaggerDelay());
        }

        /// <summary>
        /// Stops playback and shows the first frame of an animation without firing markers or Completed, for
        /// example to hide something until its turn in a sequence. Clips that start from the current value
        /// go back to where the last forward play started, or to the rest value when there has not been one.
        /// </summary>
        public void Rewind(string animationName)
        {
            if (TryFind(animationName, out var animation)) {
                Rewind(animation);
            }
        }

        public void Rewind(TweenAnimation animation)
        {
            Stop();

            m_current = animation;
            m_duration = animation.Duration;
            m_time = 0;

            animation.Prepare(Origins, false);
            animation.Evaluate(m_time);
        }

        /// <summary>
        /// Stops playback, leaving every property where it is.
        /// </summary>
        public void Stop()
        {
            m_playId++;
            m_playing = false;
            m_hurried = false;
            m_startMarkersPending = false;

            UpdateRunner();
            CancelAwait();
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
            if (TryFind(animationName, out var animation)) {
                Play(animation, reverse);
            }
        }

        internal Awaitable PlayAsync(TweenAnimation animation, bool reverse, float delay)
        {
            CancelAwait();

            if (m_completion == null) {
                m_completion = new AwaitableCompletionSource();
            } else {
                m_completion.Reset();
            }

            // Held locally because a marker fired by Begin can start another awaited play, which resets the source
            var awaitable = m_completion.Awaitable;
            m_awaited = true;

            // An inactive Tweenline is never ticked, so the play could not finish
            if (!isActiveAndEnabled) {
                CancelAwait();
            }

            Begin(animation, reverse, delay);

            return awaitable;
        }

        private void Begin(TweenAnimation animation, bool reverse, float delay)
        {
            var direction = reverse ? -1 : 1;

            if (CanTurnAround(animation, direction)) {
                TurnAround(direction);
                return;
            }

            m_playId++;
            m_current = animation;
            m_duration = animation.Duration;
            m_direction = direction;
            m_time = reverse ? m_duration : 0;
            m_hurried = false;
            m_delay = delay;
            m_playing = true;

            UpdateRunner();

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
        /// Whether a play is asking for the animation that is already playing, in the other direction. A
        /// PingPong animation changes direction by itself, so it always starts over.
        /// </summary>
        private bool CanTurnAround(TweenAnimation animation, int direction)
        {
            return m_playing && m_current == animation && m_direction != direction && animation.Wrap != TweenWrap.PingPong;
        }

        /// <summary>
        /// Carries on from the current time in the other direction, as a new play. The clip values of the
        /// play being turned around are kept, so it runs back through the same motion. A stagger delay is
        /// not applied, since that would freeze the animation part way.
        /// </summary>
        private void TurnAround(int direction)
        {
            m_playId++;
            m_direction = direction;
            m_hurried = false;
            m_delay = 0;

            // Set when the play was still waiting on its stagger delay. Nothing has moved, so there is
            // nothing for its start markers to announce
            m_startMarkersPending = false;

            var atEdge = direction > 0 ? m_time >= m_duration : m_time <= 0;

            if (atEdge) {
                Finish();
            }
        }

        internal static Awaitable CompletedAwaitable()
        {
            var completion = new AwaitableCompletionSource();
            completion.SetResult();

            return completion.Awaitable;
        }

        internal float StaggerDelay()
        {
            return m_stagger * transform.GetSiblingIndex();
        }

        /// <summary>
        /// Finds an animation by name, logging a warning when there is none.
        /// </summary>
        internal bool TryFind(string animationName, out TweenAnimation animation)
        {
            animation = Find(animationName);

            if (animation == null) {
                Debug.LogWarning($"Tweenline [{name}] has no animation named [{animationName}]", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Puts this on the TweenlineRunner while it is playing and enabled, and takes it off otherwise. Only
        /// in play mode, since nothing ticks in edit mode and the runner's object can't be made there.
        /// </summary>
        private void UpdateRunner()
        {
            var shouldTick = m_playing && isActiveAndEnabled && Application.isPlaying;

            if (shouldTick && RunnerIndex < 0) {
                TweenlineRunner.Add(this);
            } else if (!shouldTick && RunnerIndex >= 0) {
                TweenlineRunner.Remove(this);
            }
        }

        /// <summary>
        /// Ends a pending PlayAsync with a cancellation. Its continuation runs inside this call.
        /// </summary>
        private void CancelAwait()
        {
            if (!m_awaited) {
                return;
            }

            m_awaited = false;
            m_completion.SetCanceled();
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
            m_hurried = false;

            UpdateRunner();

            var animation = m_current;

            // Before the events, so a listener that plays something else does not cancel a play that finished
            if (m_awaited) {
                m_awaited = false;
                m_completion.SetResult();
            }

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

        /// <summary>
        /// The plays started by one PlayAllAsync. Completes when every play has finished and is cancelled as
        /// soon as any of them is interrupted, instead of waiting for the plays before it. Groups are pooled
        /// and go back to the pool once every play has reported, since a cancelled group still has plays
        /// running that will report later.
        /// </summary>
        private sealed class PlayGroup
        {
            private static readonly Stack<PlayGroup> s_pool = new Stack<PlayGroup>();

            public readonly AwaitableCompletionSource Completion = new AwaitableCompletionSource();

            private int m_pending;
            private bool m_done;

            /// <summary>
            /// A group holding one report for the caller, released once every play has been started, so
            /// plays that finish straight away can't complete it early.
            /// </summary>
            public static PlayGroup Rent()
            {
                var group = s_pool.Count > 0 ? s_pool.Pop() : new PlayGroup();

                group.Completion.Reset();
                group.m_pending = 1;
                group.m_done = false;

                return group;
            }

            public void Watch(Awaitable play)
            {
                m_pending++;
                _ = WatchAsync(play);
            }

            public void Release()
            {
                if (!m_done && m_pending == 1) {
                    m_done = true;
                    Completion.SetResult();
                }

                if (--m_pending == 0) {
                    s_pool.Push(this);
                }
            }

            private async Awaitable WatchAsync(Awaitable play)
            {
                try {
                    await play;
                } catch (OperationCanceledException) {
                    if (!m_done) {
                        m_done = true;
                        Completion.SetCanceled();
                    }
                }

                Release();
            }
        }
    }
}
