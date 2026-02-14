using UnityEngine;

namespace Tekly.Common.Utils
{
	/// <summary>
	/// Has a unique ID based on the GUID of the asset
	/// </summary>
	public class UniqueIdScriptableObject : ScriptableObject
	{
		[SerializeField, HideInInspector] private ulong m_uniqueId;

		public ulong UniqueId => m_uniqueId;

#if UNITY_EDITOR
		protected virtual void OnValidate()
		{
			TryAssignId();
		}

		private void TryAssignId()
		{
			if (m_uniqueId != 0UL) {
				return;
			}

			var guid = UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(this));
			if (string.IsNullOrEmpty(guid)) {
				return;
			}

			m_uniqueId = HashGuidToUlong(guid);
			UnityEditor.EditorUtility.SetDirty(this);
		}

		private static ulong HashGuidToUlong(string guid)
		{
			// FNV-1a Hash
			const ulong offset = 14695981039346656037UL;
			const ulong prime = 1099511628211UL;

			var hash = offset;
			for (var i = 0; i < guid.Length; i++) {
				hash ^= guid[i];
				hash *= prime;
			}

			// 0 is uninitialized so we'll just bump it to 1
			if (hash == 0UL) {
				hash = 1UL;
			}

			return hash;
		}
#endif
	}
}