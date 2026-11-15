using System.Text.Json;
using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Common.Models;
using BurnOffTheFat.Common.Utils;
using BurnOffTheFat.Core.Utils;

namespace BurnOffTreFat.Common.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var points = JsonSerializer.Deserialize<List<Location>>(_track);
        GeoPoint? lastPoint = null;
        var totalDistance = 0.0;
        var newTotalDistance = 0.0;
        var filter = new RealtimeGpsKalmanFilter(SpeedMode.Walking);
        var trackPoints = new List<Location>();
        foreach(var point in points!)
        {
            var newPoint = point.ToGeoPoint();
            // получаем все параметры пройденного пути
            var distance = newPoint.GetDistanceM(lastPoint);
            var timeDelta = newPoint.Timestamp.GetTimeDeltaSec(lastPoint?.Timestamp ?? newPoint.Timestamp);
            var currentSpeedMS = newPoint.GetCurrentSpeedMS(lastPoint);
            var acceleration = newPoint.GetAcceleration(lastPoint);
            var log = "";
            // производим грубую фильтрацию
            if (!GeoPointsUtils.CoarsePointFilter(
                newPoint,
                lastPoint,
                currentSpeedMS.SpeedMSToKmH(),
                acceleration,
                distance,
                SpeedMode.Walking,
                out var reason
                ))
            {
                // Если точка нам не подходит, возвращаем параметры старой точки
                log = "не подходит";
                continue;
            }
            // Корректировка фильтром Калмана
            filter.SetProfile(SpeedMode.Walking);
            var kalmansPoint = point.ToGeoPoint();
            (kalmansPoint.Latitude, kalmansPoint.Longitude) = filter.Process(newPoint);
            // Пересчет расстояния и скорости
            var dispersy = kalmansPoint.GetDistanceM(newPoint);
            var newDistance = kalmansPoint.GetDistanceM(lastPoint);
            var newCurrentSpeedMS = kalmansPoint.GetCurrentSpeedMS(lastPoint);
            lastPoint = kalmansPoint;
            newTotalDistance += newDistance;
            trackPoints.Add(kalmansPoint.ToLocation());
        }

        var gpx = LocationUtils.BuildGpx(trackPoints);

    }

    const string _track = "[{\"Timestamp\":\"2026-11-15T09:21:55+00:00\",\"Latitude\":52.28837157,\"Longitude\":104.26700263,\"Altitude\":392.73992919921875,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.2300000190734863,\"Course\":199.3000030517578},{\"Timestamp\":\"2026-11-15T09:21:55.863+00:00\",\"Latitude\":52.2886199,\"Longitude\":104.2675855,\"Altitude\":396.9000244140625,\"Accuracy\":97.24700164794922,\"VerticalAccuracy\":21.928714752197266,\"Speed\":null,\"Course\":null},{\"Timestamp\":\"2026-11-15T09:21:57+00:00\",\"Latitude\":52.28834952,\"Longitude\":104.26700339,\"Altitude\":393.0069274902344,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.913745403289795,\"Speed\":0.8600000143051147,\"Course\":187.8000030517578},{\"Timestamp\":\"2026-11-15T09:21:57.865+00:00\",\"Latitude\":52.2883951,\"Longitude\":104.2675045,\"Altitude\":394.1000061035156,\"Accuracy\":40.56500244140625,\"VerticalAccuracy\":7.4942216873168945,\"Speed\":null,\"Course\":null},{\"Timestamp\":\"2026-11-15T09:21:59+00:00\",\"Latitude\":52.28832757,\"Longitude\":104.26701375,\"Altitude\":392.8277079264323,\"Accuracy\":4.189278602600098,\"VerticalAccuracy\":3.5748088359832764,\"Speed\":0.9399999976158142,\"Course\":176.8000030517578},{\"Timestamp\":\"2026-11-15T09:22:01+00:00\",\"Latitude\":52.28830926,\"Longitude\":104.26700535,\"Altitude\":392.7261454264323,\"Accuracy\":4.347639083862305,\"VerticalAccuracy\":3.9345693588256836,\"Speed\":1.399999976158142,\"Course\":212.6999969482422},{\"Timestamp\":\"2026-11-15T09:22:01.857+00:00\",\"Latitude\":52.2882103,\"Longitude\":104.2672967,\"Altitude\":394.1000061035156,\"Accuracy\":21.722000122070312,\"VerticalAccuracy\":3.654066562652588,\"Speed\":null,\"Course\":null},{\"Timestamp\":\"2026-11-15T09:22:03+00:00\",\"Latitude\":52.28828805,\"Longitude\":104.26699339,\"Altitude\":392.83899943033856,\"Accuracy\":4.329116344451904,\"VerticalAccuracy\":3.9915380477905273,\"Speed\":1.2699999809265137,\"Course\":211.10000610351562},{\"Timestamp\":\"2026-11-15T09:22:05+00:00\",\"Latitude\":52.2882681,\"Longitude\":104.26698486,\"Altitude\":392.8168233235677,\"Accuracy\":4.04299259185791,\"VerticalAccuracy\":3.860785484313965,\"Speed\":1.2799999713897705,\"Course\":207.3000030517578},{\"Timestamp\":\"2026-11-15T09:22:07+00:00\",\"Latitude\":52.28824108,\"Longitude\":104.26697566,\"Altitude\":392.89688958062067,\"Accuracy\":4.045466899871826,\"VerticalAccuracy\":3.767116069793701,\"Speed\":1.649999976158142,\"Course\":203.6999969482422},{\"Timestamp\":\"2026-11-15T09:22:07.592+00:00\",\"Latitude\":52.288318,\"Longitude\":104.2674503,\"Altitude\":395.20001220703125,\"Accuracy\":29.280000686645508,\"VerticalAccuracy\":4.617008686065674,\"Speed\":null,\"Course\":null},{\"Timestamp\":\"2026-11-15T09:22:09+00:00\",\"Latitude\":52.28820552,\"Longitude\":104.26697088,\"Altitude\":393.0111488059715,\"Accuracy\":4.04335880279541,\"VerticalAccuracy\":3.679792642593384,\"Speed\":1.5,\"Course\":196.89999389648438},{\"Timestamp\":\"2026-11-15T09:22:11+00:00\",\"Latitude\":52.28818229,\"Longitude\":104.26696279,\"Altitude\":392.9369757852437,\"Accuracy\":3.809497594833374,\"VerticalAccuracy\":3.3241097927093506,\"Speed\":1.2899999618530273,\"Course\":209.10000610351562},{\"Timestamp\":\"2026-11-15T09:22:13+00:00\",\"Latitude\":52.28815718,\"Longitude\":104.26693763,\"Altitude\":392.8322114277278,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.8928043842315674,\"Speed\":1.3799999952316284,\"Course\":221.8000030517578},{\"Timestamp\":\"2026-11-15T09:22:13.511+00:00\",\"Latitude\":52.2882109,\"Longitude\":104.2674513,\"Altitude\":397.9000244140625,\"Accuracy\":29.04599952697754,\"VerticalAccuracy\":10.181758880615234,\"Speed\":null,\"Course\":null},{\"Timestamp\":\"2026-11-15T09:22:15+00:00\",\"Latitude\":52.28812831,\"Longitude\":104.26690823,\"Altitude\":392.7714487463031,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.694467782974243,\"Speed\":1.7100000381469727,\"Course\":215.8000030517578},{\"Timestamp\":\"2026-11-15T09:22:17+00:00\",\"Latitude\":52.28809849,\"Longitude\":104.2668937,\"Altitude\":392.7425240120715,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.4800000190734863,\"Course\":201.6999969482422},{\"Timestamp\":\"2026-11-15T09:22:19+00:00\",\"Latitude\":52.28806955,\"Longitude\":104.2668764,\"Altitude\":392.739186147131,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.3200000524520874,\"Course\":206.1999969482422},{\"Timestamp\":\"2026-11-15T09:22:19.441+00:00\",\"Latitude\":52.2883816,\"Longitude\":104.2675205,\"Altitude\":393.3000183105469,\"Accuracy\":30.573999404907227,\"VerticalAccuracy\":7.559307098388672,\"Speed\":null,\"Course\":null},{\"Timestamp\":\"2026-11-15T09:22:21+00:00\",\"Latitude\":52.28804272,\"Longitude\":104.26686016,\"Altitude\":392.74566771245446,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.3200000524520874,\"Course\":201.8000030517578},{\"Timestamp\":\"2026-11-15T09:22:23+00:00\",\"Latitude\":52.28801475,\"Longitude\":104.26684329,\"Altitude\":392.73091356751047,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.2799999713897705,\"Course\":206},{\"Timestamp\":\"2026-11-15T09:22:25+00:00\",\"Latitude\":52.2879902,\"Longitude\":104.2668252,\"Altitude\":392.6951910725784,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.1799999475479126,\"Course\":217.5},{\"Timestamp\":\"2026-11-15T09:22:24.392+00:00\",\"Latitude\":52.2882377,\"Longitude\":104.267439,\"Altitude\":393.20001220703125,\"Accuracy\":42.229000091552734,\"VerticalAccuracy\":1.7224726676940918,\"Speed\":null,\"Course\":null},{\"Timestamp\":\"2026-11-15T09:22:27+00:00\",\"Latitude\":52.2879662,\"Longitude\":104.26680869,\"Altitude\":392.7167018378952,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.1399999856948853,\"Course\":212.8000030517578},{\"Timestamp\":\"2026-11-15T09:22:29+00:00\",\"Latitude\":52.28793961,\"Longitude\":104.26679462,\"Altitude\":392.7498533830845,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.2699999809265137,\"Course\":199},{\"Timestamp\":\"2026-11-15T09:22:31+00:00\",\"Latitude\":52.28791373,\"Longitude\":104.26677774,\"Altitude\":392.63943846602984,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.3600000143051147,\"Course\":197.60000610351562},{\"Timestamp\":\"2026-11-15T09:22:30.407+00:00\",\"Latitude\":52.2883381,\"Longitude\":104.2673318,\"Altitude\":393.3000183105469,\"Accuracy\":67.22100067138672,\"VerticalAccuracy\":6.769529342651367,\"Speed\":null,\"Course\":null},{\"Timestamp\":\"2026-11-15T09:22:33+00:00\",\"Latitude\":52.28788739,\"Longitude\":104.26675613,\"Altitude\":392.68434548941224,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.340000033378601,\"Course\":203},{\"Timestamp\":\"2026-11-15T09:22:35+00:00\",\"Latitude\":52.28786084,\"Longitude\":104.26674169,\"Altitude\":392.595885493974,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.6270511150360107,\"Speed\":1.1399999856948853,\"Course\":191.6999969482422},{\"Timestamp\":\"2026-11-15T09:22:37+00:00\",\"Latitude\":52.28784548,\"Longitude\":104.26674026,\"Altitude\":392.5722850036597,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":0.8899999856948853,\"Course\":182.8000030517578},{\"Timestamp\":\"2026-11-15T09:22:36.38+00:00\",\"Latitude\":52.2882243,\"Longitude\":104.2672487,\"Altitude\":392.6000061035156,\"Accuracy\":37.45399856567383,\"VerticalAccuracy\":3.544828414916992,\"Speed\":null,\"Course\":null},{\"Timestamp\":\"2026-11-15T09:22:39+00:00\",\"Latitude\":52.28782523,\"Longitude\":104.26671434,\"Altitude\":392.5896990924851,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.2400000095367432,\"Course\":205},{\"Timestamp\":\"2026-11-15T09:22:41+00:00\",\"Latitude\":52.28780417,\"Longitude\":104.266686,\"Altitude\":392.74743460785385,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.2599999904632568,\"Course\":202.6999969482422},{\"Timestamp\":\"2026-11-15T09:22:41.826+00:00\",\"Latitude\":52.286941,\"Longitude\":104.2669564,\"Altitude\":420.4000244140625,\"Accuracy\":39.40999984741211,\"VerticalAccuracy\":12.440194129943848,\"Speed\":null,\"Course\":null},{\"Timestamp\":\"2026-11-15T09:22:43+00:00\",\"Latitude\":52.28778658,\"Longitude\":104.2666835,\"Altitude\":392.70282206703814,\"Accuracy\":3.7900924682617188,\"VerticalAccuracy\":2.5,\"Speed\":1.340000033378601,\"Course\":181.5}]";
}
