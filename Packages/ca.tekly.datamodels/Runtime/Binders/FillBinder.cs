using Tekly.Common.Ui.ProgressBars;
using UnityEngine;

namespace Tekly.DataModels.Binders
{
    public class FillBinder : BasicValueBinder<double>
    {
        [SerializeField] private Filled m_filled;
        
        protected override void BindValue(double value)
        {
            m_filled.Fill = (float) value;
        }
    }
}