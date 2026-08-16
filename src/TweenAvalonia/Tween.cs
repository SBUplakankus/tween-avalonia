using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Animation.Easings;

namespace TweenAvalonia;

/// <summary>
/// Programmatic, controllable tweens for Avalonia properties and raw values.
/// A tween starts as soon as it is created and returns a handle you keep in a
/// variable to stop, start, pause or resume it later. Starting a new tween on
/// the same target property automatically stops the previous one.
/// <para>
/// Only the target and the end value are required — everything else has a
/// sensible default (1 second duration, <see cref="DefaultEasing"/>, no delay),
/// so the common case reads as one line: <c>Tween.Opacity(visual, 0);</c>.
/// </para>
/// <para>
/// Handles are structs wrapping a pooled instance plus a version; once a tween
/// dies the instance returns to the pool and every stale handle becomes inert
/// (except <see cref="OnComplete"/>, which runs immediately, and <c>await</c>,
/// which completes). Tweens are not reusable: start a fresh one in the desired
/// direction instead of trying to revive a completed tween.
/// </para>
/// </summary>
public readonly partial struct Tween
{
    /// <summary>
    /// Easing used when none is specified. Settable so an app can change the
    /// library-wide default once instead of passing <c>easing:</c> everywhere.
    /// </summary>
    public static IEasing DefaultEasing { get; set; } = new SineEaseInOut();

    private static readonly LinearEasing Linear = new();

    /// <summary>
    /// Tracks the newest tween per target property so starting a new one
    /// supersedes (silently stops) the previous one.
    /// </summary>
    private static readonly ConditionalWeakTable<AvaloniaObject, Dictionary<AvaloniaProperty, TweenInstance>> ActiveByTarget = new();

    /// <summary>
    /// Sentinel dictionary key for target-keyed <see cref="Custom{T}(T,T,Action{T},double,IEasing?,double,AvaloniaObject?)"/>
    /// and <see cref="Delay(double,System.Action?,AvaloniaObject?)"/> tweens: gives raw tweens the same
    /// latest-wins and <see cref="StopAll(AvaloniaObject)"/> / <see cref="CompleteAll(AvaloniaObject)"/>
    /// lifecycle as property tweens, without ever writing to the target.
    /// </summary>
    internal static readonly AvaloniaProperty CustomSentinel =
        AvaloniaProperty.Register<AvaloniaObject, double>("TweenCustomSentinel");

    /// <summary>
    /// Raised when a tween callback (OnComplete, OnUpdate, continuation) throws;
    /// the exception is otherwise swallowed so one bad callback can't break the
    /// ticker.
    /// </summary>
    public static event Action<Exception>? UnhandledException;

    private readonly TweenInstance? _instance;
    private readonly int _version;

    internal Tween(TweenInstance? instance)
    {
        _instance = instance;
        _version = instance?.Version ?? 0;
    }

    /// <summary>
    /// True while the handle still points at the live tween it was created from.
    /// </summary>
    private bool IsCurrent => _instance is { } instance && instance.Version == _version;

    /// <summary>
    /// True while the tween is still running (or paused) in the engine.
    /// </summary>
    public bool IsAlive => _instance is { IsAlive: true } instance && instance.Version == _version;

    /// <summary>
    /// The tween's total duration (excluding delay), or zero if the handle is stale.
    /// </summary>
    public TimeSpan Duration => IsCurrent ? _instance!.Duration : TimeSpan.Zero;

    /// <summary>
    /// Active (post-delay) elapsed time. Settable to scrub the animation forward
    /// or backward; the value is snapped to the interpolated position.
    /// </summary>
    public TimeSpan ElapsedTime
    {
        get => IsCurrent ? _instance!.ElapsedTime : TimeSpan.Zero;
        set
        {
            if (IsCurrent)
            {
                _instance!.ElapsedTime = value;
            }
        }
    }

    /// <summary>
    /// Normalized progress in [0, 1] of the active duration. Settable to scrub.
    /// </summary>
    public double Progress
    {
        get => IsCurrent ? _instance!.Progress : 0;
        set
        {
            if (IsCurrent)
            {
                _instance!.Progress = value;
            }
        }
    }

    /// <summary>
    /// Registers a callback invoked exactly once when the tween finishes naturally.
    /// If the tween already completed, the callback runs immediately. Stopped,
    /// canceled or superseded tweens never fire it.
    /// </summary>
    public Tween OnComplete(Action onComplete)
    {
        ArgumentNullException.ThrowIfNull(onComplete);

        if (_instance is { } instance && instance.Version == _version)
        {
            instance.SetOnComplete(onComplete);
        }
        else
        {
            onComplete();
        }

        return this;
    }

    /// <summary>
    /// Registers a target-based completion callback, invoked exactly once when the
    /// tween finishes naturally. Write the callback as a static lambda to keep it
    /// allocation-free: <c>tween.OnComplete(target: this, static t => t.Commit())</c>.
    /// </summary>
    public Tween OnComplete<TTarget>(TTarget target, Action<TTarget> onComplete) where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(onComplete);

        if (_instance is { } instance && instance.Version == _version)
        {
            instance.SetOnComplete(target, CallbackCache.WrapComplete(target, onComplete));
        }
        else
        {
            onComplete(target);
        }

        return this;
    }

    /// <summary>
    /// Registers a per-frame callback invoked after every value write with the
    /// eased interpolation factor (0 at the start, 1 at the end) — useful for
    /// driving dependent animations from this one.
    /// </summary>
    public Tween OnUpdate(Action<double> onUpdate)
    {
        ArgumentNullException.ThrowIfNull(onUpdate);
        if (IsCurrent)
        {
            _instance!.SetOnUpdate(onUpdate);
        }

        return this;
    }

    /// <summary>
    /// Registers a target-based per-frame callback (zero-alloc with a static lambda).
    /// </summary>
    public Tween OnUpdate<TTarget>(TTarget target, Action<TTarget, double> onUpdate) where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(onUpdate);
        if (IsCurrent)
        {
            _instance!.SetOnUpdate(target, CallbackCache.WrapUpdate(target, onUpdate));
        }

        return this;
    }

    /// <summary>
    /// Stops the tween as soon as <paramref name="token"/> is canceled, leaving
    /// the animated value where it is. The token is polled on each tick (no
    /// allocation); cancellation applies within one frame. If the tween is
    /// awaited, the await throws <see cref="OperationCanceledException"/>.
    /// </summary>
    public Tween CancelOn(CancellationToken token)
    {
        if (IsCurrent)
        {
            _instance!.SetCancellationToken(token);
        }

        return this;
    }

    /// <summary>
    /// Stops the tween, leaving the animated value where it is.
    /// </summary>
    public void Stop()
    {
        if (IsCurrent)
        {
            _instance!.Stop();
        }
    }

    /// <summary>
    /// Stops the tween and snaps the animated value to the end value.
    /// </summary>
    public void Complete()
    {
        if (IsCurrent)
        {
            _instance!.Complete();
        }
    }

    /// <summary>
    /// Pauses the tween; elapsed time is frozen until <see cref="Resume"/> is called.
    /// </summary>
    public void Pause()
    {
        if (IsCurrent)
        {
            _instance!.Pause();
        }
    }

    /// <summary>
    /// Resumes a paused tween from where it left off.
    /// </summary>
    public void Resume()
    {
        if (IsCurrent)
        {
            _instance!.Resume();
        }
    }

    /// <summary>
    /// Restarts a still-alive tween from the beginning. Completed tweens are
    /// pooled and cannot be revived — start a fresh tween instead.
    /// </summary>
    public void Start()
    {
        if (IsCurrent)
        {
            _instance!.Restart();
        }
    }

    /// <summary>
    /// Awaits the tween: resumes when it completes naturally, is stopped,
    /// completed, canceled or superseded. Cancellation surfaces as
    /// <see cref="OperationCanceledException"/>. No threads are used; the
    /// awaiter itself is a struct, so awaiting allocates only the async state
    /// machine.
    /// </summary>
    public TweenAwaiter GetAwaiter() => new(_instance, _version);

    /// <summary>
    /// Animates a typed property of <paramref name="target"/> to <paramref name="to"/>,
    /// starting from its current value. <typeparamref name="T"/> is checked at
    /// compile time against the property's value type.
    /// </summary>
    public static Tween To<T>(AvaloniaObject target, AvaloniaProperty<T> property, T to, double duration = 1,
        IEasing? easing = null, double delay = 0)
        => To(target, property, to, Seconds(duration, nameof(duration)), easing, SecondsOrZero(delay, nameof(delay)));

    /// <summary>
    /// Animates a typed property of <paramref name="target"/> to <paramref name="to"/>,
    /// starting from its current value.
    /// </summary>
    public static Tween To<T>(AvaloniaObject target, AvaloniaProperty<T> property, T to, TimeSpan duration,
        IEasing? easing = null, TimeSpan delay = default)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);

        T from = property switch
        {
            StyledProperty<T> styled => target.GetValue(styled),
            DirectPropertyBase<T> direct => target.GetValue(direct),
            _ => (T)target.GetValue(property)!,
        };
        return Start(TweenInstance<T>.Acquire(
            TweenEngine.Instance, target, property, from, to, duration, delay, easing ?? DefaultEasing, null));
    }

    /// <summary>
    /// Animates a typed property of <paramref name="target"/> using reusable settings.
    /// </summary>
    public static Tween To<T>(AvaloniaObject target, AvaloniaProperty<T> property, TweenSettings<T> settings)
        => To(target, property, settings.To, settings.Duration, settings.Easing, settings.Delay);

    /// <summary>
    /// Animates a raw value, invoking <paramref name="onValueChange"/> on every update.
    /// Pass <paramref name="target"/> to give the tween the same latest-wins and
    /// <see cref="StopAll(AvaloniaObject)"/> lifecycle as property tweens: a new tween
    /// with the same target supersedes the previous one, so callers don't need a
    /// stored handle or manual <see cref="Stop"/> bookkeeping.
    /// </summary>
    public static Tween Custom(double from, double to, Action<double> onValueChange, double duration = 1,
        IEasing? easing = null, double delay = 0, AvaloniaObject? target = null)
        => Custom(from, to, onValueChange, Seconds(duration, nameof(duration)), easing,
            SecondsOrZero(delay, nameof(delay)), target);

    /// <summary>
    /// Animates a raw value, invoking <paramref name="onValueChange"/> on every update.
    /// Pass <paramref name="target"/> to give the tween the same latest-wins and
    /// <see cref="StopAll(AvaloniaObject)"/> lifecycle as property tweens.
    /// </summary>
    public static Tween Custom(double from, double to, Action<double> onValueChange, TimeSpan duration,
        IEasing? easing = null, TimeSpan delay = default, AvaloniaObject? target = null)
        => StartCustom(from, to, onValueChange, duration, easing, delay, target);

    /// <summary>
    /// Animates a raw typed value, invoking <paramref name="onValueChange"/> on every update.
    /// Supported value types: double, float, int, Color, Point, Vector, Thickness, Rect.
    /// Pass <paramref name="target"/> to give the tween the same latest-wins and
    /// <see cref="StopAll(AvaloniaObject)"/> lifecycle as property tweens.
    /// </summary>
    public static Tween Custom<T>(T from, T to, Action<T> onValueChange, double duration = 1,
        IEasing? easing = null, double delay = 0, AvaloniaObject? target = null)
        => Custom(from, to, onValueChange, Seconds(duration, nameof(duration)), easing,
            SecondsOrZero(delay, nameof(delay)), target);

    /// <summary>
    /// Animates a raw typed value, invoking <paramref name="onValueChange"/> on every update.
    /// Supported value types: double, float, int, Color, Point, Vector, Thickness, Rect.
    /// Pass <paramref name="target"/> to give the tween the same latest-wins and
    /// <see cref="StopAll(AvaloniaObject)"/> lifecycle as property tweens.
    /// </summary>
    public static Tween Custom<T>(T from, T to, Action<T> onValueChange, TimeSpan duration,
        IEasing? easing = null, TimeSpan delay = default, AvaloniaObject? target = null)
        => StartCustom(from, to, onValueChange, duration, easing, delay, target);

    /// <summary>
    /// Animates a raw value, invoking a target-based <paramref name="onValueChange"/>
    /// on every update. Write the callback as a static lambda
    /// (<c>static (vm, v) =&gt; vm.ArtOpacity = v</c>) to keep it allocation-free.
    /// </summary>
    public static Tween Custom<TTarget>(TTarget target, double from, double to, Action<TTarget, double> onValueChange,
        double duration = 1, IEasing? easing = null, double delay = 0) where TTarget : class
        => Custom(target, from, to, onValueChange, Seconds(duration, nameof(duration)), easing,
            SecondsOrZero(delay, nameof(delay)));

    /// <summary>
    /// Animates a raw value, invoking a target-based <paramref name="onValueChange"/>
    /// on every update (zero-alloc with a static lambda).
    /// </summary>
    public static Tween Custom<TTarget>(TTarget target, double from, double to, Action<TTarget, double> onValueChange,
        TimeSpan duration, IEasing? easing = null, TimeSpan delay = default) where TTarget : class
        => StartTargetCustom(target, from, to, onValueChange, duration, easing, delay);

    /// <summary>
    /// Animates a raw typed value, invoking a target-based <paramref name="onValueChange"/>
    /// on every update. Supported value types: double, float, int, Color, Point,
    /// Vector, Thickness, Rect. Write the callback as a static lambda
    /// (<c>static (vm, v) =&gt; vm.ArtOpacity = v</c>) to keep it allocation-free.
    /// </summary>
    public static Tween Custom<TTarget, TValue>(TTarget target, TValue from, TValue to,
        Action<TTarget, TValue> onValueChange, double duration = 1, IEasing? easing = null, double delay = 0)
        where TTarget : class
        => Custom(target, from, to, onValueChange, Seconds(duration, nameof(duration)), easing,
            SecondsOrZero(delay, nameof(delay)));

    /// <summary>
    /// Animates a raw typed value, invoking a target-based <paramref name="onValueChange"/>
    /// on every update (zero-alloc with a static lambda). Supported value types:
    /// double, float, int, Color, Point, Vector, Thickness, Rect.
    /// </summary>
    public static Tween Custom<TTarget, TValue>(TTarget target, TValue from, TValue to,
        Action<TTarget, TValue> onValueChange, TimeSpan duration, IEasing? easing = null, TimeSpan delay = default)
        where TTarget : class
        => StartTargetCustom(target, from, to, onValueChange, duration, easing, delay);

    /// <summary>
    /// A tween that does nothing but wait; <paramref name="onComplete"/> fires when
    /// the delay elapses. Usable with <c>await</c> as a UI-thread sleep. Pass
    /// <paramref name="target"/> so <see cref="StopAll(AvaloniaObject)"/> cancels the
    /// pending callback (e.g. leave a screen before a delayed action fires).
    /// </summary>
    public static Tween Delay(double duration, Action? onComplete = null, AvaloniaObject? target = null)
    {
        if (duration <= 0 || double.IsNaN(duration))
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Delay duration must be positive.");
        }

        return Delay(TimeSpan.FromSeconds(duration), onComplete, target);
    }

    /// <summary>
    /// A tween that does nothing but wait; <paramref name="onComplete"/> fires when
    /// the delay elapses. Usable with <c>await</c> as a UI-thread sleep. Pass
    /// <paramref name="target"/> so <see cref="StopAll(AvaloniaObject)"/> cancels the
    /// pending callback (e.g. leave a screen before a delayed action fires).
    /// </summary>
    public static Tween Delay(TimeSpan duration, Action? onComplete = null, AvaloniaObject? target = null)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Delay duration must be positive.");
        }

        Tween tween = Start(TweenInstance<double>.Acquire(
            TweenEngine.Instance, target, target != null ? CustomSentinel : null, 0d, 0d, duration, default, Linear, null));
        if (onComplete != null)
        {
            tween.OnComplete(onComplete);
        }

        return tween;
    }

    /// <summary>
    /// Stops every tween currently animating <paramref name="target"/>, leaving
    /// the animated values where they are. No-op when nothing animates the target.
    /// </summary>
    public static void StopAll(AvaloniaObject target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (ActiveByTarget.TryGetValue(target, out Dictionary<AvaloniaProperty, TweenInstance>? map))
        {
            foreach (TweenInstance instance in map.Values.ToArray())
            {
                instance.Stop();
            }
        }
    }

    /// <summary>
    /// Stops every tween currently animating <paramref name="target"/> and snaps
    /// the animated values to their end values. No-op when nothing animates it.
    /// </summary>
    public static void CompleteAll(AvaloniaObject target)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (ActiveByTarget.TryGetValue(target, out Dictionary<AvaloniaProperty, TweenInstance>? map))
        {
            foreach (TweenInstance instance in map.Values.ToArray())
            {
                instance.Complete();
            }
        }
    }

    /// <summary>
    /// Raises the <see cref="UnhandledException"/> event.
    /// </summary>
    internal static void RaiseUnhandledException(Exception ex) => UnhandledException?.Invoke(ex);

    private static Tween Start(TweenInstance instance)
    {
        TweenEngine.Instance.Add(instance);
        return new Tween(instance);
    }

    private static Tween StartCustom<T>(T from, T to, Action<T> onValueChange, TimeSpan duration, IEasing? easing,
        TimeSpan delay, AvaloniaObject? target)
    {
        ArgumentNullException.ThrowIfNull(onValueChange);
        return Start(TweenInstance<T>.Acquire(
            TweenEngine.Instance, target, target != null ? CustomSentinel : null, from, to, duration, delay,
            easing ?? DefaultEasing, onValueChange));
    }

    private static Tween StartTargetCustom<TTarget, TValue>(TTarget target, TValue from, TValue to,
        Action<TTarget, TValue> onValueChange, TimeSpan duration, IEasing? easing, TimeSpan delay) where TTarget : class
    {
        ArgumentNullException.ThrowIfNull(onValueChange);
        TweenInstance<TValue> instance = TweenInstance<TValue>.Acquire(
            TweenEngine.Instance, null, null, from, to, duration, delay, easing ?? DefaultEasing, null);
        instance.SetValueChange(target, CallbackCache.WrapValue(target, onValueChange));
        return Start(instance);
    }

    private static TimeSpan Seconds(double value, string name)
    {
        if (value <= 0 || double.IsNaN(value))
        {
            throw new ArgumentOutOfRangeException(name, "Tween duration must be positive.");
        }

        return TimeSpan.FromSeconds(value);
    }

    private static TimeSpan SecondsOrZero(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(name);
        }

        return TimeSpan.FromSeconds(Math.Max(0, value));
    }

    /// <summary>
    /// Registers the tween as the newest one for its target property, silently
    /// stopping any tween it supersedes.
    /// </summary>
    internal static void Register(TweenInstance instance)
    {
        if (instance.Target is not { } target || instance.Property is not { } property)
        {
            return;
        }

        Dictionary<AvaloniaProperty, TweenInstance> map = ActiveByTarget.GetOrCreateValue(target);
        if (map.TryGetValue(property, out TweenInstance? existing) && !ReferenceEquals(existing, instance))
        {
            existing.Stop();
        }

        map[property] = instance;
    }

    /// <summary>
    /// Removes the tween from the latest-wins table, but only if it is still the
    /// registered tween for its target property.
    /// </summary>
    internal static void UnregisterIfCurrent(TweenInstance instance)
    {
        if (instance.Target is not { } target || instance.Property is not { } property)
        {
            return;
        }

        if (ActiveByTarget.TryGetValue(target, out Dictionary<AvaloniaProperty, TweenInstance>? map)
            && map.TryGetValue(property, out TweenInstance? current)
            && ReferenceEquals(current, instance))
        {
            map.Remove(property);
        }
    }
}
