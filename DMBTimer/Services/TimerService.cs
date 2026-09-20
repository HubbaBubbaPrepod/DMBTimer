using Microsoft.UI.Xaml;
using System;

namespace DMBTimer.Services;

public sealed class TimerService : IDisposable
{
    private readonly DispatcherTimer _timer;
    public event EventHandler<object>? Tick;

    public TimerService()
    {
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
    }

    public void Start() => _timer.Start();
    public void Stop() => _timer.Stop();
    private void OnTimerTick(object? sender, object e) => Tick?.Invoke(sender, e);

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }
}
