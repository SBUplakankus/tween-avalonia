<div align="center"><img src="../assets/TweenAvaloniaLogo.svg" alt="TweenAvalonia" width="48"/></div>

# Architecture

## Engine

`TweenEngine.Instance` is one shared ticker driving every tween. No per-animation timers, no setup. It ticks on the UI thread at about 60 Hz via a `DispatcherTimer` at Render priority while tweens are running, and stops itself (no timer, no allocations) while nothing animates. Frame deltas are clamped to 100 ms so stalls or debugger breaks never make tweens jump.

- `ActiveCount` / `MaxActiveCount` debugging surface.
- `StopAll()` stops everything, leaving animated values in place.
- `Tween.UnhandledException` raised when a callback throws. Callbacks are otherwise swallowed so one bad callback cannot break the ticker.
- Public `Update(TimeSpan)` for tests and custom pumps.

## Instances and pooling

A tween is a `TweenInstance<T>`, generic over the value type so per-frame writes stay unboxed. Instances live in a static per-value-type free list. `Acquire()` pops or allocates. Every death path (completion, stop, complete, cancellation, superseding) releases the instance back, clearing target/property references and callbacks so pooled instances never leak.

**Versioned handles.** The public `Tween` handle is a struct holding `(instance, version)`. Releasing to the pool bumps the version, so a stale handle can never control the instance's next owner: `IsAlive`/`Stop`/`Pause`/`Start`/`Progress`/`CancelOn` are no-ops on stale handles, `OnComplete` runs immediately, `await` completes. Tweens are non-reusable by design. Start a fresh one in the desired direction.

## Callbacks

Two styles, both stored on the instance:

- **Closure callbacks** (`Action`, `Action<double>`, `Action<T>`) allocate at the call site.
- **Target-based callbacks** (`OnComplete(target, Action<TTarget>)`, `OnUpdate(target, Action<TTarget,double>)`, `Custom(target, from, to, Action<TTarget,TValue>)`). The target is stored as `object`; the callback is a cached untyped wrapper (`CallbackCache`, keyed by method and target). With static lambdas the wrapper is created once per call site and reused, so these allocate nothing after first use.

## Cancellation

`CancelOn(CancellationToken)` stores the token and polls `IsCancellationRequested` each tick. No `token.Register`, no allocation. Cancellation applies within a frame. An already-canceled token stops the tween immediately. Canceled tweens leave the value where it is. Awaiting them throws `OperationCanceledException`.

## Awaiting

`GetAwaiter()` returns a struct `TweenAwaiter`. The continuation is stored on the instance and runs on any death (completion, stop, complete, cancellation, superseding), so awaiting never hangs. The only allocation from `await` is the async state machine itself.

## Latest-wins superseding

A `ConditionalWeakTable<AvaloniaObject, Dictionary<AvaloniaProperty, TweenInstance>>` tracks the newest tween per target property. A new tween on the same target property silently stops the previous one. `Tween.StopAll(target)` / `Tween.CompleteAll(target)` operate on the same table. Target-keyed `Custom`/`Delay` tweens opt into the same lifecycle via an internal sentinel property key.

## Performance

- About 0 allocations per frame while animating, for every supported type. Values are written via generic `SetValue` overloads and cached per-type interpolators (`Interpolator<T>`), no boxing. Guarded by tests (5,000 ticks under 1 KB).
- About 0 allocations per tween start after warm-up. Pooled instances, struct handles, cached target-based callbacks. Guarded by tests (10,000 start/stop cycles under 1 KB after warm-up).
- Closure callbacks (`() => ...`) and `await` still allocate. The async state machine and captured delegates are inherent to C#. Use target-based callbacks for hot paths.
