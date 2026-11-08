namespace Tekly.Stagecraft
{
	/// <summary>
	/// Implemented by a behaviour that takes per-spawn data. <see cref="Initialize"/> runs inside Spawn/Activate
	/// while the GameObject is still inactive, before Awake/OnEnable on a first spawn and before any OnSpawned, so
	/// the object wakes up already holding this spawn's data. Store the args here and do the work in OnSpawned;
	/// components cached in Awake are not available yet on the first spawn.
	/// TArgs must be a plain struct with no object references so a spawn can be described as data.
	/// </summary>
	public interface ISpawnArgs<TArgs> where TArgs : struct
	{
		void Initialize(in TArgs args);
	}
}
