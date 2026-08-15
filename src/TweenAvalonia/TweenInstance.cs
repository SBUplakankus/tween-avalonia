using System;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Threading;

namespace TweenAvalonia;

/// <summary>
/// A single running animation. Owned by <see cref="TweenEngine"/>; created via the
/// <see cref="Tween"/> factory methods and controlled through the returned handle.
/// </summary>
internal abstract class TweenInstance
{
    protected enum State
    {
        Running,
        Paused,
        Stopped,
        Completed
    }

    private readonly TweenEngine _engine;
    private Action? _onComplete;
    private Action<double>? _onUpdate;
    private Action? _deathHook;
    private CancellationTokenRegistration _cancelRegistration;

    protected TimeSpan Elapsed;
    protected TimeSpan Delay;
    protected State CurrentState = State.Running;

    protected TweenInstance(TweenEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// True while the tween is still ticking (or paused) in the engine.
    /// </summary>
    internal bool IsAlive => CurrentState is State.Running or State.Paused;

    /// <summary>
    /// True when the tween was stopped by a cancellation token.
    /// </summary>
    internal bool Canceled { get; private set; }

    /// <summary>
    /// The object the tween writes to, or null for raw value and delay tweens.
    /// </summary>
    internal virtual AvaloniaObject? Target => null;

    /// <summary>
    /// The property the tween writes, or null for raw value and delay tweens.
    /// </summary>
    internal virtual AvaloniaProperty? Property => null;

    internal abstract TimeSpan Duration { get; }

    /// <summary>
    /// Writes the interpolated value for the given active (post-delay) elapsed time
    /// and returns the eased interpolation factor.
    /// </summary>
    internal abstract double WriteCurrentValue(TimeSpan activeElapsed);

    /// <summary>
    /// Writes the exact end value.
    /// </summary>
    internal abstract void WriteEndValue();

    /// <summary>
    /// Active (post-delay) elapsed time; clamped to [0, duration] when set.
    /// </summary>
    internal TimeSpan ElapsedTime
    {
        get => Elapsed > Delay ? Elapsed - Delay : TimeSpan.Zero;
        set
        {
            if (!IsAlive)
            {
                return;
            }

            TimeSpan active = value < TimeSpan.Zero ? TimeSpan.Zero : value > Duration ? Duration : value;
            Elapsed = Delay + active;
            if (active >= Duration)
            {
                WriteEndValue();
            }
            else
            {
                WriteCurrentValue(active);
            }
        }
    }

    /// <summary>
    /// Normalized progress in [0, 1] of the active duration; settable to scrub.
    /// </summary>
    internal double Progress
    {
        get => (double)ElapsedTime.Ticks / Duration.Ticks;
        set => ElapsedTime = TimeSpan.FromTicks((long)(Math.Clamp(value, 0d, 1d) * Duration.Ticks));
    }

    internal void Tick(TimeSpan delta)
    {
        if (CurrentState != State.Running)
        {
            return;
        }

        Elapsed += delta;
        if (Elapsed < Delay)
        {
            return;
        }

        TimeSpan active = Elapsed - Delay;
        if (active >= Duration)
        {
            WriteEndValue();
            _onUpdate?.Invoke(1.0);
            CurrentState = State.Completed;
            RunOnComplete();
            FireDeathHook();
        }
        else
        {
            double factor = WriteCurrentValue(active);
            _onUpdate?.Invoke(factor);
        }
    }

    /// <summary>
    /// Stores the completion callback; invoked exactly once when the tween finishes
    /// naturally. If the tween already completed, the callback runs immediately.
    /// </summary>
    internal void SetOnComplete(Action onComplete)
    {
        if (CurrentState == State.Completed)
        {
            onComplete();
            return;
        }

        _onComplete = onComplete;
    }

    /// <summary>
    /// Stores the per-frame update callback; receives the eased interpolation
    /// factor (0 at start, 1 at the end) after every value write.
    /// </summary>
    internal void SetOnUpdate(Action<double> onUpdate)
    {
        _onUpdate = onUpdate;
    }

    /// <summary>
    /// Registers a hook invoked when the tween dies for any reason (natural
    /// completion, stop, complete, cancellation, superseding). Used by the awaiter
    /// so awaiting a tween never hangs. If the tween is already dead, the hook runs
    /// immediately.
    /// </summary>
    internal void AttachDeathHook(Action hook)
    {
        if (!IsAlive)
        {
            hook();
            return;
        }

        _deathHook = hook;
    }

    /// <summary>
    /// Stops the tween, leaving the animated value where it is.
    /// </summary>
    internal void Stop()
    {
        if (!IsAlive)
        {
            return;
        }

        CurrentState = State.Stopped;
        _engine.Remove(this);
        FireDeathHook();
    }

    /// <summary>
    /// Stops the tween and snaps the animated value to the end value.
    /// </summary>
    internal void Complete()
    {
        if (!IsAlive)
        {
            return;
        }

        WriteEndValue();
        CurrentState = State.Completed;
        _engine.Remove(this);
        FireDeathHook();
    }

    /// <summary>
    /// Pauses the tween; elapsed time is frozen until resumed.
    /// </summary>
    internal void Pause()
    {
        if (CurrentState == State.Running)
        {
            CurrentState = State.Paused;
        }
    }

    /// <summary>
    /// Resumes a paused tween from where it left off.
    /// </summary>
    internal void Resume()
    {
        if (CurrentState == State.Paused)
        {
            CurrentState = State.Running;
        }
    }

    /// <summary>
    /// Restarts the tween from the beginning, re-registering it with the engine
    /// and superseding any newer tween on the same target property.
    /// </summary>
    internal void Restart()
    {
        Elapsed = default;
        CurrentState = State.Running;
        _engine.Add(this);
    }

    /// <summary>
    /// Stops the tween as soon as the token is canceled. If the tween is awaited,
    /// the await throws <see cref="OperationCanceledException"/>.
    /// </summary>
    internal void SetCancellationToken(CancellationToken token)
    {
        if (token.IsCancellationRequested)
        {
            Cancel();
            return;
        }

        _cancelRegistration = token.Register(OnCancelToken);
    }

    private void OnCancelToken()
    {
        try
        {
            Dispatcher dispatcher = Dispatcher.UIThread;
            if (!dispatcher.CheckAccess())
            {
                dispatcher.Post(Cancel, DispatcherPriority.Render);
                return;
            }
        }
        catch
        {
            // No UI dispatcher available (e.g. headless tests): cancel inline.
        }

        Cancel();
    }

    private void Cancel()
    {
        if (!IsAlive)
        {
            return;
        }

        Canceled = true;
        Stop();
    }

    private void FireDeathHook()
    {
        Action? hook = _deathHook;
        _deathHook = null;
        try
        {
            hook?.Invoke();
        }
        catch (Exception ex)
        {
            Tween.RaiseUnhandledException(ex);
        }
    }

    private void RunOnComplete()
    {
        if (_onComplete == null)
        {
            return;
        }

        Action callback = _onComplete;
        _onComplete = null;
        try
        {
            callback();
        }
        catch (Exception ex)
        {
            Tween.RaiseUnhandledException(ex);
        }
    }
}

/// <summary>
/// A single typed running animation: holds the from/to values and writes the
/// interpolated value to a target property or a callback. Generic so per-frame
/// writes stay unboxed for every supported value type.
/// </summary>
internal sealed class TweenInstance<T> : TweenInstance
{
    private readonly AvaloniaObject? _target;
    private readonly AvaloniaProperty? _property;
    private readonly T _from;
    private readonly T _to;
    private readonly TimeSpan _duration;
    private readonly IEasing _easing;
    private readonly Action<T>? _onValueChange;

    internal TweenInstance(
        TweenEngine engine,
        AvaloniaObject? target,
        AvaloniaProperty? property,
        T from,
        T to,
        TimeSpan duration,
        TimeSpan delay,
        IEasing easing,
        Action<T>? onValueChange)
        : base(engine)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Tween duration must be positive.");
        }

        _ = Interpolator<T>.Value; // Fail fast for unsupported value types.
        _target = target;
        _property = property;
        _from = from;
        _to = to;
        _duration = duration;
        Delay = delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        _easing = easing;
        _onValueChange = onValueChange;
    }

    internal override AvaloniaObject? Target => _target;

    internal override AvaloniaProperty? Property => _property;

    internal override TimeSpan Duration => _duration;

    internal override double WriteCurrentValue(TimeSpan activeElapsed)
    {
        double progress = (double)activeElapsed.Ticks / _duration.Ticks;
        double factor = _easing.Ease(progress);
        WriteValue(Interpolator<T>.Value(_from, _to, factor));
        return factor;
    }

    internal override void WriteEndValue() => WriteValue(_to);

    private void WriteValue(T value)
    {
        if (_onValueChange != null)
        {
            _onValueChange(value);
            return;
        }

        if (_target is not { } target || _property is not { } property)
        {
            return;
        }

        // Target-keyed Custom/Delay tweens use a sentinel key, never a real write.
        if (ReferenceEquals(property, Tween.CustomSentinel))
        {
            return;
        }

        // The generic SetValue overloads avoid boxing the value every frame.
        if (property is StyledProperty<T> styled)
        {
            target.SetValue(styled, value);
        }
        else if (property is DirectPropertyBase<T> direct)
        {
            target.SetValue(direct, value);
        }
        else
        {
            target.SetValue((AvaloniaProperty)property, value!);
        }
    }
}
