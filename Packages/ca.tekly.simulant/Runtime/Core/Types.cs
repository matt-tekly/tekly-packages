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

		public static readonly WorldConfig Default = new WorldConfig {
			EntityCapacity = 512,
			DataPools = new DataPoolConfig {
				Capacity = 512,
				RecycleCapacity = 127
			}
		};
		
		public static readonly WorldConfig Large = new WorldConfig {
			EntityCapacity = 32 *  1024,
			DataPools = new DataPoolConfig {
				Capacity = 32 *  1024,
				RecycleCapacity = 1024
			}
		};
	}
	
	public enum Modification
	{
		Add,
		Remove
	}
}