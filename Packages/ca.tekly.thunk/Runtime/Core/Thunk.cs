using System;
using Tekly.Common.LifeCycles;
using Tekly.Common.Utils;
using Tekly.Thunk.Music;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Thunk.Core
{
    public class Thunk : Singleton<Thunk>, IDisposable
    {
        public const int INVALID_ID = -1;
        
        /// <summary>
        /// Tracks when Unity itself is paused, which can pause all audio
        /// </summary>
        public bool Paused { get; private set; }
        
        public ThunkClipStateManager ClipStateManager { get; } = new ThunkClipStateManager();

        public ThunkTrackManager TrackManager => m_trackManager ??= new ThunkTrackManager();
        public ThunkEmitter OneShot => m_oneShotEmitter ??= CreateEmitter("[Thunk] OneShot");
        
        internal int NextClipStateId;
        
        private ThunkTrackManager m_trackManager;
        private ThunkEmitter m_oneShotEmitter;
        
        public Thunk()
        {
            LifeCycle.Instance.Pause += OnPause;
            
            LifeCycle.Instance.Update += () => {
                ClipStateManager.Tick(Time.deltaTime, Time.unscaledDeltaTime);
            };
        }
        
        private void OnPause(bool paused)
        {
            Paused = paused;
        }

        public void Dispose()
        {
            ClipStateManager.Dispose();
        }

        private ThunkEmitter CreateEmitter(string name)
        {
            if (m_oneShotEmitter == null) {
                var go = new GameObject(name);
                Object.DontDestroyOnLoad(go);
                
                m_oneShotEmitter = go.AddComponent<ThunkEmitter>();
            }

            return m_oneShotEmitter;
        }
    }
}
