using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace ModScope.App.Services;

/// <summary>
/// Avalonia implementation of IStorageDialogService using TopLevel.StorageProvider.
/// </summary>
public class AvaloniaStorageDialogService : IStorageDialogService
{
    private static IStorageProvider? GetStorageProvider()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow != null)
            {
                return TopLevel.GetTopLevel(desktop.MainWindow)?.StorageProvider;
            }
        }
        return null;
    }

    public async Task<string?> OpenSettingsFileDialogAsync()
    {
        var provider = GetStorageProvider();
        if (provider == null) return null;

        var files = await provider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load ModScope Workspace Settings",
            AllowMultiple = false,
            FileTypeFilter = new List<FilePickerFileType>
            {
                new("ModScope Configuration (*.json)")
                {
                    Patterns = new[] { "*.json" },
                    MimeTypes = new[] { "application/json" }
                },
                new("All Files (*.*)")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Count > 0)
        {
            return files[0].Path.LocalPath;
        }

        return null;
    }

    public async Task<string?> SaveSettingsFileDialogAsync(string defaultFileName = "modscope_settings.json")
    {
        var provider = GetStorageProvider();
        if (provider == null) return null;

        var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save ModScope Workspace Settings As",
            DefaultExtension = "json",
            SuggestedFileName = defaultFileName,
            FileTypeChoices = new List<FilePickerFileType>
            {
                new("ModScope Configuration (*.json)")
                {
                    Patterns = new[] { "*.json" },
                    MimeTypes = new[] { "application/json" }
                },
                new("All Files (*.*)")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (file != null)
        {
            return file.Path.LocalPath;
        }

        return null;
    }

    public async Task<string?> SaveExportFileDialogAsync(string defaultFileName, string extensionFilter)
    {
        var provider = GetStorageProvider();
        if (provider == null) return null;

        var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export File",
            DefaultExtension = extensionFilter,
            SuggestedFileName = defaultFileName,
            FileTypeChoices = new List<FilePickerFileType>
            {
                new($"{extensionFilter.ToUpperInvariant()} File (*.{extensionFilter})")
                {
                    Patterns = new[] { $"*.{extensionFilter}" }
                },
                new("All Files (*.*)")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (file != null)
        {
            return file.Path.LocalPath;
        }

        return null;
    }
}
