using TimerTestApp.Models;
using TimerTestApp.Shared;

namespace TimerTestApp.Services;

public interface ITimerService : IComputeService
{
    Task<TimerState> GetState(CancellationToken cancellationToken);
    Task<IReadOnlyList<TimerRecord>> GetRecords(CancellationToken cancellationToken);
    Task Start(int duration);
    Task Pause();
    Task Reset();
}