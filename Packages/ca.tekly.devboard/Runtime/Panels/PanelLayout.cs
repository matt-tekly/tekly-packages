using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tekly.DevBoard.Panels
{
	/// <summary>
	/// What's saved about a panel so it comes back the same next session.
	/// </summary>
	[Serializable]
	internal class PanelRecord
	{
		public string Id;
		public DockSlot Dock;
		public string Path;
		public bool Overlay;
		public bool Collapsed;
		public bool ShowWhenHidden;
	}

	/// <summary>
	/// The saved panel layout, as JSON in PlayerPrefs.
	/// </summary>
	[Serializable]
	internal class PanelLayout
	{
		private const string PREFS_KEY = "Tekly.DevBoard.PanelLayout";

		public List<PanelRecord> Panels = new();

		public static PanelLayout Load()
		{
			var json = PlayerPrefs.GetString(PREFS_KEY, null);

			if (string.IsNullOrEmpty(json)) {
				return new PanelLayout();
			}

			try {
				var layout = JsonUtility.FromJson<PanelLayout>(json) ?? new PanelLayout();
				layout.Panels ??= new List<PanelRecord>();
				layout.Panels.RemoveAll(record => record == null || string.IsNullOrEmpty(record.Id));

				foreach (var record in layout.Panels) {
					if (!Enum.IsDefined(typeof(DockSlot), record.Dock)) {
						record.Dock = DockSlot.TopLeft;
					}
				}

				return layout;
			} catch (Exception exception) {
				Debug.LogWarning($"[DevBoard] Couldn't read the saved panel layout, starting fresh: {exception.Message}");
				return new PanelLayout();
			}
		}

		public void Save()
		{
			PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(this));
			PlayerPrefs.Save();
		}
	}
}
