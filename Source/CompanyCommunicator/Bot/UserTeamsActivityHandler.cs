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
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard;
    using Microsoft.Teams.Apps.FAQPlusPlus.Common.Providers.Translator;
    using Newtonsoft.Json;

    /// <summary>
    /// Company Communicator User Bot.
    /// Captures user data, team data.
    /// </summary>
    public class UserTeamsActivityHandler : TeamsActivityHandler
    {
        private static readonly string TeamRenamedEventType = "teamRenamed";
        private readonly ISendingNotificationDataRepository notificationRepo;
        private readonly IBotTelemetryClient botTelemetryClient;

        private readonly TeamsDataCapture teamsDataCapture;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTeamsActivityHandler"/> class.
        /// </summary>
        /// <param name="teamsDataCapture">Teams data capture service.</param>
        public UserTeamsActivityHandler(TeamsDataCapture teamsDataCapture, 
            ISendingNotificationDataRepository notificationRepo, 
            IBotTelemetryClient botTelemetryClient
            )
        {
            this.teamsDataCapture = teamsDataCapture ?? throw new ArgumentNullException(nameof(teamsDataCapture));
            this.notificationRepo = notificationRepo ?? throw new ArgumentNullException(nameof(notificationRepo));
            this.botTelemetryClient = botTelemetryClient ?? throw new ArgumentNullException(nameof(botTelemetryClient));
        }

        protected override async Task OnReactionsAddedAsync(IList<MessageReaction> messageReactions, ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var reaction in messageReactions)
            {
                var activity = turnContext.Activity as IActivity;

                var fromObjectId = activity.From?.AadObjectId;

                var properties = new Dictionary<string, string>
                {
                    { "ActivityId", activity.Id },
                    { "ReplyToId", activity.ReplyToId },
                    { "Activity.Conversation.Id", turnContext.Activity.Conversation.Id },
                    { "Activity.Conversation.Name", turnContext.Activity.Conversation.Name },
                    { "Role", activity.From.Role },
                    { "ReactionType", reaction.Type },
                    { "UserAadObjectId", fromObjectId },
                };

                this.botTelemetryClient.TrackEvent("OnReactionsAddedEvent", properties);
            }
        }

        protected override async Task OnReactionsRemovedAsync(IList<MessageReaction> messageReactions, ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var reaction in messageReactions)
            {
                var newReaction = $"You removed the reaction '{reaction.Type}' from the following message: '{turnContext.Activity.ReplyToId}'";
                var replyActivity = MessageFactory.Text(newReaction);
                await turnContext.SendActivityAsync(replyActivity, cancellationToken);
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
        protected override Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(
            ITurnContext<IInvokeActivity> turnContext,
            TaskModuleRequest taskModuleRequest,
            CancellationToken cancellationToken)
        {
            string card = "{\r\n  \"$schema\": \"http://adaptivecards.io/schemas/adaptive-card.json\",\r\n  \"type\": \"AdaptiveCard\",\r\n  \"version\": \"1.0\",\r\n  \"body\": [\r\n    {\r\n      \"type\": \"Container\",\r\n      \"items\": [\r\n        {\r\n          \"type\": \"TextBlock\",\r\n          \"text\": \"Publish Adaptive Card schema\",\r\n          \"weight\": \"bolder\",\r\n          \"size\": \"medium\"\r\n        },\r\n        {\r\n          \"type\": \"ColumnSet\",\r\n          \"columns\": [\r\n            {\r\n              \"type\": \"Column\",\r\n              \"width\": \"auto\",\r\n              \"items\": [\r\n                {\r\n                  \"size\": \"small\",\r\n                  \"style\": \"person\",\r\n                  \"type\": \"Image\",\r\n                  \"url\": \"https://pbs.twimg.com/profile_images/3647943215/d7f12830b3c17a5a9e4afcc370e3a37e_400x400.jpeg\"\r\n                }\r\n              ]\r\n            },\r\n            {\r\n              \"type\": \"Column\",\r\n              \"width\": \"stretch\",\r\n              \"items\": [\r\n                {\r\n                  \"type\": \"TextBlock\",\r\n                  \"text\": \"Matt Hidinger\",\r\n                  \"weight\": \"bolder\",\r\n                  \"wrap\": true\r\n                },\r\n                {\r\n                  \"type\": \"TextBlock\",\r\n                  \"spacing\": \"none\",\r\n                  \"text\": \"Created {{DATE(2017-02-14T06:08:39Z, SHORT)}}\",\r\n                  \"isSubtle\": true,\r\n                  \"wrap\": true\r\n                }\r\n              ]\r\n            }\r\n          ]\r\n        }\r\n      ]\r\n    },\r\n    {\r\n      \"type\": \"Container\",\r\n      \"items\": [\r\n        {\r\n          \"type\": \"TextBlock\",\r\n          \"text\": \"Now that we have defined the main rules and features of the format, we need to produce a schema and publish it to GitHub. The schema will be the starting point of our reference documentation.\",\r\n          \"wrap\": true\r\n        },\r\n        {\r\n          \"type\": \"FactSet\",\r\n          \"facts\": [\r\n            {\r\n              \"title\": \"Board:\",\r\n              \"value\": \"Adaptive Card\"\r\n            },\r\n            {\r\n              \"title\": \"List:\",\r\n              \"value\": \"Backlog\"\r\n            },\r\n            {\r\n              \"title\": \"Assigned to:\",\r\n              \"value\": \"Matt Hidinger\"\r\n            },\r\n            {\r\n              \"title\": \"Due date:\",\r\n              \"value\": \"Not set\"\r\n            }\r\n          ]\r\n        }\r\n      ]\r\n    }\r\n  ],\r\n  \"actions\": [\r\n    {\r\n      \"type\": \"Action.ShowCard\",\r\n      \"title\": \"Set due date\",\r\n      \"card\": {\r\n        \"type\": \"AdaptiveCard\",\r\n        \"version\": \"1.0\",\r\n        \"body\": [\r\n          {\r\n            \"type\": \"Input.Date\",\r\n            \"id\": \"dueDate\"\r\n          }\r\n        ],\r\n        \"actions\": [\r\n          {\r\n            \"type\": \"Action.Submit\",\r\n            \"title\": \"OK\"\r\n          }\r\n        ]\r\n      }\r\n    },\r\n    {\r\n      \"type\": \"Action.ShowCard\",\r\n      \"title\": \"Comment\",\r\n      \"card\": {\r\n        \"type\": \"AdaptiveCard\",\r\n        \"version\": \"1.0\",\r\n        \"body\": [\r\n          {\r\n            \"type\": \"Input.Text\",\r\n            \"id\": \"comment\",\r\n            \"isMultiline\": true,\r\n            \"placeholder\": \"Enter your comment\"\r\n          }\r\n        ],\r\n        \"actions\": [\r\n          {\r\n            \"type\": \"Action.Submit\",\r\n            \"title\": \"OK\"\r\n          }\r\n        ]\r\n      }\r\n    }\r\n  ]\r\n}\r\n";
            string video = "{ \"type\": \"Media\", \"poster\": \"https://adaptivecards.io/content/poster-video.png\", \"sources\": [ { \"mimeType\": \"video /mp4\", \"url\": \"https://adaptivecardsblob.blob.core.windows.net/assets/AdaptiveCardsOverviewVideo.mp4\" } ]   }";
            string cardVideo = "{ \"$schema \": \"http://adaptivecards.io/schemas/adaptive-card.json\",\"type\": \"AdaptiveCard\",  \"version\": \"1.3\",\"body\": [" + video + " ]}";

            try
            {
                var parsedResult = AdaptiveCard.FromJson(card);
                var attachment = new Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = parsedResult.Card,
                };
                var taskInfo = new TaskModuleTaskInfo();

                taskInfo.Card = attachment;
                SetTaskInfo(taskInfo, TaskModuleUIConstants.AdaptiveCard);

                return Task.FromResult(taskInfo.ToTaskModuleResponse());

                //return Task.FromResult(this.GetTaskModuleResponse("https://microsoft.com",
                //    "microsoft task module", attachment));
            }
            catch (AdaptiveSerializationException e)
            {
                // handle JSON parsing error
                // or, re-throw
                throw;
            }
        }


        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleSubmitAsync(ITurnContext<IInvokeActivity> turnContext,
            TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text("OnTeamsTaskModuleSubmitAsync Value: " + JsonConvert.SerializeObject(taskModuleRequest));
            //await turnContext.SendActivityAsync(reply, cancellationToken);

            var properties = new Dictionary<string, string>
                {
                    { "taskModuleRequest", JsonConvert.SerializeObject(taskModuleRequest) },
                };

            this.botTelemetryClient.TrackEvent("OnTeamsTaskModuleSubmitAsync", properties);

            var activity = turnContext.Activity as IActivity;
            LogActivityTelemetry(activity);

            return TaskModuleResponseFactory.CreateResponse("Thanks!");
        }

        /// <summary>
        /// Log telemetry about the incoming activity.
        /// </summary>
        /// <param name="activity">The activity</param>
        private void LogActivityTelemetry(IActivity activity)
        {
            var fromObjectId = activity.From?.AadObjectId;
            var clientInfoEntity = activity.Entities?.Where(e => e.Type == "clientInfo")?.FirstOrDefault();
            var channelData = activity.GetChannelData<TeamsChannelData>();

            var properties = new Dictionary<string, string>
            {
                { "ActivityId", activity.Id },
                { "ActivityType", activity.Type },
                { "UserAadObjectId", fromObjectId },
                {
                    "ConversationType",
                    string.IsNullOrWhiteSpace(activity.Conversation?.ConversationType) ? "personal" : activity.Conversation.ConversationType
                },
                { "ConversationId", activity.Conversation?.Id },
                { "TeamId", channelData?.Team?.Id },
                { "activity.GetLocale()", activity.GetLocale() },
                { "Locale", clientInfoEntity?.Properties["locale"]?.ToString() },
                { "Country", clientInfoEntity?.Properties["country"]?.ToString() },
                { "TimeZone", clientInfoEntity?.Properties["timezone"]?.ToString() },
                { "Platform", clientInfoEntity?.Properties["platform"]?.ToString() },
            };
            this.botTelemetryClient.TrackEvent("UserActivity", properties);
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
                        Card = attachment
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

        /// <summary>
        /// Handle translate button click.
        /// </summary>
        /// <param name="turnContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            //this.LogActivityTelemetry(turnContext.Activity);

            //if (!string.IsNullOrEmpty(turnContext.Activity.ReplyToId))
            //{
            //    var txt = turnContext.Activity.Text;
            //    dynamic value = turnContext.Activity.Value;

            //    // Check if the activity came from a submit action.
            //    if (string.IsNullOrEmpty(txt) && value != null)
            //    {
            //        var properties = new Dictionary<string, string> { { "translation", turnContext.Activity.Value.ToString() } };
            //        this.botTelemetryClient.TrackEvent("Translation", properties);

            //        string notificationId = value["notificationId"];
            //        bool translation = Convert.ToBoolean(value["translation"]);

            //        // Download serialized AC from blob storage.
            //        var jsonAC = await this.notificationRepo.GetAdaptiveCardAsync(notificationId);
            //        var acResult = AdaptiveCard.FromJson(jsonAC);
            //        var card = acResult.Card;

            //        AdaptiveSubmitAction translateButton = null;
            //        AdaptiveOpenUrlAction openUrlButton = null;

            //        // only first two buttons matter
            //        for (int i = 0; i < card.Actions.Count && i < 2; i++)
            //        {
            //            var action = card.Actions[i];

            //            if (action is AdaptiveSubmitAction) /* translate button */
            //            {
            //                translateButton = action as AdaptiveSubmitAction;
            //            }
            //            else if (action is AdaptiveOpenUrlAction) // standard call to action button
            //            {
            //                openUrlButton = action as AdaptiveOpenUrlAction;
            //            }
            //        }

            //        if (translation)
            //        {
            //            var detectedUserLocale = turnContext.Activity.Locale;
            //            string userLanguage = string.Empty;
            //            if (detectedUserLocale.Contains('-'))
            //            {
            //                userLanguage = detectedUserLocale.Split('-')[0];
            //                var title = card.Body[0] as AdaptiveTextBlock;
            //                if (title != null)
            //                {
            //                    title.Text = await this.translator.TranslateAsync(title.Text, userLanguage);
            //                }

            //                var summary = card.Body[1] as AdaptiveTextBlock;
            //                if (summary != null)
            //                {
            //                    summary.Text = await this.translator.TranslateAsync(summary.Text, userLanguage);
            //                }

            //                if (openUrlButton != null)
            //                {
            //                    openUrlButton.Title = await this.translator.TranslateAsync(openUrlButton.Title, userLanguage);
            //                }
            //            }
            //        }

            //        if (translateButton != null && translation)
            //        {
            //            translateButton.Title = Strings.ShowOriginalButton; // "See original message";
            //            translateButton.DataJson = JsonConvert.SerializeObject(new { notificationId = notificationId, translation = false });
            //        }
            //        else
            //        {
            //            translateButton.Title = Strings.TranslateButton; // "Translate";
            //            translateButton.DataJson = JsonConvert.SerializeObject(new { notificationId = notificationId, translation = true });
            //        }


            //        var adaptiveCardAttachment = new Attachment()
            //        {
            //            ContentType = AdaptiveCard.ContentType,
            //            Content = card,
            //        };

            //        var activity = MessageFactory.Attachment(adaptiveCardAttachment);
            //        activity.Id = turnContext.Activity.ReplyToId;
            //        await turnContext.UpdateActivityAsync(activity, cancellationToken);
            //    }
            //}
            //else
            //{
            //    await base.OnMessageActivityAsync(turnContext, cancellationToken);
            //}
        }

        //public override Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        //{
        //    if (string.IsNullOrEmpty(turnContext.Activity.Text))
        //    {
        //        dynamic value = turnContext.Activity.Value;
        //        if (value != null)
        //        {
        //            if (value["translate"] == "translate")
        //            {
        //                string text = value["translate"];  // The property will be named after your text input's ID                        
        //                turnContext.Activity.Text = text;
        //            }
        //        }
        //    }
        //    return base.OnTurnAsync(turnContext, cancellationToken);
        //}

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

    public class UISettings
    {
        public UISettings(int width, int height, string title, string id, string buttonTitle)
        {
            Width = width;
            Height = height;
            Title = title;
            Id = id;
            ButtonTitle = buttonTitle;
        }

        public int Height { get; set; }
        public int Width { get; set; }
        public string Title { get; set; }
        public string ButtonTitle { get; set; }
        public string Id { get; set; }
    }

    public static class TaskModuleUIConstants
    {
        public static UISettings YouTube { get; set; } =
            new UISettings(1000, 700, "You Tube Video", TaskModuleIds.YouTube, "You Tube");
        public static UISettings CustomForm { get; set; } =
            new UISettings(510, 450, "Custom Form", TaskModuleIds.CustomForm, "Custom Form");
        public static UISettings AdaptiveCard { get; set; } =
            new UISettings(400, 400, "Adaptive Card: Inputs", TaskModuleIds.AdaptiveCard, "Adaptive Card");
    }

    public static class TaskModuleIds
    {
        public const string YouTube = "YouTube";
        public const string CustomForm = "CustomForm";
        public const string AdaptiveCard = "AdaptiveCard";
    }

    public static class TaskModuleResponseFactory
    {
        public static TaskModuleResponse CreateResponse(string message)
        {
            return new TaskModuleResponse
            {
                Task = new TaskModuleMessageResponse()
                {
                    Value = message,
                },
            };
        }

        public static TaskModuleResponse CreateResponse(TaskModuleTaskInfo taskInfo)
        {
            return new TaskModuleResponse
            {
                Task = new TaskModuleContinueResponse()
                {
                    Value = taskInfo,
                },
            };
        }

        public static TaskModuleResponse ToTaskModuleResponse(this TaskModuleTaskInfo taskInfo)
        {
            return CreateResponse(taskInfo);
        }
    }
}