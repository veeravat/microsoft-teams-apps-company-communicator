// <copyright file="AdaptiveCardCreator.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard
{
    using System;
    using System.Collections.Generic;
    using AdaptiveCards;
    using Microsoft.Bot.Schema;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Repositories.NotificationData;
    using Microsoft.Teams.Apps.CompanyCommunicator.Common.Resources;
    using Newtonsoft.Json;

    /// <summary>
    /// Adaptive Card Creator service.
    /// </summary>
    public class AdaptiveCardCreator
    {
        /// <summary>
        /// Creates an adaptive card.
        /// </summary>
        /// <param name="notificationDataEntity">Notification data entity.</param>
        /// <param name="translate">Translate equals true in case of the Translate Button is ready to translate message.</param>
        /// <returns>An adaptive card.</returns>
        public virtual AdaptiveCard CreateAdaptiveCard(NotificationDataEntity notificationDataEntity, 
            bool translate = false, bool acknowledged = false, 
            bool voted = false, string selectedChoice = "")
        {
            return this.CreateAdaptiveCard(
                notificationDataEntity.Title,
                notificationDataEntity.ImageLink,
                notificationDataEntity.Summary,
                notificationDataEntity.Author,
                notificationDataEntity.ButtonTitle,
                notificationDataEntity.ButtonLink,
                notificationDataEntity.Id,
                notificationDataEntity.PollOptions,
                notificationDataEntity.PollQuizAnswers,
                translate,
                notificationDataEntity.Ack,
                acknowledged,
                notificationDataEntity.IsPollMultipleChoice,
                voted,
                selectedChoice
                );
        }

        /// <summary>
        /// Create an adaptive card instance.
        /// </summary>
        /// <param name="title">The adaptive card's title value.</param>
        /// <param name="imageUrl">The adaptive card's image URL.</param>
        /// <param name="summary">The adaptive card's summary value.</param>
        /// <param name="author">The adaptive card's author value.</param>
        /// <param name="buttonTitle">The adaptive card's button title value.</param>
        /// <param name="buttonUrl">The adaptive card's button url value.</param>
        /// <param name="notificationId">The notification id, required for translation button.</param>
        /// <param name="translate">Translate equals true in case of the Translate Button is ready to translate message.</param>
        /// <returns>The created adaptive card instance.</returns>
        public AdaptiveCard CreateAdaptiveCard(
            string title,
            string imageUrl,
            string summary,
            string author,
            string buttonTitle,
            string buttonUrl,
            string notificationId,
            string pollOptions,
            string pollQuizAnswers,
            bool translate = false,
            bool ack = false,
            bool acknowledged = false,
            bool isMutipleChoice = false,
            bool voted = false,
            string selectedChoice = "")
        {
            var version = new AdaptiveSchemaVersion(1, 3);
            AdaptiveCard card = new AdaptiveCard(version);

            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = title,
                Size = AdaptiveTextSize.ExtraLarge,
                Weight = AdaptiveTextWeight.Bolder,
                Wrap = true,
            });

            if (!string.IsNullOrWhiteSpace(imageUrl))
            {
                var img = new AdaptiveImageWithLongUrl()
                {
                    LongUrl = imageUrl,
                    Spacing = AdaptiveSpacing.Default,
                    Size = AdaptiveImageSize.Stretch,
                    AltText = string.Empty,
                };

                // Image enlarge support for Teams web/desktop client.
                img.AdditionalProperties.Add("msteams", new { AllowExpand = true });

                card.Body.Add(img);
            }

            if (!string.IsNullOrWhiteSpace(summary))
            {
                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = summary,
                    Wrap = true,
                });
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = author,
                    Size = AdaptiveTextSize.Small,
                    Weight = AdaptiveTextWeight.Lighter,
                    Wrap = true,
                });
            }

            if (!string.IsNullOrWhiteSpace(pollOptions) && pollOptions != "[]")
            {
                string[] options = JsonConvert.DeserializeObject<string[]>(pollOptions);
                string[] answers = JsonConvert.DeserializeObject<string[]>(pollQuizAnswers);

                var adaptiveCoices = new List<AdaptiveChoice>();
                for (int i = 0; i < options.Length; i++)
                {
                    string optionTitle = options[i];
                    var result = Array.Find(answers, element => element == i.ToString());
                    if (voted && !string.IsNullOrWhiteSpace(result))
                    {
                        optionTitle = Strings.PollQuizValidAnswer + " " + optionTitle;
                    }

                    adaptiveCoices.Add(new AdaptiveChoice() { Title = optionTitle, Value = i.ToString() });
                }

                var choiceSet = new AdaptiveChoiceSetInput
                {
                    Type = AdaptiveChoiceSetInput.TypeName,
                    Id = "PollChoices",
                    IsRequired = true,
                    ErrorMessage = Strings.PollErrorMessageSelectOption,
                    Style = AdaptiveChoiceInputStyle.Expanded,
                    IsMultiSelect = isMutipleChoice,
                    Choices = adaptiveCoices,
                };

                if (voted)
                {
                    choiceSet.Value = selectedChoice;
                }

                card.Body.Add(choiceSet);

                if (!voted)
                {
                    card.Actions.Add(new AdaptiveSubmitAction()
                    {
                        Title = Strings.PollSubmitVote,
                        Id = "votePoll",
                        Data = "votePoll",
                        DataJson = JsonConvert.SerializeObject(
                        new { notificationId = notificationId }),
                    });
                }
                else
                {
                    card.Body.Add(new AdaptiveTextBlock()
                    {
                        Text = Strings.PollThanks,
                        Size = AdaptiveTextSize.ExtraLarge,
                        Weight = AdaptiveTextWeight.Bolder,
                        Color = AdaptiveTextColor.Good,
                        Wrap = true,
                    });
                }
            }

            if (!string.IsNullOrWhiteSpace(buttonTitle)
                && !string.IsNullOrWhiteSpace(buttonUrl))
            {
                card.Actions.Add(new AdaptiveOpenUrlAction()
                {
                    Title = buttonTitle,
                    Url = new Uri(buttonUrl, UriKind.RelativeOrAbsolute),
                });
            }

            // if (!string.IsNullOrEmpty(notificationId))
            // {
            //    card.Actions.Add(new AdaptiveSubmitAction()
            //    {
            //        Title = !translate ? Strings.TranslateButton : Strings.ShowOriginalButton,
            //        Id = "translate",
            //        Data = "translate",
            //        DataJson = JsonConvert.SerializeObject(
            //            new { notificationId = notificationId, translation = !translate }),
            //    });

            if (ack && !string.IsNullOrWhiteSpace(notificationId))
            {
                card.Body.Add(new AdaptiveTextBlock()
                {
                    Text = acknowledged ? Strings.AckConfirmation : Strings.AckAlert,
                    Size = AdaptiveTextSize.Small,
                    Weight = AdaptiveTextWeight.Lighter,
                    Wrap = true,
                    Id = notificationId,
                });
            }

            if (ack && !acknowledged)
            {
                card.Actions.Add(new AdaptiveSubmitAction()
                {
                    Title = Strings.AckButtonTitle,
                    Id = "acknowledge",
                    Data = "acknowledge",
                    DataJson = JsonConvert.SerializeObject(
                        new { notificationId = notificationId }),
                });
            }

            // Full width Adaptive card.
            // card.AdditionalProperties.Add("msteams", new { width = "full" });

            return card;
        }
    }
}
