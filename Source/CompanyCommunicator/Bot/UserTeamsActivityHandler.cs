// <copyright file="UserTeamsActivityHandler.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Translator;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Company Communicator User Bot.
    /// Captures user data, team data.
    /// </summary>
    public class UserTeamsActivityHandler : TeamsActivityHandler
    {
        private static readonly string TeamRenamedEventType = "teamRenamed";

        private readonly TeamsDataCapture teamsDataCapture;
        private readonly IBotTelemetryClient botTelemetryClient;
        //private readonly ISendingNotificationDataRepository sendingNotificationRepo;
        //private readonly ISentNotificationDataRepository sentNotificationDataRepository;
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly AdaptiveCardCreator adaptiveCardCreator;
        //private readonly ITranslator translator;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTeamsActivityHandler"/> class.
        /// </summary>
        /// <param name="teamsDataCapture">Teams data capture service.</param>
        public UserTeamsActivityHandler(TeamsDataCapture teamsDataCapture,
            IBotTelemetryClient botTelemetryClient,
            //ITranslator translator,
            //ISentNotificationDataRepository sentNotificationDataRepository,
            INotificationDataRepository notificationDataRepository,
            AdaptiveCardCreator adaptiveCardCreator)
        {
            this.botTelemetryClient = botTelemetryClient ?? throw new ArgumentNullException(nameof(botTelemetryClient));
            //this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentNullException(nameof(sentNotificationDataRepository));
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentException(nameof(notificationDataRepository));
            this.teamsDataCapture = teamsDataCapture ?? throw new ArgumentNullException(nameof(teamsDataCapture));
            //this.translator = translator ?? throw new ArgumentException(nameof(translator));
            this.adaptiveCardCreator = adaptiveCardCreator ?? throw new ArgumentException(nameof(adaptiveCardCreator));
        }

        /// <summary>
        /// Invoked when a conversation update activity is received from the channel.
        /// </summary>
        /// <param name="turnContext">The context object for this turn.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        protected override async Task OnConversationUpdateActivityAsync(
            ITurnContext<IConversationUpdateActivity> turnContext,
            CancellationToken cancellationToken)
        {
            // base.OnConversationUpdateActivityAsync is useful when it comes to responding to users being added to or removed from the conversation.
            // For example, a bot could respond to a user being added by greeting the user.
            // By default, base.OnConversationUpdateActivityAsync will call <see cref="OnMembersAddedAsync(IList{ChannelAccount}, ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
            // if any users have been added or <see cref="OnMembersRemovedAsync(IList{ChannelAccount}, ITurnContext{IConversationUpdateActivity}, CancellationToken)"/>
            // if any users have been removed. base.OnConversationUpdateActivityAsync checks the member ID so that it only responds to updates regarding members other than the bot itself.
            await base.OnConversationUpdateActivityAsync(turnContext, cancellationToken);

            var activity = turnContext.Activity;

            var isTeamRenamed = this.IsTeamInformationUpdated(activity);
            if (isTeamRenamed)
            {
                await this.teamsDataCapture.OnTeamInformationUpdatedAsync(activity);
            }

            if (activity.MembersAdded != null)
            {
                await this.teamsDataCapture.OnBotAddedAsync(turnContext, activity, cancellationToken);
            }

            if (activity.MembersRemoved != null)
            {
                await this.teamsDataCapture.OnBotRemovedAsync(activity);
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(turnContext.Activity.ReplyToId))
            {
                var txt = turnContext.Activity.Text;
                dynamic value = turnContext.Activity.Value;

                // Check if the activity came from a submit action
                if (string.IsNullOrEmpty(txt) && value != null)
                {
                    string notificationId = value["notificationId"];
                    //bool translation = Convert.ToBoolean(value["translation"]);

                    var notificationEntity = await this.notificationDataRepository.GetAsync(NotificationDataTableNames.SentNotificationsPartition, notificationId);
                    if (!string.IsNullOrWhiteSpace(notificationEntity.ButtonLink))
                    {
                        notificationEntity.ButtonLink = value["trackClickUrl"];
                    }

                    //if (translation)
                    //{
                    //    var detectedUserLocale = turnContext.Activity.Locale;
                    //    string userLanguage = string.Empty;
                    //    if (detectedUserLocale.Contains('-'))
                    //    {
                    //        userLanguage = detectedUserLocale.Split('-')[0];
                    //    }

                    //    notificationEntity.Title = await this.translator.TranslateAsync(notificationEntity.Title, userLanguage);
                    //    if (!string.IsNullOrWhiteSpace(notificationEntity.Summary))
                    //    {
                    //        notificationEntity.Summary = await this.translator.TranslateAsync(notificationEntity.Summary, userLanguage);
                    //    }

                    //    if (!string.IsNullOrWhiteSpace(notificationEntity.ButtonTitle))
                    //    {
                    //        notificationEntity.ButtonTitle = await this.translator.TranslateAsync(notificationEntity.ButtonTitle, userLanguage);
                    //    }
                    //}

                    // Download base64 data from blob convert to base64 string.
                    if (!string.IsNullOrEmpty(notificationEntity.ImageBase64BlobName))
                    {
                        notificationEntity.ImageLink = await this.notificationDataRepository.GetImageAsync(notificationEntity.ImageLink, notificationEntity.ImageBase64BlobName);
                    }

                    var card = this.adaptiveCardCreator.CreateAdaptiveCard(notificationEntity, translate: false, acknowledged: true);

                    var adaptiveCardAttachment = new Attachment()
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = card,
                    };

                    var activity = turnContext.Activity;
                    var properties = new Dictionary<string, string>
                    {
                        { "notificationId", notificationId },
                        { "notificationTitle", notificationEntity.Title },
                        { "notificationUrl", notificationEntity.ButtonLink },
                        { "notificationAuthor", notificationEntity.Author },
                        { "notificationCreatedBy", notificationEntity.CreatedBy },
                        { "notificationSendCompletedDate", notificationEntity.SentDate?.ToString() },
                        { "userId", activity.From?.AadObjectId },
                    };
                    this.LogActivityTelemetry(activity, "TrackAck", properties);

                    var newActivity = MessageFactory.Attachment(adaptiveCardAttachment);
                    newActivity.Id = turnContext.Activity.ReplyToId;
                    await turnContext.UpdateActivityAsync(newActivity, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Log telemetry about the incoming activity.
        /// </summary>
        /// <param name="activity">The activity</param>
        private void LogActivityTelemetry(IActivity activity, string eventName = "UserInfo", Dictionary<string, string> properties = null)
        {
            if (properties == null)
            {
                properties = new Dictionary<string, string>();
            }

            properties.Add("UserAadObjectId", activity.From?.AadObjectId);
            properties.Add("UserName", activity.From?.Name);

            // client info
            var clientInfoEntity = activity.Entities?.Where(e => e.Type == "clientInfo")?.FirstOrDefault();
            properties.Add("Locale", clientInfoEntity?.Properties["locale"]?.ToString());
            properties.Add("Country", clientInfoEntity?.Properties["country"]?.ToString());
            properties.Add("TimeZone", clientInfoEntity?.Properties["timezone"]?.ToString());
            properties.Add("Platform", clientInfoEntity?.Properties["platform"]?.ToString());

            properties.Add("ActivityId", activity.Id);
            properties.Add("ActivityType", activity.Type);
            properties.Add("ConversationType", string.IsNullOrWhiteSpace(activity.Conversation?.ConversationType) ? "personal" : activity.Conversation.ConversationType);
            properties.Add("ConversationId", activity.Conversation?.Id);

            var channelData = activity.GetChannelData<TeamsChannelData>();
            properties.Add("TeamId", channelData?.Team?.Id);

            this.botTelemetryClient.TrackEvent(eventName, properties);
        }

        private bool IsTeamInformationUpdated(IConversationUpdateActivity activity)
        {
            if (activity == null)
            {
                return false;
            }

            var channelData = activity.GetChannelData<TeamsChannelData>();
            if (channelData == null)
            {
                return false;
            }

            return UserTeamsActivityHandler.TeamRenamedEventType.Equals(channelData.EventType, StringComparison.OrdinalIgnoreCase);
        }
    }
}