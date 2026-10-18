namespace GmailAutoSender.Core.Models;

public sealed class EmailDraftRequest
{
    public string RecipientEmail { get; init; } = string.Empty;

    public string Subject { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public IReadOnlyCollection<EmailAttachment> Attachments
    { get; init; } = Array.Empty<EmailAttachment>();
}
