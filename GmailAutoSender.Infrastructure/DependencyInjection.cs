using GmailAutoSender.Core.Interfaces;
using GmailAutoSender.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GmailAutoSender.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string credentialsPath,
        string tokenDirectory)
    {
        services.AddSingleton(
            new GoogleOAuthService(
                credentialsPath,
                tokenDirectory));

        services.AddSingleton<IRecipientImportService,
            RecipientImportService>();

        services.AddSingleton<IGmailDraftService,
            GmailDraftService>();

        return services;
    }
}