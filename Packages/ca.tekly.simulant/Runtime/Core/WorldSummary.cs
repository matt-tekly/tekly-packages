using System.Linq;
using System.Text;
using Tekly.Common.Utils;

namespace Tekly.Simulant.Core
{
	public partial class World
	{
		public WorldSummary GetSummary()
		{
			return new WorldSummary {
				EntityCount = Entities.Count,
				EntityRecycled = m_recycledEntities.Count,
				DataPools = m_poolMap.Values.Select(x => x.GetSummary()).ToArray()
			};
		}
	}

	public class WorldSummary
	{
		public int EntityCount;
		public int EntityRecycled;

		public DataPoolSummary[] DataPools;

		public override string ToString()
		{
			var sb = new StringBuilder();
			ToString(sb);
			return sb.ToString();
		}

		public void ToString(StringBuilder sb)
		{
			sb.AppendLine("World");
			sb.AppendLine("---------------------------");
			sb.AppendLine($"Entities: [{EntityCount}]");
			sb.AppendLine($"Recycled: [{EntityRecycled}]");
			sb.AppendLine("---------------------------");

			sb.AppendLine();

			sb.AppendLine("Data");
			sb.AppendLine("---------------------------");

			var rows = DataPools.Select(x => x.AsRow()).ToList();
			Tableify.WriteRows(new[] { "Type", "Blittable", "Size", "Transient", "Init", "Recycle", "Count" }, rows, sb);

			sb.AppendLine("---------------------------");
		}
	}

	public class DataPoolSummary
	{
		public string Type;
		public bool Blittable;
		public int Size;
		public bool Transient;
		public bool Init;
		public bool Recycle;
		public bool AutoRecycle;
		public int Count;
		
		public string[] AsRow()
		{
			return new[] {
				Type,
				Blittable ? "True" : "False",
				Size.ToString(),
				Transient ? "Transient" : "Persist",
				Init ? "Init" : "None",
				Recycle ? "Recycle" : AutoRecycle ? "Auto" : "None",
				Count.ToString()
			};
		}
	}
}