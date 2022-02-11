//=============================================================================	
// Copyright Matthew King. All rights reserved.
//=============================================================================

#if UNITY_EDITOR && WEBSTER_ENABLE_EDITOR
#define WEBSTER_ENABLE
#endif

using UnityEngine;

namespace Tekly.Webster
{
	internal class WebsterBehaviour : MonoBehaviour
	{
#if WEBSTER_ENABLE
		public static WebsterBehaviour Instance;

		internal static void CreateInstance()
		{
			if (Instance != null) {
				return;
			}

			var go = new GameObject("Webster");
			go.AddComponent<WebsterBehaviour>();

			DontDestroyOnLoad(go);
		}

		private void Awake()
		{
			if (Instance != null) {
				Destroy(gameObject);
			} else {
				Instance = this;
			}
		}

		private void Update()
		{
			WebsterServer.MainThreadUpdate();
		}

		private void OnDestroy()
		{
			WebsterServer.Stop();
			Instance = null;
		}
#endif
	}
}