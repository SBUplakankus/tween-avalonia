# Changelog

All notable changes to TweenAvalonia. Semver: 0.x until the API is validated by a real consumer, then 1.0.

## 0.3.0 (unreleased)

### Added
- **Zero-alloc tween starts** — instances are pooled per value type; handles are versioned structs, so stale handles can never control a pooled instance's next owner (stale `Stop`/`Pause`/`Start`/`Progress` are no-ops, `OnComplete` runs immediately, `await` completes)
- **Target-based callbacks** — `OnComplete(target, Action<TTarget>)`, `OnUpdate(target, Action<TTarget,double>)`, `Custom(target, from, to, Action<TTarget,TValue>, ...)` with static lambdas; wrappers are cached per call site, so callbacks allocate nothing after their first use
- **Struct awaiter** — `await tween` allocates only the async state machine; the continuation is stored on the instance
- **Polled cancellation** — `CancelOn` stores the token and checks it each tick (no registration allocation); cancellation applies within a frame; already-canceled tokens stop immediately

### Changed
- `Start()` is alive-only — completed tweens are pooled and cannot be revived (PrimeTween-style non-reusable model)
- `TweenEngine.StopAll()` no longer snapshots the tween list (no allocation)

## 0.2.1 (unreleased)

### Changed
- **Zero-configuration engine** — `TweenEngine.Attach(TopLevel)` removed; the shared ticker runs itself on a 60 Hz UI-thread dispatcher timer (Render priority) and sleeps while idle. No setup calls in app code.

## 0.2.0 (unreleased)

### Added
- **Typed API** — generic `Tween.To<T>` with compile-time property type checks; sugar factories `Tween.Opacity`, `Tween.Color` (SolidColorBrush), `Tween.Margin`, `Tween.Width`, `Tween.Height`
- **Interpolators** — `double`, `float`, `int`, `Color`, `Point`, `Vector`, `Thickness`, `Rect`; unboxed per-frame writes for all of them; unsupported types throw `NotSupportedException` at creation
- **PrimeTween-style defaults** — only `(target, to)` required (1s duration, default easing, no delay); `Tween.DefaultEasing` settable app-wide; `TimeSpan` overloads retained
- `TweenSettings` / `TweenSettings<T>` reusable animation bundles
- `Tween.Delay` as a first-class tween (`await`-able UI-thread sleep)
- **Target-keyed `Custom`/`Delay`** — optional `target:` opt-in to latest-wins superseding and `StopAll`/`CompleteAll` coverage
- `ElapsedTime` / `Progress` get+set (scrubbing); `OnUpdate(Action<double>)` per-frame eased-factor callback
- `await tween` (custom awaiter, no threads) + `CancelOn(CancellationToken)`; cancellation throws `OperationCanceledException` on await
- `Tween.StopAll(target)` / `Tween.CompleteAll(target)`; `TweenEngine.StopAll()`, `ActiveCount`, `MaxActiveCount`
- net8.0 + net10.0 multi-targeting, embedded debug symbols, package icon, repository metadata

### Changed
- `Tween.Custom` signature reordered to `(from, to, onValueChange, duration, easing, delay, target)` — the callback now precedes the optional parameters
- `Tween.To` (non-generic, runtime type check) replaced by typed `Tween.To<T>`

### Fixed
- Unsupported tween value types now fail fast with a clear message instead of failing mid-animation

## 0.1.0 (never published)

- Core engine: `Tween.To` / `Tween.Opacity` / `Tween.Custom` returning `Tween` handles; `Stop`/`Complete`/`Pause`/`Resume`/`Start`/`OnComplete`/`IsAlive`
- Latest-wins superseding on the same target property
- One shared frame loop (`TopLevel.RequestAnimationFrame`, 60 Hz `DispatcherTimer` fallback, idle when empty, 100 ms delta clamp)
- ~0 allocations per frame; `Tween.UnhandledException` hook
- 15 NUnit tests; standalone package repo
