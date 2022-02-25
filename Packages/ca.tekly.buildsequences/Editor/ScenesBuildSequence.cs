using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tekly.BuildSequence.Editor
{
    [CreateAssetMenu(menuName = "Tekly/Build Sequences/Scenes")]
    public class ScenesBuildSequence : BuildSequence
    {
        public SceneAsset[] Scenes;
        
        public override void PreBuild(BuildSequenceContext context)
        {
            foreach (var scene in Scenes) {
                var path = AssetDatabase.GetAssetPath(scene);
                Debug.Log(path);
            }
        }

        public override void PostBuild(BuildSequenceContext context, BuildReport report)
        {
            
        }
    }
}