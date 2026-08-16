using System;
using System.Runtime.CompilerServices;

namespace TweenAvalonia;

/// <summary>
/// Awaitable for <see cref="Tween"/>: <c>await Tween.Opacity(...)</c> resumes when
/// the tween dies (completes naturally, is stopped, completed, canceled or
/// superseded). If the tween was canceled, the await throws
/// <see cref="OperationCanceledException"/>. Awaiting a stale or dead handle
/// completes immediately. The awaiter is a struct; the continuation itself is
/// stored on the tween instance, so awaiting allocates only the async state
/// machine. No threads are involved — continuations run on the engine's (UI) thread.
/// </summary>
public readonly struct TweenAwaiter : INotifyCompletion
{
    private readonly TweenInstance? _instance;
    private readonly int _version;

    internal TweenAwaiter(TweenInstance? instance, int version)
    {
        _instance = instance;
        _version = version;
    }

    /// <summary>
    /// True when the tween is already dead, so awaiting it returns immediately.
    /// </summary>
    public bool IsCompleted => _instance is not { IsAlive: true } instance || instance.Version != _version;

    /// <summary>
    /// Schedules the continuation; runs it immediately if the tween is already dead.
    /// </summary>
    public void OnCompleted(Action continuation)
    {
        ArgumentNullException.ThrowIfNull(continuation);

        if (_instance is not { } instance || instance.Version != _version || !instance.IsAlive)
        {
            continuation();
            return;
        }

        instance.SetContinuation(continuation);
    }

    /// <summary>
    /// Throws <see cref="OperationCanceledException"/> if the tween was canceled
    /// via <see cref="Tween.CancelOn(System.Threading.CancellationToken)"/>.
    /// </summary>
    public void GetResult()
    {
        if (_instance is { Canceled: true } instance && instance.Version == _version)
        {
            throw new OperationCanceledException();
        }
    }
}
