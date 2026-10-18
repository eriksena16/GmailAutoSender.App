namespace GmailAutoSender.Core.Models;

public sealed class ImportResult
{
    public IReadOnlyList<Recipient> Recipients { get; init; }
        = Array.Empty<Recipient>();

    public IReadOnlyList<string> Errors { get; init; }
        = Array.Empty<string>();

    public bool Success => Errors.Count == 0;
}