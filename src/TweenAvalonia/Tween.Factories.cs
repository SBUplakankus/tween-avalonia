using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Visuals;

namespace TweenAvalonia;

/// <summary>
/// Convenience sugar factories for common Avalonia properties. Each method is a
/// thin wrapper over <c>Tween.To</c> with the property baked in, so
/// the common case reads as one line: <c>Tween.Opacity(visual, 0);</c>.
/// </summary>
public readonly partial struct Tween
{
    /// <summary>
    /// Animates a visual's opacity to <paramref name="to"/>, starting from its current value.
    /// </summary>
    public static Tween Opacity(Visual target, double to, double duration = 1, IEasing? easing = null, double delay = 0)
        => To(target, Visual.OpacityProperty, to, duration, easing, delay);

    /// <summary>
    /// Animates a visual's opacity to <paramref name="to"/>, starting from its current value.
    /// </summary>
    public static Tween Opacity(Visual target, double to, TimeSpan duration, IEasing? easing = null,
        TimeSpan delay = default)
        => To(target, Visual.OpacityProperty, to, duration, easing, delay);

    /// <summary>
    /// Animates a visual's opacity using reusable settings.
    /// </summary>
    public static Tween Opacity(Visual target, TweenSettings<double> settings)
        => To(target, Visual.OpacityProperty, settings);

    /// <summary>
    /// Animates a solid brush's color to <paramref name="to"/>, starting from its current value.
    /// </summary>
    public static Tween Color(SolidColorBrush target, Color to, double duration = 1, IEasing? easing = null,
        double delay = 0)
        => To(target, SolidColorBrush.ColorProperty, to, duration, easing, delay);

    /// <summary>
    /// Animates a solid brush's color to <paramref name="to"/>, starting from its current value.
    /// </summary>
    public static Tween Color(SolidColorBrush target, Color to, TimeSpan duration, IEasing? easing = null,
        TimeSpan delay = default)
        => To(target, SolidColorBrush.ColorProperty, to, duration, easing, delay);

    /// <summary>
    /// Animates a solid brush's color using reusable settings.
    /// </summary>
    public static Tween Color(SolidColorBrush target, TweenSettings<Color> settings)
        => To(target, SolidColorBrush.ColorProperty, settings);

    /// <summary>
    /// Animates a control's margin to <paramref name="to"/>, starting from its current value.
    /// </summary>
    public static Tween Margin(Layoutable target, Thickness to, double duration = 1, IEasing? easing = null,
        double delay = 0)
        => To(target, Layoutable.MarginProperty, to, duration, easing, delay);

    /// <summary>
    /// Animates a control's margin to <paramref name="to"/>, starting from its current value.
    /// </summary>
    public static Tween Margin(Layoutable target, Thickness to, TimeSpan duration, IEasing? easing = null,
        TimeSpan delay = default)
        => To(target, Layoutable.MarginProperty, to, duration, easing, delay);

    /// <summary>
    /// Animates a control's margin using reusable settings.
    /// </summary>
    public static Tween Margin(Layoutable target, TweenSettings<Thickness> settings)
        => To(target, Layoutable.MarginProperty, settings);

    /// <summary>
    /// Animates a control's width to <paramref name="to"/>, starting from its current value.
    /// </summary>
    public static Tween Width(Layoutable target, double to, double duration = 1, IEasing? easing = null,
        double delay = 0)
        => To(target, Layoutable.WidthProperty, to, duration, easing, delay);

    /// <summary>
    /// Animates a control's width to <paramref name="to"/>, starting from its current value.
    /// </summary>
    public static Tween Width(Layoutable target, double to, TimeSpan duration, IEasing? easing = null,
        TimeSpan delay = default)
        => To(target, Layoutable.WidthProperty, to, duration, easing, delay);

    /// <summary>
    /// Animates a control's width using reusable settings.
    /// </summary>
    public static Tween Width(Layoutable target, TweenSettings<double> settings)
        => To(target, Layoutable.WidthProperty, settings);

    /// <summary>
    /// Animates a control's height to <paramref name="to"/>, starting from its current value.
    /// </summary>
    public static Tween Height(Layoutable target, double to, double duration = 1, IEasing? easing = null,
        double delay = 0)
        => To(target, Layoutable.HeightProperty, to, duration, easing, delay);

    /// <summary>
    /// Animates a control's height to <paramref name="to"/>, starting from its current value.
    /// </summary>
    public static Tween Height(Layoutable target, double to, TimeSpan duration, IEasing? easing = null,
        TimeSpan delay = default)
        => To(target, Layoutable.HeightProperty, to, duration, easing, delay);

    /// <summary>
    /// Animates a control's height using reusable settings.
    /// </summary>
    public static Tween Height(Layoutable target, TweenSettings<double> settings)
        => To(target, Layoutable.HeightProperty, settings);
}
