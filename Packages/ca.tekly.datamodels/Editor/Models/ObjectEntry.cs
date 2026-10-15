using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.DataModels.Models
{
	[Serializable]
	public class ObjectEntry
	{
		public GUIContent Id = new GUIContent() ;
		public GUIContent Value = new GUIContent();
		public GUIContent Type = new GUIContent();
		public string FullPath;
		public GUIContent FullPathGui = new GUIContent();

		/// <summary>
		/// The display string Value was made from. Kept to tell if the model's display string changed without
		/// building a new string.
		/// </summary>
		public string RawValue;

		public int Index;
		public int Depth;
		public bool IsObject;
		public bool IsCommonType;
		public bool IsOverridden;
		public bool Visible;

		/// <summary>
		/// The model this entry shows. Only valid in Play mode, it's not kept between domain reloads.
		/// </summary>
		[NonSerialized] public IModel Model;
	}

	public static class ObjectEntryPool
	{
		private static Queue<ObjectEntry> s_pool = new Queue<ObjectEntry>();

		public static void Return(ObjectEntry objectEntry)
		{
			s_pool.Enqueue(objectEntry);
			objectEntry.Id.text = string.Empty;
			objectEntry.Id.tooltip = string.Empty;
			objectEntry.Value.text = string.Empty;
			objectEntry.Type.text = string.Empty;
			objectEntry.FullPathGui.text = string.Empty;
			objectEntry.RawValue = null;
			objectEntry.IsOverridden = false;
			objectEntry.IsCommonType = false;
			objectEntry.Visible = false;
			objectEntry.Model = null;
		}

		public static ObjectEntry Get()
		{
			if (s_pool.Count == 0) {
				return new ObjectEntry();
			}

			return s_pool.Dequeue();
		}
	}
}
