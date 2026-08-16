using Avalonia;
using Avalonia.Animation.Easings;

namespace TweenAvalonia.Tests;

[TestFixture]
[NonParallelizable]
public class TweenZeroAllocTests
{
    private sealed class TypedTarget : AvaloniaObject
    {
        public static readonly StyledProperty<double> ValueProperty =
            AvaloniaProperty.Register<TypedTarget, double>("Value");
    }

    private sealed class CallbackTarget
    {
        public double Value;
        public int CompleteCalls;
        public int UpdateCalls;
        public double LastFactor;
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
    public void StaleHandle_CannotControlNewTween()
    {
        var target = new TypedTarget();
        Tween first = Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear);
        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));
        Assert.That(first.IsAlive, Is.False);

        Tween second = Tween.To(target, TypedTarget.ValueProperty, 20, Duration, Linear);
        first.Stop();
        first.Complete();
        first.Progress = 0.5;

        Assert.That(second.IsAlive, Is.True, "Stale handle must not touch the pooled instance's new owner");

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(100));
        Assert.That(target.GetValue(TypedTarget.ValueProperty), Is.EqualTo(20).Within(1e-9));
    }

    [Test]
    public void StartStop_AllocatesNothingAfterWarmup()
    {
        var target = new TypedTarget();
        for (int i = 0; i < 50; i++)
        {
            Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear).Stop();
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++)
        {
            Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear).Stop();
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.LessThan(1024),
            $"Tween start/stop allocated {allocated} bytes over 10,000 cycles");
    }

    [Test]
    public void TargetBasedCallbacks_Fire()
    {
        var target = new CallbackTarget();
        var property = new TypedTarget();
        Tween.To(property, TypedTarget.ValueProperty, 10, Duration, Linear)
            .OnComplete(target, static t => t.CompleteCalls++)
            .OnUpdate(target, static (t, f) =>
            {
                t.UpdateCalls++;
                t.LastFactor = f;
            });

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));

        Assert.That(target.CompleteCalls, Is.EqualTo(1));
        Assert.That(target.UpdateCalls, Is.EqualTo(2));
        Assert.That(target.LastFactor, Is.EqualTo(1.0));
    }

    [Test]
    public void TargetBasedCallbacks_AllocateNothingAfterFirstUse()
    {
        var target = new CallbackTarget();
        var property = new TypedTarget();
        TweenEngine.Instance.StopAll();

        Tween.Custom(target, 0d, 10d, static (t, v) => t.Value = v, Duration, Linear).Stop();
        Tween.To(property, TypedTarget.ValueProperty, 10, Duration, Linear)
            .OnComplete(target, static t => t.CompleteCalls++)
            .OnUpdate(target, static (t, f) => t.LastFactor = f)
            .Stop();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 5000; i++)
        {
            Tween.Custom(target, 0d, 10d, static (t, v) => t.Value = v, Duration, Linear).Stop();
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.LessThan(1024),
            $"Target-based callbacks allocated {allocated} bytes over 5,000 starts");
    }

    [Test]
    public void Custom_TargetBased_Interpolates()
    {
        var target = new CallbackTarget();
        Tween.Custom(target, 0d, 10d, static (t, v) => t.Value = v, Duration, Linear);

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.EqualTo(5).Within(1e-9));

        TweenEngine.Instance.Update(TimeSpan.FromMilliseconds(50));
        Assert.That(target.Value, Is.EqualTo(10).Within(1e-9));
    }

    [Test]
    public void GetAwaiter_AllocatesNothing()
    {
        var target = new TypedTarget();
        Tween tween = Tween.To(target, TypedTarget.ValueProperty, 10, Duration, Linear);
        tween.GetAwaiter();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 10000; i++)
        {
            tween.GetAwaiter();
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.LessThan(1024),
            $"GetAwaiter allocated {allocated} bytes over 10,000 calls");
    }
}
