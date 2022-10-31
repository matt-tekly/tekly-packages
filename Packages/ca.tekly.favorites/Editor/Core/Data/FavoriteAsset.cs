using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tekly.Favorites
{
    [Serializable]
    public class FavoriteAsset : ISerializationCallbackReceiver
    {
        public GlobalObjectId Id;

        [SerializeField] private string m_id;
        [SerializeField] private string m_lastName;
        [SerializeField] private string m_lastScene;
        [SerializeField] private Texture m_icon;

        private UnityEngine.Object m_asset;

        private const int NULL_ID = 0;
        private const int SCENE_ID = 2;

        public UnityEngine.Object Asset {
            get {
                if (m_asset == null) {
                    if (Id.Equals(default)) {
                        GlobalObjectId.TryParse(m_id, out Id);
                    }

                    m_asset = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(Id);
                }

                return m_asset;
            }
            set {
                m_asset = value;
                OnBeforeSerialize();
            }
        }

        public string DisplayName {
            get {
                return m_lastName;
            }
        }

        public Texture Icon => m_icon;

        public void OnBeforeSerialize()
        {
            if (Asset != null) {
                Id = GlobalObjectId.GetGlobalObjectIdSlow(Asset);
                m_id = Id.ToString();

                if (Id.identifierType == SCENE_ID) {
                    m_lastScene = Asset switch {
                        GameObject go => go.scene.name,
                        Component co => co.gameObject.scene.name,
                        _ => m_lastScene
                    };

                    m_icon = EditorGUIUtility.ObjectContent(m_asset, m_asset.GetType()).image;
                    if (m_icon == null) {
                        m_icon = EditorGUIUtility.GetIconForObject(m_asset);    
                    }
                    
                } else {
                    var path = AssetDatabase.GetAssetPath(Asset);
                    m_icon = AssetDatabase.GetCachedIcon(path);
                }

                m_lastName = Asset.name;
            } else {
                Id = default;
            }
        }

        public void OnAfterDeserialize()
        {
            if (!string.IsNullOrEmpty(m_id)) {
                GlobalObjectId.TryParse(m_id, out Id);
            }
        }
    }
}