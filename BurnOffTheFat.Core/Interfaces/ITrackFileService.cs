using BurnOffTheFat.Core.Models;

namespace BurnOffTheFat.Core.Interfaces;
/// <summary>
/// Методы 
/// </summary>
public interface ITrackFileService
{
    /// <summary>
    /// Создание GPX файла трека
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    Task<FileSaveResult> CreateGpxFromTextAsync(string fileName, string content);
    /// <summary>
    /// Создание файла трека в любом доступном месте файловой системы
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    Task<FileSaveResult> CreateFileFromTextAsync(string fileName, string content, CancellationToken ct);
    /// <summary>
    /// Чтение файла GPX в массив точек
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    Task<List<Location>> ReadGpxFile(Stream fileName);
}
