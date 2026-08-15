# TweenAvalonia

Programmatic, controllable tweens for Avalonia. Built because Avalonia's XAML `Transitions` are fire-and-forget: once a property change triggers a transition you can't stop it, complete it, chain to it, or get a completion callback. TweenAvalonia gives you a handle to every animation you start — and only the target and end value are required.

```csharp
using TweenAvalonia;

// Everything beyond the target and end value defaults: 1 second, SineEaseInOut, no delay
Tween.Opacity(myVisual, 0);

// Named arguments when you care about the details
Tween.Opacity(myVisual, 1, duration: 0.3, easing: Easing.OutCubic, delay: 0.1);

// Durations also accept TimeSpan
Tween.Opacity(myVisual, 0, TimeSpan.FromMilliseconds(300));

// Typed factories — the value type is checked at compile time
Tween.Color(accentBrush, Color.FromRgb(200, 40, 60));
Tween.Margin(panel, new Thickness(16));
Tween.Width(panel, 400);

// Any typed Avalonia property
Tween.To(transform, TranslateTransform.XProperty, 120);

// Raw values
Tween.Custom(0, 1, v => progressText.Text = $"{v:P0}");
```

## Target-keyed raw tweens — no stored handles

`Custom` and `Delay` accept a `target:` — the raw tween joins the same latest-wins lifecycle as property tweens: a new tween with the same target supersedes the previous one, and `StopAll`/`CompleteAll` cover it. No `Tween _field` + `.Stop()` bookkeeping:

```csharp
// Only one art fade ever runs; no stored handle needed
Tween.Custom(this, ArtOpacity, 0, v => ArtOpacity = v, 0.3, target: this);
Artwork = newArt; ArtOpacity = 0;
Tween.Custom(this, ArtOpacity, 1, v => ArtOpacity = v, 0.3, target: this);

// Cancel a pending delayed action when leaving the screen
Tween.Delay(2.0, () => ShowToast(), target: this);
Tween.StopAll(this);   // on navigation: pending delay never fires
```

## One line, done

Tween factories follow PrimeTween's shape: only `(target, to)` required, everything else optional with defaults — duration `1s`, `Tween.DefaultEasing` (SineEaseInOut), no delay.

```csharp
Tween.Opacity(visual, 1);        // fade in over 1s
Tween.Delay(0.5);                // a delay you can await
Tween.Delay(0.5, () => Close()); // delay + callback
```

`Tween.DefaultEasing` is settable, so an app can change the library-wide default once:

```csharp
Tween.DefaultEasing = Easing.OutCubic;
```

## Reusable settings

Bundle a whole animation config into a value and reuse it everywhere:

```csharp
var fadeIn = new TweenSettings<double>(to: 1, duration: 0.25, easing: Easing.OutCubic);
Tween.Opacity(panel, fadeIn);

var openAnim = new TweenSettings<Thickness>(to: new Thickness(16), duration: 0.2);
Tween.Margin(sidebar, openAnim);
```

## Control surface

Every factory returns a `Tween` handle — keep it in a variable to control the animation later:

| Member | Behaviour |
|---|---|
| `Stop()` | Stops the tween, leaving the animated value where it is. No `OnComplete`. |
| `Complete()` | Stops the tween and snaps the value to the end value. No `OnComplete`. |
| `Pause()` / `Resume()` | Freezes / continues the tween; elapsed time is preserved. |
| `Start()` | Restarts the tween from the beginning (works after completion too). |
| `OnComplete(Action)` | Invoked exactly once on natural completion; runs immediately if the tween already completed. |
| `OnUpdate(Action<double>)` | Invoked after every value write with the eased factor (0→1). |
| `ElapsedTime` / `Progress` | Readable and settable — scrub an animation forward or backward. |
| `CancelOn(CancellationToken)` | Stops the tween when the token cancels. |
| `await` | `await Tween.Opacity(...)` resumes when the tween dies; cancellation throws `OperationCanceledException`. |
| `IsAlive` | True while the tween is running or paused in the engine. |

`Tween.Custom` / `Tween.Delay` accept an optional `target:` to opt into the same lifecycle (see below).

Group control by target — handy when navigating away from a page:

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

## The engine

`TweenEngine.Instance` is one shared per-frame loop driving every tween — no per-animation timers.

- **Ticks once per rendered frame** via `TopLevel.RequestAnimationFrame` when attached to a window: `TweenEngine.Instance.Attach(window)` in your main window's constructor.
- Falls back to a 60 Hz `DispatcherTimer` when no top-level is attached.
- **Idle when nothing animates** — the loop sleeps and allocates nothing.
- Frame deltas are clamped (100ms) so a stall or debugger break never makes tweens jump.
- `TweenEngine.Instance.ActiveCount` / `MaxActiveCount` for debugging; `StopAll()` to stop everything.
- `Tween.UnhandledException` fires when a callback throws (callbacks are otherwise swallowed so one bad callback can't break the frame loop).

## Performance

- **~0 allocations per frame** while animating, for every supported type (guarded by tests: 5,000 ticks < 1 KB; values are written via generic `SetValue` overloads and cached per-type interpolators, no boxing).
- A handful of allocations per tween start (the instance + your call-site closures). This is a desktop-app library, not a game engine: no pooling, no target-based callbacks.

## Requirements

- .NET 8 or .NET 10, Avalonia 12.x (uses `TopLevel.RequestAnimationFrame` and the `IEasing` set from `Avalonia.Animation.Easings`).

## Repository layout

```
src/TweenAvalonia/          # the package (Tween, Tween.Factories, TweenInstance, TweenEngine, TweenSettings, TweenAwaiter, Interpolators)
tests/TweenAvalonia.Tests/  # NUnit tests (50, incl. per-frame allocation guards)
assets/icon.png             # NuGet package icon (lucide "move-right", ISC)
docs/
    ROADMAP.md              # planned work
    AVALONIA-ANIMATIONS-ANALYSIS.md   # what Avalonia's built-in animation system does
    PRIMETWEEN-ANALYSIS.md            # what PrimeTween does (design inspiration)
changelog.md                # release history
```

## Development

```sh
dotnet build TweenAvalonia.slnx     # net8.0 + net10.0
dotnet test TweenAvalonia.slnx
dotnet pack src/TweenAvalonia -c Release
```

## License

GPL-3.0 (see [LICENSE](LICENSE)). Package icon adapted from the [lucide](https://lucide.dev) icon set (ISC license).
