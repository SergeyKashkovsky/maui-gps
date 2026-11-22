using BurnOffTheFat.Common.Enums;
using BurnOffTheFat.Common.Models;

namespace BurnOffTheFat.Common.Utils;
/// <summary>
/// Фильтр Калмана для сглаживания трека
/// </summary>
public class RealtimeGpsKalmanFilter
{
    // --- состояние ---
    private double _e, _n, _ve, _vn;          // метры, м/с
    private double _lat0, _lng0;              // опорная точка
    private long _timeStampMs;
    private double _variance = -1;            // признак «не инициализирован»

    // --- ковариация 4x4 (храним как массив 16) ---
    private readonly double[] _p = new double[16];

    private SpeedMode _currentProfile;
    private double _sigmaAccel;               // м/с² — шум ускорения (q)

    private const long ResetTimeoutMs = 30_000;
    private const double MaxAccuracyMeters = 20.0;
    private const double GateSigma = 3.0;      // 3σ по Махаланобису (2 DoF ≈ 9)
    private const double MetersPerDegLat = 111_320.0;

    /// <summary>
    /// Создаёт фильтр с начальным профилем движения.
    /// </summary>
    /// <param name="initialProfile">
    /// Профиль движения, определяющий шум процесса <c>σ_a</c> (см. <see cref="InitProfile"/>).
    /// </param>
    /// <remarks>
    /// Состояние фильтра не инициализировано: первая точка, переданная в <see cref="Process"/>,
    /// задаст опорную точку и начальную ковариацию.
    /// </remarks>
    public RealtimeGpsKalmanFilter(SpeedMode initialProfile)
    {
        _currentProfile = initialProfile;
        InitProfile(initialProfile);
    }

    /// <summary>
    /// Устанавливает шум процесса <c>σ_a</c> (среднеквадратичное ускорение, м/с²) для заданного профиля движения.
    /// </summary>
    /// <param name="profile">Профиль движения.</param>
    /// <remarks>
    /// <para>Значения <c>σ_a</c> подобраны эмпирически под типичную динамику:</para>
    /// <list type="bullet">
    ///   <item><description><see cref="SpeedMode.Walking"/> — 0.5 м/с²;</description></item>
    ///   <item><description><see cref="SpeedMode.Running"/> — 1.5 м/с²;</description></item>
    ///   <item><description><see cref="SpeedMode.Cycling"/> — 2.0 м/с²;</description></item>
    ///   <item><description><see cref="SpeedMode.Motorized"/> — 3.0 м/с².</description></item>
    /// </list>
    /// <para>
    /// Чем больше <c>σ_a</c>, тем сильнее фильтр доверяет измерениям и тем меньше сглаживает
    /// (риск: шумные зигзаги). Чем меньше — тем сильнее сглаживание (риск: «срезание» пути
    /// и занижение пройденного расстояния).
    /// </para>
    /// <para>
    /// Метод можно вызывать в любой момент; изменение вступает в силу со следующего <see cref="Process"/>.
    /// </para>
    /// </remarks>
    public void InitProfile(SpeedMode profile)
    {
        // σ_a — типичное ускорение для режима (м/с²)
        _sigmaAccel = profile switch
        {
            SpeedMode.Walking => 0.5,
            SpeedMode.Running => 1.5,
            SpeedMode.Cycling => 2.0,
            SpeedMode.Motorized => 3.0,
            _ => 3.0
        };
    }

    /// <summary>
    /// Обновляет профиль движения, если он отличается от текущего.
    /// </summary>
    /// <param name="profile">Новый профиль движения.</param>
    /// <remarks>
    /// Если <paramref name="profile"/> совпадает с текущим, метод ничего не делает.
    /// В противном случае вызывает <see cref="InitProfile"/> и обновляет <c>σ_a</c>.
    /// </remarks>
    public void SetProfile(SpeedMode profile)
    {
        if (_currentProfile == profile) return;
        _currentProfile = profile;
        InitProfile(profile);
    }

    /// <summary>
    /// Сбрасывает состояние фильтра.
    /// </summary>
    /// <remarks>
    /// После вызова фильтр считается неинициализированным: следующая точка в <see cref="Process"/>
    /// задаст новую опорную точку и начальную ковариацию. Используется при длительной потере сигнала
    /// (см. <c>ResetTimeoutMs</c>) или принудительно.
    /// </remarks>
    public void Reset()
    {
        _variance = -1;
        Array.Clear(_p, 0, _p.Length);
    }

    /// <summary>
    /// Обрабатывает очередную GPS-точку: выполняет шаг предсказания (predict) и шаг коррекции (update).
    /// </summary>
    /// <param name="newPoint">
    /// Входная точка. Используются поля <c>Latitude</c>, <c>Longitude</c>, <c>Accuracy</c>,
    /// <c>Timestamp</c>, а на этапе инициализации — также <c>Speed</c> и <c>Course</c> (если заданы).
    /// </param>
    /// <returns>
    /// Сглаженные координаты (широта, долгота) в градусах. Если точка отброшена как выброс
    /// или фильтр ещё не инициализирован, возвращается последняя оценка (для неинициализированного —
    /// координаты входной точки).
    /// </returns>
    /// <remarks>
    /// <para>Алгоритм:</para>
    /// <list type="number">
    ///   <item><description>
    ///     Отбраковка по <c>Accuracy</c>: если <c>Accuracy &gt; MaxAccuracyMeters</c>, точка игнорируется.
    ///   </description></item>
    ///   <item><description>
    ///     При разрыве во времени больше <c>ResetTimeoutMs</c> выполняется <see cref="Reset"/>.
    ///   </description></item>
    ///   <item><description>
    ///     При первой точке — инициализация: опорная точка, начальная ковариация <c>P0</c>,
    ///     начальная скорость из <c>Speed/Course</c> (если есть).
    ///   </description></item>
    ///   <item><description>
    ///     Predict: <c>x = F·x</c>, <c>P = F·P·Fᵀ + Q</c> с <c>dt</c> из разности <c>Timestamp</c>.
    ///     При <c>dt ≤ 0</c> (дубликат или обратный ход времени) predict пропускается.
    ///   </description></item>
    ///   <item><description>
    ///     Гейтинг по Махаланобису: если <c>d² = yᵀ·S⁻¹·y &gt; 9</c> (3σ, 2 DoF), точка отбрасывается.
    ///   </description></item>
    ///   <item><description>
    ///     Update: <c>K = P·Hᵀ·S⁻¹</c>, <c>x += K·y</c>, <c>P = (I − K·H)·P</c>, затем симметризация <c>P</c>.
    ///   </description></item>
    /// </list>
    /// <para>
    /// Метод изменяет внутреннее состояние и не является потокобезопасным.
    /// </para>
    /// </remarks>
    public (double Latitude, double Longitude) Process(GeoPoint newPoint)
    {
        var lat = newPoint.Latitude;
        var lng = newPoint.Longitude;
        var accuracy = newPoint.Accuracy ?? 0;
        if (accuracy < 1) accuracy = 1;
        var ts = newPoint.Timestamp.ToUnixTimeMilliseconds();

        if (accuracy > MaxAccuracyMeters)
            return (_variance < 0 ? (lat, lng) : ToLatLng());

        if (_variance >= 0 && (ts - _timeStampMs) > ResetTimeoutMs)
            Reset();

        if (_variance < 0)
        {
            // инициализация
            _lat0 = lat;
            _lng0 = lng;
            _e = 0; _n = 0;
            _ve = 0; _vn = 0;

            // если есть Speed/Course — стартуем сразу с ненулевой скоростью
            if (newPoint.Speed.HasValue && newPoint.Course.HasValue)
            {
                var (vx, vy) = SpeedCourseToEnu(newPoint.Speed.Value, newPoint.Course.Value);
                _ve = vx; _vn = vy;
            }

            _timeStampMs = ts;

            // P0: положение — по accuracy, скорость — широкая
            var r0 = accuracy * accuracy;
            var v0 = 10.0 * 10.0;   // σ_v = 10 м/с
            Array.Clear(_p, 0, _p.Length);
            _p[0] = r0;    // ee
            _p[5] = r0;    // nn
            _p[10] = v0;   // ve ve
            _p[15] = v0;   // vn vn

            _variance = r0;
            return (lat, lng);
        }

        var dt = (ts - _timeStampMs) / 1000.0;
        if (dt <= 0)
        {
            // дубликат/обратный ход времени — только update, без predict
            dt = 0;
        }
        else
        {
            _timeStampMs = ts;
        }

        // ---------- PREDICT ----------
        if (dt > 0)
        {
            // x = F x,  F = [[1,0,dt,0],[0,1,0,dt],[0,0,1,0],[0,0,0,1]]
            _e += _ve * dt;
            _n += _vn * dt;

            // P = F P Fᵀ + Q
            PredictCovariance(dt);
        }

        // ---------- UPDATE ----------
        // измерения: z = [e, n], H = [[1,0,0,0],[0,0,1,0]]
        var r = accuracy * accuracy;

        // инновация (в метрах)
        var (measE, measN) = LatLngToEnu(lat, lng);
        var yE = measE - _e;
        var yN = measN - _n;

        // S = H P Hᵀ + R  (2x2)
        var s00 = _p[0] + r;
        var s11 = _p[5] + r;
        var s01 = _p[1];       // P_en

        // гейтинг по Махаланобису: d² = yᵀ S⁻¹ y  (2 DoF, 99% ≈ 9.21)
        var det = s00 * s11 - s01 * s01;
        if (det <= 0)
            return ToLatLng();
        var inv00 = s11 / det;
        var inv11 = s00 / det;
        var inv01 = -s01 / det;
        var d2 = yE * (inv00 * yE + inv01 * yN) + yN * (inv01 * yE + inv11 * yN);
        if (d2 > GateSigma * GateSigma * 2.0)   // ~9 для 3σ, 2 DoF
            return ToLatLng();

        // K = P Hᵀ S⁻¹  (4x2)
        // Hᵀ = [[1,0],[0,1],[0,0],[0,0]]
        // P Hᵀ = первые два столбца P
        // K[i,0] = (P[i,0]*inv00 + P[i,1]*inv01)
        // K[i,1] = (P[i,0]*inv01 + P[i,1]*inv11)
        var k = new double[8];   // 4x2
        for (int i = 0; i < 4; i++)
        {
            var pi0 = _p[i * 4 + 0];
            var pi1 = _p[i * 4 + 1];
            k[i * 2 + 0] = pi0 * inv00 + pi1 * inv01;
            k[i * 2 + 1] = pi0 * inv01 + pi1 * inv11;
        }

        // x += K y
        _e += k[0] * yE + k[1] * yN;
        _n += k[2] * yE + k[3] * yN;
        _ve += k[4] * yE + k[5] * yN;
        _vn += k[6] * yE + k[7] * yN;

        // P = (I - K H) P
        // K H — 4x4, где столбцы 0 и 2 — это K[:,0], K[:,1]
        // (I - K H) P = P - K (H P),  H P — первые две строки P
        var hp = new double[8];  // 2x4
        for (int j = 0; j < 4; j++)
        {
            hp[0 * 4 + j] = _p[0 * 4 + j];   // строка e
            hp[1 * 4 + j] = _p[1 * 4 + j];   // строка n
        }
        var newP = new double[16];
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                var kh = k[i * 2 + 0] * hp[0 * 4 + j] + k[i * 2 + 1] * hp[1 * 4 + j];
                newP[i * 4 + j] = _p[i * 4 + j] - kh;
            }
        }
        Array.Copy(newP, _p, 16);

        // симметризация (борьба с накоплением ошибок округления)
        Symmetrize();

        return ToLatLng();
    }

    /// <summary>
    /// Выполняет шаг предсказания ковариации: <c>P = F·P·Fᵀ + Q</c>.
    /// </summary>
    /// <param name="dt">Шаг времени в секундах (<c>dt &gt; 0</c>).</param>
    /// <remarks>
    /// <para>
    /// Матрица перехода <c>F = [[1,0,dt,0],[0,1,0,dt],[0,0,1,0],[0,0,0,1]]</c>
    /// (модель постоянной скорости).
    /// </para>
    /// <para>
    /// Матрица шума процесса <c>Q</c> построена для непрерывного белого шума ускорения
    /// с интенсивностью <c>σ_a²</c>:
    /// <c>Q = σ_a² · [[dt⁴/4, 0, dt³/2, 0], [0, dt⁴/4, 0, dt³/2], [dt³/2, 0, dt², 0], [0, dt³/2, 0, dt²]]</c>.
    /// </para>
    /// <para>
    /// После вычисления выполняется симметризация <c>P</c> (см. <see cref="Symmetrize"/>) для борьбы
    /// с накоплением ошибок округления.
    /// </para>
    /// </remarks>
    private void PredictCovariance(double dt)
    {
        // P = F P Fᵀ  (без Q)
        // Проще: вычислим FP, затем (FP)Fᵀ
        var fp = new double[16];
        for (int j = 0; j < 4; j++)
        {
            fp[0 * 4 + j] = _p[0 * 4 + j] + dt * _p[2 * 4 + j]; // e + dt*ve
            fp[1 * 4 + j] = _p[1 * 4 + j] + dt * _p[3 * 4 + j]; // n + dt*vn
            fp[2 * 4 + j] = _p[2 * 4 + j];
            fp[3 * 4 + j] = _p[3 * 4 + j];
        }
        var newP = new double[16];
        for (int i = 0; i < 4; i++)
        {
            newP[i * 4 + 0] = fp[i * 4 + 0] + dt * fp[i * 4 + 2];
            newP[i * 4 + 1] = fp[i * 4 + 1] + dt * fp[i * 4 + 3];
            newP[i * 4 + 2] = fp[i * 4 + 2];
            newP[i * 4 + 3] = fp[i * 4 + 3];
        }

        // Q для модели CV (непрерывный белый шум ускорения)
        var q = _sigmaAccel * _sigmaAccel;
        var dt2 = dt * dt;
        var dt3 = dt2 * dt;
        var dt4 = dt2 * dt2;
        var qEE = q * dt4 / 4.0;
        var qEV = q * dt3 / 2.0;
        var qVV = q * dt2;

        newP[0] += qEE; newP[2] += qEV;
        newP[5] += qEE; newP[7] += qEV;
        newP[8] += qEV; newP[10] += qVV;
        newP[13] += qEV; newP[15] += qVV;

        Array.Copy(newP, _p, 16);
        Symmetrize();
    }

    /// <summary>
    /// Приводит матрицу ковариации <c>P</c> к симметричному виду: <c>P[i,j] = P[j,i] = (P[i,j] + P[j,i]) / 2</c>.
    /// </summary>
    /// <remarks>
    /// Математически <c>P</c> всегда симметрична, но при вычислениях с плавающей точкой
    /// асимметрия накапливается. Симметризация на каждом шаге предотвращает «расхождение»
    /// фильтра на длинных треках.
    /// </remarks>
    private void Symmetrize()
    {
        for (int i = 0; i < 4; i++)
            for (int j = i + 1; j < 4; j++)
            {
                var m = 0.5 * (_p[i * 4 + j] + _p[j * 4 + i]);
                _p[i * 4 + j] = m;
                _p[j * 4 + i] = m;
            }
    }

    /// <summary>
    /// Переводит географические координаты в локальные метры (восток/север) относительно опорной точки.
    /// </summary>
    /// <param name="lat">Широта, градусы.</param>
    /// <param name="lng">Долгота, градусы.</param>
    /// <returns>Кортеж <c>(E, N)</c> — смещение на восток и на север в метрах.</returns>
    /// <remarks>
    /// Используется плоская аппроксимация: <c>1° широты ≈ 111 320 м</c>,
    /// <c>1° долготы ≈ 111 320 · cos(lat₀)</c>. Для треков до нескольких километров
    /// погрешность менее 0.1%.
    /// </remarks>
    private (double E, double N) LatLngToEnu(double lat, double lng)
    {
        var mLng = MetersPerDegLat * Math.Cos(_lat0 * Math.PI / 180.0);
        return ((lng - _lng0) * mLng, (lat - _lat0) * MetersPerDegLat);
    }

    /// <summary>
    /// Переводит локальные метры (восток/север) обратно в географические координаты.
    /// </summary>
    /// <returns>Кортеж <c>(Latitude, Longitude)</c> в градусах.</returns>
    /// <remarks>Обратное преобразование к <see cref="LatLngToEnu"/>.</remarks>
    private (double Lat, double Lng) ToLatLng()
    {
        var mLng = MetersPerDegLat * Math.Cos(_lat0 * Math.PI / 180.0);
        return (_lat0 + _n / MetersPerDegLat,
                _lng0 + _e / mLng);
    }

    /// <summary>
    /// Переводит скорость и курс в компоненты скорости восток/север.
    /// </summary>
    /// <param name="speed">Скорость, м/с.</param>
    /// <param name="courseDeg">Курс в градусах: от севера по часовой стрелке (как в Android/GPX).</param>
    /// <returns>Кортеж <c>(Vx, Vy)</c> — скорости на восток и на север, м/с.</returns>
    /// <remarks>
    /// <para>
    /// Соглашение: <c>Vx = speed · sin(course)</c>, <c>Vy = speed · cos(course)</c>.
    /// Используется только на этапе инициализации (для начальной скорости фильтра),
    /// если во входной точке заданы <c>Speed</c> и <c>Course</c>.
    /// </para>
    /// <para>
    /// Если источник данных использует другое соглашение (например, курс от востока),
    /// поменяйте местами <c>Sin</c> и <c>Cos</c>.
    /// </para>
    /// </remarks>
    private static (double Vx, double Vy) SpeedCourseToEnu(double speed, double courseDeg)
    {
        var rad = courseDeg * Math.PI / 180.0;
        return (speed * Math.Sin(rad), speed * Math.Cos(rad));
    }
}