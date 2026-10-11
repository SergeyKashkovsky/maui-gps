using BurnOffTheFat.Core.Interfaces;
using BurnOffTheFat.Core.Models;
using BurnOffTheFat.Core.Utils;
using CommunityToolkit.Maui.Storage;
using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace BurnOffTheFat.Core.Services;
/// <summary>
/// Сервис для работы с файлами треков
/// </summary>
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
    /// <inheritdoc/>
    public async Task<List<Location>> ReadGpxFile(Stream stream)
    {
        if (stream == null) return new();

        try
        {
            // Загружаем XML асинхронно
            var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

            return LocationUtils.ReadGpx(doc);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[GPX Reader] Ошибка парсинга файла: {ex.Message}");
            throw; // Перенаправляем ошибку выше для отображения в UI
        }
    }
}
