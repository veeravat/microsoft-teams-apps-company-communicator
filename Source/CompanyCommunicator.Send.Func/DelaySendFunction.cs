
namespace Microsoft.Teams.Apps.CompanyCommunicator.Send.Func
{

    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.DataQueue;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.MessageQueues.PrepareToSendQueue;

    public class DelaySendFunction
    {
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;
        private readonly IPrepareToSendQueue prepareToSendQueue;
        private readonly IDataQueue dataQueue;
        private readonly double forceCompleteMessageDelayInSeconds = 86400;

        public DelaySendFunction(INotificationDataRepository notificationDataRepository,
            ISentNotificationDataRepository sentNotificationDataRepository,
            IPrepareToSendQueue prepareToSendQueue,
            IDataQueue dataQueue)
        {
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentNullException(nameof(notificationDataRepository));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.prepareToSendQueue = prepareToSendQueue ?? throw new ArgumentNullException(nameof(prepareToSendQueue));
            this.dataQueue = dataQueue ?? throw new ArgumentNullException(nameof(dataQueue));
        }

        [FunctionName("DelaySendFunction")]
        public async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var notificationEntities = await this.notificationDataRepository.GetAllDraftNotificationsAsync();
            foreach (var draft in notificationEntities)
            {
                var draftNotificationDataEntity = await this.notificationDataRepository.GetAsync(
                    NotificationDataTableNames.DraftNotificationsPartition,
                    draft.Id);

                log.LogInformation($"draft.Id and ScheduledDateTime {draftNotificationDataEntity.Id}-{draftNotificationDataEntity.Title}....{draftNotificationDataEntity.ScheduledDateTime}");
                if (draftNotificationDataEntity.ScheduledDateTime <= DateTime.Now)
                {
                    log.LogInformation($"that means shceduledDateTime less than now: {draftNotificationDataEntity.ScheduledDateTime}");

                    var newSentNotificationId =
                    await this.notificationDataRepository.MoveDraftToSentPartitionAsync(draftNotificationDataEntity);
                    log.LogInformation($"newSentNotificationId {newSentNotificationId}");

                    // Ensure the data table needed by the Azure Functions to send the notifications exist in Azure storage.
                    await this.sentNotificationDataRepository.EnsureSentNotificationDataTableExistsAsync();

                    // Update user app id if proactive installation is enabled.
                    //await this.UpdateUserAppIdAsync();

                    var prepareToSendQueueMessageContent = new PrepareToSendQueueMessageContent
                    {
                        NotificationId = newSentNotificationId,
                    };
                    await this.prepareToSendQueue.SendAsync(prepareToSendQueueMessageContent);

                    // Send a "force complete" message to the data queue with a delay to ensure that
                    // the notification will be marked as complete no matter the counts
                    var forceCompleteDataQueueMessageContent = new DataQueueMessageContent
                    {
                        NotificationId = newSentNotificationId,
                        ForceMessageComplete = true,
                    };
                    await this.dataQueue.SendDelayedAsync(
                        forceCompleteDataQueueMessageContent,
                        this.forceCompleteMessageDelayInSeconds);
                }
            }
        }

        /// <summary>
        /// Updates user app id if its not already synced.
        /// </summary>
        //private async Task UpdateUserAppIdAsync(ILogger log)
        //{
        //    // check if proactive installation is enabled.
        //    if (!this.userAppOptions.ProactivelyInstallUserApp)
        //    {
        //        return;
        //    }

        //    // check if we have already synced app id.
        //    var appId = await this.appSettingsService.GetUserAppIdAsync();
        //    if (!string.IsNullOrWhiteSpace(appId))
        //    {
        //        return;
        //    }

        //    try
        //    {
        //        // Fetch and store user app id in App Catalog.
        //        appId = await this.appCatalogService.GetTeamsAppIdAsync(this.userAppOptions.UserAppExternalId);

        //        // Graph SDK returns empty id if the app is not found.
        //        if (string.IsNullOrEmpty(appId))
        //        {
        //            log.LogError($"Failed to find an app in AppCatalog with external Id: {this.userAppOptions.UserAppExternalId}");
        //            return;
        //        }

        //        await this.appSettingsService.SetUserAppIdAsync(appId);
        //    }
        //    catch (Graph.ServiceException exception)
        //    {
        //        // Failed to fetch app id.
        //        log.LogError(exception, $"Failed to get catalog app id. Error message: {exception.Message}.");
        //    }
        //}
    }
}
