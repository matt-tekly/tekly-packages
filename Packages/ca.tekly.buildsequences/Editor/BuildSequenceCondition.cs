using UnityEditor;
using UnityEngine;

namespace Tekly.BuildSequences.Editor
{
    [CreateAssetMenu(menuName = "Tekly/Build Sequences/Condition")]
    public class BuildSequenceCondition : ScriptableObject
    {
        public bool iOS = true;
        public bool Android = true;
        public bool Standalone = true;
        
        public virtual bool IsActive(BuildSequenceContext context)
        {
            if (iOS && context.BuildTarget == BuildTarget.iOS) {
                return true;
            }
            
            if (Android && context.BuildTarget == BuildTarget.Android) {
                return true;
            }
            
            if (Standalone && IsStandaloneTarget(context.BuildTarget)) {
                return true;
            }
            
            return false;
        }

        private static bool IsStandaloneTarget(BuildTarget buildTarget)
        {
            return buildTarget == BuildTarget.StandaloneWindows 
            || buildTarget == BuildTarget.StandaloneLinux64 
            || buildTarget == BuildTarget.StandaloneOSX;
        }
    }
}