using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Tekly.Common.Observables;
using UnityEngine;

namespace Tekly.DataModels.Models
{
	public enum ReferenceType
	{
		Owner,
		Shared
	}

	[DebuggerDisplay("Key=[{Key}] ReferenceType=[{ReferenceType}]")]
	public struct ModelReference
	{
		public readonly IModel Model;
		public readonly ReferenceType ReferenceType;
		public readonly string Key;
		public readonly int Hash;
		public readonly string TypeName;

		public ModelReference(IModel model, ReferenceType referenceType, string key, int hash)
		{
			Model = model;
			ReferenceType = referenceType;
			Key = key;
			Hash = hash;
			
#if UNITY_EDITOR
			TypeName = model.GetType().Name;
#else
			TypeName = null;
#endif
		}
	}

	public class ObjectModel : ModelBase
	{
		public IReadOnlyList<ModelReference> Models => m_models;

		public ITriggerable<ObjectModel> Modified => m_modified;

		private readonly Triggerable<ObjectModel> m_modified = new Triggerable<ObjectModel>();

		private readonly List<ModelReference> m_models = new List<ModelReference>(8);

		public bool DisableModifiedTrigger { get; set; }

		public void EmitModified()
		{
			if (!DisableModifiedTrigger) {
				m_modified.Emit(this);
			}
		}
		
		public void Add(string name, IModel model, ReferenceType referenceType = ReferenceType.Owner)
		{
			m_models.Add(new ModelReference(model, referenceType, name, name.GetHashCode()));
			EmitModified();
		}
		
		public T Add<T>(string name, T model, ReferenceType referenceType = ReferenceType.Owner) where T : IModel
		{
			m_models.Add(new ModelReference(model, referenceType, name, name.GetHashCode()));
			EmitModified();
			
			return model;
		}
		
		public StringValueModel Add(string name, string value, bool needsLocalization = false)
		{
			var model = new StringValueModel(value, needsLocalization);
			Add(name, model);

			return model;
		}
		
		public BoolValueModel Add(string name, bool value)
		{
			var model = new BoolValueModel(value);
			Add(name, model);

			return model;	
		}
		
		public NumberValueModel Add(string name, double value)
		{
			var model = new NumberValueModel(value);
			Add(name, model);

			return model;		
		}
		
		public NumberValueModel Add(string name, float value)
		{
			var model = new NumberValueModel(value);
			Add(name, model);

			return model;		
		}
		
		public NumberValueModel Add(string name, int value)
		{
			var model = new NumberValueModel(value);
			Add(name, model);

			return model;		
		}

		public SpriteValueModel Add(string name, Sprite value)
		{
			var model = new SpriteValueModel(value);
			Add(name, model);

			return model;		
		}

		public ButtonModel AddButton(string name)
		{
			var model = new ButtonModel();
			Add(name, model);

			return model;
		}
		
		public TriggerModel AddTrigger(string name)
		{
			var model = new TriggerModel();
			Add(name, model);

			return model;
		}

		public ObjectModel AddObject(string name)
		{
			var model = new ObjectModel();
			Add(name, model);

			return model;
		}

		public void Add(int name, IModel model)
		{
			Add(name.ToString(), model);
		}

		public void BulkModify(Action action)
		{
			DisableModifiedTrigger = true;
			action();
			DisableModifiedTrigger = false;
			EmitModified();
		}

		public void RemoveModel(string name)
		{
			for (var index = 0; index < m_models.Count; index++) {
				var modelReference = m_models[index];
				if (modelReference.Key == name) {
					m_models.RemoveAt(index);
				}
			}

			EmitModified();
		}

		public bool TryGetModel<T>(ModelKey modelKey, int index, out T targetModel) where T : class, IModel
		{
			var found = TryGetModel(modelKey, index, out var model);
			targetModel = model as T;

			return found;
		}

		public bool TryGetModel(ModelKey modelKey, int index, out IModel model)
		{
			if (modelKey.Keys.Length == 0) {
				model = this;
				return true;
			}

			var currentKey = modelKey.Keys[index];

			if (!TryGetModel(currentKey, out model)) {
				return false;
			}

			if (index == modelKey.Keys.Length - 1) {
				return true;
			}

			if (model is ObjectModel objectModel) {
				return objectModel.TryGetModel(modelKey, index + 1, out model);
			}

			return false;
		}

		public bool TryGetModel<T>(string modelKey, out T targetModel) where T : class, IModel
		{
			var found = TryGetModel(modelKey, out var model);
			targetModel = model as T;

			return found;
		}

		public virtual bool TryGetModel(string modelKey, out IModel model)
		{
			for (var index = 0; index < m_models.Count; index++) {
				var modelReference = m_models[index];
				if (modelReference.Key == modelKey) {
					model = modelReference.Model;
					return true;
				}
			}

			model = null;
			return false;
		}

		public void Clear()
		{
			for (var index = 0; index < m_models.Count; index++) {
				var modelReference = m_models[index];
				if (modelReference.ReferenceType == ReferenceType.Owner) {
					modelReference.Model.Dispose();
				}
			}
			
			m_models.Clear();
			EmitModified();
		}
		
		protected override void OnDispose()
		{
			base.OnDispose();
			Clear();
		}

		public override void ToJson(StringBuilder stringBuilder)
		{
			stringBuilder.Append("{");

			for (var index = 0; index < m_models.Count; index++) {
				var modelReference = m_models[index];

				stringBuilder.Append($"\"{modelReference.Key}\":");
				modelReference.Model.ToJson(stringBuilder);

				if (index < m_models.Count - 1) {
					stringBuilder.Append(",");
				}
			}

			stringBuilder.Append("}");
		}
	}
}