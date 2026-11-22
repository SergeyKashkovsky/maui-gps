
using BurnOffTheFat.Common.Interfaces;
using BurnOffTheFat.Common.Models.Dto;
using BurnOffTheFat.Common.Utils;
using BurnOffTheFat.Core.Events;
using BurnOffTheFat.Core.Interfaces;
using System.Diagnostics;

namespace BurnOffTheFat.Core.Services;

/// <summary>
/// Запись трека в фоновом режиме в БД
/// </summary>
public class TrackRecordingCoordinator : IDisposable
{
    private readonly IGpsLocationService _gpsService;
    private readonly ITrackRepository _trackRepository;

    private readonly List<TrackPoint> _pointBuffer = new();
    private readonly SemaphoreSlim _bufferLock = new(1, 1);
    private readonly Stopwatch _durationTimer = new();

    private const int BatchSize = 15;
    private Guid? _currentTrackId;

    public bool IsRecording => _currentTrackId.HasValue;

    public TrackRecordingCoordinator(
        IGpsLocationService gpsService,
        ITrackRepository trackRepository)
    {
        _gpsService = gpsService;
        _trackRepository = trackRepository;

        // Подписываемся на валидные точки от GPS-сервиса
        _gpsService.LocationReceived += OnLocationReceived;
    }
    /// <summary>
    /// Запуск тренировки
    /// </summary>
    /// <param name="user"></param>
    /// <param name="trackTitle"></param>
    /// <returns></returns>
    public async Task<bool> StartWorkoutAsync(UserProfile user, string trackTitle)
    {
        if (IsRecording) return false;

        // 1. Применяем настройки из профиля пользователя к GPS-сервису
        _gpsService.SpeedMode = user.FastTreiningMode;

        // 2. Запускаем запись GPS
        bool started = await _gpsService.StartRecordingAsync();
        if (!started) return false;

        // 3. Создаем заголовок трека в БД
        var track = await _trackRepository.CreateTrackAsync(user.Id, trackTitle);
        _currentTrackId = track.Id;

        await _bufferLock.WaitAsync();
        try { _pointBuffer.Clear(); }
        finally { _bufferLock.Release(); }

        _durationTimer.Restart();
        return true;
    }
    /// <summary>
    /// Обработка события получения точки
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void OnLocationReceived(object? sender, LocationPointEventArgs e)
    {
        if (_currentTrackId == null || !_gpsService.IsRecording || !e.IsPointValid)
            return;

        var loc = e.Location;

        var trackPoint = new TrackPoint
        {
            TrackId = _currentTrackId.Value,
            Latitude = loc.Latitude,
            Longitude = loc.Longitude,
            Altitude = loc.Altitude,
            Speed = loc.Speed,
            Accuracy = loc.Accuracy,
            Timestamp = loc.Timestamp.UtcDateTime
        };

        List<TrackPoint>? batchToFlush = null;

        await _bufferLock.WaitAsync();
        try
        {
            _pointBuffer.Add(trackPoint);

            if (_pointBuffer.Count >= BatchSize)
            {
                batchToFlush = [.. _pointBuffer];
                _pointBuffer.Clear();
            }
        }
        finally
        {
            _bufferLock.Release();
        }

        if (batchToFlush != null)
        {
            await _trackRepository.AddPointsBatchAsync(batchToFlush);
        }
    }
    /// <summary>
    /// Завершение тренировки
    /// </summary>
    /// <returns></returns>
    public async Task StopWorkoutAsync()
    {
        if (_currentTrackId == null) return;

        await _gpsService.StopRecordingAsync();
        _durationTimer.Stop();

        Guid trackId = _currentTrackId.Value;
        _currentTrackId = null;

        await _bufferLock.WaitAsync();
        try
        {
            if (_pointBuffer.Count > 0)
            {
                await _trackRepository.AddPointsBatchAsync(_pointBuffer);
                _pointBuffer.Clear();
            }
        }
        finally
        {
            _bufferLock.Release();
        }

        await _trackRepository.FinishTrackAsync(
            trackId,
            _gpsService.TotalDistance,
            _durationTimer.Elapsed);
    }
    /// <summary>
    /// Восстановление не завершенных треков
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<Track?> RecoverUnfinishedTrackAsync(Guid userId)
    {
        var unfinished = await _trackRepository.GetUnfinishedTrackAsync(userId);
        if (unfinished == null) return null;

        var orderedPoints = unfinished.Points.OrderBy(p => p.Timestamp).ToList();

        // Если трек оборвался сразу после старта и точек почти нет — удаляем
        if (orderedPoints.Count < 2)
        {
            await _trackRepository.DeleteTrackAsync(unfinished.Id);
            return null;
        }

        // Пересчитываем дистанцию и длительность по последней сохраненной точке
        double recoveredDistance = 0;
        for (int i = 1; i < orderedPoints.Count; i++)
        {
            var p1 = orderedPoints[i - 1];
            var p2 = orderedPoints[i];
            recoveredDistance += p1.CalculateDistanceInMeters(p2);
        }

        var lastPointTime = orderedPoints.Last().Timestamp;
        var recoveredDuration = lastPointTime - unfinished.StartTime;
        if (recoveredDuration < TimeSpan.Zero)
            recoveredDuration = TimeSpan.Zero;

        // Закрываем трек, чтобы он появился в истории походов
        await _trackRepository.FinishTrackAsync(unfinished.Id, recoveredDistance, recoveredDuration);

        unfinished.TotalDistanceMeters = recoveredDistance;
        unfinished.Duration = recoveredDuration;
        unfinished.EndTime = lastPointTime.LocalDateTime;

        return unfinished;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _gpsService.LocationReceived -= OnLocationReceived;
        _bufferLock.Dispose();
    }
}
