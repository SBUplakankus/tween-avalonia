using System;
using Avalonia;
using Avalonia.Animation.Easings;

namespace TweenAvalonia;

/// <summary>
/// A single running animation. Owned by <see cref="TweenEngine"/>; created via the
/// <see cref="Tween"/> factory methods and controlled through the returned handle.
/// Instances are pooled per value type: after the first few tweens of a type,
/// starting and finishing tweens allocate nothing. A handle stores the instance
/// plus its <see cref="Version"/>; when the instance is released to the pool the
/// version is bumped, so stale handles can never touch the instance's next owner.
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
    private object? _onCompleteTarget;
    private Action<object?>? _onCompleteCallback;
    private Action<double>? _onUpdate;
    private object? _onUpdateTarget;
    private Action<object?, double>? _onUpdateCallback;
    private Action? _continuation;
    private CancellationToken _cancellationToken;

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
    /// Bumped every time the instance is released back to the pool, so handles
    /// taken before the release can never control the instance's next owner.
    /// </summary>
    internal int Version { get; private set; }

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

        if (_cancellationToken.IsCancellationRequested)
        {
            Canceled = true;
            CurrentState = State.Stopped;
            RunContinuation(TakeContinuation());
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
            _onUpdateCallback?.Invoke(_onUpdateTarget!, 1.0);
            CurrentState = State.Completed;
            RunOnComplete();
            RunContinuation(TakeContinuation());
        }
        else
        {
            double factor = WriteCurrentValue(active);
            _onUpdate?.Invoke(factor);
            _onUpdateCallback?.Invoke(_onUpdateTarget!, factor);
        }
    }

    /// <summary>
    /// Stores the completion callback; invoked exactly once when the tween finishes
    /// naturally. If the tween is no longer alive, the callback runs immediately.
    /// </summary>
    internal void SetOnComplete(Action onComplete)
    {
        if (!IsAlive)
        {
            onComplete();
            return;
        }

        _onComplete = onComplete;
        _onCompleteCallback = null;
        _onCompleteTarget = null;
    }

    /// <summary>
    /// Stores a target-based completion callback (zero-alloc with static lambdas);
    /// invoked exactly once when the tween finishes naturally.
    /// </summary>
    internal void SetOnComplete(object target, Action<object?> onComplete)
    {
        if (!IsAlive)
        {
            onComplete(target);
            return;
        }

        _onCompleteCallback = onComplete;
        _onCompleteTarget = target;
        _onComplete = null;
    }

    /// <summary>
    /// Stores the per-frame update callback; receives the eased interpolation
    /// factor (0 at start, 1 at the end) after every value write.
    /// </summary>
    internal void SetOnUpdate(Action<double> onUpdate)
    {
        _onUpdate = onUpdate;
        _onUpdateCallback = null;
        _onUpdateTarget = null;
    }

    /// <summary>
    /// Stores a target-based per-frame update callback (zero-alloc with static lambdas).
    /// </summary>
    internal void SetOnUpdate(object target, Action<object?, double> onUpdate)
    {
        _onUpdateCallback = onUpdate;
        _onUpdateTarget = target;
        _onUpdate = null;
    }

    /// <summary>
    /// Stores the await continuation; runs when the tween dies for any reason
    /// (completion, stop, complete, cancellation, superseding), so awaiting never
    /// hangs. If the tween is already dead, runs immediately.
    /// </summary>
    internal void SetContinuation(Action continuation)
    {
        if (!IsAlive)
        {
            continuation();
            return;
        }

        _continuation = continuation;
    }

    /// <summary>
    /// Stops the tween as soon as the token is canceled, leaving the animated
    /// value where it is. The token is polled on each tick (no registration, so
    /// no allocation); an already-canceled token stops the tween immediately.
    /// If the tween is awaited, the await throws <see cref="OperationCanceledException"/>.
    /// </summary>
    internal void SetCancellationToken(CancellationToken token)
    {
        _cancellationToken = token;
        if (token.IsCancellationRequested)
        {
            Cancel();
        }
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

        Action? continuation = TakeContinuation();
        CurrentState = State.Stopped;
        _engine.Remove(this);
        RunContinuation(continuation);
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

        Action? continuation = TakeContinuation();
        WriteEndValue();
        CurrentState = State.Completed;
        _engine.Remove(this);
        RunContinuation(continuation);
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
    /// Restarts a live tween from the beginning, re-registering it with the
    /// engine and superseding any newer tween on the same target property.
    /// Completed tweens are pooled and cannot be revived.
    /// </summary>
    internal void Restart()
    {
        if (!IsAlive)
        {
            return;
        }

        Elapsed = default;
        CurrentState = State.Running;
        _engine.Add(this);
    }

    /// <summary>
    /// Clears every callback and timestamp, bumps the version (invalidating all
    /// outstanding handles) and returns the instance to the pool.
    /// </summary>
    internal virtual void ReleaseToPool()
    {
        Version++;
        _onComplete = null;
        _onCompleteTarget = null;
        _onCompleteCallback = null;
        _onUpdate = null;
        _onUpdateTarget = null;
        _onUpdateCallback = null;
        _continuation = null;
        _cancellationToken = default;
        Canceled = false;
        Elapsed = default;
        Delay = default;
        CurrentState = State.Running;
    }

    /// <summary>
    /// Immediate cancellation path (already-canceled token at
    /// <see cref="SetCancellationToken"/> time). The tween is removed from the
    /// engine and the await continuation runs.
    /// </summary>
    private void Cancel()
    {
        if (!IsAlive)
        {
            return;
        }

        Action? continuation = TakeContinuation();
        Canceled = true;
        CurrentState = State.Stopped;
        _engine.Remove(this);
        RunContinuation(continuation);
    }

    private Action? TakeContinuation()
    {
        Action? continuation = _continuation;
        _continuation = null;
        return continuation;
    }

    private static void RunContinuation(Action? continuation)
    {
        try
        {
            continuation?.Invoke();
        }
        catch (Exception ex)
        {
            Tween.RaiseUnhandledException(ex);
        }
    }

    private void RunOnComplete()
    {
        try
        {
            _onComplete?.Invoke();
            _onCompleteCallback?.Invoke(_onCompleteTarget!);
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
/// writes stay unboxed for every supported value type. Instances are pooled in
/// a static per-type free list.
/// </summary>
internal sealed class TweenInstance<T> : TweenInstance
{
    private static TweenInstance<T>? _poolHead;

    private readonly TweenEngine _engine;
    private AvaloniaObject? _target;
    private AvaloniaProperty? _property;
    private T _from;
    private T _to;
    private TimeSpan _duration;
    private IEasing _easing;
    private Action<T>? _onValueChange;
    private object? _onValueTarget;
    private Action<object?, T>? _onValueCallback;
    private TweenInstance<T>? _poolNext;

    internal override AvaloniaObject? Target => _target;

    internal override AvaloniaProperty? Property => _property;

    internal override TimeSpan Duration => _duration;

    private TweenInstance(TweenEngine engine) : base(engine)
    {
        _engine = engine;
        _from = default!;
        _to = default!;
        _easing = null!;
    }

    /// <summary>
    /// Takes an instance from the pool (or allocates one) and configures it for
    /// the given tween. Validation is performed here so pooled reuse skips nothing.
    /// </summary>
    internal static TweenInstance<T> Acquire(
        TweenEngine engine,
        AvaloniaObject? target,
        AvaloniaProperty? property,
        T from,
        T to,
        TimeSpan duration,
        TimeSpan delay,
        IEasing easing,
        Action<T>? onValueChange)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Tween duration must be positive.");
        }

        _ = Interpolator<T>.Value;

        TweenInstance<T>? instance = _poolHead;
        if (instance != null)
        {
            _poolHead = instance._poolNext;
            instance._poolNext = null;
        }
        else
        {
            instance = new TweenInstance<T>(engine);
        }

        instance._target = target;
        instance._property = property;
        instance._from = from;
        instance._to = to;
        instance._duration = duration;
        instance.Delay = delay < TimeSpan.Zero ? TimeSpan.Zero : delay;
        instance._easing = easing;
        instance._onValueChange = onValueChange;
        instance._onValueTarget = null;
        instance._onValueCallback = null;
        return instance;
    }

    /// <summary>
    /// Stores a target-based value writer (zero-alloc with static lambdas).
    /// </summary>
    internal void SetValueChange(object target, Action<object?, T> onValueChange)
    {
        _onValueCallback = onValueChange;
        _onValueTarget = target;
        _onValueChange = null;
    }

    internal override double WriteCurrentValue(TimeSpan activeElapsed)
    {
        double progress = (double)activeElapsed.Ticks / _duration.Ticks;
        double factor = _easing.Ease(progress);
        WriteValue(Interpolator<T>.Value(_from, _to, factor));
        return factor;
    }

    internal override void WriteEndValue() => WriteValue(_to);

    internal override void ReleaseToPool()
    {
        base.ReleaseToPool();
        _target = null;
        _property = null;
        _from = default!;
        _to = default!;
        _duration = default;
        _easing = null!;
        _onValueChange = null;
        _onValueTarget = null;
        _onValueCallback = null;
        _poolNext = _poolHead;
        _poolHead = this;
    }

    private void WriteValue(T value)
    {
        if (_onValueChange != null)
        {
            _onValueChange(value);
            return;
        }

        if (_onValueCallback != null)
        {
            _onValueCallback(_onValueTarget!, value);
            return;
        }

        if (_target is not { } target || _property is not { } property)
        {
            return;
        }

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
