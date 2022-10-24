using Tekly.Common.LocalFiles;
using UnityEngine;

namespace Tekly.Common.LocalPrefs
{
    public class PrefsContainer
    {
        private const float DURATION_DIRTY_TO_SAVE = 0.25f;
        
        private bool m_dirty;
        private float m_timeMarkedDirty;
        
        private readonly string m_localFilePath;

        private readonly PrefsStore m_store;
        
        public PrefsContainer(string localFilePath)
        {
            m_localFilePath = localFilePath;

            if (LocalFile.Exists(m_localFilePath)) {
                var json = LocalFile.ReadAllText(m_localFilePath);
                m_store = JsonUtility.FromJson<PrefsStore>(json);
            } else {
                m_store = new PrefsStore();
            }
        }
        
        public void Set(string name, float value)
        {
            if (m_store.Set(name, value)) {
                MarkDirty();
            }
        }
        
        public void Set(string name, bool value)
        {
            if (m_store.Set(name, value)) {
                MarkDirty();
            }
        }
        
        public void Set(string name, string value)
        {
            if (m_store.Set(name, value)) {
                MarkDirty();
            }
        }

        public float GetFloat(string name, float value = default)
        {
            return m_store.GetFloat(name, value);
        }
        
        public bool GetBool(string name, bool value = default)
        {
            return m_store.GetBool(name, value);
        }
        
        public string GetString(string name, string value = default)
        {
            return m_store.GetString(name, value);
        }
        
        public bool TryGetFloat(string name, out float value)
        {
            return m_store.TryGetFloat(name, out value);
        }
        
        public bool TryGetBool(string name, out bool value)
        {
            return m_store.TryGetBool(name, out value);
        }
        
        public bool TryGetString(string name, out string value)
        {
            return m_store.TryGetString(name, out value);
        }

        public void Update()
        {
            if (!m_dirty) {
                return;
            }

            if (m_timeMarkedDirty >= DURATION_DIRTY_TO_SAVE) {
                Save();
            }
        }

        public void Flush()
        {
            Save();
        }

        private void Save()
        {
            if (!m_dirty) {
                return;
            }
            
            m_dirty = false;
            var json = JsonUtility.ToJson(m_store);
            LocalFile.WriteAllText(m_localFilePath, json);
        }

        private void MarkDirty()
        {
            if (m_dirty) {
                return;
            }
            
            m_dirty = true;
            m_timeMarkedDirty = Time.realtimeSinceStartup;
        }
    }
}