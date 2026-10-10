using BurnOffTheFat.Core.Models;

namespace BurnOffTheFat.Core.Events;

/// <summary>
/// Аргументы события завершения записи трека. TODO: возможно, не нужно
/// </summary>
public class TrackSavedEventArgs : EventArgs
{
    /// <summary>
    /// Количество точек
    /// </summary>
    public int PointCount { get; }
    /// <summary>
    /// Путь к файлу
    /// </summary>
    public string? FilePath { get; }
    /// <summary>
    /// Признак успешной записи
    /// </summary>
    public bool Success { get; }
    /// <summary>
    /// Сообщение
    /// </summary>
    public string Message { get; }

    /// <summary>
    /// Создание из результата сохранения файла
    /// </summary>
    /// <param name="pointsCount"></param>
    /// <param name="result"></param>
    public TrackSavedEventArgs(int pointsCount, FileSaveResult result)
    {
        PointCount = pointsCount;
        FilePath = result.FilePath;
        Success = result.Success;
        Message = result.Message;
    }
    /// <summary>
    /// Создание из сообщения об ошибке
    /// </summary>
    /// <param name="errorMessage"></param>
    public TrackSavedEventArgs(string errorMessage)
    {
        Message = errorMessage;
    }
}
