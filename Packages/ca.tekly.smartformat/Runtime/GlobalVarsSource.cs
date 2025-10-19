using System.Collections.Generic;
using SmartFormat.Core.Extensions;

namespace Tekly.SmartFormat
{
	public class GlobalVarsSource : ISource
	{
		private readonly IDictionary<string, object> m_vars;
		public GlobalVarsSource(IDictionary<string, object> vars) => m_vars = vars;

		public bool TryEvaluateSelector(ISelectorInfo selectorInfo)
		{
			if (!m_vars.TryGetValue(selectorInfo.SelectorText, out var value)) {
				return false;
			}

			selectorInfo.Result = value;
			return true;
		}
	}
}