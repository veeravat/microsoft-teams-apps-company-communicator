using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Teams.Apps.FAQPlusPlus.Common.Providers.Translator
{
    /// <summary>
    /// Array of translated results from Translator API v3.
    /// </summary>
    internal class TranslatorResponse
    {
        [JsonProperty("translations")]
        public IEnumerable<TranslatorResult> Translations { get; set; }
    }
}
