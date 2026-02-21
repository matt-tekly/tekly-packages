namespace Tekly.Common.Poolables
{
	public enum PoolableStatus
	{
		/// Not yet assigned to a pool
		None, 
		/// Owned by the pool, inactive
		InPool,
		/// Checked out, being used by gameplay
		InUse, 
		/// OnDestroy has run, never use again
		Destroyed 
	}
}