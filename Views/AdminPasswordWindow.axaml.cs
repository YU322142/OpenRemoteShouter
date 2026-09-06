using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using RemoteShouter.Services;

namespace RemoteShouter.Views;

public partial class AdminPasswordWindow : Window
{
    private readonly ShoutServer? _server;
    private bool _dialogResult;

    // Avalonia's runtime XAML loader requires a public parameterless entry
    // point even though production code uses the server-aware constructor.
    public AdminPasswordWindow()
    {
        InitializeComponent();
    }

    public AdminPasswordWindow(ShoutServer server, string operation)
    {
        InitializeComponent();
        _server = server;
        OperationText.Text = operation;
        Opened += (_, _) =>
        {
            PasswordBox.Focus();
        };
    }

    public static async Task<bool> ConfirmAsync(
        ShoutServer server,
        Window? owner,
        string operation)
    {
        var dialog = new AdminPasswordWindow(server, operation);
        return owner is not null
            ? await dialog.ShowDialog<bool>(owner)
            : await ShowWithoutOwnerAsync(dialog);
    }

    private static Task<bool> ShowWithoutOwnerAsync(AdminPasswordWindow dialog)
    {
        var completion = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        dialog.Closed += (_, _) =>
        {
            if (!completion.Task.IsCompleted)
            {
                completion.TrySetResult(dialog._dialogResult);
            }
        };
        dialog.Show();
        return completion.Task;
    }

    private void ConfirmButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var password = PasswordBox.Text ?? string.Empty;
        if (_server is not null && _server.VerifyAnyAdministratorPassword(password))
        {
            _dialogResult = true;
            Close(true);
            return;
        }

        ErrorText.Text = "密码不正确，或没有可用的管理员账户。";
        ErrorText.IsVisible = true;
        PasswordBox.SelectAll();
        PasswordBox.Focus();
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _dialogResult = false;
        Close(false);
    }

    private void PasswordBox_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ConfirmButton_OnClick(sender, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _dialogResult = false;
            Close(false);
            e.Handled = true;
        }
    }
}
