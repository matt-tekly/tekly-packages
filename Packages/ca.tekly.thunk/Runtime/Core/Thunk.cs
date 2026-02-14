using Tekly.Common.LifeCycles;
using Tekly.Common.Utils;

namespace Tekly.Thunk.Core
{
    public class Thunk : Singleton<Thunk>
    {
        public const int INVALID_ID = -1;
        public bool Paused { get; private set; }
        public ThunkClipStateManager ClipStateManager { get; } = new ThunkClipStateManager();
        
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
    }
}
