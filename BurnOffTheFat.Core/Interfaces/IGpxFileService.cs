using BurnOffTheFat.Core.Events;

namespace BurnOffTheFat.Core.Interfaces;
/// <summary>
/// Методы 
/// </summary>
public interface IGpxFileService
{
    /// <summary>
    /// Создание GPX файла трека
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    Task<FileSaveResult> CreateGpxFromTextAsync(string fileName, string content);
}
