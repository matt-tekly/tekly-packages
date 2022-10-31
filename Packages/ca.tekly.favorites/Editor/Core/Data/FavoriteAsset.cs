using System;
using UnityEditor;
using UnityEngine;

namespace Tekly.Favorites
{
    [Serializable]
    public class FavoriteAsset : ISerializationCallbackReceiver
    {
        public string AssetPath;
        
        private UnityEngine.Object m_asset;
        public UnityEngine.Object Asset {
            get {
                if (m_asset == null && !string.IsNullOrEmpty(AssetPath)) {
                    m_asset = AssetDatabase.LoadMainAssetAtPath(AssetPath);
                }

                return m_asset;
            }
            set => m_asset = value;
        }
        
        public void OnBeforeSerialize()
        {
            if (Asset != null) {
                AssetPath = AssetDatabase.GetAssetPath(Asset);
            }
        }

        public void OnAfterDeserialize()
        {
           
        }
    }
}