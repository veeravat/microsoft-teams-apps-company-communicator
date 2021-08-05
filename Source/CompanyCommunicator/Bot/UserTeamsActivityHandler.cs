// <copyright file="UserTeamsActivityHandler.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Bot
{
    using System;
    using System.Text.RegularExpressions;
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
        private readonly ISendingNotificationDataRepository sendingNotificationRepo;
        private readonly AdaptiveCardCreator adaptiveCardCreator;
        private readonly ITranslator translator;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTeamsActivityHandler"/> class.
        /// </summary>
        /// <param name="teamsDataCapture">Teams data capture service.</param>
        public UserTeamsActivityHandler(TeamsDataCapture teamsDataCapture,
            ITranslator translator,
            ISendingNotificationDataRepository sendingNotificationRepo,
            AdaptiveCardCreator adaptiveCardCreator)
        {
            this.sendingNotificationRepo = sendingNotificationRepo ?? throw new ArgumentNullException(nameof(sendingNotificationRepo));
            this.teamsDataCapture = teamsDataCapture ?? throw new ArgumentNullException(nameof(teamsDataCapture));
            this.translator = translator ?? throw new ArgumentException(nameof(translator));
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
                    bool translation = Convert.ToBoolean(value["translation"]);

                    // Download serialized AC from blob storage.
                    var jsonAC = await this.sendingNotificationRepo.GetAdaptiveCardAsync(notificationId);

                    // remove base64 encoding as it is too verbose and will not meet message size limit of 64kb from audit service
                    // ex: "imagePath": "data:image/png;name=..png;base64,iVBORw0KGgoAAAANS" -> "imagePath": "data:image/png;name=..png;base64,<encoded>"
                    var regex = new Regex(";base64,([^\"]+)\"", RegexOptions.Compiled);
                    jsonAC = regex.Replace(jsonAC, ";base64,<encoded>\"");

                    var acResult = AdaptiveCard.FromJson(jsonAC);
                    var card = acResult.Card;

                    AdaptiveSubmitAction translateButton = null;
                    AdaptiveOpenUrlAction openUrlButton = null;
                    for (int i = 0; i < card.Actions.Count; i++)
                    {
                        var action = card.Actions[i];
                        if (action is AdaptiveSubmitAction)
                        {
                            translateButton = action as AdaptiveSubmitAction;
                        }
                        else if (action is AdaptiveOpenUrlAction)
                        {
                            openUrlButton = action as AdaptiveOpenUrlAction;
                        }
                    }

                    if (translation)
                    {
                        var detectedUserLocale = turnContext.Activity.Locale;
                        string userLanguage = string.Empty;
                        if (detectedUserLocale.Contains('-'))
                        {
                            userLanguage = detectedUserLocale.Split('-')[0];
                        }

                        var title = card.Body[0] as AdaptiveTextBlock;
                        if (title != null)
                        {
                            title.Text = await this.translator.TranslateAsync(title.Text, userLanguage);
                        }

                        var summary = card.Body[1] as AdaptiveTextBlock;
                        if (summary != null)
                        {
                            summary.Text = await this.translator.TranslateAsync(summary.Text, userLanguage);
                        }

                        openUrlButton.Title = await this.translator.TranslateAsync(openUrlButton.Title, userLanguage);
                    }

                    if (translateButton != null && translation)
                    {
                        translateButton.Title = Strings.ShowOriginalButton;
                        translateButton.DataJson = JsonConvert.SerializeObject(new { notificationId = notificationId, translation = false });
                    }
                    else
                    {
                        translateButton.Title = Strings.TranslateButton;
                        translateButton.DataJson = JsonConvert.SerializeObject(new { notificationId = notificationId, translation = true });
                    }


                    var adaptiveCardAttachment = new Attachment()
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = card,
                    };

                    var activity = MessageFactory.Attachment(adaptiveCardAttachment);
                    activity.Id = turnContext.Activity.ReplyToId;
                    await turnContext.UpdateActivityAsync(activity, cancellationToken);
                }
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
}