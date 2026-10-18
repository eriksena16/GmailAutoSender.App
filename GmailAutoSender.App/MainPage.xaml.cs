
using System.Collections.ObjectModel;
using GmailAutoSender.Core.Interfaces;
using GmailAutoSender.Core.Models;
using Microsoft.Maui.Storage;
using EmailAttachment = GmailAutoSender.Core.Models.EmailAttachment;

namespace GmailAutoSender.App;

public partial class MainPage : ContentPage
{
    private readonly IRecipientImportService _importService;
    private readonly IGmailDraftService _draftService;

    private readonly ObservableCollection<Recipient> _recipients = new();
    private readonly ObservableCollection<Core.Models.EmailAttachment> _attachments = new();

    private bool _isBusy;

    public MainPage(
        IRecipientImportService importService,
        IGmailDraftService draftService)
    {
        InitializeComponent();

        _importService = importService;
        _draftService = draftService;

        RecipientsCollection.ItemsSource = _recipients;
        AttachmentsCollection.ItemsSource = _attachments;
    }

    private async void ImportRecipientsButton_Clicked(
        object sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "Selecione a lista de destinatários",
                    FileTypes = new FilePickerFileType(
                        new Dictionary<DevicePlatform, IEnumerable<string>>
                        {
                            [DevicePlatform.WinUI] = new[]
                            {
                                ".csv", ".xlsx"
                            },
                            [DevicePlatform.Android] = new[]
                            {
                                "text/csv",
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                            },
                            [DevicePlatform.iOS] = new[]
                            {
                                "public.comma-separated-values-text",
                                "org.openxmlformats.spreadsheetml.sheet"
                            },
                            [DevicePlatform.MacCatalyst] = new[]
                            {
                                "public.comma-separated-values-text",
                                "org.openxmlformats.spreadsheetml.sheet"
                            }
                        })
                });

            if (file is null)
                return;

            var localPath = await CopyFileToAppDataAsync(file);

            var result = await _importService.ImportAsync(localPath.FilePath);

            _recipients.Clear();

            foreach (var recipient in result.Recipients)
                _recipients.Add(recipient);

            RecipientCountLabel.Text = _recipients.Count.ToString();
            ImportStatusLabel.Text =
                $"{Path.GetFileName(file.FileName)} · {_recipients.Count} contatos";

            var message = $"{_recipients.Count} destinatário(s) importado(s).";

            if (result.Errors.Count > 0)
            {
                message += $"\n\n{result.Errors.Count} registro(s) inválido(s) ou duplicado(s):\n" +
                           string.Join("\n", result.Errors.Take(10));
            }

            await DisplayAlert("Importação concluída", message, "OK");
            StatusLabel.Text = "Lista de destinatários carregada.";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro na importação", ex.Message, "OK");
        }
    }

    private async void SelectAttachmentsButton_Clicked(
        object sender, EventArgs e)
    {
        try
        {
            var files = await FilePicker.Default.PickMultipleAsync(
                new PickOptions
                {
                    PickerTitle = "Selecione os arquivos para anexar"
                });

            if (files is null)
                return;

            foreach (var file in files)
            {
                var attachment = await CopyFileToAppDataAsync(file);

                if (_attachments.Any(a =>
                    string.Equals(
                        a.FilePath,
                        attachment.FilePath,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                AttachmentCountLabel.Text = _attachments.Count.ToString();
                StatusLabel.Text = $"{_attachments.Count} anexo(s) selecionado(s).";

                _attachments.Add(new EmailAttachment
                {
                    FilePath = attachment.FilePath,
                    FileName = attachment.FileName,
                });
            }

            AttachmentCountLabel.Text = _attachments.Count.ToString();
            StatusLabel.Text = $"{_attachments.Count} anexo(s) selecionado(s).";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Erro nos anexos", ex.Message, "OK");
        }
    }

    private void AttachmentsCollection_SelectionChanged(
        object sender, SelectionChangedEventArgs e)
    {
        // A seleção é utilizada pelo botão de remoção.
    }


    private async void RemoveAttachmentButton_Clicked(
        object sender, EventArgs e)
    {
        if (sender is not Button button ||
            button.CommandParameter is not EmailAttachment attachment)
        {
            return;
        }

        bool confirm = await DisplayAlert(
            "Remover anexo",
            $"Deseja remover '{attachment.FileName}' da lista?",
            "Remover",
            "Cancelar");

        if (!confirm)
            return;

        _attachments.Remove(attachment);

        AttachmentCountLabel.Text = _attachments.Count.ToString();

        StatusLabel.Text = _attachments.Count == 0
            ? "Nenhum anexo selecionado."
            : $"{_attachments.Count} anexo(s) selecionado(s).";
    }


    private async void CreateDraftButton_Clicked(
        object sender, EventArgs e)
    {
        if (_isBusy)
            return;

        var subject = SubjectEntry.Text?.Trim() ?? string.Empty;
        var body = BodyEditor.Text ?? string.Empty;
        var individualEmail = RecipientEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(subject))
        {
            await DisplayAlert("Validação", "Informe o assunto.", "OK");
            return;
        }

        if (_attachments.Count == 0)
        {
            await DisplayAlert(
                "Validação",
                "Selecione pelo menos um arquivo para anexar.",
                "OK");
            return;
        }

        List<Recipient> targets;

        if (!string.IsNullOrWhiteSpace(individualEmail))
        {
            targets = new List<Recipient>
            {
                new() { Email = individualEmail }
            };
        }
        else
        {
            targets = _recipients.ToList();
        }

        if (targets.Count == 0)
        {
            await DisplayAlert(
                "Validação",
                "Informe um destinatário ou importe uma lista.",
                "OK");
            return;
        }

        var confirm = await DisplayAlert(
            "Confirmar criação",
            $"Serão criados {targets.Count} rascunho(s), " +
            $"cada um com {_attachments.Count} anexo(s). " +
            "Nenhum e-mail será enviado automaticamente. Continuar?",
            "Criar rascunhos",
            "Cancelar");

        if (!confirm)
            return;

        try
        {
            SetBusy(true);
            StatusLabel.Text = "Conectando ao Gmail...";
            await EnsureCredentialsAsync();
            await _draftService.InitializeAsync();

            var successful = 0;
            var errors = new List<string>();

            foreach (var recipient in targets)
            {
                try
                {
                    StatusLabel.Text =
                        $"Criando rascunho {successful + 1} de {targets.Count}...";

                    var request = new EmailDraftRequest
                    {
                        RecipientEmail = recipient.Email,
                        Subject = subject,
                        Body = body,
                        Attachments = _attachments.ToList()
                    };

                    await _draftService.CreateDraftAsync(request);
                    successful++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{recipient.Email}: {ex.Message}");
                }
            }

            StatusLabel.Text =
                $"Concluído: {successful} de {targets.Count} rascunho(s) criados.";

            var summary = StatusLabel.Text;

            if (errors.Count > 0)
            {
                summary += "\n\nFalhas:\n" +
                           string.Join("\n", errors.Take(10));
            }

            await DisplayAlert("Resultado", summary, "OK");
        }
        catch (Exception ex)
        {
            StatusLabel.Text = "Não foi possível concluir a operação.";

            await DisplayAlert(
                "Erro",
                ex.Message,
                "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }
    private async Task EnsureCredentialsAsync()
    {
        var destination = Path.Combine(
            FileSystem.AppDataDirectory,
            "credentials.json");

        if (File.Exists(destination))
            return;

        using var source =
            await FileSystem.OpenAppPackageFileAsync("credentials.json");

        using var target = File.Create(destination);

        await source.CopyToAsync(target);
    }

    private async Task<EmailAttachment> CopyFileToAppDataAsync(FileResult file)
    {
        var directory = Path.Combine(
            FileSystem.AppDataDirectory,
            "ImportedFiles");

        Directory.CreateDirectory(directory);

        var originalFileName = Path.GetFileName(file.FileName);

        var destination = Path.Combine(
            directory,
            $"{Guid.NewGuid():N}_{originalFileName}");

        await using var source = await file.OpenReadAsync();
        await using var target = File.Create(destination);

        await source.CopyToAsync(target);

        return new EmailAttachment
        {
            FilePath = destination,
            FileName = originalFileName
        };
    }


    private void SetBusy(bool busy)
    {
        _isBusy = busy;
        CreateDraftsButton.IsEnabled = !busy;
        BusyIndicator.IsVisible = busy;
        BusyIndicator.IsRunning = busy;
    }
}
