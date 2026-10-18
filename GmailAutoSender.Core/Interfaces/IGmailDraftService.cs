using GmailAutoSender.Core.Models;

namespace GmailAutoSender.Core.Interfaces
{
    public interface IGmailDraftService
    {
        Task InitializeAsync(
            CancellationToken cancellationToken = default);

        Task<string> CreateDraftAsync(
            EmailDraftRequest request,
            CancellationToken cancellationToken = default);
    }
}
