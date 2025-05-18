using Microsoft.EntityFrameworkCore;
using System.Timers;
using Timer = System.Timers.Timer;
using TimerTestApp.Data;
using TimerTestApp.Models;
using TimerTestApp.Shared;
using Microsoft.Extensions.DependencyInjection;
using ActualLab.Fusion;

namespace TimerTestApp.Services;

public class TimerService : ITimerService, IDisposable
{
    private readonly object _lock = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private TimerState _state = new TimerState(false, DateTime.MinValue, DateTime.MinValue, false, 0, 0, 0, null);
    private Timer? _timer;
    private ElapsedEventHandler? _timerHandler;

    public TimerService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    [ComputeMethod]
    public virtual async Task<TimerState> GetState(CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            double remainingSeconds;
            if (_state.IsRunning)
            {
                var currentElapsed = (DateTime.Now - _state.LastStartTime.Value).TotalSeconds;
                var totalElapsed = _state.ElapsedSeconds + currentElapsed;
                remainingSeconds = Math.Max(0, _state.TotalDuration - totalElapsed);
                if (remainingSeconds <= 0)
                {
                    var endTime = DateTime.Now;
                    _state = new TimerState(
                        IsRunning: false,
                        StartTime: _state.StartTime,
                        EndTime: endTime,
                        IsCompleted: true,
                        RemainingSeconds: 0,
                        ElapsedSeconds: _state.TotalDuration,
                        TotalDuration: _state.TotalDuration,
                        LastStartTime: null
                    );
                    StopTimer();
                }
                else
                {
                    return _state with { RemainingSeconds = remainingSeconds };
                }
            }
            else
            {
                remainingSeconds = _state.TotalDuration - _state.ElapsedSeconds;
                return _state with { RemainingSeconds = remainingSeconds };
            }
            return _state;
        }
    }

    [ComputeMethod]
    public virtual async Task<IReadOnlyList<TimerRecord>> GetRecords(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.TimerRecords
            .OrderByDescending(r => r.EndTime)
            .Take(10)
            .ToListAsync(cancellationToken);
    }

    public async Task Start(int duration)
    {
        bool shouldStartTimer = false;

        lock (_lock)
        {
            if (_state.IsRunning)
                return;

            if (!_state.IsRunning && _state.StartTime != DateTime.MinValue && !_state.IsCompleted)
            {
                // Resuming
                _state = _state with {
                    IsRunning = true,
                    LastStartTime = DateTime.Now
                };
            }
            else
            {
                // Starting new timer
                var startTime = DateTime.Now;
                _state = new TimerState(
                    IsRunning: true,
                    StartTime: startTime,
                    EndTime: DateTime.MinValue, // Set when completed
                    IsCompleted: false,
                    RemainingSeconds: duration,
                    ElapsedSeconds: 0,
                    TotalDuration: duration,
                    LastStartTime: startTime
                );
            }

            StopTimer();

            _timerHandler = async (sender, e) => await TimerElapsed(sender, e);
            _timer = new Timer(100);
            _timer.Elapsed += _timerHandler;
            _timer.AutoReset = true;
            _timer.Start();

            shouldStartTimer = true;
        }

        if (shouldStartTimer)
        {
            using (Invalidation.Begin())
                await GetState(CancellationToken.None);
        }
    }

    public async Task Pause()
    {
        lock (_lock)
        {
            if (_state.IsRunning)
            {
                var currentElapsed = (DateTime.Now - _state.LastStartTime.Value).TotalSeconds;
                var totalElapsed = _state.ElapsedSeconds + currentElapsed;
                var remainingSeconds = Math.Max(0, _state.TotalDuration - totalElapsed);
                _state = _state with {
                    IsRunning = false,
                    ElapsedSeconds = totalElapsed,
                    RemainingSeconds = remainingSeconds,
                    LastStartTime = null
                };
                StopTimer();
            }
        }

        using (Invalidation.Begin())
            await GetState(CancellationToken.None);
    }

    public async Task Reset()
    {
        lock (_lock)
        {
            _state = new TimerState(false, DateTime.MinValue, DateTime.MinValue, false, 0, 0, 0, null);
            StopTimer();
        }
        using (Invalidation.Begin())
            await GetState(CancellationToken.None);
    }

    private async Task TimerElapsed(object? sender, ElapsedEventArgs e)
    {
        bool shouldSaveRecord = false;
        TimerRecord? newRecord = null;

        lock (_lock)
        {
            if (_state.IsRunning)
            {
                var currentElapsed = (DateTime.Now - _state.LastStartTime.Value).TotalSeconds;
                var totalElapsed = _state.ElapsedSeconds + currentElapsed;
                if (totalElapsed >= _state.TotalDuration)
                {
                    var endTime = DateTime.Now;
                    _state = new TimerState(
                        IsRunning: false,
                        StartTime: _state.StartTime,
                        EndTime: endTime,
                        IsCompleted: true,
                        RemainingSeconds: 0,
                        ElapsedSeconds: _state.TotalDuration,
                        TotalDuration: _state.TotalDuration,
                        LastStartTime: null
                    );
                    newRecord = new TimerRecord
                    {
                        StartTime = _state.StartTime,
                        EndTime = endTime
                    };
                    shouldSaveRecord = true;
                    StopTimer();
                }
            }
        }

        if (shouldSaveRecord && newRecord != null)
        {
            await SaveRecordAsync(newRecord);
        }

        using (Invalidation.Begin())
        {
            await GetState(CancellationToken.None);
            if (shouldSaveRecord)
                await GetRecords(CancellationToken.None);
        }
    }

    private void StopTimer()
    {
        if (_timer != null)
        {
            _timer.Stop();
            if (_timerHandler != null)
                _timer.Elapsed -= _timerHandler;
            _timer.Dispose();
            _timer = null;
            _timerHandler = null;
        }
    }

    private async Task SaveRecordAsync(TimerRecord record)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.TimerRecords.Add(record);
        await dbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        StopTimer();
    }
}