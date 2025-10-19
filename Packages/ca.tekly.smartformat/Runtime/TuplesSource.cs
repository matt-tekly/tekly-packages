using System;
using SmartFormat.Core.Extensions;

namespace Tekly.SmartFormat
{
	public sealed class TuplesSource : ISource
	{
		public bool TryEvaluateSelector(ISelectorInfo selectorInfo)
		{
			if (selectorInfo.CurrentValue is ValueTuple<string, object>[] tuples) {
				foreach (var tuple in tuples) {
					if (string.Equals(tuple.Item1, selectorInfo.SelectorText, StringComparison.OrdinalIgnoreCase)) {
						selectorInfo.Result = tuple.Item2;
						return true;
					}
				}
			}

			return false;
		}
	}
}