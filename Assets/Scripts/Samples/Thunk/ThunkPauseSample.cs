using Tekly.DevBoard;
using Tekly.DevBoard.Nodes;
using Tekly.Thunk;
using UnityEngine;

namespace TeklySample.Samples.Thunk
{
    /// <summary>
    /// A DevBoard page, "Sample/Thunk Pause", for hearing what AudioListener.pause does to each kind of sound: a
    /// loop on an ordinary emitter, the same loop on an emitter that plays while paused, a chain and a timeline.
    /// </summary>
    public class ThunkPauseSample : MonoBehaviour
    {
        private const string PAGE = "Sample/Thunk Pause";

        [Tooltip("An ordinary emitter. What it plays goes silent while the listener is paused.")]
        [SerializeField] private ThunkEmitter m_emitter;

        [Tooltip("An emitter with Play While Paused ticked. What it plays carries on through a pause.")]
        [SerializeField] private ThunkEmitter m_pausedEmitter;

        [SerializeField] private ThunkSound m_loop;
        [SerializeField] private ThunkSound m_chain;
        [SerializeField] private ThunkSound m_timeline;

        private ThunkHandle m_loopHandle;
        private ThunkHandle m_pausedLoopHandle;
        private ThunkHandle m_chainHandle;
        private ThunkHandle m_timelineHandle;

        private void Start()
        {
            // The segments are removed when this GameObject is destroyed
            DevBoard.Page(PAGE, Listener, "Listener", 0, owner: gameObject);
            DevBoard.Page(PAGE, Loops, "Loops", 1, owner: gameObject);
            DevBoard.Page(PAGE, Chain, "Chain", 2, owner: gameObject);
            DevBoard.Page(PAGE, Timeline, "Timeline", 3, owner: gameObject);
        }

        private void OnDisable()
        {
            StopAll();
            AudioListener.pause = false;
        }

        private void Listener(Segment page)
        {
            page.Toggle("AudioListener.pause", () => AudioListener.pause, value => AudioListener.pause = value);
            page.Button("Stop Everything", StopAll);
        }

        private void Loops(Segment page)
        {
            page.Label("The same sound on two emitters. Only the second should be heard while paused.");

            page.Property("Ordinary emitter", () => State(m_loopHandle));
            var row = page.Row();
            row.Button("Play", () => m_loopHandle = Play(m_emitter, m_loop)).InteractableWhen(() => !m_loopHandle.IsPlaying);
            row.Button("Stop", () => m_loopHandle.Stop()).InteractableWhen(() => m_loopHandle.IsPlaying);

            page.Property("Emitter that plays while paused", () => State(m_pausedLoopHandle));
            row = page.Row();
            row.Button("Play", () => m_pausedLoopHandle = Play(m_pausedEmitter, m_loop)).InteractableWhen(() => !m_pausedLoopHandle.IsPlaying);
            row.Button("Stop", () => m_pausedLoopHandle.Stop()).InteractableWhen(() => m_pausedLoopHandle.IsPlaying);
        }

        private void Chain(Segment page)
        {
            page.Label("A three second rising sweep, then a steady tone that carries straight on from it. Pause during the sweep, wait, then resume and listen to the join.");

            page.Property("Chain", () => State(m_chainHandle));
            var row = page.Row();
            row.Button("Play", () => m_chainHandle = Play(m_emitter, m_chain)).InteractableWhen(() => !m_chainHandle.IsPlaying);
            row.Button("Release", () => m_chainHandle.Release()).InteractableWhen(() => m_chainHandle.IsPlaying);
            row.Button("Stop", () => m_chainHandle.Stop()).InteractableWhen(() => m_chainHandle.IsPlaying);
        }

        private void Timeline(Segment page)
        {
            page.Label("Three blips, at 0, 2 and 4 seconds. Pause after the first, wait, then resume. The second should be late by as long as the pause lasted.");

            page.Property("Timeline", () => State(m_timelineHandle));
            var row = page.Row();
            row.Button("Play", () => m_timelineHandle = Play(m_emitter, m_timeline)).InteractableWhen(() => !m_timelineHandle.IsPlaying);
            row.Button("Stop", () => m_timelineHandle.Stop()).InteractableWhen(() => m_timelineHandle.IsPlaying);
        }

        private static string State(ThunkHandle handle)
        {
            return handle.IsPlaying ? "Playing" : "Stopped";
        }

        private static ThunkHandle Play(ThunkEmitter emitter, ThunkSound sound)
        {
            return emitter != null && sound != null ? emitter.Play(sound) : ThunkHandle.None;
        }

        private void StopAll()
        {
            m_loopHandle.Stop();
            m_pausedLoopHandle.Stop();
            m_chainHandle.Stop();
            m_timelineHandle.Stop();
        }
    }
}
