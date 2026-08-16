# API reference

## Control surface

Every factory returns a `Tween` handle:

| Member | Behaviour |
|---|---|
| `Stop()` | Stop, leave the value where it is. No `OnComplete`. |
| `Complete()` | Stop and snap to the end value. No `OnComplete`. |
| `Pause()` / `Resume()` | Freeze / continue. Elapsed time is kept. |
| `Start()` | Restart a still-running tween. Completed tweens are pooled, not reusable. |
| `OnComplete(Action)` / `OnComplete(target, Action<TTarget>)` | Once on natural completion. Runs immediately if already completed. |
| `OnUpdate(Action<double>)` / `OnUpdate(target, Action<TTarget,double>)` | Every value write, with the eased factor (0 to 1). |
| `ElapsedTime` / `Progress` | Read and set. Scrub the animation. |
| `CancelOn(CancellationToken)` | Stop when the token cancels. Polled each tick. |
| `await` | Resumes when the tween dies. Cancellation throws `OperationCanceledException`. |
| `IsAlive` | True while running or paused. |

Handles are structs over pooled instances. Once a tween dies, stale handles do nothing (`Stop`, `Pause`, `Start`, `Progress`), `OnComplete` runs immediately, `await` completes.

`Custom` and `Delay` take an optional `target:` to opt into the same lifecycle as property tweens (latest-wins superseding, `StopAll`/`CompleteAll` coverage).

## Factories

Only `(target, to)` is required. Defaults: 1s duration, `Tween.DefaultEasing` (SineEaseInOut, settable), no delay. All factories have `double`-seconds and `TimeSpan` overloads plus a `TweenSettings<T>` overload.

- `Tween.To<T>(AvaloniaObject, AvaloniaProperty<T>, T to, ...)` any typed Avalonia property, compile-time checked.
- `Tween.Opacity(Visual, ...)` fade a visual's opacity.
- `Tween.Color(SolidColorBrush, ...)` fade a solid brush's color.
- `Tween.Margin(Layoutable, ...)`, `Tween.Width/Height(Layoutable, ...)` layout tweens.
- `Tween.Custom(from, to, Action<T>, ...)` raw values.
- `Tween.Custom(target, from, to, Action<TTarget,T>, ...)` raw values with zero-alloc target callbacks.
- `Tween.Delay(duration, onComplete, target)` awaitable UI-thread sleep.
- `Tween.StopAll(target)` / `Tween.CompleteAll(target)` group control.
- `TweenSettings` / `TweenSettings<T>` reusable animation bundles.

## Zero-allocation callbacks

Closure callbacks allocate per call. Pass the target with a static lambda instead:

```csharp
tween.OnComplete(target: this, static t => t.Commit());
tween.OnUpdate(target: this, static (t, f) => t.HandleFactor(f));
Tween.Custom(this, ArtOpacity, 0, static (vm, v) => vm.ArtOpacity = v, 0.3);
```

## Group control

```csharp
Tween.StopAll(panel);      // stop everything on the panel, keep values
Tween.CompleteAll(panel);  // stop everything, snap to end values
```

Latest-wins: a new tween on the same target property stops the previous one.

```csharp
Tween.Opacity(visual, 1, 0.2);
Tween.Opacity(visual, 0.5, 0.2);   // stops the first
```

## Value types

`double`, `float`, `int`, `Color`, `Point`, `Vector`, `Thickness`, `Rect` for properties and raw values. Unsupported types throw `NotSupportedException` at creation.
