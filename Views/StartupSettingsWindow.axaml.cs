using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RemoteShouter.Models;
using RemoteShouter.Services;

namespace RemoteShouter.Views;

public partial class StartupSettingsWindow : Window
{
    private readonly StartupSettingsService? _settingsService;

    public StartupSettingsWindow()
    {
        InitializeComponent();
    }

    public StartupSettingsWindow(StartupSettingsService settingsService)
        : this()
    {
        _settingsService = settingsService;
        Icon = new WindowIcon(AppIconFactory.CreateStream());
        SettingsPathText.Text = $"配置文件：{settingsService.SettingsPath}";
        var settings = settingsService.Load();
        AllowLanToggle.IsChecked = settings.AllowLan;
        TrustedProxyIpsBox.Text = settings.TrustedProxyIps;
        AllowTrustedProxySetupToggle.IsChecked = settings.AllowTrustedProxySetup;
        SetupTokenBox.Text = settings.TrustedProxySetupToken;
        CertificatePathBox.Text = settings.HttpsCertificatePath;
        CertificatePasswordBox.Text = settings.HttpsCertificatePassword;
        RequireHttpsToggle.IsChecked = settings.RequireHttps;
        CustomSettingsFolderBox.Text = Path.GetDirectoryName(settingsService.SettingsPath);
        var isDefaultLocation = PathsEqual(settingsService.SettingsPath, settingsService.DefaultSettingsPath);
        DefaultLocationRadioButton.IsChecked = isDefaultLocation;
        CustomLocationRadioButton.IsChecked = !isDefaultLocation;
        LocationFilePathText.Text = $"位置记录：{settingsService.LocationFilePath}";
        UpdateLocationControls();
    }

    private void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_settingsService is null)
        {
            return;
        }

        SaveButton.IsEnabled = false;
        ErrorBorder.IsVisible = false;
        try
        {
            _settingsService.Save(new StartupSettings(
                AllowLanToggle.IsChecked == true,
                TrustedProxyIpsBox.Text?.Trim() ?? string.Empty,
                AllowTrustedProxySetupToggle.IsChecked == true,
                SetupTokenBox.Text ?? string.Empty,
                CertificatePathBox.Text?.Trim() ?? string.Empty,
                CertificatePasswordBox.Text ?? string.Empty,
                RequireHttpsToggle.IsChecked == true), GetSelectedSettingsPath());
            Close(true);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }

    private void CancelButton_OnClick(object? sender, RoutedEventArgs e) => Close(false);

    private void GenerateTokenButton_OnClick(object? sender, RoutedEventArgs e)
    {
        SetupTokenBox.Text = StartupSettingsService.GenerateSetupToken();
        TokenStatusText.Text = "已生成新令牌，保存后生效。";
    }

    private async void CopyTokenButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(SetupTokenBox.Text))
            {
                TokenStatusText.Text = "请先输入或生成令牌。";
                return;
            }

            if (Clipboard is null)
            {
                TokenStatusText.Text = "当前无法使用剪贴板。";
                return;
            }

            await Clipboard.SetTextAsync(SetupTokenBox.Text);
            TokenStatusText.Text = "已复制令牌，请妥善保管剪贴板内容。";
        }
        catch
        {
            ShowError("复制令牌失败，请稍后重试。");
        }
    }

    private void ShowTokenCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        SetupTokenBox.PasswordChar = ShowTokenCheckBox.IsChecked == true ? '\0' : '●';
    }

    private void ShowCertificatePasswordCheckBox_OnClick(object? sender, RoutedEventArgs e)
    {
        CertificatePasswordBox.PasswordChar = ShowCertificatePasswordCheckBox.IsChecked == true ? '\0' : '●';
    }

    private void LocationChoice_OnChanged(object? sender, RoutedEventArgs e) => UpdateLocationControls();

    private void CustomSettingsFolderBox_OnTextChanged(object? sender, TextChangedEventArgs e) => UpdateLocationControls();

    private void UpdateLocationControls()
    {
        if (_settingsService is null)
        {
            return;
        }

        var isCustom = CustomLocationRadioButton.IsChecked == true;
        CustomSettingsFolderBox.IsEnabled = isCustom;
        BrowseSettingsFolderButton.IsEnabled = isCustom;
        try
        {
            SettingsPathText.Text = $"保存到：{GetSelectedSettingsPath()}";
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException)
        {
            SettingsPathText.Text = "请选择有效的配置保存文件夹。";
        }
        catch (InvalidOperationException ex)
        {
            SettingsPathText.Text = ex.Message;
        }
    }

    private string GetSelectedSettingsPath()
    {
        if (_settingsService is null)
        {
            throw new InvalidOperationException("配置服务不可用。");
        }

        if (CustomLocationRadioButton.IsChecked != true)
        {
            return _settingsService.DefaultSettingsPath;
        }

        var folder = CustomSettingsFolderBox.Text?.Trim();
        if (string.IsNullOrEmpty(folder) || !Path.IsPathFullyQualified(folder))
        {
            throw new InvalidOperationException("请选择配置保存文件夹，或填写文件夹的完整路径。");
        }

        return Path.GetFullPath(Path.Combine(folder, "startup-settings.json"));
    }

    private static bool PathsEqual(string left, string right) => string.Equals(
        Path.GetFullPath(left), Path.GetFullPath(right),
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

    private async void BrowseSettingsFolderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "选择配置保存文件夹",
                AllowMultiple = false
            });
            if (folders.Count > 0 && folders[0].TryGetLocalPath() is { } path)
            {
                CustomSettingsFolderBox.Text = path;
                CustomLocationRadioButton.IsChecked = true;
                UpdateLocationControls();
            }
        }
        catch
        {
            ShowError("无法打开文件夹选择器，请直接填写文件夹的完整路径。");
        }
    }

    private async void BrowseCertificateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "选择 HTTPS 证书",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("PFX / PKCS#12 证书") { Patterns = ["*.pfx", "*.p12"] }
                ]
            });
            if (files.Count > 0 && files[0].TryGetLocalPath() is { } path)
            {
                CertificatePathBox.Text = path;
            }
        }
        catch
        {
            ShowError("无法打开证书选择器，请直接填写证书的完整路径。");
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.IsVisible = true;
    }
}
