using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Tekly.DevBoard
{
	/// <summary>
	/// View state that should survive a widget being destroyed and rebuilt, like whether a foldout is open, which
	/// tab is selected, or what a list is searched for. Kept in memory for the play session, and values set as
	/// persistent (bools, ints, floats and strings) are saved in PlayerPrefs too, so they come back next session.
	///
	/// Each front end makes its own keys: nodes' views use ViewContext.StateKey. Keys are built from node paths,
	/// which anonymous layout isn't part of, so saved state survives a page's layout being rearranged.
	/// </summary>
	public class DevBoardState
	{
		private const string PREFS_KEY = "Tekly.DevBoard.State";

		private readonly Dictionary<string, object> m_values = new();

		// The keys whose values are saved between sessions
		private readonly HashSet<string> m_persistent = new();

		public DevBoardState()
		{
			Load();
		}

		public bool TryGet<T>(string key, out T value)
		{
			if (key != null && m_values.TryGetValue(key, out var stored) && stored is T typed) {
				value = typed;
				return true;
			}

			value = default;
			return false;
		}

		/// <summary>
		/// Stores value under key. With persistent, it's also saved between sessions: bools, ints, floats and
		/// strings only, anything else is kept for the session.
		/// </summary>
		public void Set<T>(string key, T value, bool persistent = false)
		{
			if (key == null) {
				return;
			}

			m_values[key] = value;

			if (persistent && IsSavable(value)) {
				m_persistent.Add(key);
				Save();
			}
		}

		public void Remove(string key)
		{
			if (key == null || !m_values.Remove(key)) {
				return;
			}

			if (m_persistent.Remove(key)) {
				Save();
			}
		}

		/// <summary>
		/// Forgets every saved value, in memory and in PlayerPrefs. Values kept for the session only stay.
		/// </summary>
		public void ClearSaved()
		{
			foreach (var key in m_persistent) {
				m_values.Remove(key);
			}

			m_persistent.Clear();
			PlayerPrefs.DeleteKey(PREFS_KEY);
			PlayerPrefs.Save();
		}

		private static bool IsSavable(object value)
		{
			return value is bool or int or float or string;
		}

		private void Load()
		{
			var json = PlayerPrefs.GetString(PREFS_KEY, null);

			if (string.IsNullOrEmpty(json)) {
				return;
			}

			Saved saved;

			try {
				saved = JsonUtility.FromJson<Saved>(json);
			} catch (Exception exception) {
				Debug.LogWarning($"[DevBoard] Couldn't read the saved view state, starting fresh: {exception.Message}");
				return;
			}

			if (saved?.Entries == null) {
				return;
			}

			foreach (var entry in saved.Entries) {
				if (string.IsNullOrEmpty(entry.Key) || !TryParse(entry, out var value)) {
					continue;
				}

				m_values[entry.Key] = value;
				m_persistent.Add(entry.Key);
			}
		}

		private void Save()
		{
			var saved = new Saved();

			foreach (var key in m_persistent) {
				if (m_values.TryGetValue(key, out var value)) {
					saved.Entries.Add(ToEntry(key, value));
				}
			}

			PlayerPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(saved));
			PlayerPrefs.Save();
		}

		private static Entry ToEntry(string key, object value)
		{
			var entry = new Entry { Key = key };

			switch (value) {
				case bool flag:
					entry.Kind = Kind.Bool;
					entry.Value = flag ? "1" : "0";
					break;
				case int number:
					entry.Kind = Kind.Int;
					entry.Value = number.ToString(CultureInfo.InvariantCulture);
					break;
				case float number:
					entry.Kind = Kind.Float;
					entry.Value = number.ToString("R", CultureInfo.InvariantCulture);
					break;
				default:
					entry.Kind = Kind.String;
					entry.Value = (string) value;
					break;
			}

			return entry;
		}

		private static bool TryParse(Entry entry, out object value)
		{
			switch (entry.Kind) {
				case Kind.Bool:
					value = entry.Value == "1";
					return true;
				case Kind.Int when int.TryParse(entry.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number):
					value = number;
					return true;
				case Kind.Float when float.TryParse(entry.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number):
					value = number;
					return true;
				case Kind.String:
					value = entry.Value ?? string.Empty;
					return true;
				default:
					value = null;
					return false;
			}
		}

		private enum Kind
		{
			Bool,
			Int,
			Float,
			String
		}

		[Serializable]
		private class Entry
		{
			public string Key;
			public Kind Kind;
			public string Value;
		}

		[Serializable]
		private class Saved
		{
			public List<Entry> Entries = new();
		}
	}
}
