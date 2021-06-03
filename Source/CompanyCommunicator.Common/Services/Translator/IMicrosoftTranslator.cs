using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Teams.Apps.FAQPlusPlus.Common.Providers.Translator
{
    public interface IMicrosoftTranslator
    {
        Task<string> TranslateAsync(string text, string targetLocale,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
