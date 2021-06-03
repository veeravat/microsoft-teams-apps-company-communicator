using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Translator
{
    /// <summary>
    /// Array of translated results from Translator API v3.
    /// </summary>
    internal class TranslationResponse
    {
        [JsonProperty("translations")]
        public IEnumerable<TranslationResult> Translations { get; set; }
    }
}
