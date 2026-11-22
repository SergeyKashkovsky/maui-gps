using BurnOffTheFat.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace BurnOffTheFat.Common.Models.Dto;
/// <summary>
/// Профиль пользователя
/// </summary>
public class UserProfile
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Имя пользователя (логин)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;
    /// <summary>
    /// Режим быстрого начала тренировки
    /// </summary>
    public SpeedMode FastTreiningMode { get; set; }
    /// <summary>
    /// Дата-время последней синхронизации с сервером
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Признак синхронизации
    /// </summary>
    public bool IsSynced { get; set; }
    /// <summary>
    /// Коллекция треков
    /// </summary>
    public List<Track> Tracks { get; set; } = new();
}
