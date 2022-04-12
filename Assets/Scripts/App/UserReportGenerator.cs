using System;
using System.IO;
using System.Linq;
using Tekly.Common.LocalFiles;
using Tekly.Logging;
using Tekly.Logging.LogDestinations;
using Tekly.Webster.Routes;
using Tekly.ZipFile;
using UnityEngine;

namespace TeklySample.App
{
    [Serializable]
    public class UserReport
    {
        public string Message;
        public string Version;
    }
    
    public static class UserReportGenerator
    {
        public static void Generate(Stream outputStream)
        {
            using var zipFile = new ZipFile(outputStream);

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
            
            zipFile.AddAsJson(report, "report.json");
            zipFile.AddAsJson(InfoSummary.Get(), "info_summary.json");

            zipFile.Finish();
        }
    }
}