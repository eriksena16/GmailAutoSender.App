using GmailAutoSender.Core.Models;

namespace GmailAutoSender.Core.Interfaces
{
    public interface IRecipientImportService
    {
        Task<ImportResult> ImportAsync(
            string filePath,
            CancellationToken cancellationToken = default);
    }
}
