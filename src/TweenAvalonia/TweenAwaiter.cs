using System;
using System.Runtime.CompilerServices;

namespace TweenAvalonia;

/// <summary>
/// Awaitable for <see cref="Tween"/>: <c>await Tween.Opacity(...)</c> resumes when
/// the tween dies (completes naturally, is stopped, completed, canceled or
/// superseded). If the tween was canceled, the await throws
/// <see cref="OperationCanceledException"/>. Awaiting a tween that is already dead
/// completes immediately. No threads are involved — continuations run on the
/// engine's (UI) thread.
/// </summary>
public sealed class TweenAwaiter : INotifyCompletion
{
    private readonly TweenInstance? _instance;
    private Action? _continuation;

    internal TweenAwaiter(TweenInstance? instance)
    {
        _instance = instance;
    }

    /// <summary>
    /// True when the tween is already dead, so awaiting it returns immediately.
    /// </summary>
    public bool IsCompleted => _instance is not { IsAlive: true };

    /// <summary>
    /// Schedules the continuation; runs it immediately if the tween is already dead.
    /// </summary>
    public void OnCompleted(Action continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        if (_instance is not { } instance || !instance.IsAlive)
        {
            continuation();
            return;
        }

        _continuation = continuation;
        instance.AttachDeathHook(RunContinuation);
    }

    /// <summary>
    /// Throws <see cref="OperationCanceledException"/> if the tween was canceled
    /// via <see cref="Tween.CancelOn(System.Threading.CancellationToken)"/>.
    /// </summary>
    public void GetResult()
    {
        if (_instance is { Canceled: true })
        {
            throw new OperationCanceledException();
        }
    }

    private void RunContinuation()
    {
        Action? continuation = _continuation;
        _continuation = null;
        continuation?.Invoke();
    }
}
