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
		public string FullPath;
		public GUIContent FullPathGuid = new GUIContent();

		public int Index;
		public int Depth;
		public bool IsObject;
		public bool Visible;
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
			objectEntry.FullPathGuid.text = string.Empty;
			objectEntry.Visible = false;
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