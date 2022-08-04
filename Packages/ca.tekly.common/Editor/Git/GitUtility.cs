using System;
using System.IO;
using UnityEngine.Assertions;

namespace Tekly.Common.Git
{
    [Serializable]
    public class GitBranchInfo
    {
        public string Branch;
        public string CommitHash;
    }
    
    public static class GitUtility
    {
        public static bool TryFindBranchInfo(out GitBranchInfo branchInfo, int iterations = 3)
        {
            if (TryFindRepositoryRoot(Directory.GetCurrentDirectory(), out var repositoryRoot, iterations)) {
                return TryGetBranchInfo(repositoryRoot, out branchInfo);
            }

            branchInfo = default;
            return false;
        }
        
        public static bool TryFindRepositoryRoot(string directory, out string repoDirectory, int iterations = 3)
        {
            Assert.IsNotNull(directory);
            
            for (var i = 0; i < iterations; i++) {
                var gitDirName = Path.Combine(directory, ".git");
                if (Directory.Exists(gitDirName)) {
                    repoDirectory = directory;
                    return true;
                }

                var parent = Directory.GetParent(directory);

                if (!parent.Exists) {
                    break;
                }

                directory = parent.FullName;
            }

            repoDirectory = default;
            return false;
        }
        
        public static bool TryGetBranchInfo(string repositoryRoot, out GitBranchInfo branchInfo)
        {
            var gitDir = Path.Combine(repositoryRoot ?? "", ".git");
            
            if (!Directory.Exists(gitDir)) {
                branchInfo = default;
                return false;
            }

            var headPath = Path.Combine(gitDir, "HEAD");
            var headText = File.ReadAllText(headPath);

            if (headText.StartsWith("ref:")) {
                var refPath = headText.Substring("ref: ".Length).TrimEnd(); 
                
                var branchName = refPath.Substring("refs/heads/".Length).TrimEnd();
                var commitHash = File.ReadAllText(Path.Combine(gitDir, refPath)).TrimEnd();

                branchInfo = new GitBranchInfo() {
                    Branch = branchName,
                    CommitHash = commitHash
                };
                
                return true;
            }

            branchInfo = new GitBranchInfo() {
                Branch = "DETACHED",
                CommitHash = headText
            };
                
            return true;
        }
    }
}