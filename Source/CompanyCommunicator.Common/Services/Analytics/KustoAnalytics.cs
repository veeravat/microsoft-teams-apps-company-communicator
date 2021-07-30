namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Analytics
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    public class KustoService : IAnalyticsService
    {
        private const string Host = "https://api.applicationinsights.io/v1/apps/{0}/query?query={1}&timespan={2}";

        private static HttpClient client = new HttpClient();

        private readonly string appInsightsId;
        private readonly string apiKey;

        private readonly string uniqueViewsKustoQuery = "customEvents| extend notificationId = tostring(customDimensions['notificationId']), userId = tostring(customDimensions['userId'])"
                          + "| where name == 'TrackView'  and notificationId == '{0}' | summarize Count=dcount(userId) by notificationId";

        private readonly string totalViewsKustoQuery = "customEvents| extend notificationId = tostring(customDimensions['notificationId']), userId = tostring(customDimensions['userId'])"
                          + "| where name == 'TrackView'  and notificationId == '{0}' | summarize Count = count() by notificationId";

        private readonly string uniqueClicksKustoQuery = "customEvents| extend notificationId = tostring(customDimensions['notificationId']), userId = tostring(customDimensions['userId'])"
                          + "| where name == 'TrackUrl'  and notificationId == '{0}' | summarize Count=dcount(userId) by notificationId";

        private readonly string acknowledgementsCountKustoQuery = "customEvents| extend notificationId = tostring(customDimensions['notificationId']), userId = tostring(customDimensions['userId'])"
                          + "| where name == 'TrackAck'  and notificationId == '{0}' | summarize Count= count() by notificationId";

        private readonly string timespan = "P90D";

        /// <summary>
        /// Instance to send logs to the telemetry service.
        /// </summary>
        private readonly ILogger<KustoService> logger;

        public KustoService(IConfiguration configuration, ILogger<KustoService> logger)
        {
            var appInsightsId = configuration["AppInsightsId"];
            this.appInsightsId = appInsightsId ?? throw new ArgumentNullException(nameof(appInsightsId));

            var key = configuration["AppInsightsApiKey"];
            this.apiKey = key ?? throw new ArgumentNullException(nameof(key));

            this.logger = logger ?? throw new ArgumentException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<int> GetUniqueViewsCountByNotificationIdAsync(string notificationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = string.Format(this.uniqueViewsKustoQuery, notificationId);
            var uri = string.Format(Host, this.appInsightsId, query, this.timespan);

            var result = await this.GetKustoQueryResultAsync(query, uri, cancellationToken);

            try
            {
                var row = result?.Tables?[0]?.Rows?[0];
                if (row.Count < 2)
                {
                    return 0;
                }

                var count = row[1];
                if (count != null)
                {
                    return Convert.ToInt32(count);
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogCritical($"notificationId={notificationId}");
                this.logger.LogCritical($"query={query}");
                this.logger.LogCritical($"uri={uri}");
                this.logger.LogError(ex, $"Error getting result from Application Insights.");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetUniqueClicksCountByNotificationIdAsync(string notificationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = string.Format(this.uniqueClicksKustoQuery, notificationId);
            var uri = string.Format(Host, this.appInsightsId, query, this.timespan);

            var result = await this.GetKustoQueryResultAsync(query, uri, cancellationToken);

            try
            {
                var row = result?.Tables?[0]?.Rows?[0];
                if (row.Count < 2)
                {
                    return 0;
                }

                var count = row[1];
                if (count != null)
                {
                    return Convert.ToInt32(count);
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogCritical($"notificationId={notificationId}");
                this.logger.LogCritical($"query={query}");
                this.logger.LogCritical($"uri={uri}");
                this.logger.LogError(ex, $"GetUniqueClicksCountByNotificationIdAsync Error getting result from Application Insights.");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetAcknowledgementsCountByNotificationIdAsync(string notificationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = string.Format(this.acknowledgementsCountKustoQuery, notificationId);
            var uri = string.Format(Host, this.appInsightsId, query, this.timespan);

            var result = await this.GetKustoQueryResultAsync(query, uri, cancellationToken);

            try
            {
                var row = result?.Tables?[0]?.Rows?[0];
                if (row.Count < 2)
                {
                    return 0;
                }

                var count = row[1];
                if (count != null)
                {
                    return Convert.ToInt32(count);
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogCritical($"notificationId={notificationId}");
                this.logger.LogCritical($"query={query}");
                this.logger.LogCritical($"uri={uri}");
                this.logger.LogError(ex, $"GetAcknowledgementsCountByNotificationIdAsync. Error getting result from Application Insights.");
                return 0;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetTotalViewsCountByNotificationIdAsync(string notificationId, CancellationToken cancellationToken = default(CancellationToken))
        {
            var query = string.Format(this.totalViewsKustoQuery, notificationId);
            var uri = string.Format(Host, this.appInsightsId, query, this.timespan);

            var result = await this.GetKustoQueryResultAsync(query, uri, cancellationToken);

            try
            {
                var row = result?.Tables?[0]?.Rows?[0];
                if (row.Count < 2)
                {
                    return 0;
                }

                var count = row[1];
                if (count != null)
                {
                    return Convert.ToInt32(count);
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                this.logger.LogCritical($"notificationId={notificationId}");
                this.logger.LogCritical($"query={query}");
                this.logger.LogCritical($"uri={uri}");
                this.logger.LogError(ex, $"GetTotalViewsCountByNotification. Error getting result from Application Insights.");
                return 0;
            }
        }

        private async Task<KustoQueryResult> GetKustoQueryResultAsync(string query, string uri, CancellationToken cancellationToken = default(CancellationToken))
        {
            using var request = new HttpRequestMessage();
            request.Method = HttpMethod.Get;
            request.Headers.Add("x-api-key", this.apiKey);
            request.RequestUri = new Uri(uri);

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"The call to the AppInsights service returned HTTP status code {response.StatusCode}.");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            this.logger.LogCritical($"GetKustoQueryResult {responseBody}");
            return JsonConvert.DeserializeObject<KustoQueryResult>(responseBody);
        }
    }
}
