using System.ComponentModel.DataAnnotations;

namespace BurnOffTheFat.Common.Models.Dto;
/// <summary>
/// МОдель данных точки
/// </summary>
public class TrackPoint: GeoPoint
{
    /// <summary>
    /// Идентификатор
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>
    /// Идентификатор трека
    /// </summary>
    public Guid TrackId { get; set; }

}
