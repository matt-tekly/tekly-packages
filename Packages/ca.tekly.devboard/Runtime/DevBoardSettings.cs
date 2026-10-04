using System;
using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// DevBoard's own options, saved as JSON in PlayerPrefs. Shown on the Options page.
	/// </summary>
	[Serializable]
	public class DevBoardSettings
	{
		public const float MIN_SCALE = 0.5f;
		public const float MAX_SCALE = 4f;
		public const float SCALE_STEP = 0.25f;

		private const string PREFS_KEY = "Tekly.DevBoard.Settings";

		/// <summary>
		/// Raised after any setting changes.
		/// </summary>
		public event Action Changed;

		[SerializeField] private float m_scale = 1f;
		[SerializeField] private bool m_keepPhysicalSizeInEditor = true;

		/// <summary>
		/// The scale factor of the DevBoard canvas. 1 is the size the widgets were designed at.
		/// </summary>
		public float Scale {
			get => m_scale;
			set => Set(ref m_scale, Mathf.Clamp(value, MIN_SCALE, MAX_SCALE));
		}

		/// <summary>
		/// In the editor, scale DevBoard against the Game view's zoom so it stays the same size on the monitor.
		/// Has no effect in builds.
		/// </summary>
		public bool KeepPhysicalSizeInEditor {
			get => m_keepPhysicalSizeInEditor;
			set => Set(ref m_keepPhysicalSizeInEditor, value);
		}

		public static DevBoardSettings Load()
		{
			var json = PlayerPrefs.GetString(PREFS_KEY, null);

			if (!string.IsNullOrEmpty(json)) {
				try {
					var settings = JsonUtility.FromJson<DevBoardSettings>(json);

					if (settings != null) {
						settings.m_scale = Mathf.Clamp(settings.m_scale, MIN_SCALE, MAX_SCALE);
						return settings;
					}
				} catch (Exception exception) {
					Debug.LogWarning($"[DevBoard] Couldn't read the saved settings, using defaults: {exception.Message}");
				}
			}

			return new DevBoardSettings();
		}

		public void ResetToDefaults()
		{
			m_scale = 1f;
			m_keepPhysicalSizeInEditor = true;
			OnChanged();
		}

		private void Set<T>(ref T field, T value)
		{
			if (Equals(field, value)) {
				return;
			}

			field = value;
			OnChanged();
		}

		private void OnChanged()
		{
			PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(this));
			PlayerPrefs.Save();

			Changed?.Invoke();
		}
	}
}
