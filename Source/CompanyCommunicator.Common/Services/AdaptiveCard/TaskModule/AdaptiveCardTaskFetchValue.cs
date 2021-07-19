

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.AdaptiveCard.TaskModule
{
    using Newtonsoft.Json;

    public class AdaptiveCardTaskFetchValue<T>
    {
        [JsonProperty("msteams")]
        public object Type { get; set; } = JsonConvert.DeserializeObject("{\"type\": \"task/fetch\" }");

        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
