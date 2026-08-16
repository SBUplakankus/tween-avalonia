# Quick start

TweenAvalonia was built for a real problem. XeniaManager.BigScreen, a fullscreen game dashboard, needed an artwork crossfade that Avalonia's built-in animations could not express.

## Why the built-ins fell short

The dashboard crossfades the background art on every game selection change:

1. fade the current art out (the base background shows through)
2. swap in the new art
3. fade the new art in

Avalonia has two built-in systems. Neither works here.

| Need | `Transitions` | `Animation` |
|---|---|---|
| Callback when a fade completes | no | no |
| Stop an in-flight fade | no | no |
| Ordered sequence (out, swap, in) | no | no |
| Drive from code | no | no |
| Animate a VM property | no | no |

Transitions are declarative and fire-and-forget. There is no completion event, no handle, no cancel. The swap step needs a completion trigger, and rapid game changes need stale swaps cancelled.

The fade also has to live in the view model: the VM owns `ArtOpacity`, bound to the image layer's opacity. The built-ins cannot animate a plain VM property.

## The use case

Dashboard art layer in `MainWindow.axaml`:

```xml
<Image Source="{Binding Dashboard.Artwork}"
       Opacity="{Binding Dashboard.ArtOpacity}" ... />
```

On every selection change, crossfade to the new game's art. Settings changes snap instantly. Only the latest request wins.

## The code

`DashboardViewModel` owns the fade:

```csharp
private Tween _artFade;

private Tween FadeArtOpacity(double to) =>
    Tween.Custom(this, ArtOpacity, to, static (vm, v) => vm.ArtOpacity = v, 0.3);

private void CommitArtwork()
{
    Artwork = _pendingArtwork;
    ArtOpacity = 0;
    _artFade = FadeArtOpacity(1);
}

private void UpdateArtworkLayer(Bitmap? newArt, bool hasArtwork, bool fade)
{
    if (!hasArtwork) { _artFade.Stop(); Artwork = null; ArtOpacity = 0; return; }
    if (ReferenceEquals(newArt, Artwork)) { _artFade.Stop(); ArtOpacity = 1; return; }
    if (fade)
    {
        _artFade.Stop();
        _pendingArtwork = newArt;
        _artFade = FadeArtOpacity(0).OnComplete(target: this, static t => t.CommitArtwork());
        return;
    }
    _artFade.Stop();
    Artwork = newArt;
    ArtOpacity = 1;
}
```

## What each piece does

- `Tween.Custom(this, ArtOpacity, to, static (vm, v) => vm.ArtOpacity = v, 0.3)`. Animates the VM's `ArtOpacity` double to `to` over 0.3s. The VM is the target. The static lambda writes the value back; it is cached per call site, so it allocates nothing. The binding carries the value to the image layer's opacity.
- `_artFade`. The handle. `Stop()` cancels the previous fade and leaves the value where it is. Transitions cannot do this.
- `OnComplete(target: this, static t => t.CommitArtwork())`. Fires only on natural completion. A superseded fade is stopped, so its callback never runs and a stale swap cannot happen.
- `CommitArtwork()`. The middle step: swap the art, reset opacity, fade back in.

For a visual the view owns, the same pattern is one line:

```csharp
_headerFade.Stop();
HeaderRow.Opacity = 0;
_headerFade = Tween.Opacity(HeaderRow, 1, TimingConstants.LaunchFadeDuration);
```

`Tween.Opacity` takes the visual and animates its real `OpacityProperty`.

## Result

The crossfade is three ordered steps in about 30 lines of VM code. Rapid selection changes are safe by construction. Zero allocations per frame and per start after warm-up (see [Architecture](ARCHITECTURE.md)). The view binds `Artwork` and `ArtOpacity`; the VM animates.
