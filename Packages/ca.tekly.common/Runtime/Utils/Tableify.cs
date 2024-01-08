using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tekly.Common.Utils
{
	public static class Tableify
	{
		public static void WriteRows(string[] header, List<string[]> data, StringBuilder sb)
		{
			var lengths = header.Select(x => x.Length).ToArray();
			
			for (var i = 0; i < lengths.Length; i++) {
				lengths[i] = Math.Max(lengths[i], data.Select(x => x[i].Length).Max());
			}

			WriteRow(header, lengths, sb);
				
			foreach (var row in data) {
				WriteRow(row, lengths, sb);
			}
		}

		public static void WriteRow(string[] row, int[] lengths, StringBuilder sb)
		{
			for (var index = 0; index < row.Length; index++) {
				var column = row[index];
				var length = lengths[index];

				sb.Append(column.PadRight(length));
				sb.Append(" ");
			}
			
			sb.AppendLine();
		}
		
		public static void WriteRows(List<string[]> data, StringBuilder sb)
		{
			var lengths = new int[data[0].Length];

			for (var i = 0; i < lengths.Length; i++) {
				lengths[i] = data.Select(x => x[i].Length).Max();
			}

			foreach (var row in data) {
				WriteRow(row, lengths, sb);
			}
		}
	}
}