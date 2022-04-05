using System.IO;
using Tekly.Common.Terminal.Commands;
using UnityEngine;

namespace Tekly.Common.LocalFiles
{
    public class LocalFileCommands : ICommandSource
    {
        [Command("app.localfile.path")]
        [Help("Get the local file path")]
        public string LocalFilePath()
        {
            return Path.GetFullPath(LocalFile.GetPath(""));
        }
        
        [Command("app.localfile.list")]
        [Help("List the local files")]
        public string LocalFiles(string path = null)
        {
            if (path == null) {
                return string.Join("\n", LocalFile.GetFiles(""));
            }
            
            return string.Join("\n", LocalFile.GetFiles(path));
        }
        
        [Command("app.localfile.listr")]
        [Help("List the local files recursive")]
        public string LocalFilesRecursive(string path = null)
        {
            if (path == null) {
                return string.Join("\n", LocalFile.GetFiles("", SearchOption.AllDirectories));
            }
            
            return string.Join("\n", LocalFile.GetFiles(path, SearchOption.AllDirectories));
        }
        
        [Command("app.localfile.search")]
        [Help("Serach the local files")]
        public string Search(string search)
        {
            return string.Join("\n", LocalFile.GetFiles("", search, SearchOption.AllDirectories));
        }
        
#if UNITY_STANDALONE
        [Command("app.localfile.open")]
        [Help("Open")]
        public void Open(string search)
        {
            search = LocalFile.GetPath(search.Replace("\\", "/").TrimEnd('/'));
            search = Path.GetFullPath(search);
            
            Application.OpenURL("file://" + search);
        }
#endif
    }
}