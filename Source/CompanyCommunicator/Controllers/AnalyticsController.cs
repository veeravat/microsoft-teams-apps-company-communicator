using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Analytics;

namespace Microsoft.Teams.Apps.CompanyCommunicator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService analyticsService;
        private readonly ILogger<AnalyticsController> logger;

        public AnalyticsController(IAnalyticsService analyticsService, ILoggerFactory loggerFactory)
        {
            this.analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            this.logger = loggerFactory?.CreateLogger<AnalyticsController>() ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        [HttpGet("viewcount/{id}")]
        public async Task<int> GetViewCount(string id)
        {
            return await this.analyticsService.GetUniqueViewsCountByNotificationIdAsync(id);
        }

        [HttpGet("clickcount/{id}")]
        public async Task<int> GetClickCount(string id)
        {
            return await this.analyticsService.GetUniqueClicksCountByNotificationIdAsync(id);
        }

        [HttpGet("reactionscount/{id}")]
        public async Task<int> GetReactionsCount(string id)
        {
            return await this.analyticsService.GetReactionsCountByNotificationIdAsync(id);
        }

        [HttpGet("pollresult/{id}")]
        public async Task<KustoQueryResult> GetPollResult(string id)
        {
            return await this.analyticsService.GetPollVoteResultByNotificationIdAsync(id);
        }

        [HttpGet("ackcount/{id}")]
        public async Task<int> GetAckCount(string id)
        {
            return await this.analyticsService.GetAcknowledgementsCountByNotificationIdAsync(id);
        }

        //[HttpGet("GetUserActivityByTime")]
        //public async Task<KustoQueryResult> GetUserActivityByTime(string timespan)
        //{
        //    // like a sdk to ai the same thing like LogsQueryClient but works with both types of AI (classic and workspace)
        //    return await this.analyticsService.GetUserActivityTimeline(timespan);
        //}
    }
}
