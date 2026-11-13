using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.Common.Tweenlines
{
    [Serializable]
    public class TweenMarker
    {
        public float Time;
        public string Name;
        public UnityEvent Event = new UnityEvent();
    }

    /// <summary>
    /// A named timeline of tracks and markers. Time starts at 0.
    /// </summary>
    [Serializable]
    public class TweenAnimation
    {
        public string Name {
            get => m_name;
            set => m_name = value;
        }

        public TweenWrap Wrap {
            get => m_wrap;
            set => m_wrap = value;
        }

        public List<TweenTrack> Tracks => m_tracks;
        public List<TweenMarker> Markers => m_markers;

        public float Duration {
            get {
                var duration = 0f;

                foreach (var track in m_tracks) {
                    if (track != null) {
                        duration = Mathf.Max(duration, track.End);
                    }
                }

                foreach (var marker in m_markers) {
                    duration = Mathf.Max(duration, marker.Time);
                }

                return duration;
            }
        }

        [SerializeField] private string m_name;
        [SerializeField] private TweenWrap m_wrap;
        [SerializeReference] private List<TweenTrack> m_tracks = new List<TweenTrack>();
        [SerializeField] private List<TweenMarker> m_markers = new List<TweenMarker>();

        [NonSerialized] private TweenMixer m_mixer;

        public TweenAnimation() { }

        public TweenAnimation(string name)
        {
            m_name = name;
        }

        public void CaptureOrigins(TweenOrigins origins)
        {
            foreach (var track in m_tracks) {
                track?.CaptureOrigins(origins);
            }
        }

        /// <summary>
        /// Resolves every track before playback. readCurrent reads each channel's live value as the start
        /// value used by ClipFrom.Current and by step tracks before their first switch.
        /// </summary>
        public void Prepare(TweenOrigins origins, bool readCurrent)
        {
            m_mixer ??= new TweenMixer();
            m_mixer.BeginPrepare(origins, readCurrent);

            // Base tracks first so layered tracks can see which channels have a base track
            foreach (var track in m_tracks) {
                if (track != null && !(track is ILayeredTrack)) {
                    track.Prepare(m_mixer);
                }
            }

            foreach (var track in m_tracks) {
                if (track is ILayeredTrack) {
                    track.Prepare(m_mixer);
                }
            }
        }

        /// <summary>
        /// Samples every track at a time and writes each driven channel once. Call Prepare first.
        /// </summary>
        public void Evaluate(float time)
        {
            if (m_mixer == null) {
                return;
            }

            m_mixer.BeginEvaluate();

            foreach (var track in m_tracks) {
                if (track != null && track.Enabled && !(track is ILayeredTrack)) {
                    track.Evaluate(time);
                }
            }

            foreach (var track in m_tracks) {
                if (track != null && track.Enabled && track is ILayeredTrack) {
                    track.Evaluate(time);
                }
            }

            m_mixer.Flush();
        }

        /// <summary>
        /// Fires markers crossed moving from one time to another, in either direction. The from time is
        /// exclusive unless includeFrom is set and the to time is inclusive.
        /// </summary>
        public void FireMarkers(float from, float to, bool includeFrom = false)
        {
            foreach (var marker in m_markers) {
                var time = marker.Time;
                bool crossed;

                if (to >= from) {
                    crossed = (includeFrom ? time >= from : time > from) && time <= to;
                } else {
                    crossed = (includeFrom ? time <= from : time < from) && time >= to;
                }

                if (crossed) {
                    marker.Event?.Invoke();
                }
            }
        }
    }
}
