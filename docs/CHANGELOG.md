# Changelog

Semver: 0.x until the API is validated by a real consumer, then 1.0.

## 0.3.0 (unreleased)

### Added

- Zero-alloc tween starts. Pooled per-type instances, versioned struct handles.
- Target-based callbacks (`OnComplete`, `OnUpdate`, `Custom`) with per-call-site cached wrappers.
- Struct awaiter. `await` allocates only the async state machine.
- Polled cancellation. No registration allocation; applies within a frame.

### Changed

- `Start()` is alive-only. Completed tweens are pooled and cannot be revived.
- `TweenEngine.StopAll()` no longer snapshots the tween list.

## 0.2.1

### Changed

- Zero-configuration engine. `TweenEngine.Attach(TopLevel)` removed. The shared ticker runs itself and sleeps while idle.

## 0.2.0

### Added

- Typed API (`Tween.To<T>`, Opacity/Color/Margin/Width/Height sugar).
- Interpolators for double, float, int, Color, Point, Vector, Thickness, Rect.
- PrimeTween-style defaults. Settable `Tween.DefaultEasing`, `TimeSpan` overloads.
- `TweenSettings`/`TweenSettings<T>`, `Tween.Delay`, target-keyed `Custom`/`Delay`.
- `ElapsedTime`/`Progress` scrubbing, `OnUpdate`, `await`, `CancelOn`, `StopAll`/`CompleteAll(target)`.
- net8.0 + net10.0 multi-targeting, embedded symbols, package icon, repository metadata.

### Changed

- `Tween.Custom` signature reordered to `(from, to, onValueChange, ...)`.
- Non-generic `Tween.To` replaced by typed `Tween.To<T>`.

## 0.1.0 (never published)

- Core engine, latest-wins superseding, one shared frame loop, about 0 allocations per frame, `Tween.UnhandledException`, 15 tests.
