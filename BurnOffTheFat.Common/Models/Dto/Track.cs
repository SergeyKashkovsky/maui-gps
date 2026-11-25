using BurnOffTheFat.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace BurnOffTheFat.Common.Models.Dto;
/// <summary>
/// Данные трека
/// </summary>
public class Track
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// Заголовок
    /// </summary>
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Дата-время начала
    /// </summary>
    public DateTime StartTime { get; set; }
    /// <summary>
    /// Дата-время завершения
    /// </summary>
    public DateTime? EndTime { get; set; }

    #region Кэшированные показатели для быстрого отображения списка треков
    /// <summary>
    /// Пройденный путь
    /// </summary>
    public double TotalDistanceMeters { get; set; }
    /// <summary>
    /// Продолжительность
    /// </summary>
    public TimeSpan Duration { get; set; }

    #endregion

    /// <summary>
    /// Тип тренировки
    /// </summary>
    public SpeedMode SpeedMode { get; set; }

    #region  Поля для будущей синхронизации с сервером
    /// <summary>
    /// Трек обновлен
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Трек синхронизирован с сервером
    /// </summary>
    public bool IsSynced { get; set; }
    /// <summary>
    /// Трек удален
    /// </summary>
    public bool IsDeleted { get; set; }
    #endregion 

    /// <summary>
    /// Коллекция точек
    /// </summary>
    public List<TrackPoint> Points { get; set; } = new();
}
