namespace Microsoft.Teams.Apps.CompanyCommunicator.Common.Services.Translator
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface ITranslator
    {
        Task<string> TranslateAsync(string text, string targetLocale,
            CancellationToken cancellationToken = default(CancellationToken));
    }
}
