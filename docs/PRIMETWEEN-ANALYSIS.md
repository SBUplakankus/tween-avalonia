# PrimeTween — analysis (design inspiration)

PrimeTween is a Unity tween library (asset-store + UPM, GPL-3.0). It was the design reference for TweenAvalonia. Findings from its README, benchmark suite, and the author's performance discussion.

## Design philosophy

- **One line, static factories, struct handles.** `Tween.PositionY(transform, endValue: 10, duration: 1, ease: Ease.InOutSine)` returns a `Tween` struct — cheap to hold, copy, and check. The struct wraps a shared pooled instance.
- **Non-reusable tweens.** A tween is "dead" after completion and can't be replayed. For toggle-style animations (open/close, show/hide) you start a **fresh tween in the desired direction** instead of caching one — starting is so cheap that caching is a net loss. The new tween seamlessly continues from the current value and overwrites running tweens on the same target.
- **Zero allocations, ever** (runtime): pooled instances, cached delegates, and **target-based callbacks** to avoid closures: `tween.OnComplete(target: this, target => target.SomeMethod())` instead of `() => SomeMethod()`.
- **Inspector integration** as a cornerstone: serializable `TweenSettings<T>` passed straight to the factories; PRO adds visual animation authoring.

## Control surface

`tween.isAlive`, `Stop()` (leave value), `Complete()` (snap to end), `isPaused`, `elapsedTime` (settable), `progress`, `interpolationFactor`, `timeScale`, `SetRemainingCycles(...)`, `SetCancellationToken(...)`. Group control: `Tween.StopAll(onTarget:)`, `Tween.CompleteAll(onTarget:)`, `Tween.PausedAll(...)`.

## Sequencing & async

`Sequence.Create()` with `Chain(...)` (sequential), `Group(...)` (parallel with the previous item), `Insert(atTime, ...)` (overlapping), `ChainCallback`, `InsertCallback`; cycles on the sequence too. Plus coroutine (`ToYieldInstruction()`) and `async/await` support with `CancellationToken`.

## Cycles

`cycles: N, cycleMode: CycleMode.X` — `Restart` (default), `Yoyo`, `Incremental`, `Rewind`; `-1` = infinite. The library is explicitly game-oriented: shakes, punches, camera effects, time scale, Update/Late/Fixed update types, parametric easing (`Easing.BounceExact(amplitude)`, `Easing.Overshoot(strength)`).

## Performance (vendor benchmarks)

From the official comparison (Unity 2022.3.9, MacBook Pro M1, IL2CPP, 100k iterations, average frame time ms — lower is better):

| Test | DOTween | LeanTween | UnityTweens | **PrimeTween** | vs 2nd best |
|---|---|---|---|---|---|
| Animation start | 33.54 | 15.00 | 43.18 | **5.76** | 2.6x |
| Animation start (all params) | 38.01 | 24.68 | 39.73 | **6.72** | 3.67x |
| Position animation | 8.91 | 12.54 | 7.88 | **4.34** | 1.82x |
| Custom animation | 4.93 | 4.45 | 4.78 | **3.28** | 1.36x |
| Delay | 4.17 | 4.26 | 1.99 | 2.07 | **0.96x (loses)** |
| Sequence (3 tweens) | 9.36 | 9.49 | — | **2.83** | 3.31x |
| Sequence start | 45.59 | 49,963! | — | **8.83** | 5.16x |

GCAlloc per iteration: PrimeTween **0 B** vs DOTween 734 B, LeanTween 292 B, UnityTweens 878 B (animation start).

Caveats worth knowing:
- The win is **zero GC, not raw speed**: on low-end Android, real-world reports show PrimeTween ~0.9–1.3x of DOTween at small tween counts; the author calls 30–40 tweens "impossible to measure precisely".
- Assertions are on in dev builds; the benchmarks run with `PRIME_TWEEN_DISABLE_ASSERTIONS` for release performance.
- Delays lose to LeanTween (delays are just regular tweens there — a deliberate trade).

## What TweenAvalonia borrowed

| Idea | Where |
|---|---|
| Static `Tween.` factories returning struct handles | `Tween.To<T>` / `Tween.Opacity` / `Tween.Color` / `Tween.Margin` / `Tween.Width` / `Tween.Height` / `Tween.Custom<T>` / `Tween.Delay` |
| Defaults for everything but the essentials | only `(target, to)` required — 1s duration, `Tween.DefaultEasing` (settable), no delay; `TimeSpan` overloads retained |
| `Stop` (leave value) / `Complete` (snap) / `IsAlive` | handle ops |
| Non-reusable, start-fresh pattern | handles are one-shot; `Start()` restarts for the rare reuse case |
| Same-target overwrite without bookkeeping | latest-wins `ConditionalWeakTable`; extended to raw tweens via target-keyed `Custom`/`Delay` (sentinel key) |
| One global update loop instead of per-tween timers | `TweenEngine` via `RequestAnimationFrame` |
| `OnComplete` as the chaining primitive | used for fade-out → swap → fade-in crossfades |
| `TweenSettings<T>` reusable animation bundles | `TweenSettings` / `TweenSettings<T>` with optional-param constructors |
| Settable elapsed time / progress | `ElapsedTime` / `Progress` get+set (scrubbing) |
| `await tween` | custom `TweenAwaiter`, no threads; `CancelOn(CancellationToken)` throws `OperationCanceledException` |

## What we deliberately left out (and why)

- **Sequences (`Chain`/`Group`/`Insert`)** — `await` + `Tween.Delay` composes the same linear flows in app code; no overlapping-timing need in a desktop UI. This is a .NET app library, not a game engine.
- **Cycles / Yoyo / repeats** — nothing in the target apps repeats; the direction pattern (start a fresh tween) covers pulses.
- **timeScale (per-tween or global)** — no slow-mo/pause needs; an engine-level pause would be the simple answer if a freeze ever surfaces.
- **Target-based zero-alloc callbacks / pooling** — a desktop app creates a handful of tweens per interaction, not thousands; the guarantee kept is ~0 allocations per *frame*, not per *start*.
- **Shakes/punches/parametric easing, update types, inspector integration** — game- or editor-oriented.
- **`WithDirection()` on settings** — exists for Unity inspector serialization (start+end values); the app pattern `isOpen ? 1 : 0` is one line.
