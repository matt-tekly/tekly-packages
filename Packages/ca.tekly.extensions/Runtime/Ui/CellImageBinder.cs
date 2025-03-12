using System;
using System.Text;
using Tekly.DataModels.Binders;
using Tekly.DataModels.Models;
using Tekly.TwoD.Cells;
using UnityEngine;

namespace Tekly.Extensions.Ui
{
	public class CellSpriteValueModel : ValueModel<CellSprite>
	{
		public override void ToJson(StringBuilder sb)
		{
			sb.Append(Value != null ? Value.name : "[null]");
		}

		protected override string OnToDisplayString()
		{
			return Value != null ? Value.name : "[null]";
		}

		public override int CompareTo(IValueModel valueModel)
		{
			throw new Exception($"Trying to compare [{GetType().Name}] to [{valueModel.GetType().Name}]");
		}
		
		public override void SetOverrideValue(string value)
		{
			Debug.LogWarning("Can't override CellSpriteValueModel");
		}
	}
	
	public class CellImageBinder : Binder
	{
		[SerializeField] private ModelRef m_key;
		[SerializeField] private CellImage m_cell;

		private IDisposable m_disposable;
        
		public override void Bind(BinderContainer container)
		{
			if (container.TryGet(m_key.Path, out CellSpriteValueModel cellSpriteValueModel)) {
				m_disposable?.Dispose();
				m_disposable = cellSpriteValueModel.Subscribe(BindSprite);
			}
		}

		private void BindSprite(CellSprite value)
		{
			m_cell.Sprite = value;
		}
        
		protected override void OnDestroy()
		{
			base.OnDestroy();
			m_disposable?.Dispose();
		}
        
#if UNITY_EDITOR
		private void OnValidate()
		{
			if (m_cell == null) {
				m_cell = GetComponent<CellImage>();
			}
		}
#endif
	}
}