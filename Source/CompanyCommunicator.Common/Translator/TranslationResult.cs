namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Translator
{
    using Newtonsoft.Json;

    /// <summary>
    /// Translation result from Translator API v3.
    /// </summary>
    internal class TranslationResult
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }
    }
}
