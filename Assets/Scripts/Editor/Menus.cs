using Tekly.Common.Git;
using UnityEditor;
using UnityEngine;

namespace TeklySample.Editor
{
    public static class Menus
    {
        [MenuItem("Tools/Tekly/Git/Branch Info")]
        public static void FindBranchInfo()
        {
            if (GitUtility.TryFindBranchInfo(out var branchInfo)) {
                Debug.Log($"Branch: [{branchInfo.Branch}] Hash: [{branchInfo.CommitHash}]");
            } else {
                Debug.Log("Failed to find git branch info");
            }
        }
    }
}