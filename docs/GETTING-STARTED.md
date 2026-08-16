# Getting started

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

## Target-keyed raw tweens — no stored handles

`Custom` and `Delay` accept a `target:` — the raw tween joins the same latest-wins lifecycle as property tweens: a new tween with the same target supersedes the previous one, and `StopAll`/`CompleteAll` cover it. No `Tween _field` + `.Stop()` bookkeeping:

```csharp
Tween.Custom(this, ArtOpacity, 0, v => ArtOpacity = v, 0.3, target: this);
Artwork = newArt;
ArtOpacity = 0;
Tween.Custom(this, ArtOpacity, 1, v => ArtOpacity = v, 0.3, target: this);

Tween.Delay(2.0, () => ShowToast(), target: this);
Tween.StopAll(this);   // on navigation: pending delay never fires
```
