using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Teams.Apps.FAQPlusPlus.Common.Providers.Translator
{
    /// <summary>
    /// Translation result from Translator API v3.
    /// </summary>
    internal class TranslatorResult
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }
    }
}
