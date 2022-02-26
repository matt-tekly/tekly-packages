using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tekly.BuildSequences.Editor
{
    public abstract class BuildSequence : ScriptableObject
    {
        public BuildSequenceCondition Condition;

        public abstract void PreBuild(BuildSequenceContext context);

        public abstract void PostBuild(BuildSequenceContext context, BuildReport report);
    }
}