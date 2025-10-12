using Tekly.Common.Observables;
using Tekly.Common.Registrys;

namespace Tekly.TreeState
{
    public struct TreeActivityModeChangedEvt
    {
        public string Manager;
        public string State;
        public string ActivityType;
        public ActivityMode Mode;
        public ActivityMode PreviousMode;
        public bool IsState;
    }
    
    public class TreeStateRegistry : SingletonRegistry<TreeStateManager, TreeStateRegistry>
    {
        public readonly Triggerable<TreeActivityModeChangedEvt> ActivityModeChanged = new Triggerable<TreeActivityModeChangedEvt>();
    }
}