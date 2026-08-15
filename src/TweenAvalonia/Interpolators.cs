using System;
using Avalonia;
using Avalonia.Media;

namespace TweenAvalonia;

/// <summary>
/// Per-type interpolation functions used by the typed tween instances.
/// Kept allocation-free: values are computed as plain structs, never boxed.
/// </summary>
internal static class Interpolators
{
    public static double LerpDouble(double from, double to, double t) => from + (to - from) * t;

    public static float LerpSingle(float from, float to, double t) => (float)(from + (to - from) * t);

    public static int LerpInt32(int from, int to, double t) => (int)Math.Round(from + (to - from) * t);

    public static Color LerpColor(Color from, Color to, double t) => new(
        (byte)Math.Round(from.A + (to.A - from.A) * t),
        (byte)Math.Round(from.R + (to.R - from.R) * t),
        (byte)Math.Round(from.G + (to.G - from.G) * t),
        (byte)Math.Round(from.B + (to.B - from.B) * t));

    public static Point LerpPoint(Point from, Point to, double t) => new(
        from.X + (to.X - from.X) * t,
        from.Y + (to.Y - from.Y) * t);

    public static Vector LerpVector(Vector from, Vector to, double t) => new(
        from.X + (to.X - from.X) * t,
        from.Y + (to.Y - from.Y) * t);

    public static Thickness LerpThickness(Thickness from, Thickness to, double t) => new(
        from.Left + (to.Left - from.Left) * t,
        from.Top + (to.Top - from.Top) * t,
        from.Right + (to.Right - from.Right) * t,
        from.Bottom + (to.Bottom - from.Bottom) * t);

    public static Rect LerpRect(Rect from, Rect to, double t) => new(
        from.X + (to.X - from.X) * t,
        from.Y + (to.Y - from.Y) * t,
        from.Width + (to.Width - from.Width) * t,
        from.Height + (to.Height - from.Height) * t);
}

/// <summary>
/// Cached per-value-type interpolation delegate. The delegate is created once
/// per type and reused by every tween of that type, so per-frame writes stay
/// allocation-free. Accessing <see cref="Value"/> for an unsupported type
/// throws at tween creation, so mistakes fail fast instead of mid-animation.
/// </summary>
internal static class Interpolator<T>
{
    /// <summary>
    /// Lazy so an unsupported type throws <see cref="NotSupportedException"/>
    /// directly (not wrapped in <see cref="TypeInitializationException"/>).
    /// </summary>
    private static readonly Lazy<Func<T, T, double, T>> LazyValue =
        new(Create, LazyThreadSafetyMode.ExecutionAndPublication);

    public static Func<T, T, double, T> Value => LazyValue.Value;

    private static Func<T, T, double, T> Create()
    {
        if (typeof(T) == typeof(double)) return (Func<T, T, double, T>)(object)Interpolators.LerpDouble;
        if (typeof(T) == typeof(float)) return (Func<T, T, double, T>)(object)Interpolators.LerpSingle;
        if (typeof(T) == typeof(int)) return (Func<T, T, double, T>)(object)Interpolators.LerpInt32;
        if (typeof(T) == typeof(Color)) return (Func<T, T, double, T>)(object)Interpolators.LerpColor;
        if (typeof(T) == typeof(Point)) return (Func<T, T, double, T>)(object)Interpolators.LerpPoint;
        if (typeof(T) == typeof(Vector)) return (Func<T, T, double, T>)(object)Interpolators.LerpVector;
        if (typeof(T) == typeof(Thickness)) return (Func<T, T, double, T>)(object)Interpolators.LerpThickness;
        if (typeof(T) == typeof(Rect)) return (Func<T, T, double, T>)(object)Interpolators.LerpRect;

        throw new NotSupportedException(
            $"TweenAvalonia does not support tweening '{typeof(T).Name}'. " +
            "Supported types: double, float, int, Color, Point, Vector, Thickness, Rect.");
    }
}
