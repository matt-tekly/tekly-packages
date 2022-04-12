using System.Linq;
using System.Net;
using System.Text;
using Tekly.Common.Utils;
using Tekly.Logging;
using Tekly.Logging.LogDestinations;
using Tekly.Webster.Routing;

namespace TeklySample.App
{
    [Route("/game")]
    public class SampleWebsterHandler
    {
        [Hidden]
        [Get("/report/generate")]
        public void HandleReport(HttpListenerResponse response)
        {
            response.ContentType = "application/zip";
            response.ContentEncoding = Encoding.Default;

            var resultFile = $"report_{DateUtils.UtcNowForFileName()}.zip";
            response.AddHeader("content-disposition", $"attachment; filename={resultFile}");

            UserReportGenerator.Generate(response.OutputStream);
        }
        
        [Get("/logging/reset")]
        public string ResetLogs()
        {
            foreach (var destination in TkLogger.Destinations.OfType<FileLogDestination>()) {
                destination.QueueReset();
            }
            
            return "Reset successful.";
        }
    }
}