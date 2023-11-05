using UnityEditor;
using UnityEditor.SceneManagement;

namespace TeklySample.Editor
{
	/// <summary>
	/// When pressing F1 the editor will play and load the SCENE_PATH scene
	/// </summary>
	[InitializeOnLoad]
	public static class PlayModeMenu
	{
		private const string SCENE_PATH = "Assets/Scenes/launch_scn.unity";

		static PlayModeMenu()
		{
			EditorApplication.playModeStateChanged += ClearPlayModeStartScene;
		}

		private static void ClearPlayModeStartScene(PlayModeStateChange state)
		{
			// If we don't clear the playModeStartScene here every time you press play you will play
			// the SCENE_PATH scene instead of the active scene
			if (state == PlayModeStateChange.ExitingPlayMode) {
				EditorSceneManager.playModeStartScene = null;	
			}
		}
		
		[MenuItem("Game/Launch _F1")]
		private static void PlayGame()
		{
			if (EditorApplication.isPlaying) {
				EditorApplication.ExitPlaymode();
			} else {
				var targetScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(SCENE_PATH);
				EditorSceneManager.playModeStartScene = targetScene;
				EditorApplication.EnterPlaymode();	
			}
		}
	}
}