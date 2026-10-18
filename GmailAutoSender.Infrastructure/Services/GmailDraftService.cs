using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using GmailAutoSender.Core.Interfaces;
using GmailAutoSender.Core.Models;
using MimeKit;

namespace GmailAutoSender.Infrastructure.Services;

public sealed class GmailDraftService : IGmailDraftService
{
    private readonly GoogleOAuthService _oauthService;

    private GmailService? _gmailService;

    public GmailDraftService(GoogleOAuthService oauthService)
    {
        _oauthService = oauthService;
    }

    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        if (_gmailService is not null)
            return;

        var credential = await _oauthService.AuthorizeAsync(
            cancellationToken);

        _gmailService = new GmailService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Gmail Auto Sender"
            });
    }



    public async Task<string> CreateDraftAsync(
        EmailDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (_gmailService is null)
        {
            throw new InvalidOperationException(
                "Inicialize a autenticação antes de criar rascunhos.");
        }

        if (string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            throw new ArgumentException(
                "O destinatário é obrigatório.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new ArgumentException(
                "O assunto é obrigatório.");
        }

        if (request.Attachments is null ||
            request.Attachments.Count == 0)
        {
            throw new ArgumentException(
                "Selecione pelo menos um anexo.");
        }

        var recipient = MailboxAddress.Parse(
            request.RecipientEmail.Trim());

        var message = new MimeMessage();
        message.To.Add(recipient);
        message.Subject = request.Subject.Trim();

        var bodyBuilder = new BodyBuilder
        {
            TextBody = request.Body ?? string.Empty
        };

        foreach (var attachment in request.Attachments)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(attachment.FilePath) ||
                !File.Exists(attachment.FilePath))
            {
                throw new FileNotFoundException(
                    "Um dos arquivos selecionados não foi encontrado.",
                    attachment.FilePath);
            }

            var fileName = string.IsNullOrWhiteSpace(attachment.FileName)
                ? Path.GetFileName(attachment.FilePath)
                : attachment.FileName;

            var contentType = MimeTypes.GetMimeType(fileName);

            var mimePart = new MimePart(contentType)
            {
                Content = new MimeContent(
                    File.OpenRead(attachment.FilePath)),
                ContentDisposition = new ContentDisposition(
                    ContentDisposition.Attachment),
                ContentTransferEncoding = ContentEncoding.Base64,
                FileName = fileName
            };

            bodyBuilder.Attachments.Add(mimePart);
        }

        message.Body = bodyBuilder.ToMessageBody();

        using var stream = new MemoryStream();

        await message.WriteToAsync(stream, cancellationToken);

        var rawMessage = Convert.ToBase64String(stream.ToArray())
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        var draft = new Draft
        {
            Message = new Message
            {
                Raw = rawMessage
            }
        };

        var createdDraft = await _gmailService.Users.Drafts
            .Create(draft, "me")
            .ExecuteAsync(cancellationToken);

        return createdDraft.Id;
    }


}