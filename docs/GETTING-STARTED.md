# Getting started

Only `(target, to)` is required. Everything else defaults: 1s duration, `Tween.DefaultEasing` (SineEaseInOut), no delay.

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

Bundle a config and reuse it:

```csharp
var fadeIn = new TweenSettings<double>(to: 1, duration: 0.25, easing: Easing.OutCubic);
Tween.Opacity(panel, fadeIn);

var openAnim = new TweenSettings<Thickness>(to: new Thickness(16), duration: 0.2);
Tween.Margin(sidebar, openAnim);
```

## Raw tweens keyed by target

`Custom` and `Delay` take an optional `target:`. The tween joins latest-wins and `StopAll`/`CompleteAll` like property tweens. No stored handle, no `Stop()` bookkeeping:

```csharp
Tween.Custom(this, ArtOpacity, 0, v => ArtOpacity = v, 0.3, target: this);
Artwork = newArt;
ArtOpacity = 0;
Tween.Custom(this, ArtOpacity, 1, v => ArtOpacity = v, 0.3, target: this);

Tween.Delay(2.0, () => ShowToast(), target: this);
Tween.StopAll(this);   // on navigation: pending delay never fires
```
