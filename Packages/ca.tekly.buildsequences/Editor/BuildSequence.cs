using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Tekly.BuildSequence.Editor
{
    public abstract class BuildSequence : ScriptableObject
    {
        public BuildSequenceCondition Condition;

        public abstract void PreBuild(BuildSequenceContext context);

        public abstract void PostBuild(BuildSequenceContext context, BuildReport report);
    }
}