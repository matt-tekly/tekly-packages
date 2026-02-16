using System;
using Tekly.Common.LifeCycles;
using Tekly.Common.Utils;
using Tekly.Thunk.Music;

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

        internal int NextId;
        
        private ThunkTrackManager m_trackManager;
        
        public Thunk()
        {
            LifeCycle.Instance.Pause += OnPause;
            
            LifeCycle.Instance.Update += () => {
                ClipStateManager.Tick();
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
    }
}
