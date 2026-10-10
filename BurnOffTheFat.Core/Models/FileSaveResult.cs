namespace BurnOffTheFat.Core.Models;

/// <summary>
/// Результат сохранения файла
/// </summary>
/// <param name="Success"></param>
/// <param name="FilePath"></param>
/// <param name="Message"></param>
public record FileSaveResult(bool Success, string? FilePath, string Message);
