using System;
using Avalonia.Animation.Easings;

namespace TweenAvalonia;

/// <summary>
/// Reusable animation settings shared by all value types: duration, easing and
/// delay. Every field defaults (1 second, <see cref="Tween.DefaultEasing"/>, no
/// delay), so a settings value can be built from just the parts that matter and
/// passed to any <c>Tween.</c> factory as the only extra argument.
/// </summary>
public readonly struct TweenSettings
{
    /// <summary>
    /// Duration in seconds.
    /// </summary>
    public double Duration { get; }

    /// <summary>
    /// Easing; captured from <see cref="Tween.DefaultEasing"/> at construction when not specified.
    /// </summary>
    public IEasing Easing { get; }

    /// <summary>
    /// Delay before the animation starts, in seconds.
    /// </summary>
    public double Delay { get; }

    /// <param name="duration">Duration in seconds; must be positive.</param>
    /// <param name="easing">Easing; defaults to <see cref="Tween.DefaultEasing"/> when null.</param>
    /// <param name="delay">Delay in seconds before the animation starts; negative clamps to zero.</param>
    public TweenSettings(double duration = 1, IEasing? easing = null, double delay = 0)
    {
        if (duration <= 0 || double.IsNaN(duration))
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Tween duration must be positive.");
        }

        Duration = duration;
        Easing = easing ?? Tween.DefaultEasing;
        Delay = Math.Max(0, delay);
    }

    /// <param name="duration">Duration; must be positive.</param>
    /// <param name="easing">Easing; defaults to <see cref="Tween.DefaultEasing"/> when null.</param>
    /// <param name="delay">Delay before the animation starts.</param>
    public TweenSettings(TimeSpan duration, IEasing? easing = null, TimeSpan delay = default)
        : this(duration.TotalSeconds, easing, delay.TotalSeconds)
    {
    }
}

/// <summary>
/// Reusable animation settings for a specific value type. Like <see cref="TweenSettings"/>
/// but carries the end value, so an animation can be configured once and reused
/// everywhere: <c>Tween.Opacity(panel, myFadeSettings)</c>.
/// </summary>
public readonly struct TweenSettings<T>
{
    /// <summary>
    /// The end value the animation animates to.
    /// </summary>
    public T To { get; }

    /// <summary>
    /// Duration in seconds.
    /// </summary>
    public double Duration { get; }

    /// <summary>
    /// Easing; captured from <see cref="Tween.DefaultEasing"/> at construction when not specified.
    /// </summary>
    public IEasing Easing { get; }

    /// <summary>
    /// Delay before the animation starts, in seconds.
    /// </summary>
    public double Delay { get; }

    /// <param name="to">The end value the animation animates to.</param>
    /// <param name="duration">Duration in seconds; must be positive.</param>
    /// <param name="easing">Easing; defaults to <see cref="Tween.DefaultEasing"/> when null.</param>
    /// <param name="delay">Delay in seconds before the animation starts; negative clamps to zero.</param>
    public TweenSettings(T to, double duration = 1, IEasing? easing = null, double delay = 0)
    {
        if (duration <= 0 || double.IsNaN(duration))
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Tween duration must be positive.");
        }

        To = to;
        Duration = duration;
        Easing = easing ?? Tween.DefaultEasing;
        Delay = Math.Max(0, delay);
    }

    /// <param name="to">The end value the animation animates to.</param>
    /// <param name="duration">Duration; must be positive.</param>
    /// <param name="easing">Easing; defaults to <see cref="Tween.DefaultEasing"/> when null.</param>
    /// <param name="delay">Delay before the animation starts.</param>
    public TweenSettings(T to, TimeSpan duration, IEasing? easing = null, TimeSpan delay = default)
        : this(to, duration.TotalSeconds, easing, delay.TotalSeconds)
    {
    }
}
