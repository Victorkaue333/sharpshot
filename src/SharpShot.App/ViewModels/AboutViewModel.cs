using System.Reflection;
using SharpShot.App.Services;

namespace SharpShot.App.ViewModels;

public sealed class AboutViewModel
{
    public const string RepositoryUrl = "https://github.com/Victorkaue333/sharpshot";

    private readonly ShellService _shell;

    public AboutViewModel(ShellService shell)
    {
        _shell = shell;
        OpenRepositoryCommand = new RelayCommand(() => _shell.OpenUrl(RepositoryUrl));
        OpenLicenseCommand = new RelayCommand(() => _shell.OpenUrl(RepositoryUrl + "/blob/main/LICENSE"));
    }

    public static string Version
    {
        get
        {
            var assembly = Assembly.GetExecutingAssembly();
            var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(informational))
            {
                var plus = informational.IndexOf('+');
                return plus > 0 ? informational[..plus] : informational;
            }

            return assembly.GetName().Version?.ToString(3) ?? "0.1.0";
        }
    }

    public string Name => "SharpShot";

    public string VersionText => $"Versão {Version}";

    public string Description => "Utilitário nativo de capturas de tela para Windows 10 e 11. Captura a tela inteira ou uma região selecionada e salva em PNG sem perda de qualidade, na resolução original.";

    public string Technologies => "C# · .NET 10 · WinUI 3 · Windows App SDK · Windows Graphics Capture · Direct3D 11 · Windows Imaging Component";

    public string License => "Licença MIT";

    public string Repository => RepositoryUrl;

    public RelayCommand OpenRepositoryCommand { get; }

    public RelayCommand OpenLicenseCommand { get; }
}
