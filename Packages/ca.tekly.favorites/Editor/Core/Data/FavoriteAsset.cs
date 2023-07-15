using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tekly.Favorites
{
	[Serializable]
	public class FavoriteAsset : ISerializationCallbackReceiver
	{
		public GlobalObjectId Id;

		[SerializeField] private string m_id;
		[SerializeField] private string m_lastName;
		[SerializeField] private Texture m_icon;
		[SerializeField] private string m_path;
		[SerializeField] private bool m_isInPrefabStage;

		private Object m_asset;

		private const int SCENE_ID = 2;

		public Object Asset {
			get {
				if (m_asset == null) {
					if (Id.Equals(default)) {
						GlobalObjectId.TryParse(m_id, out Id);
					}

					m_asset = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(Id);

					if (m_asset == null && m_isInPrefabStage) {
						var stage = PrefabStageUtility.GetCurrentPrefabStage();
						if (stage != null) {
							var foundTransform = stage.prefabContentsRoot.transform.Find(m_path);
							if (foundTransform != null) {
								m_asset = foundTransform.gameObject;
							}
						}
					}
				}

				return m_asset;
			}
			set {
				m_asset = value;
				SaveData();
			}
		}

		public string DisplayName => Asset != null ? Asset.name : m_lastName;

		public Texture Icon => m_icon;

		public void SaveData()
		{
			if (Asset != null) {
				Id = GlobalObjectId.GetGlobalObjectIdSlow(Asset);

				m_id = Id.ToString();

				if (Id.identifierType == SCENE_ID) {
					var targetGo = Asset switch {
						GameObject go => go.gameObject,
						Component co => co.gameObject,
						_ => null
					};

					if (targetGo != null) {
						var stage = PrefabStageUtility.GetCurrentPrefabStage();
						if (stage != null) {
							m_isInPrefabStage = true;
							m_path = AnimationUtility.CalculateTransformPath(targetGo.transform, stage.prefabContentsRoot.transform);
						}
					}

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

		public void TryToUpdateIcon()
		{
			if (m_asset == null) {
				return;
			}
			
			if (Id.identifierType == SCENE_ID) {
				
				m_icon = EditorGUIUtility.ObjectContent(m_asset, m_asset.GetType()).image;

				if (m_icon == null) {
					m_icon = EditorGUIUtility.GetIconForObject(m_asset);
				}
			} else {
				var path = AssetDatabase.GetAssetPath(Asset);
				m_icon = AssetDatabase.GetCachedIcon(path);
			}
		}

		public void OnBeforeSerialize() { }

		public void OnAfterDeserialize()
		{
			if (!string.IsNullOrEmpty(m_id)) {
				GlobalObjectId.TryParse(m_id, out Id);
			}
		}
	}
}