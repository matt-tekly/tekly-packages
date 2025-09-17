using System;
using System.Text;

namespace Tekly.DataModels.Models
{
	public class TimeSpanModel : ValueModel<TimeSpan>
	{
		public override bool IsTruthy => Value.TotalMilliseconds > 0;
		public TimeSpanModel(TimeSpan value) : base(value) {}
		
		public override int CompareTo(IValueModel valueModel)
		{
			var other = valueModel as TimeSpanModel;
			return m_value.CompareTo(other.Value);
		}
        
		public override void ToJson(StringBuilder sb)
		{
			sb.Append($"\"{Value}\"");
		}

		protected override string OnToDisplayString()
		{
			return Value.ToString();
		}
		
		public override void SetOverrideValue(string value)
		{
			OverrideValue = true;
			TimeSpan.TryParse(value, out m_value);
		}
	}
}