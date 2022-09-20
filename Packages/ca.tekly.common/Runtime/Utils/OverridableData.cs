using Tekly.Common.LocalFiles;
using UnityEngine;

namespace Tekly.Common.Utils
{
    public class OverridableData : ScriptableObject
    {
        public const string Directory = "overrides";
        
#if DEBUG && !UNITY_EDITOR
        private void OnEnable()
        {
            LoadFromOverrides();
        }
#endif

        private string GetOverridableFileName()
        {
            return Directory + "/" + name + ".json";
        }

        public void LoadFromOverrides()
        {
            var fileName = GetOverridableFileName();
            if (LocalFile.Exists(fileName)) {
                var json = LocalFile.ReadAllText(fileName);
                JsonUtility.FromJsonOverwrite(json, this);
            }
        }

        public static void ReloadAll()
        {
            var overridables = Resources.FindObjectsOfTypeAll<OverridableData>();

            foreach (var overridable in overridables) {
                overridable.LoadFromOverrides();
            }
        }

        public static void WriteAll()
        {
            var overridables = Resources.FindObjectsOfTypeAll<OverridableData>();
            
            foreach (var overridable in overridables) {
                var fileName = overridable.GetOverridableFileName();
                var json = JsonUtility.ToJson(overridable, true);
                LocalFile.WriteAllText(fileName, json);
            }
        }
    }
}