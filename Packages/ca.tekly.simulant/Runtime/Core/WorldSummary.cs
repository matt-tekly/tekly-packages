using System.Collections.Generic;
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
				DataPools = m_poolMap.Values.Select(x => x.GetSummary()).OrderBy(x => x.Type).ToArray()
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
			sb.AppendLine("World Summary");
			sb.AppendLine("------------------------------------------------------------------------------------------");
			var worldHeaders = new[] { "Entities", "Recycled" };
			var worldRow = new List<string[]> { new[] { $"{EntityCount - EntityRecycled}", $"{EntityRecycled}" } };
			Tableify.WriteRows(worldHeaders, worldRow, sb);
			sb.AppendLine("------------------------------------------------------------------------------------------");

			sb.AppendLine();

			sb.AppendLine("Data Pool Summary");
			sb.AppendLine("------------------------------------------------------------------------------------------");

			var rows = DataPools.Select(x => x.AsRow()).ToList();
			var poolHeaders = new[] { "Type", "Blittable", "Size", "Transient", "Init", "Recycle", "Count" };
			Tableify.WriteRows(poolHeaders, rows, sb);

			sb.AppendLine("------------------------------------------------------------------------------------------");
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