using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites.Editor.Core.Settings
{
    public class FavoritesSettings : ScriptableObject
    {
        private static string FavoritesPostPrefsKey = "/tekly/favorites/data";
        private const string SettingsPrefsKey = "tekly/favorites/settings";

        private static FavoritesSettings s_instance = null;

        public static FavoritesSettings Instance {
            get {
                if (s_instance == null) {
                    var tmp = Resources.FindObjectsOfTypeAll<FavoritesSettings>();

                    if (tmp.Length > 0) {
                        s_instance = tmp[0];
                    } else {
                        s_instance = CreateInstance<FavoritesSettings>();
                        var json = EditorPrefs.GetString(SettingsPrefsKey);
                        EditorJsonUtility.FromJsonOverwrite(json, s_instance);
                    }

                    s_instance.hideFlags = HideFlags.DontSave;
                }

                return s_instance;
            }
        }

        private bool m_useFullProjectPathForKey = true;
        public bool UseFullProjectPathForKey {
            get => m_useFullProjectPathForKey;
            set {
                if (value != m_useFullProjectPathForKey) {
                    Undo.RecordObject(this, "Modify Favorites Settings");
                    m_useFullProjectPathForKey = value;
                    Save();
                }
            }
        }

        public string FavoritesPrefsKey {
            get {
                if (UseFullProjectPathForKey) {
                    return Directory.GetCurrentDirectory() + FavoritesPostPrefsKey;
                }

                return GetProjectName() + FavoritesPostPrefsKey;
            }
        }

        public string GetProjectName()
        {
            string[] s = Application.dataPath.Split('/');
            return s[s.Length - 2];
        }

        public void Save()
        {
            var json = EditorJsonUtility.ToJson(this);
            EditorPrefs.SetString(FavoritesPrefsKey, json);
        }

    }
}