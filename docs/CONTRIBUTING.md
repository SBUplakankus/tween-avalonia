# Contributing

## Project structure

- `src/TweenAvalonia/` the package
- `tests/TweenAvalonia.Tests/` NUnit tests

## Naming conventions

- Methods and properties: PascalCase
- Private fields: `_camelCase`
- Local variables: camelCase

## Coding standards

- File-scoped namespaces
- Using directives sorted alphabetically, system namespaces first
- Prefer `var` when the type is obvious, explicit types when clarity is needed
- XML doc comments on public and internal types and members
- Inline comments sparingly, only when the intent is not obvious from the code
- 4-space indentation, opening braces on a new line
- Expression-bodied members for simple methods and properties
- Argument validation with `ArgumentNullException.ThrowIfNull` / `ArgumentOutOfRangeException`. Let exceptions propagate; the package has no logging

## Tests

- New behavior needs NUnit tests; fixtures are `[NonParallelizable]`
- The build must stay at 0 warnings on net8.0 and net10.0 (`TreatWarningsAsErrors`)
- Zero-allocation guarantees are guarded: per-frame (5,000 ticks < 1 KB) and per-start (10,000 start/stop cycles < 1 KB after warm-up). New code paths that run per frame or per tween start must stay inside these budgets
- Tests drive the engine manually (`TweenEngine.Instance.AutoPumpEnabled = false` + `Update(...)`)

## Adding a value type

1. Implement the lerp in `Interpolators`
2. Register it in `Interpolator<T>.Create` so it resolves at creation time
3. Add a typed interpolation test and an allocation guard

## Adding a tween factory

1. Add the overloads (double-seconds + `TimeSpan`, plus `TweenSettings<T>` when it carries a value) in `Tween.cs` or `Tween.Factories.cs`
2. Keep every parameter beyond `(target, to)` optional with the library defaults
3. Add tests covering defaults, overload equivalence, and the handle lifecycle

## Versioning and releases

- Manual `Version` in `src/TweenAvalonia/TweenAvalonia.csproj`; 0.x until the API is validated by a real consumer
- Release with a `vX.Y.Z` git tag. `publish.yml` verifies the tag matches the package version, builds and tests, then publishes via trusted publishing (no API keys)
- New entries go in `docs/CHANGELOG.md`

## Submitting changes

- Branch naming: `feature/description`, `bugfix/description`, `refactor/description`, `docs/description`
- Commit format: `[Feature]`, `[Bugfix]`, `[Refactor]`, `[Docs]` prefixes. Keep commits atomic and focused
- Push and open a pull request targeting `main`
