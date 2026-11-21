using BurnOffTheFat.Common.Enums;
using System.Globalization;

namespace GpsTrackerPrototype.Converters;

/// <summary>
/// Преобразование режима тренировки в текст, отображаемый на экране приложения
/// </summary>
public sealed class SpeedModeToDisplayConverter : IValueConverter
{
    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is SpeedMode mode
            ? mode switch
            {
                SpeedMode.Walking => "ходьба",
                SpeedMode.Running => "бег",
                SpeedMode.Cycling => "велосипед",
                SpeedMode.Motorized => "авто/мото",
                _ => mode.ToString()
            }
            : string.Empty;
    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}