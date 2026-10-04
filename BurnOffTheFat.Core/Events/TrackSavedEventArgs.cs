namespace BurnOffTheFat.Core.Events
{
    /// <summary>
    /// Аргументы события завершения записи трека.
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

        public TrackSavedEventArgs(int pointCount, string? filePath, bool success, string message)
        {
            PointCount = pointCount;
            FilePath = filePath;
            Success = success;
            Message = message;
        }
    }
}
