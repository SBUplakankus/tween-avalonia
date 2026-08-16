<div align="center">

# TweenAvalonia

![NuGet](https://img.shields.io/nuget/v/TweenAvalonia)
![Downloads](https://img.shields.io/nuget/dt/TweenAvalonia)
![CI](https://img.shields.io/github/actions/workflow/status/SBUplakankus/tween-avalonia/ci.yml)
![License](https://img.shields.io/github/license/SBUplakankus/tween-avalonia)

Programmatic tweens for Avalonia. Avalonia's XAML `Transitions` are fire-and-forget: you can't stop them, complete them, or get a completion callback. TweenAvalonia gives you a handle to every animation you start. Only the target and end value are required.

</div>

```csharp
using TweenAvalonia;

Tween.Opacity(myVisual, 0);       // 1s, SineEaseInOut, no delay
Tween.Opacity(myVisual, 1, duration: 0.3, easing: Easing.OutCubic, delay: 0.1);
Tween.Opacity(myVisual, 0, TimeSpan.FromMilliseconds(300));

Tween.Color(accentBrush, Color.FromRgb(200, 40, 60));
Tween.Margin(panel, new Thickness(16));
Tween.Width(panel, 400);

Tween.To(transform, TranslateTransform.XProperty, 120);

Tween.Custom(0, 1, v => progressText.Text = $"{v:P0}");
```

## Defaults

Only `(target, to)` is required. Defaults: 1s duration, `Tween.DefaultEasing` (SineEaseInOut), no delay.

```csharp
Tween.Opacity(visual, 1);        // fade in over 1s
Tween.Delay(0.5);                // a delay you can await
Tween.Delay(0.5, () => Close()); // delay with callback
```

Change the library-wide default once:

```csharp
Tween.DefaultEasing = Easing.OutCubic;
```

## Settings

Bundle an animation config and reuse it:

```csharp
var fadeIn = new TweenSettings<double>(to: 1, duration: 0.25, easing: Easing.OutCubic);
Tween.Opacity(panel, fadeIn);

var openAnim = new TweenSettings<Thickness>(to: new Thickness(16), duration: 0.2);
Tween.Margin(sidebar, openAnim);
```

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

`Custom` and `Delay` take an optional `target:` to opt into the same lifecycle.

## Raw tweens keyed by target

`Custom(target: ...)` and `Delay(target: ...)` join latest-wins and `StopAll`/`CompleteAll` like property tweens. No stored handle, no `Stop()` bookkeeping:

```csharp
Tween.Custom(this, ArtOpacity, 0, v => ArtOpacity = v, 0.3, target: this);
Artwork = newArt;
ArtOpacity = 0;
Tween.Custom(this, ArtOpacity, 1, v => ArtOpacity = v, 0.3, target: this);

Tween.Delay(2.0, () => ShowToast(), target: this);
Tween.StopAll(this);   // on navigation: pending delay never fires
```

## Zero-allocation callbacks

Closure callbacks allocate per call. Pass the target with a static lambda instead:

```csharp
tween.OnComplete(target: this, static t => t.Commit());
tween.OnUpdate(target: this, static (t, f) => t.HandleFactor(f));
Tween.Custom(this, ArtOpacity, 0, static (vm, v) => vm.ArtOpacity = v, 0.3);
```

Group control:

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

`double`, `float`, `int`, `Color`, `Point`, `Vector`, `Thickness`, `Rect` for properties (`Tween.To<T>`, `Tween.Color`, `Tween.Margin`) and raw values (`Tween.Custom<T>`). Unsupported types throw at creation.

## Documentation

| Doc | What's in it |
|---|---|
| [Quick start](https://github.com/SBUplakankus/tween-avalonia/blob/main/docs/QUICKSTART.md) | Case study: the BigScreen artwork crossfade |
| [Getting started](https://github.com/SBUplakankus/tween-avalonia/blob/main/docs/GETTING-STARTED.md) | Usage, settings, target-keyed raw tweens |
| [API reference](https://github.com/SBUplakankus/tween-avalonia/blob/main/docs/API.md) | Control surface, factories, value types |
| [Architecture](https://github.com/SBUplakankus/tween-avalonia/blob/main/docs/ARCHITECTURE.md) | Engine, pooling, handles, cancellation, performance |
| [Changelog](https://github.com/SBUplakankus/tween-avalonia/blob/main/docs/CHANGELOG.md) | Release history |
| [Contributing](https://github.com/SBUplakankus/tween-avalonia/blob/main/docs/CONTRIBUTING.md) | Build, test, code style |
| [License](https://github.com/SBUplakankus/tween-avalonia/blob/main/docs/LICENSE.md) | GPL-3.0 |

## Requirements

.NET 8 or .NET 10, Avalonia 12.x.

## Repository layout

```
src/TweenAvalonia/          # the package
tests/TweenAvalonia.Tests/  # NUnit tests
docs/                       # documentation
assets/icon.png             # package icon (lucide "move-right", ISC)
```

## License

GPL-3.0 (see [docs/LICENSE.md](docs/LICENSE.md)). Icon adapted from [lucide](https://lucide.dev) (ISC).
