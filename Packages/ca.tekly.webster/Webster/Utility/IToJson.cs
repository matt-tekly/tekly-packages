//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Tekly.Webster.Utility
{
	public interface IToJson
	{
		void ToJson(StringBuilder sb);
	}

	public static class ToJsonExtensions
	{
		public static void Write<T>(this StringBuilder sb, string property, IList<T> list, bool appendComma) where T : IToJson
		{
			var comma = appendComma ? "," : "";
			sb.Append($"\"{property}\":[");
			for (var i = 0; i < list.Count; i++) {
				list[i].ToJson(sb);
				if (i != list.Count - 1) {
					sb.Append(",");
				}
			}

			sb.Append($"]{comma}");
		}

		public static void Write(this StringBuilder sb, string property, IToJson value, bool appendComma)
		{
			sb.AppendFormat("\"{0}\":", property);
			value.ToJson(sb);
			if (appendComma) {
				sb.Append(',');
			}
		}

		public static void Write(this StringBuilder sb, string property, string value, bool appendComma)
		{
			var comma = appendComma ? "," : "";
			sb.AppendFormat("\"{0}\": \"{1}\"{2}", property, value, comma);
		}

		public static void Write(this StringBuilder sb, string property, bool value, bool appendComma)
		{
			var comma = appendComma ? "," : "";
			var jsonValue = value ? "true" : "false";
			sb.AppendFormat("\"{0}\": {1}{2}", property, jsonValue, comma);
		}

		public static void Write(this StringBuilder sb, string property, int value, bool appendComma)
		{
			var comma = appendComma ? "," : "";
			sb.AppendFormat("\"{0}\": {1}{2}", property, value, comma);
		}

		public static void Write(this StringBuilder sb, string property, long value, bool appendComma)
		{
			var comma = appendComma ? "," : "";
			sb.AppendFormat("\"{0}\": {1}{2}", property, value, comma);
		}

		public static void Write(this StringBuilder sb, string property, float value, bool appendComma)
		{
			var comma = appendComma ? "," : "";
			sb.AppendFormat("\"{0}\": {1}{2}", property, value, comma);
		}

		public static void Write(this StringBuilder sb, string property, double value, bool appendComma)
		{
			var comma = appendComma ? "," : "";
			sb.AppendFormat("\"{0}\": {1}{2}", property, value, comma);
		}

		public static void Write(this StringBuilder sb, string property, DateTime value, bool appendComma)
		{
			var comma = appendComma ? "," : "";
			sb.AppendFormat("\"{0}\": \"{1}\"{2}", property, value, comma);
		}

		public static void Write(this StringBuilder sb, string property, Vector2 vector, bool appendComma)
		{
			var comma = appendComma ? "," : "";
			sb.AppendFormat("\"{0}\": {{\"x\": {1}, \"y\": {2}}}{3}", property, vector.x, vector.y, comma);
		}

		public static void Write(this StringBuilder sb, string property, Vector3 vector, bool appendComma)
		{
			var comma = appendComma ? "," : "";
			sb.AppendFormat("\"{0}\": {{\"x\": {1}, \"y\": {2}, \"z\": {3}}}{4}", property, vector.x, vector.y,
				vector.z, comma);
		}
	}
}