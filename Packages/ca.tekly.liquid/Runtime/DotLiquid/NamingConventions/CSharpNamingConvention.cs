using System;

namespace DotLiquid.NamingConventions
{
	public class CSharpNamingConvention : INamingConvention
	{
		public StringComparer StringComparer => StringComparer.OrdinalIgnoreCase;

		public string GetMemberName(string name)
		{
			return name.ToLowerInvariant();
		}

		public bool OperatorEquals(string testedOperator, string referenceOperator)
		{
			return string.Equals(testedOperator, referenceOperator, StringComparison.OrdinalIgnoreCase);
		}
	}
}