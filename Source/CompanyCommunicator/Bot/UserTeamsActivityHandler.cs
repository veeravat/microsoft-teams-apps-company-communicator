// <copyright file="UserTeamsActivityHandler.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.SentNotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard.TaskModule;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Company Communicator User Bot.
    /// Captures user data, team data.
    /// </summary>
    public class UserTeamsActivityHandler : TeamsActivityHandler
    {
        private static readonly string TeamRenamedEventType = "teamRenamed";
        private readonly IBotTelemetryClient botTelemetryClient;
        private readonly INotificationDataRepository notificationDataRepository;
        private readonly ISentNotificationDataRepository sentNotificationDataRepository;
        private readonly AdaptiveCardCreator adaptiveCardCreator;
        private readonly TeamsDataCapture teamsDataCapture;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTeamsActivityHandler"/> class.
        /// </summary>
        /// <param name="teamsDataCapture">Teams data capture service.</param>
        public UserTeamsActivityHandler(TeamsDataCapture teamsDataCapture,
            IBotTelemetryClient botTelemetryClient,
            AdaptiveCardCreator adaptiveCardCreator,
            INotificationDataRepository notificationDataRepository,
            ISentNotificationDataRepository sentNotificationDataRepository)
        {
            this.teamsDataCapture = teamsDataCapture ?? throw new ArgumentNullException(nameof(teamsDataCapture));
            this.botTelemetryClient = botTelemetryClient ?? throw new ArgumentNullException(nameof(botTelemetryClient));
            this.adaptiveCardCreator = adaptiveCardCreator ?? throw new ArgumentException(nameof(adaptiveCardCreator));
            this.notificationDataRepository = notificationDataRepository ?? throw new ArgumentException(nameof(notificationDataRepository));
            this.sentNotificationDataRepository = sentNotificationDataRepository ?? throw new ArgumentException(nameof(sentNotificationDataRepository));
            //_currentContext = context.HttpContext;
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

                    var notificationEntity = await this.notificationDataRepository.GetAsync(NotificationDataTableNames.SentNotificationsPartition, notificationId);

                    var card = this.adaptiveCardCreator.CreateAdaptiveCard(notificationEntity, true);

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
                    this.LogActivityTelemetry(turnContext.Activity, "TrackAck", properties);

                    var newActivity = MessageFactory.Attachment(adaptiveCardAttachment);
                    newActivity.Id = turnContext.Activity.ReplyToId;
                    await turnContext.UpdateActivityAsync(newActivity, cancellationToken);
                }
            }
            else
            {
                await base.OnMessageActivityAsync(turnContext, cancellationToken);
            }
        }

        protected override async Task OnReactionsAddedAsync(IList<MessageReaction> messageReactions, ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var reaction in messageReactions)
            {
                // The ReplyToId property of the inbound MessageReaction will correspond to a Message Activity
                // which had previosly been sent from this bot.
                var originalActivityId = turnContext.Activity.ReplyToId;

                //var notificationId = this.sentNotificationDataRepository.GetWithFilterAsync();
                //    this.GetNotificationIdbyActivityId(originalActivityId);

                //var notificationEntity = await this.notificationDataRepository.GetAsync(NotificationDataTableNames.SentNotificationsPartition, notificationId);
                //var attachment = activity.AsMessageActivity().Attachments.FirstOrDefault();
                //if (attachment != null)
                {
                    //var card = attachment.Content.ToString();
                    var properties = new Dictionary<string, string>
                    {
                        //{ "card", card },
                        { "activityId", originalActivityId },
                        { "userId", turnContext.Activity.From?.AadObjectId },
                        { "reactionType", reaction.Type },
                    };
                    this.LogActivityTelemetry(turnContext.Activity, "OnReactionsAdded", properties);
                }
                //else
                //{
                //    this.botTelemetryClient.TrackEvent("OnReactionsAddedAsync attachment is null");
                //}
            }
        }

        private string getTextFromActivity(Activity activity)
        {
            List<string> textList = new List<string>();

            if (!string.IsNullOrEmpty(activity.Text)) textList.Add(activity.Text);

            activity.Attachments?.All(delegate (Attachment attachment)
            {
                if (!string.IsNullOrEmpty(attachment.Content as string)) textList.Add(attachment.Content as string);
                else if (null != (attachment.Content as HeroCard)) textList.Add((attachment.Content as HeroCard).Text);
                else if (null != (attachment.Content as ThumbnailCard)) textList.Add((attachment.Content as ThumbnailCard).Text);

                return false;
            });

            return string.Join(Environment.NewLine, textList);
        }

        protected override async Task OnReactionsRemovedAsync(IList<MessageReaction> messageReactions, ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var reaction in messageReactions)
            {
                var activity = turnContext.Activity as IActivity;
                this.TrackReaction(activity, "OnReactionsRemoved", reaction.Type);
            }
        }

        /// <summary>
        /// When OnTurn method receives a fetch invoke activity on bot turn, it calls this method.
        /// </summary>
        /// <param name="turnContext">Context object containing information cached for a single turn of conversation with a user.</param>
        /// <param name="taskModuleRequest">Task module invoke request value payload.</param>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>A task that represents a task module response.</returns>
        /// <remarks>
        /// Reference link: https://docs.microsoft.com/en-us/dotnet/api/microsoft.bot.builder.teams.teamsactivityhandler.onteamstaskmodulefetchasync?view=botbuilder-dotnet-stable.
        /// </remarks>
        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(
            ITurnContext<IInvokeActivity> turnContext,
            TaskModuleRequest taskModuleRequest,
            CancellationToken cancellationToken)
        {
            

            string card = "{\r\n  \"$schema\": \"http://adaptivecards.io/schemas/adaptive-card.json\",\r\n  \"type\": \"AdaptiveCard\",\r\n  \"version\": \"1.0\",\r\n  \"body\": [\r\n    {\r\n      \"type\": \"Container\",\r\n      \"items\": [\r\n        {\r\n          \"type\": \"TextBlock\",\r\n          \"text\": \"Publish Adaptive Card schema\",\r\n          \"weight\": \"bolder\",\r\n          \"size\": \"medium\"\r\n        },\r\n        {\r\n          \"type\": \"ColumnSet\",\r\n          \"columns\": [\r\n            {\r\n              \"type\": \"Column\",\r\n              \"width\": \"auto\",\r\n              \"items\": [\r\n                {\r\n                  \"size\": \"small\",\r\n                  \"style\": \"person\",\r\n                  \"type\": \"Image\",\r\n                  \"url\": \"https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg\"\r\n                }\r\n              ]\r\n            },\r\n            {\r\n              \"type\": \"Column\",\r\n              \"width\": \"stretch\",\r\n              \"items\": [\r\n                {\r\n                  \"type\": \"TextBlock\",\r\n                  \"text\": \"Matt Hidinger\",\r\n                  \"weight\": \"bolder\",\r\n                  \"wrap\": true\r\n                },\r\n                {\r\n                  \"type\": \"TextBlock\",\r\n                  \"spacing\": \"none\",\r\n                  \"text\": \"Created {{DATE(2017-02-14T06:08:39Z, SHORT)}}\",\r\n                  \"isSubtle\": true,\r\n                  \"wrap\": true\r\n                }\r\n              ]\r\n            }\r\n          ]\r\n        }\r\n      ]\r\n    },\r\n    {\r\n      \"type\": \"Container\",\r\n      \"items\": [\r\n        {\r\n          \"type\": \"TextBlock\",\r\n          \"text\": \"Now that we have defined the main rules and features of the format, we need to produce a schema and publish it to GitHub. The schema will be the starting point of our reference documentation.\",\r\n          \"wrap\": true\r\n        },\r\n        {\r\n          \"type\": \"FactSet\",\r\n          \"facts\": [\r\n            {\r\n              \"title\": \"Board:\",\r\n              \"value\": \"Adaptive Card\"\r\n            },\r\n            {\r\n              \"title\": \"List:\",\r\n              \"value\": \"Backlog\"\r\n            },\r\n            {\r\n              \"title\": \"Assigned to:\",\r\n              \"value\": \"Matt Hidinger\"\r\n            },\r\n            {\r\n              \"title\": \"Due date:\",\r\n              \"value\": \"Not set\"\r\n            }\r\n          ]\r\n        }\r\n      ]\r\n    }\r\n  ],\r\n  \"actions\": [\r\n    {\r\n      \"type\": \"Action.ShowCard\",\r\n      \"title\": \"Set due date\",\r\n      \"card\": {\r\n        \"type\": \"AdaptiveCard\",\r\n        \"version\": \"1.0\",\r\n        \"body\": [\r\n          {\r\n            \"type\": \"Input.Date\",\r\n            \"id\": \"dueDate\"\r\n          }\r\n        ],\r\n        \"actions\": [\r\n          {\r\n            \"type\": \"Action.Submit\",\r\n            \"title\": \"OK\"\r\n          }\r\n        ]\r\n      }\r\n    },\r\n    {\r\n      \"type\": \"Action.ShowCard\",\r\n      \"title\": \"Comment\",\r\n      \"card\": {\r\n        \"type\": \"AdaptiveCard\",\r\n        \"version\": \"1.0\",\r\n        \"body\": [\r\n          {\r\n            \"type\": \"Input.Text\",\r\n            \"id\": \"comment\",\r\n            \"isMultiline\": true,\r\n            \"placeholder\": \"Enter your comment\"\r\n          }\r\n        ],\r\n        \"actions\": [\r\n          {\r\n            \"type\": \"Action.Submit\",\r\n            \"title\": \"OK\"\r\n          }\r\n        ]\r\n      }\r\n    }\r\n  ]\r\n}\r\n";
            string video = "{ \"type\": \"Media\", \"poster\": \"https://adaptivecards.io/content/poster-video.png\", \"sources\": [ { \"mimeType\": \"video /mp4\", \"url\": \"https://adaptivecardsblob.blob.core.windows.net/assets/AdaptiveCardsOverviewVideo.mp4\" } ]   }";
            string cardVideo = "{ \"$schema \": \"http://adaptivecards.io/schemas/adaptive-card.json\",\"type\": \"AdaptiveCard\",  \"version\": \"1.3\",\"body\": [" + video + " ]}";

            var asJobject = JObject.FromObject(taskModuleRequest.Data);
            var properties = new Dictionary<string, string>
                {
                    { "taskModuleRequest", JsonConvert.SerializeObject(taskModuleRequest) },
                    { "asJobject", JsonConvert.SerializeObject(taskModuleRequest) },
                };
            this.LogActivityTelemetry(turnContext.Activity, "OnTeamsTaskModuleFetchAsync", properties);
            var notificationId = asJobject.ToObject<CardTaskFetchValue<string>>()?.Data;

            var taskInfo = new TaskModuleTaskInfo();
            var notificationEntity = await this.notificationDataRepository.GetAsync(NotificationDataTableNames.SentNotificationsPartition, notificationId);
            var ac = this.adaptiveCardCreator.CreateMessageDetailsAdaptiveCard(notificationEntity.Summary);

            var attachment = new Attachment
            {
                ContentType = AdaptiveCard.ContentType,
                Content = ac,
            };
            taskInfo.Card = attachment;
            SetTaskInfo(taskInfo, TaskModuleUIConstants.AdaptiveCard);

            //switch (value)
            //{
            //    case TaskModuleIds.YouTube:
            //        //taskInfo.Url = taskInfo.FallbackUrl = _baseUrl + "/" + TaskModuleIds.YouTube;
            //        SetTaskInfo(taskInfo, TaskModuleUIConstants.YouTube);
            //        break;
            //    case TaskModuleIds.CustomForm:
            //        //taskInfo.Url = taskInfo.FallbackUrl = _baseUrl + "/" + TaskModuleIds.CustomForm;
            //        SetTaskInfo(taskInfo, TaskModuleUIConstants.CustomForm);
            //        break;
            //    case TaskModuleIds.AdaptiveCard:

            //        break;
            //    default:
            //        break;
            //}

            return await Task.FromResult(taskInfo.ToTaskModuleResponse());

            //try
            //{
            //    var parsedResult = AdaptiveCard.FromJson(card);
            //    var attachment = new Attachment
            //    {
            //        ContentType = AdaptiveCard.ContentType,
            //        Content = parsedResult.Card,
            //    };
            //    var taskInfo = new TaskModuleTaskInfo();
            //    taskInfo.Card = attachment;
            //    SetTaskInfo(taskInfo, TaskModuleUIConstants.AdaptiveCard);

            //    return Task.FromResult(taskInfo.ToTaskModuleResponse());

            //    //return Task.FromResult(this.GetTaskModuleResponse("https://microsoft.com",
            //    //    "microsoft task module", attachment));
            //}
            //catch (AdaptiveSerializationException e)
            //{
            //    // handle JSON parsing error
            //    // or, re-throw
            //    throw;
            //}
        }

        protected override Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity as IEventActivity;
            var properties = new Dictionary<string, string>
                {
                    { "IEventActivity.Value", activity.Value?.ToString() },
                    { "IEventActivity.Name", activity.Name?.ToString() },
                };
            this.LogActivityTelemetry(turnContext.Activity, "OnEventActivityAsync");
            return base.OnEventActivityAsync(turnContext, cancellationToken);
        }

        public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            this.LogActivityTelemetry(turnContext.Activity, "OnTurnAsync");
            return base.OnTurnAsync(turnContext, cancellationToken);
            
            //switch (turnContext.Activity.Type)
            //{
            //    // handle invokes
            //    case ActivityTypes.Invoke:
            //        return OnInvokeActivityAsync(new DelegatingTurnContext<IInvokeActivity>(turnContext), cancellationToken);
            //    default:
            //        return base.OnTurnAsync(turnContext, cancellationToken);
            //}
        }

        protected virtual async Task OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            switch (turnContext.Activity.Name)
            {
                case "task/fetch":
                    // Show task module
                    break;
                case "task/submit":
                    // Handle task module submit
                    await SendResponse(turnContext);
                    break;
            }
        }

        private static async Task SendResponse(ITurnContext<IInvokeActivity> turnContext, object body = null)
        {
            await turnContext.SendActivityAsync(new Activity
            {
                Value = new InvokeResponse { Status = 200, Body = body },
                Type = ActivityTypesEx.InvokeResponse,
            });
        }


        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleSubmitAsync(ITurnContext<IInvokeActivity> turnContext,
            TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            var activity = turnContext.Activity;
            var properties = new Dictionary<string, string>
                {
                    { "taskModuleRequest", JsonConvert.SerializeObject(taskModuleRequest) },
                };
            this.LogActivityTelemetry(activity, "OnTeamsTaskModuleSubmitAsync", properties);
            
            return TaskModuleResponseFactory.CreateResponse("Thanks!");
        }

        private void TrackReaction(IActivity activity, string eventType, string reactionType)
        {
            var fromObjectId = activity.From?.AadObjectId;

            var properties = new Dictionary<string, string>
                {
                    { "ActivityId", activity.Id },
                    { "ReplyToId", activity.ReplyToId },
                    { "Activity.Conversation.Id", activity.Conversation.Id },
                    { "Activity.Conversation.Name", activity.Conversation.Name },
                    { "Role", activity.From.Role },
                    { "ReactionType", reactionType },
                    { "UserAadObjectId", fromObjectId },
                };

            this.botTelemetryClient.TrackEvent(eventType, properties);
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

        /// <summary>
        /// Get task module response object.
        /// </summary>
        /// <param name="url">Task module URL.</param>
        /// <param name="title">Title for task module.</param>
        /// <returns>TaskModuleResponse object.</returns>
        private TaskModuleResponse GetTaskModuleResponse(string url, string title, Attachment attachment)
        {
            return new TaskModuleResponse
            {
                Task = new TaskModuleContinueResponse
                {
                    Type = "continue",
                    Value = new TaskModuleTaskInfo()
                    {
                        Height = 600,
                        Width = 600,
                        Title = title,
                        Card = attachment,
                    },
                },
            };
        }

        private static void SetTaskInfo(TaskModuleTaskInfo taskInfo, UISettings uIConstants)
        {
            taskInfo.Height = uIConstants.Height;
            taskInfo.Width = uIConstants.Width;
            taskInfo.Title = uIConstants.Title.ToString();
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