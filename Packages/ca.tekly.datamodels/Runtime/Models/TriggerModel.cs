using System.Text;
using Tekly.Common.Observables;

namespace Tekly.DataModels.Models
{
	public class TriggerModel : Triggerable<Unit>, IModel
	{
		public void Emit()
		{
			Emit(Unit.Default);
		}
		
		public void Dispose() { }

		public void Tick() { }

		public void ToJson(StringBuilder sb)
		{
			sb.Append("trigger");
		}
	}
}