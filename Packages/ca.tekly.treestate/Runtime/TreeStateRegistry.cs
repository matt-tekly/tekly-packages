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
        public string StateFullName;
    }

    public struct TreeActivitySlowEvt
    {
        public string Manager;
        public string State;
        public string ActivityType;
        public ActivityMode Mode;
        public float Duration;
        public bool Completed;
        public bool IsState;
        public string StateFullName;
    }

    public class TreeStateRegistry : SingletonRegistry<TreeStateManager, TreeStateRegistry>
    {
	    /// <summary>
	    /// Should TreeStateManagers update
	    /// </summary>
	    public bool Update { get; set; } = true;

	    /// <summary>
	    /// Loads or unloads that take longer than this many seconds emit ActivitySlow. 0 disables it.
	    /// </summary>
	    public float SlowThreshold { get; set; }

        public readonly Triggerable<TreeActivitySlowEvt> ActivitySlow = new Triggerable<TreeActivitySlowEvt>();

        public readonly Triggerable<TreeActivityModeChangedEvt> ActivityModeChanged = new Triggerable<TreeActivityModeChangedEvt>();
    }
}