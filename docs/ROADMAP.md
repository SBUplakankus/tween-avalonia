# Roadmap

Status of the package and what's planned. Roughly in priority order; nothing here is committed until it's actually needed by a consumer.

## Done (0.2.0)

- [x] Core engine: `Tween.To` / `Tween.Opacity` / `Tween.Custom` returning `Tween` handles
- [x] Control ops: `Stop` (leave value), `Complete` (snap), `Pause`/`Resume`, `Start` (restart), `OnComplete`, `IsAlive`
- [x] Latest-wins: new tween on the same target property silently supersedes the previous one
- [x] One shared frame loop (`TopLevel.RequestAnimationFrame`, 60 Hz `DispatcherTimer` fallback, idle when empty, 100ms delta clamp)
- [x] ~0 allocations per frame (generic `SetValue` writes, cached per-type interpolators, allocation guard tests)
- [x] `Tween.UnhandledException` hook for throwing callbacks
- [x] **Typed API**: generic `Tween.To<T>` with compile-time type checks; `Tween.Color` / `Tween.Margin` / `Tween.Width` / `Tween.Height` sugar
- [x] **PrimeTween-style defaults**: only `(target, to)` required — 1s duration, default easing, no delay; `Tween.DefaultEasing` settable app-wide; `TimeSpan` overloads retained
- [x] **Interpolators**: double, float, int, Color, Point, Vector, Thickness, Rect; unboxed per-frame writes for all of them; unsupported types fail at creation
- [x] `TweenSettings` / `TweenSettings<T>` reusable animation bundles with optional-param constructors
- [x] `Tween.Delay` as a first-class tween
- [x] **Target-keyed `Custom`/`Delay`** — optional `target:` gives raw tweens latest-wins superseding and `StopAll`/`CompleteAll` coverage, killing stored-handle + `Stop()` boilerplate (dashboard art crossfade pattern)
- [x] `ElapsedTime` / `Progress` get/set (scrubbing), `OnUpdate(Action<double>)` per-frame factor callback
- [x] `await tween` (custom awaiter, no threads) + `CancelOn(CancellationToken)`; cancellation throws `OperationCanceledException` on await
- [x] `Tween.StopAll(target)` / `Tween.CompleteAll(target)`; `TweenEngine.StopAll()`, `ActiveCount`, `MaxActiveCount`
- [x] 50 NUnit tests incl. per-type allocation guards, await/cancel, scrubbing, settings, target-keyed lifecycle
- [x] **Cleanup pass**: net8.0 + net10.0 multi-targeting, embedded symbols, package icon (lucide), repository metadata, `Tween.cs` split into handle + factories partials, changelog, .editorconfig, global.json SDK pin
- [x] Standalone package repo, `dotnet pack`-able, v0.2.0

## Short term

- [ ] **Consume the package from XeniaManager** — replace `XeniaManager.Core.Tweening` with the `TweenAvalonia` package reference (BigScreen background crossfade + settings slider paths), delete the in-repo copy; refactor the art crossfade to target-keyed `Tween.Custom`
- [ ] **Migrate legacy timer loops** in XeniaManager: `AnimationExtensions.AnimateOpacity` (16ms `DispatcherTimer`, drifts) and `NotificationService`'s 120fps loops
- [ ] **Publish** — CI (GitHub Actions: build + test + pack + tag-triggered push to nuget.org), versioning policy (manual `Version` + `vX.Y.Z` git tags), first release after the XeniaManager consumption validates the API

## Medium term

- [ ] **Auto-stop on detach** — stop tweens when their `Visual` target leaves the visual tree (`DetachedFromVisualTree`), so removed pages never animate dead controls
- [ ] **Avalonia release support matrix** — test against stable Avalonia releases; net8.0 multi-targeting when a consumer needs it
- [ ] More interpolators if consumers need them (`Size`, `CornerRadius`, `Vector3`-style math types)

## Later (only if a consumer actually needs them)

- [ ] Cycles/repeat (`IterationCount`, yoyo) — not a UI-tween need so far
- [ ] Per-tween `timeScale`, global time scale
- [ ] Easing parity with the Avalonia `Easing` class (extra curve types)
- [ ] Zero-alloc tween starts — instance pooling and target-based callbacks (`OnComplete(target, cb)` like PrimeTween)

## Explicitly not planned (game-oriented, wrong fit for a .NET app library)

- Sequences (`Chain`/`Group`/`Insert`), shakes/punches, parametric easing, LateUpdate/FixedUpdate update types, inspector integration
