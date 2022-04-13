namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Translator
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Array of translated results from Translator API v3.
    /// </summary>
    internal class TranslationResponse
    {
        [JsonProperty("translations")]
        public IEnumerable<TranslationResult> Translations { get; set; }
    }
}
