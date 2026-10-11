using Avalonia.Platform.Storage;

namespace ModScope.App.Services;

/// <summary>
/// Abstraction for user dialog interactions (file open/save) across platforms.
/// </summary>
public interface IStorageDialogService
{
    Task<string?> OpenSettingsFileDialogAsync();
    Task<string?> SaveSettingsFileDialogAsync(string defaultFileName = "modscope_settings.json");
    Task<string?> SaveExportFileDialogAsync(string defaultFileName, string extensionFilter);
}
