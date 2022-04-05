using System.IO;
using Tekly.Common.LocalFiles;
using Tekly.Logging;
using Tekly.Logging.LogDestinations;
using UnityEngine;

namespace TeklySample.App
{
    public static class AppLogging
    {
        private const string CURR_LOG_FILE = "logs/current.log";
        private const string PREV_LOG_FILE = "logs/previous.log";
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Initialize()
        {
            if (LocalFile.Exists(CURR_LOG_FILE)) {
                LocalFile.Rename(CURR_LOG_FILE, PREV_LOG_FILE);
            }
            
            var fileStream = LocalFile.GetStream(CURR_LOG_FILE, FileMode.Create);
            var destination = new FlatFileLogDestination(fileStream, TkLogLevel.Info);
            // var destination = new StructuredFileLogDestination(fileStream, TkLogLevel.Info);
            
            TkLogger.Destinations.Add(destination);
        }
    }
}