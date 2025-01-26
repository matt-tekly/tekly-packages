using Tekly.Common.LocalFiles;
using Tekly.Common.Utils;

namespace Tekly.Backtick.Commands
{
    public class OverridableDataCommands : ICommandSource
    {
        [Command("overrides.write")]
        [Help("Write all Overridable objects that are in memory to JSON")]
        public void WriteAll()
        {
            OverridableData.WriteAll();
        }
        
        [Command("overrides.reload")]
        [Help("Reload all Overridable objects that are in memory from JSON")]
        public void Reload()
        {
            OverridableData.ReloadAll();
        }
        
#if UNITY_STANDALONE
        [Command("overrides.open")]
        [Help("Open the overrides directory")]
        public void Open()
        {
            FileUtils.RevealInFileBrowser(LocalFile.GetFullPath(OverridableData.Directory));
        }
#endif
    }
}