# API reference

## Control surface

Every factory returns a `Tween` handle — keep it in a variable to control the animation later:

| Member | Behaviour |
|---|---|
| `Stop()` | Stops the tween, leaving the animated value where it is. No `OnComplete`. |
| `Complete()` | Stops the tween and snaps the value to the end value. No `OnComplete`. |
| `Pause()` / `Resume()` | Freezes / continues the tween; elapsed time is preserved. |
| `Start()` | Restarts a still-running tween from the beginning. Completed tweens are pooled and not reusable — start a fresh one instead. |
| `OnComplete(Action)` / `OnComplete(target, Action<TTarget>)` | Invoked exactly once on natural completion; runs immediately if the tween already completed. |
| `OnUpdate(Action<double>)` / `OnUpdate(target, Action<TTarget,double>)` | Invoked after every value write with the eased factor (0→1). |
| `ElapsedTime` / `Progress` | Readable and settable — scrub an animation forward or backward. |
| `CancelOn(CancellationToken)` | Stops the tween when the token cancels (polled each tick, within a frame). |
| `await` | `await Tween.Opacity(...)` resumes when the tween dies; cancellation throws `OperationCanceledException`. |
| `IsAlive` | True while the tween is running or paused in the engine. |

A handle is a struct wrapping a pooled instance plus a version: once the tween dies, stale handles become inert — `Stop`/`Pause`/`Start`/`Progress` are no-ops, `OnComplete` runs immediately, `await` completes.

`Tween.Custom` / `Tween.Delay` accept an optional `target:` to opt into the same lifecycle as property tweens (latest-wins superseding + `StopAll`/`CompleteAll` coverage).

## Factories

Only `(target, to)` is required; everything else defaults — duration `1s`, `Tween.DefaultEasing` (SineEaseInOut, settable app-wide), no delay. All factories have `double`-seconds and `TimeSpan` overloads plus a `TweenSettings<T>` overload.

- `Tween.To<T>(AvaloniaObject, AvaloniaProperty<T>, T to, ...)` — any typed Avalonia property, compile-time checked.
- `Tween.Opacity(Visual, ...)` — fade a visual's opacity.
- `Tween.Color(SolidColorBrush, ...)` — fade a solid brush's color.
- `Tween.Margin(Layoutable, ...)`, `Tween.Width/Height(Layoutable, ...)` — layout tweens.
- `Tween.Custom(from, to, Action<T>, ...)` — raw values.
- `Tween.Custom(target, from, to, Action<TTarget,T>, ...)` — raw values with zero-alloc target callbacks.
- `Tween.Delay(duration, onComplete, target)` — awaitable UI-thread sleep.
- `Tween.StopAll(target)` / `Tween.CompleteAll(target)` — group control.
- `TweenSettings` / `TweenSettings<T>` — reusable animation bundles.

## Zero-allocation callbacks

Closure callbacks (`() => ...`) allocate a delegate per call. For allocation-free callbacks, pass the target and use a **static lambda** — the callback is cached per call site after its first use:

```csharp
tween.OnComplete(target: this, static t => t.Commit());
tween.OnUpdate(target: this, static (t, f) => t.HandleFactor(f));
Tween.Custom(this, ArtOpacity, 0, static (vm, v) => vm.ArtOpacity = v, 0.3);
```

## Group control by target

Handy when navigating away from a page:

```csharp
Tween.StopAll(panel);      // kill every animation on the panel, keep values
Tween.CompleteAll(panel);  // kill every animation, snap to end values
```

**Latest-wins built in:** starting a new tween on the same target property silently stops the previous one. Rapid-fire calls can't stack up — ideal for hover/reveal states and art swaps:

```csharp
Tween.Opacity(visual, 1, 0.2);     // the second tween automatically
Tween.Opacity(visual, 0.5, 0.2);   // stops the first — no bookkeeping
```

## Supported value types

`double`, `float`, `int`, `Color`, `Point`, `Vector`, `Thickness`, `Rect` — for properties (`Tween.To<T>` / `Tween.Color` / `Tween.Margin` / ...) and raw values (`Tween.Custom<T>`). Unsupported types throw `NotSupportedException` at creation, not mid-animation.
