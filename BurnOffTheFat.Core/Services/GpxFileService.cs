using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Models;
using CommunityToolkit.Maui.Storage;
using System.Text;

namespace BurnOffTheFat.Core.Services;

public class GpxFileService : IGpxFileService
{
    /// <inheritdoc/>
    public async Task<FileSaveResult> CreateGpxFromTextAsync(string fileName, string content)
    {
        var utf8NoBom = new UTF8Encoding(false);
        var bytes = utf8NoBom.GetBytes(content);

        using var stream = new MemoryStream(bytes);
        try
        {
            var result = await FileSaver.Default.SaveAsync(fileName, stream, cancellationToken: default);
            return result.IsSuccessful
                ? new FileSaveResult(true, result.FilePath, "OK")
                : new FileSaveResult(false, null, "Сохранение отменено пользователем");
        }
        catch (Exception ex)
        {
            return new FileSaveResult(false, null, $"Ошибка при сохранении: {ex.Message}");
        }
    }
}
