using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;

namespace GmailAutoSender.Infrastructure.Services;

public sealed class GoogleOAuthService
{
    private readonly string _credentialsPath;
    private readonly string _tokenDirectory;

    public GoogleOAuthService(
        string credentialsPath,
        string tokenDirectory)
    {
        _credentialsPath = credentialsPath;
        _tokenDirectory = tokenDirectory;
    }

    public async Task<UserCredential> AuthorizeAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_credentialsPath))
        {
            throw new FileNotFoundException(
                "Arquivo OAuth credentials.json não encontrado.",
                _credentialsPath);
        }

        using var stream = File.OpenRead(_credentialsPath);

        var secrets = GoogleClientSecrets
            .FromStream(stream)
            .Secrets;

        return await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            new[] { "https://www.googleapis.com/auth/gmail.compose" },
            "user",
            cancellationToken,
            new FileDataStore(_tokenDirectory, true));
    }
}