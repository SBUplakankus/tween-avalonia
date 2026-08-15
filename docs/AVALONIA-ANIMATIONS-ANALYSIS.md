# Avalonia's built-in animation system — analysis

Notes from reading Avalonia's animation source (`Avalonia.Base/Animation`) to understand what exists before writing a tween library. File references are from the Avalonia 12.1-era source.

## The two built-in systems

### 1. `Transitions` (declarative, fire-and-forget)

XAML-declared per-control transitions that run automatically when a bound property value changes:

```xml
<Border.Opacity, Transition on Opacity>
```

How it works end to end:

1. `Animatable.OnPropertyChangedCore` watches every property change while `Transitions` is set (Animatable.cs:151-190).
2. On a change of a transitioned property it disposes the previous transition instance (so re-triggering **does cancel** the old one) and computes `oldValue`/`newValue`. Importantly, it retargets: if a value is currently mid-animation, the new transition **starts from the current animated value**, not the stale base value (Animatable.cs:171-178).
3. `Transition<T>.Apply` creates a `TransitionInstance` (Transition.cs:27-34) — a per-transition clock — and binds the result with `BindingPriority.Animation` via `control.Bind(...)` (Transition.cs:33).
4. `TransitionInstance` subscribes its own `TransitionClock` to the global clock, normalizes time (delay + duration), and publishes progress (TransitionInstance.cs:28-73).
5. `AnimatorTransitionObservable` eases and interpolates each tick through a static `Animator<T>` (AnimatorDrivenTransition.cs:16-17), producing the value.
6. Disposing the binding (or the control) stops the animation; the property falls back to its base value automatically (that's what the `Animation` binding priority does).

What you **can't** do: stop/complete/pause it from code, chain two transitions, get a completion callback, or query progress. It's a fire-and-forget visual.

Cost notes:
- **~6–8 allocations per transition start** (estimated from source): `TransitionInstance` + `TransitionClock` + its `ClockObservable` + parent subscription + `AnimatorTransitionObservable` + the binding entry in the property store, on top of the `AvaloniaPropertyChangedEventArgs` the change itself already allocated.
- **~0 allocations per frame** while running (the observable push path).
- Avalonia's own TODO: *"This clock is still fairly expensive due to ClockBase implementation"* (TransitionInstance.cs:104) — every animation builds its own clock chain subscribed to the global clock.

### 2. `Animation` (keyframe state machine)

The XAML keyframe system (`Animation` + `KeyFrame` + `Animator<T>` + `AnimationInstance<T>`):

- Full state machine: `IterationCount`, `PlaybackDirection` (Normal/Reverse/Alternate), `FillMode` (Forward/Backward/Both), `SpeedRatio`, `Delay`, `DelayBetweenIterations`, `Easing` (Animation.cs).
- `AnimationInstance<T>` runs the math per tick: iteration counting, playback reversal, eased interpolation between keyframes, neutral-value tracking (the base value the animation falls back to), fill application on stop/detach, visibility-based pausing (`PlaybackBehavior`), and auto-stop on visual-tree detach (AnimationInstance`1.cs).
- `Animator<T>.InterpolationHandler` finds the bracketing keyframes and interpolates (Animator`1.cs:26-39); 28 animator types exist (Double, Color, Thickness, brushes, transforms, box shadows…).
- `RunAsync` awaits a whole animation but gives **no mid-flight control** (no stop/pause/progress; cancellation only via `CancellationToken`).

Cost notes:
- **~10+ allocations per animation start** (estimated): keyframe interpretation (`InterpretKeyframes` builds dictionaries/lists/subscriptions per setter), `AnimatorKeyFrame` per setter, `AnimationInstance`, another clock, the binding.
- It's authoring-oriented (XAML), not scriptable: no way to build a keyframe animation from code without heavy ceremony.

## The clock model

- `IGlobalClock` is implemented by `MediaContextClock` (Media/MediaContext.Clock.cs): a list of observers pulsed **once per rendered frame** with a stopwatch-based timestamp; `RequestAnimationFrame` queues one-shot callbacks.
- `ClockBase` accumulates internal time from parent pulses; every animation creates its own child `Clock` → the per-animation chain mentioned above.
- The public entry point for frame-synced work: **`TopLevel.RequestAnimationFrame(Action<TimeSpan>)`** (Avalonia.Controls/TopLevel.cs:602) — this is what a tween engine should drive from.

## Easings

A complete `IEasing` set in `Avalonia.Animation.Easings` (30+ classes: Quadratic→Quintic, Sine, Expo, Circ, Back, Bounce, Elastic, Spring, Spline + `KeySpline`), all just `double Ease(double progress)` — directly reusable by a tween library.

## What this means for TweenAvalonia

| Gap in Avalonia | TweenAvalonia |
|---|---|
| Fire-and-forget, dispose-only control | `Stop`/`Complete`/`Pause`/`Resume`/`Start`/`OnComplete` handles |
| No scripted chaining | `OnComplete` chaining (the app's fade-out → swap → fade-in pattern) |
| Per-animation clock chain ("fairly expensive") | One shared engine loop |
| Transitions only trigger on property *changes* | Tweens run on demand, any time |
| No latest-wins at the call site | Same-target supersede built in |
| ~6–10+ allocs per start | ~2–3 per start, ~0 per frame |
| Keyframe interpretation overhead | Direct from/to + easing per instance |

Avalonia's retargeting (start from current animated value) and `BindingPriority.Animation` restore-on-dispose are the ideas worth keeping in mind for the engine's future; the per-tween clocks and the XAML authoring machinery are what we avoid.
