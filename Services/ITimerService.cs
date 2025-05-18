using Samples.HelloBlazorServer.Models;
using Samples.HelloBlazorServer.Shared;

namespace Samples.HelloBlazorServer.Services;

public interface ITimerService : IComputeService
{
    Task<TimerState> GetState(CancellationToken cancellationToken);
    Task<IReadOnlyList<TimerRecord>> GetRecords(CancellationToken cancellationToken);
    Task Start(int duration);
    Task Pause();
    Task Reset();
}