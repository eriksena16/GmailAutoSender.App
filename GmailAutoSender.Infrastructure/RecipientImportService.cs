using System.Globalization;
using System.Net.Mail;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using GmailAutoSender.Core.Interfaces;
using GmailAutoSender.Core.Models;

namespace GmailAutoSender.Infrastructure.Services;

public sealed class RecipientImportService : IRecipientImportService
{
    public async Task<ImportResult> ImportAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filePath) ||
            !File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "O arquivo de destinatários não foi encontrado.",
                filePath);
        }

        cancellationToken.ThrowIfCancellationRequested();

        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        var rows = extension switch
        {
            ".csv" => await ImportCsvAsync(filePath, cancellationToken),
            ".xlsx" => ImportExcel(filePath),
            _ => throw new NotSupportedException(
                "Formato não suportado. Utilize CSV ou XLSX.")
        };

        var recipients = new List<Recipient>();
        var errors = new List<string>();
        var seenEmails = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < rows.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = rows[i];
            var email = row.Email.Trim();
            var name = row.Name.Trim();

            if (!IsValidEmail(email))
            {
                errors.Add($"Linha {i + 2}: e-mail inválido '{email}'.");
                continue;
            }

            if (!seenEmails.Add(email))
            {
                errors.Add($"Linha {i + 2}: e-mail duplicado '{email}'.");
                continue;
            }

            recipients.Add(new Recipient
            {
                Name = name,
                Email = email
            });
        }

        return new ImportResult
        {
            Recipients = recipients,
            Errors = errors
        };
    }

    private static async Task<List<RecipientRow>> ImportCsvAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(filePath);

        var config = new CsvConfiguration(
            CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args =>
                args.Header.Trim().ToLowerInvariant()
        };

        using var csv = new CsvReader(reader, config);

        var rows = new List<RecipientRow>();

        await foreach (var row in csv.GetRecordsAsync<RecipientRow>(
                           cancellationToken))
        {
            rows.Add(row);
        }

        return rows;
    }

    private static List<RecipientRow> ImportExcel(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);

        var worksheet = workbook.Worksheet(1);
        var headerRow = worksheet.FirstRowUsed()
            ?? throw new InvalidDataException(
                "A planilha está vazia.");

        var headers = headerRow.CellsUsed().ToDictionary(
            cell => cell.GetString().Trim().ToLowerInvariant(),
            cell => cell.Address.ColumnNumber);

        if (!headers.TryGetValue("name", out var nameColumn) ||
            !headers.TryGetValue("email", out var emailColumn))
        {
            throw new InvalidDataException(
                "A primeira linha deve conter as colunas Name e Email.");
        }

        var rows = new List<RecipientRow>();

        foreach (var row in worksheet.RowsUsed()
                     .Where(r => r.RowNumber() > headerRow.RowNumber()))
        {
            rows.Add(new RecipientRow
            {
                Name = row.Cell(nameColumn).GetString(),
                Email = row.Cell(emailColumn).GetString()
            });
        }

        return rows;
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var address = new MailAddress(email);
            return address.Address.Equals(
                email,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private sealed class RecipientRow
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}