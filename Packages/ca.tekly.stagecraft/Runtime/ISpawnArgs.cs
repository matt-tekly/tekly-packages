namespace Tekly.Stagecraft
{
	/// <summary>
	/// Implemented by a behaviour that takes per-spawn data. <see cref="Initialize"/> runs inside Spawn/Activate,
	/// after the GameObject is active (so Awake has run) and before any behaviour's OnSpawned.
	/// TArgs must be a plain struct with no object references so a spawn can be described as data.
	/// </summary>
	public interface ISpawnArgs<TArgs> where TArgs : struct
	{
		void Initialize(in TArgs args);
	}
}
