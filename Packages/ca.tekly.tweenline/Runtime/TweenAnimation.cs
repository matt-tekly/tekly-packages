using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Tekly.Tweenlines
{
    /// <summary>
    /// Why a marker fired.
    /// </summary>
    public enum TweenMarkerReason
    {
        /// <summary>Crossed during playback, including a hurried play, loops and turning around</summary>
        Played,

        /// <summary>Jumped past by Seek, in either direction</summary>
        Seeked,

        /// <summary>Jumped past by Complete</summary>
        Skipped
    }

    /// <summary>
    /// When a marker's Event is invoked.
    /// </summary>
    public enum TweenMarkerFire
    {
        /// <summary>However the marker is reached, for markers that set state</summary>
        Always,

        /// <summary>Only when played through, not seeked or skipped past, for effects like sounds</summary>
        OnlyWhenPlayed
    }

    [Serializable]
    public class TweenMarker
    {
        public float Time;
        public string Name;

        [Tooltip("Always for markers that set state, so a skip or seek still leaves things right. Only When Played for "
                 + "effects like sounds. Tweenline.MarkerFired is raised either way")]
        public TweenMarkerFire Fire = TweenMarkerFire.Always;

        public UnityEvent Event = new UnityEvent();

        public bool Fires(TweenMarkerReason reason)
        {
            return Fire == TweenMarkerFire.Always || reason == TweenMarkerReason.Played;
        }
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

        // Split once in Prepare so Evaluate doesn't check every track's type twice per frame
        [NonSerialized] private List<TweenTrack> m_baseTracks;
        [NonSerialized] private List<TweenTrack> m_layeredTracks;

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
            m_baseTracks ??= new List<TweenTrack>();
            m_layeredTracks ??= new List<TweenTrack>();

            m_mixer.BeginPrepare(origins, readCurrent);
            m_baseTracks.Clear();
            m_layeredTracks.Clear();

            foreach (var track in m_tracks) {
                if (track is ILayeredTrack) {
                    m_layeredTracks.Add(track);
                } else if (track != null) {
                    m_baseTracks.Add(track);
                }
            }

            // Base tracks first so layered tracks can see which channels have a base track
            foreach (var track in m_baseTracks) {
                track.Prepare(m_mixer);
            }

            foreach (var track in m_layeredTracks) {
                track.Prepare(m_mixer);
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

            foreach (var track in m_baseTracks) {
                if (track.Enabled) {
                    track.Evaluate(time);
                }
            }

            foreach (var track in m_layeredTracks) {
                if (track.Enabled) {
                    track.Evaluate(time);
                }
            }

            m_mixer.Flush();
        }

        /// <summary>
        /// Fires markers crossed moving from one time to another, in either direction. The from time is
        /// exclusive unless includeFrom is set and the to time is inclusive. Each one is reported to the owner,
        /// which invokes its Event when its Fire setting allows the reason.
        /// </summary>
        internal void FireMarkers(Tweenline owner, float from, float to, TweenMarkerReason reason, bool includeFrom = false)
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
                    owner.OnMarker(marker, reason);
                }
            }
        }
    }
}
