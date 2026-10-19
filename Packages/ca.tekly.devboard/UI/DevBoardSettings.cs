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

		public const float MIN_PANEL_HEIGHT = 0.3f;
		public const float MAX_PANEL_HEIGHT = 1f;
		public const float DEFAULT_PANEL_HEIGHT = 0.85f;

		private const string PREFS_KEY = "Tekly.DevBoard.Settings";

		/// <summary>
		/// Raised after any setting changes.
		/// </summary>
		public event Action Changed;

		[SerializeField] private float m_scale = 1f;
		[SerializeField] private bool m_keepPhysicalSizeInEditor = true;
		[SerializeField] private bool m_animations = true;
		[SerializeField] private float m_maxPanelHeight = DEFAULT_PANEL_HEIGHT;

		// Named for the off state so settings saved before it existed load it as on
		[SerializeField] private bool m_hideSuggestionCount;

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

		/// <summary>
		/// Whether panels ease to a new size when their page changes, rather than jumping.
		/// </summary>
		public bool Animations {
			get => m_animations;
			set => Set(ref m_animations, value);
		}

		/// <summary>
		/// The most of the screen's height panels take before their pages scroll, from 0.3 to 1. Panels docked in
		/// the same corner share it.
		/// </summary>
		public float MaxPanelHeight {
			get => m_maxPanelHeight;
			set => Set(ref m_maxPanelHeight, Mathf.Clamp(value, MIN_PANEL_HEIGHT, MAX_PANEL_HEIGHT));
		}

		/// <summary>
		/// Whether the console's suggestion box ends with a line saying how many suggestions didn't fit, or which
		/// one is picked, when there are more than it can show.
		/// </summary>
		public bool ShowSuggestionCount {
			get => !m_hideSuggestionCount;
			set => Set(ref m_hideSuggestionCount, !value);
		}

		public static DevBoardSettings Load()
		{
			var json = PlayerPrefs.GetString(PREFS_KEY, null);

			if (!string.IsNullOrEmpty(json)) {
				try {
					var settings = JsonUtility.FromJson<DevBoardSettings>(json);

					if (settings != null) {
						settings.m_scale = Mathf.Clamp(settings.m_scale, MIN_SCALE, MAX_SCALE);

						// Settings saved before this option existed load it as 0
						settings.m_maxPanelHeight = settings.m_maxPanelHeight <= 0f
							? DEFAULT_PANEL_HEIGHT
							: Mathf.Clamp(settings.m_maxPanelHeight, MIN_PANEL_HEIGHT, MAX_PANEL_HEIGHT);
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
			m_animations = true;
			m_maxPanelHeight = DEFAULT_PANEL_HEIGHT;
			m_hideSuggestionCount = false;
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
