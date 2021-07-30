using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

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
                this.logger.LogError($"delimiter={delimiter}, length={url.Length}, url={url}, Exception={ex}");
            }

            return this.pixelResponse;
        }
    }
}