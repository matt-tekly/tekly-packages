#if UNITY_EDITOR && WEBSTER_ENABLE_EDIT_MODE
using Tekly.Common.Utils;
using UnityEditor;

namespace Tekly.Webster
{
	public static class WebsterServerEditMode
	{
		[InitializeOnLoadMethod]
		private static void EditorInitialize()
		{
			if (EditorApplication.isPlayingOrWillChangePlaymode) {
				return;
			}
			
			EditorApplication.update += WebsterServer.MainThreadUpdate;
			WebsterServer.CreateServer(true);
	        
			UnityRuntimeEditorUtils.OnEnterPlayMode(OnEditorStartPlaying);
			UnityRuntimeEditorUtils.OnExitPlayMode(OnEditorStopPlaying);
		}

		private static void OnEditorStartPlaying()
		{
			EditorApplication.update -= WebsterServer.MainThreadUpdate;
			WebsterServer.KillServer();
		}

		private static void OnEditorStopPlaying()
		{
			EditorApplication.update += WebsterServer.MainThreadUpdate;
			WebsterServer.CreateServer(true);
		}
	}
}
#endif