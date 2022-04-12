using System;
using System.IO;
using System.Linq;
using Tekly.Common.LocalFiles;
using Tekly.Common.Terminal.Commands;
using Tekly.Logging;
using Tekly.Logging.LogDestinations;
using Tekly.ZipFile;
using UnityEngine;

namespace TeklySample.App
{
    public class GameCommands : ICommandSource
    {
        [Command("game.userreport.zip")]
        [Help("Create a user report zip")]
        public string UserReportZip(string localFile)
        {
            if (!localFile.EndsWith(".zip")) {
                localFile += ".zip";
            }
            
            using var fileStream = LocalFile.WriteStream(localFile);
            using var zipFile = new ZipFile(fileStream);
            
            var fileLogDestinations = TkLogger.Destinations.OfType<FileLogDestination>();

            foreach (var fileLogDestination in fileLogDestinations) {
                if (LocalFile.Exists(fileLogDestination.CurrentFilePath)) {
                    var fileName = Path.GetFileName(fileLogDestination.CurrentFilePath);
                    zipFile.AddFile(LocalFile.GetPath(fileLogDestination.CurrentFilePath), fileName);    
                }
                
                if (LocalFile.Exists(fileLogDestination.PrevFilePath)) {
                    var fileName = Path.GetFileName(fileLogDestination.PrevFilePath);
                    zipFile.AddFile(LocalFile.GetPath(fileLogDestination.PrevFilePath), fileName);    
                }
            }
            
            var report = new UserReport {
                Message = "Here is some useful information in the report",
                Version = Application.version
            };
            
            zipFile.AddEntry(JsonUtility.ToJson(report, true), "report.json");

            return $"Created report at {LocalFile.GetFullPath(localFile)}";
        }

        [Command("game.logs.reset")]
        [Help("Resets FileLogDestinations")]
        public string ResetLogs()
        {
            foreach (var destination in TkLogger.Destinations.OfType<FileLogDestination>()) {
                destination.QueueReset();
            }
            
            return "Reset successful.";
        }
    }

    [Serializable]
    public class UserReport
    {
        public string Message;
        public string Version;
    }
}