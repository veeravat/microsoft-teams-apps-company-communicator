using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    [Route("redirect")]
    [ApiController]
    public class RedirectController : ControllerBase
    {
        private readonly ILogger<RedirectController> logger;
        private readonly IBotTelemetryClient telemetryClient;

        public RedirectController(ILoggerFactory loggerFactory, IBotTelemetryClient telemetryClient)
        {
            this.logger = loggerFactory?.CreateLogger<RedirectController>() ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        }

        [HttpGet]
        public IActionResult Get(string url, string id, string userId)
        {
            try
            {
                var props = new Dictionary<string, string> {
                    { "notificationId", id },
                    { "userId", userId },
                    { "url", url },
                };
                this.telemetryClient.TrackEvent("TrackUrl", props);
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Exception={ex}");
            }

            return this.Redirect(url);
        }
    }
}