using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tekly.BuildSequences.Editor
{
    [CreateAssetMenu(menuName = "Tekly/Build Sequences/Paths")]
    public class BuildPathSequence : BuildSequence
    {
        public string FileName;
        public string Path;
        
        public override void PreBuild(BuildSequenceContext context)
        {
            context.AppFileName = FileName;
            context.LocationPath = Path;
        }

        public override void PostBuild(BuildSequenceContext context, BuildReport report)
        {
            
        }
    }
}