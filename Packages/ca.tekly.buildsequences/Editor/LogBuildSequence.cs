using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tekly.BuildSequences.Editor
{
    [CreateAssetMenu(menuName = "Tekly/Build Sequences/Log")]
    public class LogBuildSequence : BuildSequence
    {
        public string Message;
        
        public override void PreBuild(BuildSequenceContext context)
        {
            Debug.Log("[PreBuild] " + Message);
        }

        public override void PostBuild(BuildSequenceContext context, BuildReport report)
        {
            Debug.Log("[PostBuild] " + Message);
        }
    }
}