using System.Text.Json;
using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Common.Utils;
using BurnOffTheFat.Core.Utils;

namespace BurnOffTreFat.Common.Tests;

/// <summary>
/// Тестируем навигацию
/// </summary>
public class GpsTests
{
    [SetUp]
    public void Setup()
    {
    }

    /// <summary>
    /// Эмуляция записи трека из коллекции точек
    /// </summary>
    [Test]
    public void TrackTest()
    {
        var points = JsonSerializer.Deserialize<List<Location>>(GpsDefaults.RealTrack);
        var totalDistance = 0.0;
        var filter = new RealtimeGpsKalmanFilter(SpeedMode.Walking);
        //filter = null;
        var trackPoints = new List<Location>();
        var speedModel = new List<(double, double)>();
        foreach(var point in points!)
        {
            var filterResult = point.CheckNewPoint(trackPoints, SpeedMode.Walking, filter, 5);
            var log = "";
            if (!filterResult.Success)
            {
                // Если точка нам не подходит, возвращаем параметры старой точки
                log = "не подходит";
                continue;
            }
            // Корректировка фильтром Калмана
            
            totalDistance += filterResult.DistanceM;
            trackPoints.Add(filterResult.Result);
            speedModel.Add(new(filterResult.CurrentSpeed, filterResult.Result.Speed ?? 0));
        }

        var gpx = LocationUtils.BuildGpx(trackPoints);
        var max = speedModel.Max(x=> x.Item1);
        var max2 = speedModel.Max(x => x.Item2);

    }

    
}
