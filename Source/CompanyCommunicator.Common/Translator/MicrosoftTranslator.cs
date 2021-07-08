

namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Translator
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;

    /// <summary>
    /// .NET wrapper for Microsoft Cognitive Translator API.
    /// Translator allows to detect source language during translation and translate into 90+ languages.
    /// Cognitive Services translation documentation. <seealso cref="https://docs.microsoft.com/en-us/azure/cognitive-services/translator/quickstart-csharp-translate"/>
    /// </summary>
    public class MicrosoftTranslator : ITranslator
    {
        private const string Host = "https://api.cognitive.microsofttranslator.com";
        private const string Path = "/translate?api-version=3.0";
        private const string UriParams = "&to=";

        private static HttpClient client = new HttpClient();

        private readonly string key;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicrosoftTranslator"/> class.
        /// </summary>
        /// <param name="configuration"></param>
        public MicrosoftTranslator(IConfiguration configuration)
        {
            var key = configuration["TranslatorKey"];
            this.key = key ?? throw new ArgumentNullException(nameof(key));
        }

        /// <summary>
        /// Translate a string.
        /// </summary>
        /// <param name="text">Original string.</param>
        /// <param name="targetLocale">Target locale, for example ru.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<string> TranslateAsync(string text, string targetLocale, CancellationToken cancellationToken = default(CancellationToken))
        {
            var body = new object[] { new { Text = text } };
            var requestBody = JsonConvert.SerializeObject(body);

            using (var request = new HttpRequestMessage())
            {
                var uri = Host + Path + UriParams + targetLocale;
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", this.key);

                var response = await client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"The call to the translation service returned HTTP status code {response.StatusCode}.");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TranslationResponse[]>(responseBody);

                return result?.FirstOrDefault()?.Translations?.FirstOrDefault()?.Text;
            }
        }
    }
}
