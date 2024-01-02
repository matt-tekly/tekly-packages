namespace Tekly.Simulant.Core
{
	public struct DataPoolConfig
	{
		public int Capacity;
		public int RecycleCapacity;
	}

	public struct WorldConfig
	{
		public int EntityCapacity;
		public DataPoolConfig DataPools;
	}
	
	public enum Modification
	{
		Add,
		Remove
	}
}