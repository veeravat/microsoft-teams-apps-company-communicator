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
        public virtual AdaptiveCard CreateAdaptiveCard(NotificationDataEntity notificationDataEntity, bool translate = false, bool acknowledged = false)
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
                translate,
                notificationDataEntity.Ack,
                acknowledged
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
            bool translate = false,
            bool ack = false,
            bool acknowledged = false)
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
                var adaptiveCoices = new List<AdaptiveChoice>();
                for (int i = 0; i < options.Length; i++)
                {
                    adaptiveCoices.Add(new AdaptiveChoice() { Title = options[i], Value = i.ToString() });
                }

                var choiceSet = new AdaptiveChoiceSetInput
                {
                    Type = AdaptiveChoiceSetInput.TypeName,
                    Id = "PollChoices",
                    IsRequired = true,
                    ErrorMessage = Strings.PollErrorMessageSelectOption,
                    Style = AdaptiveChoiceInputStyle.Expanded,
                    IsMultiSelect = false,
                    Choices = adaptiveCoices,
                };
                card.Body.Add(choiceSet);

                card.Actions.Add(new AdaptiveSubmitAction()
                {
                    Title = "Vote Poll",
                    Id = "votePoll",
                    Data = "votePoll",
                    DataJson = JsonConvert.SerializeObject(
                        new { notificationId = notificationId }),
                });
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
