# Contributing

## Building and testing

```sh
dotnet build TweenAvalonia.slnx     # net8.0 + net10.0
dotnet test TweenAvalonia.slnx
dotnet pack src/TweenAvalonia -c Release
```

The build must stay at **0 warnings** on both target frameworks: `TreatWarningsAsErrors`, `GenerateDocumentationFile` and nullable annotations are enabled.

## Code style

- Every public member needs an XML doc comment. Enforced by the build.
- No inline comments in code; XML docs only.
- Keep the public surface minimal: defaults over parameters, `TweenSettings<T>` bundles over long signatures.

## Tests

- New behavior needs tests in `tests/TweenAvalonia.Tests/` (NUnit).
- The zero-allocation guarantees are guarded: per-frame guards (5,000 ticks < 1 KB) and per-start guards (10,000 start/stop cycles < 1 KB after warm-up). New code paths that run per frame or per tween start must stay inside these budgets.
- Tests drive the engine manually (`TweenEngine.Instance.AutoPumpEnabled = false` + `Update(...)`); fixtures are `[NonParallelizable]`.

## Adding a value type

1. Implement the lerp in `Interpolators` (e.g. `LerpThickness`).
2. Register it in `Interpolator<T>.Create` so the type resolves at creation time.
3. Add a typed interpolation test and an allocation guard for the new type.

## Adding a tween factory

1. Add the overloads (double-seconds + `TimeSpan`, plus `TweenSettings<T>` when it carries a value) in `Tween.cs` or `Tween.Factories.cs`.
2. Keep every parameter beyond `(target, to)` optional with the library defaults.
3. Add tests covering defaults, the time-span overload equivalence, and the handle lifecycle.

## Versioning

Manual `Version` in `src/TweenAvalonia/TweenAvalonia.csproj` + `vX.Y.Z` git tags. 0.x until the API is validated by a real consumer, then 1.0. New entries go in `docs/CHANGELOG.md`.
