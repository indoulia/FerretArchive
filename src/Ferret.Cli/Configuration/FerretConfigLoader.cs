using Microsoft.Extensions.Configuration;

namespace Ferret.Cli.Configuration;

/// <summary>
/// Why: Centralises config loading — ferret.json primary, FERRET_ env vars override, silent defaults when no file.
/// Thread Safety: Single Thread Only.
/// </summary>
internal static class FerretConfigLoader
{
    internal static IConfiguration Load(string? configPath)
    {
        var builder = new ConfigurationBuilder().AddEnvironmentVariables("FERRET_");
        var path = configPath ?? "ferret.json";
        if (File.Exists(path))
        {
            builder.AddJsonFile(path, optional: false, reloadOnChange: false);
        }

        return builder.Build();
    }
}
