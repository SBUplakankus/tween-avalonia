using System;
using System.Collections.Generic;
using System.Diagnostics;
using Avalonia.Threading;

namespace TweenAvalonia;

/// <summary>
/// Shared ticker that drives every running tween: one loop for all tweens
/// instead of one timer per animation. Ticks on the UI thread at ~60 Hz via a
/// dispatcher timer at Render priority and sleeps (no timer, no allocations)
/// while nothing animates. Zero configuration: the engine starts and stops
/// itself as tweens come and go.
/// </summary>
public sealed class TweenEngine
{
    /// <summary>
    /// The shared engine instance.
    /// </summary>
    public static TweenEngine Instance { get; } = new();

    /// <summary>
    /// Longest allowed tick delta; larger gaps (stalls, debugger breaks) are
    /// clamped so tweens never jump wildly.
    /// </summary>
    private static readonly TimeSpan MaxFrameDelta = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Tick interval of the shared dispatcher timer.
    /// </summary>
    private static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(16);

    private readonly List<TweenInstance> _tweens = [];
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private DispatcherTimer? _timer;
    private TimeSpan _lastTickTime;
    private bool _pumping;

    /// <summary>
    /// Disables the automatic ticker (unit tests drive <see cref="Update"/> manually).
    /// </summary>
    internal bool AutoPumpEnabled = true;

    /// <summary>
    /// Number of tweens currently running (or paused) in the engine.
    /// </summary>
    public int ActiveCount => _tweens.Count;

    /// <summary>
    /// Highest number of simultaneously alive tweens seen this session.
    /// </summary>
    public int MaxActiveCount { get; private set; }

    private TweenEngine()
    {
    }

    /// <summary>
    /// Advances every active tween by the given time. Called automatically from
    /// the ticker; public so tests (and any custom pump) can drive it.
    /// </summary>
    public void Update(TimeSpan delta)
    {
        if (_tweens.Count == 0)
        {
            return;
        }

        if (delta < TimeSpan.Zero)
        {
            delta = TimeSpan.Zero;
        }
        else if (delta > MaxFrameDelta)
        {
            delta = MaxFrameDelta;
        }

        for (int i = _tweens.Count - 1; i >= 0; i--)
        {
            TweenInstance tween = _tweens[i];
            tween.Tick(delta);
            if (!tween.IsAlive)
            {
                _tweens.RemoveAt(i);
                Tween.UnregisterIfCurrent(tween);
                tween.ReleaseToPool();
            }
        }

        if (_tweens.Count == 0)
        {
            StopPump();
        }
    }

    internal void Add(TweenInstance instance)
    {
        if (!_tweens.Contains(instance))
        {
            _tweens.Add(instance);
            MaxActiveCount = Math.Max(MaxActiveCount, _tweens.Count);
            Tween.Register(instance);
            EnsurePumping();
        }
    }

    internal void Remove(TweenInstance instance)
    {
        if (_tweens.Remove(instance))
        {
            Tween.UnregisterIfCurrent(instance);
            instance.ReleaseToPool();
            if (_tweens.Count == 0)
            {
                StopPump();
            }
        }
    }

    /// <summary>
    /// Stops every running tween, leaving animated values where they are.
    /// </summary>
    public void StopAll()
    {
        for (int i = _tweens.Count - 1; i >= 0; i--)
        {
            _tweens[i].Stop();
        }
    }

    private void EnsurePumping()
    {
        if (!AutoPumpEnabled || _pumping || _tweens.Count == 0)
        {
            return;
        }

        _pumping = true;
        _lastTickTime = _stopwatch.Elapsed;
        _timer ??= new DispatcherTimer(TickInterval, DispatcherPriority.Render, (_, _) => OnTick());
        _timer.Start();
    }

    private void OnTick()
    {
        if (!_pumping)
        {
            return;
        }

        TimeSpan now = _stopwatch.Elapsed;
        Update(now - _lastTickTime);
        _lastTickTime = now;

        if (_tweens.Count == 0)
        {
            StopPump();
        }
    }

    private void StopPump()
    {
        _pumping = false;
        _timer?.Stop();
    }
}
