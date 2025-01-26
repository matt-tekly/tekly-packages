using System.IO;
using Tekly.Common.LocalFiles;
using Tekly.Common.Utils;

namespace Tekly.Backtick.Commands
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
        [Help("Search the local files")]
        public string Search(string search)
        {
            return string.Join("\n", LocalFile.GetFiles("", search, SearchOption.AllDirectories));
        }

#if UNITY_STANDALONE
        [Command("app.localfile.open")]
        [Help("Open")]
        public void Open(string search = null)
        {
            if (search == null) {
                FileUtils.RevealInFileBrowser(LocalFile.Directory);
            } else {
                search = LocalFile.GetPath(search.Replace("\\", "/").TrimEnd('/').TrimStart('/'));
                search = Path.GetFullPath(search);

                FileUtils.RevealInFileBrowser(search);
            }
        }
#endif
    }
}