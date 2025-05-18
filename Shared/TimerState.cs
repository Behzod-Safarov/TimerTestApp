namespace TimerTestApp.Shared;


public record TimerState(
    bool IsRunning,
    DateTime StartTime,
    DateTime EndTime,
    bool IsCompleted,
    double RemainingSeconds,
    double ElapsedSeconds,
    double TotalDuration,
    DateTime? LastStartTime
);