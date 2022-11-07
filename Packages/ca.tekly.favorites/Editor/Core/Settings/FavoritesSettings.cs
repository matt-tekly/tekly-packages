using System.IO;
using UnityEngine;

namespace Tekly.Favorites.Editor.Core.Settings
{
    public class FavoritesSettings : ScriptableObject
    {
        private static string FavoritesPostPrefsKey = "/tekly/favorites/data";
        private const string SettingsPrefsKey = "tekly/favorites/settings";
        public static string FavoritesPrefsKey => Directory.GetCurrentDirectory() + FavoritesPostPrefsKey;
    }
}