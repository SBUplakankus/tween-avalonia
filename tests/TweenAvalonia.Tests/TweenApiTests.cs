using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media;

namespace TweenAvalonia.Tests;

[TestFixture]
[NonParallelizable]
public class TweenApiTests
{
    private sealed class TypedTarget : AvaloniaObject
    {
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<TypedTarget, double>("Value");

        public static readonly StyledProperty<double> OffsetProperty =
            AvaloniaProperty.Register<TypedTarget, double>("Offset");

        public static readonly StyledProperty<Color> ColorProperty =
            AvaloniaProperty.Register<TypedTarget, Color>("Color");

        public static readonly StyledProperty<Point> PointProperty =
            AvaloniaProperty.Register<TypedTarget, Point>("Point");

        public static readonly StyledProperty<Vector> VectorProperty =
            AvaloniaProperty.Register<TypedTarget, Vector>("Vector");

        public static readonly StyledProperty<Thickness> ThicknessProperty =
            AvaloniaProperty.Register<TypedTarget, Thickness>("Thickness");

        public static readonly StyledProperty<Rect> RectProperty =
            AvaloniaProperty.Register<TypedTarget, Rect>("Rect");

        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<TypedTarget, string>("Text");
    }

    private static readonly LinearEasing Linear = new();
    private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(100);

    [SetUp]
    public void Setup()
    {
        TweenEngine.Instance.AutoPumpEnabled = false;
        TweenEngine.Instance.StopAll();
    }

    [TearDown]
    public void TearDown()
    {
        TweenEngine.Instance.StopAll();
        TweenEngine.Instance.AutoPumpEnabled = true;
    }

    [Test]
    public void FactoryDefaults_DurationIsOneSecond_DefaultEasingNoDelay()
    {
        var border = new Border { Opacity = 0 };
        Tween tween = Tween.Opacity(border, 1);

        Assert.That(tween.Duration, Is.EqualTo(TimeSpan.FromSeconds(1)));
        Assert.That(border.Opacity, Is.Zero);

        for (int i = 0; i < 10; i++)
        {
            TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        }

        Assert.That(border.Opacity, Is.EqualTo(0.5).Within(1e-9), "SineEaseInOut(0.5) == 0.5");
        Assert.That(tween.IsAlive, Is.True);

        for (int i = 0; i < 10; i++)
        {
            TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        }

        Assert.That(border.Opacity, Is.EqualTo(1).Within(1e-9));
        Assert.That(tween.IsAlive, Is.False);
    }

    [Test]
    public void To_Color_InterpolatesChannels()
    {
        var target = new TypedTarget();
        target.SetValue(TypedTarget.ColorProperty, Colors.Black);
        Tween.To(target, TypedTarget.ColorProperty, Colors.White, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Color mid = target.GetValue(TypedTarget.ColorProperty);
        Assert.That(mid.R, Is.EqualTo(128).Within(1));
        Assert.That(mid.G, Is.EqualTo(128).Within(1));
        Assert.That(mid.B, Is.EqualTo(128).Within(1));
        Assert.That(mid.A, Is.EqualTo(255));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.GetValue(TypedTarget.ColorProperty), Is.EqualTo(Colors.White));
    }

    [Test]
    public void To_PointVectorThicknessRect_Interpolate()
    {
        var target = new TypedTarget();
        Tween.To(target, TypedTarget.PointProperty, new Point(10, 20), Duration, Linear);
        Tween.To(target, TypedTarget.VectorProperty, new Vector(30, 40), Duration, Linear);
        Tween.To(target, TypedTarget.ThicknessProperty, new Thickness(8), Duration, Linear);
        Tween.To(target, TypedTarget.RectProperty, new Rect(10, 20, 30, 40), Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));

        Assert.That(target.GetValue(TypedTarget.PointProperty), Is.EqualTo(new Point(5, 10)));
        Assert.That(target.GetValue(TypedTarget.VectorProperty), Is.EqualTo(new Vector(15, 20)));
        Assert.That(target.GetValue(TypedTarget.ThicknessProperty), Is.EqualTo(new Thickness(4)));
        Assert.That(target.GetValue(TypedTarget.RectProperty), Is.EqualTo(new Rect(5, 10, 15, 20)));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));

        Assert.That(target.GetValue(TypedTarget.PointProperty), Is.EqualTo(new Point(10, 20)));
        Assert.That(target.GetValue(TypedTarget.VectorProperty), Is.EqualTo(new Vector(30, 40)));
        Assert.That(target.GetValue(TypedTarget.ThicknessProperty), Is.EqualTo(new Thickness(8)));
        Assert.That(target.GetValue(TypedTarget.RectProperty), Is.EqualTo(new Rect(10, 20, 30, 40)));
    }

    [Test]
    public void Color_Sugar_AnimatesBrush()
    {
        var brush = new SolidColorBrush(Colors.Black);
        Tween.Color(brush, Colors.White, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(brush.Color.R, Is.EqualTo(128).Within(1));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(brush.Color, Is.EqualTo(Colors.White));
    }

    [Test]
    public void Margin_Sugar_AnimatesThickness()
    {
        var border = new Border();
        Tween.Margin(border, new Thickness(16), Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(border.Margin, Is.EqualTo(new Thickness(8)));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(border.Margin, Is.EqualTo(new Thickness(16)));
    }

    [Test]
    public void WidthHeight_Sugar_Animate()
    {
        var border = new Border { Width = 0, Height = 0 };
        Tween.Width(border, 100, Duration, Linear);
        Tween.Height(border, 200, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(border.Width, Is.EqualTo(50).Within(1e-9));
        Assert.That(border.Height, Is.EqualTo(100).Within(1e-9));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(border.Width, Is.EqualTo(100).Within(1e-9));
        Assert.That(border.Height, Is.EqualTo(200).Within(1e-9));
    }

    [Test]
    public void Custom_Color_Interpolates()
    {
        Color value = Colors.Black;
        Tween.Custom(Colors.Black, Colors.White, c => value = c, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(value.R, Is.EqualTo(128).Within(1));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(value, Is.EqualTo(Colors.White));
    }

    [Test]
    public void Custom_Int_Interpolates()
    {
        int value = 0;
        Tween.Custom(0, 10, v => value = v, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(value, Is.EqualTo(5));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(value, Is.EqualTo(10));
    }

    [Test]
    public void To_UnsupportedType_ThrowsAtCreation()
    {
        var target = new TypedTarget();
        Assert.Throws<NotSupportedException>(() =>
            Tween.To(target, TypedTarget.TextProperty, "hello", Duration, Linear));
    }

    [Test]
    public void Delay_CompletesAfterDuration_FiresCallback()
    {
        int calls = 0;
        Tween tween = Tween.Delay(0.1, () => calls++);
        Assert.That(tween.IsAlive, Is.True);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(calls, Is.Zero);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(tween.IsAlive, Is.False);
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void Delay_NegativeOrZeroDuration_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Tween.Delay(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Tween.Delay(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Tween.Delay(TimeSpan.Zero));
    }

    [Test]
    public void Custom_Target_NewTweenSupersedesPrevious()
    {
        var target = new TypedTarget();
        int firstCalls = 0;
        Tween first = Tween.Custom(0, 100, v => firstCalls++, Duration, Linear, target: target);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(firstCalls, Is.EqualTo(1));
        Assert.That(first.IsAlive, Is.True);

        double secondValue = 0;
        Tween second = Tween.Custom(0, 10, v => secondValue = v, Duration, Linear, target: target);
        Assert.That(first.IsAlive, Is.False, "New target-keyed custom tween supersedes the previous one");

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(firstCalls, Is.EqualTo(1), "Superseded tween stopped writing");
        Assert.That(secondValue, Is.EqualTo(5).Within(1e-9));
        Assert.That(second.IsAlive, Is.True);
    }

    [Test]
    public void Custom_Target_DoesNotInterfereWithPropertyTweens()
    {
        var target = new TypedTarget();
        double value = 0;
        Tween custom = Tween.Custom(0d, 10d, v => value = v, Duration, Linear, target: target);
        Tween property = Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear);

        Assert.That(custom.IsAlive, Is.True);
        Assert.That(property.IsAlive, Is.True);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(value, Is.EqualTo(5).Within(1e-9));
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(5).Within(1e-9));
    }

    [Test]
    public void StopAll_StopsTargetKeyedCustom_LeavingValueMidFlight()
    {
        var target = new TypedTarget();
        double value = 0;
        Tween tween = Tween.Custom(0d, 10d, v => value = v, Duration, Linear, target: target);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Tween.StopAll(target);

        Assert.That(tween.IsAlive, Is.False);
        Assert.That(value, Is.EqualTo(5).Within(1e-9));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));
        Assert.That(value, Is.EqualTo(5).Within(1e-9));
    }

    [Test]
    public void CompleteAll_CompletesTargetKeyedCustom()
    {
        var target = new TypedTarget();
        double value = 0;
        Tween tween = Tween.Custom(0d, 10d, v => value = v, Duration, Linear, target: target);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Tween.CompleteAll(target);

        Assert.That(value, Is.EqualTo(10).Within(1e-9));
        Assert.That(tween.IsAlive, Is.False);
    }

    [Test]
    public void Custom_WithoutTarget_IgnoresStopAllOnOtherTarget()
    {
        var target = new TypedTarget();
        double value = 0;
        Tween tween = Tween.Custom(0d, 10d, v => value = v, Duration, Linear);

        Tween.StopAll(target);
        Assert.That(tween.IsAlive, Is.True);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));
        Assert.That(tween.IsAlive, Is.False);
        Assert.That(value, Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void Delay_Target_StopAllCancelsPendingCallback()
    {
        var target = new TypedTarget();
        int calls = 0;
        Tween.Delay(0.1, () => calls++, target: target);

        Tween.StopAll(target);
        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(200));

        Assert.That(calls, Is.Zero);
    }

    [Test]
    public void Delay_Target_FiresCallbackNaturally()
    {
        var target = new TypedTarget();
        int calls = 0;
        Tween.Delay(0.05, () => calls++, target: target);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void OnUpdate_ReceivesEasedFactorEveryFrame()
    {
        var target = new TypedTarget();
        var factors = new List<double>();
        Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear).OnUpdate(factors.Add);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));

        Assert.That(factors, Is.EqualTo(new[] { 0.5, 1.0 }));
    }

    [Test]
    public void Progress_Setter_ScrubsValue()
    {
        var target = new TypedTarget();
        Tween tween = Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear);

        tween.Progress = 0.5;
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(5).Within(1e-9));

        tween.ElapsedTime = TimeSpan.FromMilliseconds(25);
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(2.5).Within(1e-9));

        tween.Progress = 1;
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(10).Within(1e-9));

        tween.Progress = 0.25;
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(2.5).Within(1e-9));
        Assert.That(tween.IsAlive, Is.True, "Scrubbing does not kill the tween");

        tween.Progress = 2;
        Assert.That(tween.Progress, Is.EqualTo(1).Within(1e-9), "Progress clamps to [0, 1]");

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(10).Within(1e-9));
        Assert.That(tween.IsAlive, Is.False);
    }

    [Test]
    public void ElapsedTime_Setter_ClampsToDuration()
    {
        var target = new TypedTarget();
        Tween tween = Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear);

        tween.ElapsedTime = TimeSpan.FromSeconds(5);
        Assert.That(tween.ElapsedTime, Is.EqualTo(Duration));
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(10).Within(1e-9));

        tween.ElapsedTime = TimeSpan.FromSeconds(-5);
        Assert.That(tween.ElapsedTime, Is.EqualTo(TimeSpan.Zero));
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.Zero);
    }

    [Test]
    public async Task Await_CompletesWhenTweenFinishes()
    {
        var target = new TypedTarget();
        Tween tween = Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear);
        Task completion = WaitFor(tween);

        Assert.That(completion.IsCompleted, Is.False);
        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));

        await completion;
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public async Task Await_AlreadyDead_ReturnsImmediately()
    {
        var target = new TypedTarget();
        Tween tween = Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear);
        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));

        Assert.That(tween.IsAlive, Is.False);
        await tween;
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void CancelOn_StopsTween_LeavingValueMidFlight()
    {
        var target = new TypedTarget();
        using var cts = new CancellationTokenSource();
        Tween tween = Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear).CancelOn(cts.Token);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        cts.Cancel();

        Assert.That(tween.IsAlive, Is.False);
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(5).Within(1e-9));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(5).Within(1e-9));
    }

    [Test]
    public void CancelOn_AlreadyCanceledToken_StopsImmediately()
    {
        var target = new TypedTarget();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Tween tween = Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear).CancelOn(cts.Token);

        Assert.That(tween.IsAlive, Is.False);
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.Zero);
    }

    [Test]
    public async Task Await_Canceled_ThrowsOperationCanceledException()
    {
        var target = new TypedTarget();
        using var cts = new CancellationTokenSource();
        Tween tween = Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear).CancelOn(cts.Token);
        Task completion = WaitFor(tween);

        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(() => completion);
        Assert.That(tween.IsAlive, Is.False);
    }

    [Test]
    public void StopAll_StopsEveryTweenOnTarget()
    {
        var target = new TypedTarget();
        Tween value = Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear);
        Tween offset = Tween.To(target, TypedTarget.OffsetProperty, 20, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Tween.StopAll(target);

        Assert.That(value.IsAlive, Is.False);
        Assert.That(offset.IsAlive, Is.False);
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(5).Within(1e-9));
        Assert.That(target.GetValue(TypedTarget.OffsetProperty), Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void CompleteAll_CompletesEveryTweenOnTarget()
    {
        var target = new TypedTarget();
        Tween value = Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear);
        Tween offset = Tween.To(target, TypedTarget.OffsetProperty, 20, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Tween.CompleteAll(target);

        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(10).Within(1e-9));
        Assert.That(target.GetValue(TypedTarget.OffsetProperty), Is.EqualTo(20).Within(1e-9));
        Assert.That(value.IsAlive, Is.False);
        Assert.That(offset.IsAlive, Is.False);
    }

    [Test]
    public void Settings_Overload_UsesBundledValues()
    {
        var border = new Border { Opacity = 0 };
        var settings = new TweenSettings<double>(to: 0.5, duration: 0.1, easing: Linear);
        Tween tween = Tween.Opacity(border, settings);

        Assert.That(tween.Duration, Is.EqualTo(TimeSpan.FromMilliseconds(100)));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(border.Opacity, Is.EqualTo(0.25).Within(1e-9));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(border.Opacity, Is.EqualTo(0.5).Within(1e-9));
    }

    [Test]
    public void Settings_Defaults_AreOneSecondAndDefaultEasing()
    {
        var settings = new TweenSettings<double>(to: 1);
        Assert.That(settings.Duration, Is.EqualTo(1));
        Assert.That(settings.Easing, Is.SameAs(Tween.DefaultEasing));
        Assert.That(settings.Delay, Is.Zero);
    }

    [Test]
    public void Settings_TimeSpanConstructor_ConvertsToSeconds()
    {
        var settings = new TweenSettings<double>(to: 1, TimeSpan.FromMilliseconds(250));
        Assert.That(settings.Duration, Is.EqualTo(0.25));

        var nonGeneric = new TweenSettings(TimeSpan.FromMilliseconds(500), delay: TimeSpan.FromMilliseconds(50));
        Assert.That(nonGeneric.Duration, Is.EqualTo(0.5));
        Assert.That(nonGeneric.Delay, Is.EqualTo(0.05));
    }

    [Test]
    public void Settings_DelayOverload_Applies()
    {
        var border = new Border { Opacity = 0 };
        var settings = new TweenSettings<double>(to: 1, duration: 0.1, delay: 0.05);
        Tween.Opacity(border, settings);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(border.Opacity, Is.Zero, "Delayed tween has not started");

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(border.Opacity, Is.EqualTo(0.5).Within(1e-9));
    }

    [Test]
    public void DefaultEasing_Settable_AppliesToNewTweens()
    {
        IEasing original = Tween.DefaultEasing;
        try
        {
            var border = new Border { Opacity = 0 };
            Tween.DefaultEasing = Linear;
            Tween.Opacity(border, 1);

            for (int i = 0; i < 5; i++)
            {
                TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
            }

            Assert.That(border.Opacity, Is.EqualTo(0.25).Within(1e-9), "Linear easing should be used");
        }
        finally
        {
            Tween.DefaultEasing = original;
        }
    }

    [Test]
    public void MaxActiveCount_TracksPeak()
    {
        var target = new TypedTarget();
        Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear);
        Assert.That(TweenEngine.Instance.ActiveCount, Is.EqualTo(1));

        Tween.To(target, TypedTarget.ValueProperty, 20, Duration, Linear);
        Assert.That(TweenEngine.Instance.ActiveCount, Is.EqualTo(1), "Superseding keeps one tween alive");

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));
        Assert.That(TweenEngine.Instance.ActiveCount, Is.Zero);
        Assert.That(TweenEngine.Instance.MaxActiveCount, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void Update_Color_AllocatesNothingPerFrame()
    {
        var target = new TypedTarget();
        Tween.To(target, TypedTarget.ColorProperty, Colors.White, Duration, Linear);

        for (int i = 0; i < 50; i++)
        {
            TweenEngine.Instance.Update(TimeSpan.Zero);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 5000; i++)
        {
            TweenEngine.Instance.Update(TimeSpan.Zero);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.LessThan(1024), $"Per-frame allocations detected: {allocated} bytes over 5000 ticks");
    }

    private static async Task WaitFor(Tween tween) => await tween;
}
