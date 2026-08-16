# Quick start — the XeniaManager.BigScreen case study

TweenAvalonia was built for a real problem: **XeniaManager.BigScreen**, a fullscreen game dashboard, needed an artwork crossfade it couldn't express with Avalonia's built-in animations. This doc walks through the use case, the code that replaced the built-ins, and what each piece does.

## The problem: why Avalonia's built-in animations fell short

BigScreen's dashboard shows a background image that must crossfade whenever the selected game changes:

1. fade the current artwork **out** (base background shows through — no black frame)
2. **swap** in the new artwork
3. fade the new artwork **in**

Avalonia offers two built-in systems, and neither could do this:

| Need | `Transitions` (declarative) | `Animation` (keyframes) | Result |
|---|---|---|---|
| Fire a callback when a fade **completes** | ❌ no completion event | ❌ no code callback | the swap step has no trigger |
| Stop/cancel an in-flight fade | ❌ plays to the end | ❌ runs to completion | rapid game changes queue stale swaps |
| Run an ordered sequence (out → swap → in) | ❌ single property transition | ❌ authoring-oriented | can't express the pipeline |
| Animate from code at runtime | ⚠️ only via property changes | ⚠️ XAML-authoring focused | the VM can't drive them |
| Control a fade after it started | ❌ fire-and-forget | ❌ | no handle, no Stop/Complete |

The dashboard also needed the fade to live in the **view model** (the VM owns `ArtOpacity`, bound to the image layer) — neither built-in can animate a plain VM property.

## The use case

Dashboard artwork layer in `MainWindow.axaml`:

```xml
<Image Source="{Binding Dashboard.Artwork}"
       Opacity="{Binding Dashboard.ArtOpacity}" ... />
```

On every game selection change the dashboard must crossfade `Artwork` to the new game's art — or snap instantly for settings changes. The old artwork's fade-out must complete before the swap, and only the **latest** request may win (rapid D-pad navigation).

## The code

`DashboardViewModel` owns the whole fade on its bound `ArtOpacity` value:

```csharp
private Tween _artFade;                       // handle to the in-flight fade

private Tween FadeArtOpacity(double to) =>
    Tween.Custom(this, ArtOpacity, to, static (vm, v) => vm.ArtOpacity = v, 0.3);

private void CommitArtwork()
{
    Artwork = _pendingArtwork;                // swap happens here, after fade-out
    ArtOpacity = 0;
    _artFade = FadeArtOpacity(1);             // fade back in
}

private void UpdateArtworkLayer(Bitmap? newArt, bool hasArtwork, bool fade)
{
    if (!hasArtwork) { _artFade.Stop(); Artwork = null; ArtOpacity = 0; return; }
    if (ReferenceEquals(newArt, Artwork)) { _artFade.Stop(); ArtOpacity = 1; return; }
    if (fade)
    {
        _artFade.Stop();                      // cancel any in-flight fade
        _pendingArtwork = newArt;
        _artFade = FadeArtOpacity(0).OnComplete(target: this, static t => t.CommitArtwork());
        return;
    }
    _artFade.Stop();
    Artwork = newArt;                         // instant swap (settings changes)
    ArtOpacity = 1;
}
```

## What each piece does

- **`Tween.Custom(this, ArtOpacity, to, static (vm, v) => vm.ArtOpacity = v, 0.3)`**
  Animates the VM's `ArtOpacity` double from its current value to `to` over 0.3s. The item being tweened — the view model — is passed in; the static lambda writes the value back (cached per call site, so it allocates nothing). The `ArtOpacity` change flows through the binding to the image layer's `Opacity`.
- **`_artFade`** — the handle. `_artFade.Stop()` cancels the previous fade, leaving the value where it is. This is what `Transitions` can't give you.
- **`.OnComplete(target: this, static t => t.CommitArtwork())`** — fires **only on natural completion** of the fade-out. A superseded fade (newer request wins) is stopped, so its `OnComplete` never runs and a stale swap can never happen. This is the swap trigger that doesn't exist in `Transitions`.
- **`CommitArtwork()`** — the middle step: swap `Artwork`, reset opacity to 0, start the fade-in.

The same pattern in one line, for visuals the view owns — the boot reveal:

```csharp
_headerFade.Stop();
HeaderRow.Opacity = 0;
_headerFade = Tween.Opacity(HeaderRow, 1, TimingConstants.LaunchFadeDuration);
```

`Tween.Opacity` passes the actual visual and animates its real `OpacityProperty` — the same handle/stop/supersede lifecycle, no callbacks needed.

## The result

- The crossfade is three ordered steps in ~30 lines of view-model code
- Rapid selection changes are safe by construction: latest request wins, stale callbacks never fire
- Zero allocations per frame and per start after warm-up (see [Architecture](ARCHITECTURE.md))
- The view stays dumb — it binds `Artwork` + `ArtOpacity`, the VM does the animating
