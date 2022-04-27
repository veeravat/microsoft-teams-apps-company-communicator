// <copyright file="BaseNotification.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// </copyright>

namespace Microsoft.Teams.Apps.CompanyCommunicator.Models
{
    using System;

    /// <summary>
    /// Base notification model class.
    /// </summary>
    public class BaseNotification
    {
        /// <summary>
        /// Gets or sets Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets Title value.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the Image Link value.
        /// </summary>
        public string ImageLink { get; set; }

        /// <summary>
        /// Gets or sets the blob name for the image in base64 format.
        /// </summary>
        public string ImageBase64BlobName { get; set; }

        /// <summary>
        /// Gets or sets the Summary value.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the Author value.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the Button Title value.
        /// </summary>
        public string ButtonTitle { get; set; }

        /// <summary>
        /// Gets or sets the Button Link value.
        /// </summary>
        public string ButtonLink { get; set; }

        /// <summary>
        /// Gets or sets the Created DateTime value.
        /// </summary>
        public DateTime CreatedDateTime { get; set; }

        public bool Ack { get; set; }

        public bool InlineTranslation { get; set; }

        ///// <summary>
        ///// Gets or sets the Scheduled Date and Time value.
        ///// </summary>
        public DateTime? ScheduledDateTime { get; set; }

        public bool NotifyUser { get; set; }

        public bool FullWidth { get; set; }

        public string PollOptions { get; set; }

        public string MessageType { get; set; }
        public string PollQuizAnswers { get; set; }

        public bool IsPollQuizMode { get; set; }

        public bool IsPollMultipleChoice { get; set; }
    }
}
