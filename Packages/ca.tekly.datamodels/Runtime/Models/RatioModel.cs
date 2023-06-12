namespace Tekly.DataModels.Models
{
	public class RatioModel : ObjectModel
	{
		public float Current {
			set {
				m_current.Value = value;
				m_ratio.Value = m_current.Value / m_max.Value;
				
				m_full.Value = m_ratio.Value >= 1.0f;
			}
		}

		public float Max {
			set {
				m_max.Value = value;
				m_ratio.Value = m_current.Value / m_max.Value;

				m_full.Value = m_ratio.Value >= 1.0f;
			}
		}

		public bool IsFull => m_full.Value; 

		private readonly NumberValueModel m_current = new NumberValueModel(1);
		private readonly NumberValueModel m_max = new NumberValueModel(1);
		
		private readonly NumberValueModel m_ratio = new NumberValueModel(1);
		private readonly BoolValueModel m_full = new BoolValueModel(false);

		public RatioModel()
		{
			Add("current", m_current);
			Add("max", m_max);
			Add("ratio", m_ratio);
			Add("full", m_full);
		}
	}
}