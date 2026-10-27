namespace BurnOffTheFat.Common.Utils;
/// <summary>
/// Утилита для отображения темпа
/// </summary>
public class PaceFormatter
{
    /// <summary>
    /// Форматирует темп (мин/км) из скорости в км/ч как M' SS''.
    /// </summary>
    /// <param name="speedKmh">Мгновенная скорость, км/ч.</param>
    /// <param name="minSpeedKmh">Ниже этой скорости темп не показываем (слишком медленно → ∞).</param>
    public static string Format(double speedKmh, double minSpeedKmh = 0.5)
    {
        if (double.IsNaN(speedKmh) || double.IsInfinity(speedKmh) || speedKmh < minSpeedKmh)
            return "-";

        double paceMinutes = 60.0 / speedKmh;

        // Округляем до целых секунд, потом раскладываем — иначе 59.9 сек станет "59''"
        // при округлении минут, или 60'' → "1' 0''" с погрешностью.
        int totalSeconds = (int)Math.Round(paceMinutes * 60.0, MidpointRounding.AwayFromZero);

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        return $"{minutes}' {seconds:D2}''";
    }

}
