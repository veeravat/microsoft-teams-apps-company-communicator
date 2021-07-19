using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    [Route("track")]
    [ApiController]
    public class TrackController : ControllerBase
    {
        private readonly ILogger<TrackController> logger;
        private readonly FileContentResult pixelResponse;
        private readonly IBotTelemetryClient telemetryClient;

        public TrackController(FileContentResult pixelResponse, ILoggerFactory loggerFactory, IBotTelemetryClient telemetryClient)
        {
            this.pixelResponse = pixelResponse;
            this.logger = loggerFactory?.CreateLogger<TrackController>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        [HttpGet]
        public IActionResult Get(string url)
        {
            // get request parameters.
            var parameters = this.Request.Query.Keys.ToDictionary(k => k, k => this.Request.Query[k]);

            // get request headers.
            var headers = this.Request.Headers.Keys.ToDictionary(k => k, k => this.Request.Query[k]);
            //this.telemetryClient.TrackEvent("headers", headers);

            Task.Factory.StartNew((data) =>
            {
                var dataDictionary = data as IDictionary<string, StringValues>;
                                

            }, parameters.Union(headers).ToDictionary(k => k.Key, v => v.Value)).ConfigureAwait(false);

            var delimiter = url.IndexOf('-');
            try
            {
                if (delimiter > 0 && delimiter < url.Length)
                {
                    var properties = new Dictionary<string, string>()
                {
                    { "notificationId", url.Substring(0, delimiter) },
                    { "userId", url.Substring(delimiter + 1, url.Length - delimiter - 5) },
                };

                    this.telemetryClient.TrackEvent("TrackView", properties);
                }
                else
                {
                    this.telemetryClient.TrackEvent("TrackUrl", new Dictionary<string, string> { { "url", url } });
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError($"delimiter={delimiter}, length={url.Length}, url={url}, Exception={ex.ToString()}");
            }

            return this.pixelResponse;
        }
    }
}
