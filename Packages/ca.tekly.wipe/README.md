# Tekly Wipe

Screen transitions for uGUI in one pass: wipes, irises, staggered bars and tiles, ramp textures and silhouette irises, with colour layers, live canvas reveals and Tweenline tracks.

## The model

**Coverage is how much is covered: 0 is none and 1 is all. Cover raises it and Uncover lowers it.**

A look's **Exit** decides what Uncover does. `Continue` carries the wipe on through, so it uncovers in the order it covered. `Rewind` goes back the way it came, which is what an iris wants.

## Pieces

| Piece | What it is |
|---|---|
| `Wipe` | A graphic that covers its rect with a colour or a sprite. `Coverage`, `Cover()`, `Uncover()`. |
| `WipeReveal` | On a root canvas. Wipes the canvas's own live content in and out. |
| `ScreenWipe` | A full screen `Wipe` that needs no scene setup. Play mode only. |
| `WipeLook` | An asset holding a `WipeStyle`, shared by anything above. Each can also hold its own inline style. |
| `WipeTrack`, `WipeRevealTrack` | Tweenline tracks of Cover and Uncover clips. |
| `WipeFocus` | Centers a Radial, Angular or Shape field on a Transform or a screen point. |
| `WipeShape` | A silhouette for a Shape iris, baked from a texture's alpha. A `.wipeshape` asset. |
| `WipeRamp` | A greyscale texture for a Ramp wipe, with the value ranges baked so the wipe has no dead time. A `.wiperamp` asset. |

`WipeShape` and `WipeRamp` are imported assets, made from Create > Tekly. The file holds nothing and the texture is an import setting, so pick it in the inspector and press Apply. The import depends on the texture, so the bake is redone whenever the texture changes and is never committed or stale. The texture is read back through the GPU. On a machine with no graphics device only PNG and JPG textures can be baked.

A `WipeShape` also has settings for the bake: Resolution (64 to 512, raise it for fine detail or sharp corners), Channel (Alpha, or Red for a black and white mask) and Threshold (the value from which a pixel counts as inside). A distance texture always rounds sharp corners a little, and a higher resolution rounds them less.

## A look

A look is built from parts, the way a motion design tool builds a transition from a duplicator, a stagger and layer copies.

- **Field**, where the wipe goes when: Linear (angle, mirror), Radial (circle, diamond, square, star), Angular (arms, twist), Ramp (a greyscale texture), Shape (a silhouette) or Fade.
- **Ramp**: a `WipeRamp` keeps its aspect and crops to fill the rect, or stretches to it. Either way the wipe is spread over the values that are on screen, so a ramp that never reaches black or white still starts and ends on time. `WipeRamp.Create(texture)` wraps a texture made at runtime.
- **Repeat**: None, Lanes (blinds that grow or bars that slide, with flat, round or chevron caps) or Grid (diamonds, circles or squares that grow). Each element starts when the field reaches its center.
- **Stagger**: Spread is how much of the wipe is spent waiting for the field to reach each element. Random mixes in a random start. Alternate runs every second lane the other way.
- **Edge**: Softness, and a zig zag or sine wave pushed into the edge.
- **Layers**: up to three colours that lead the fill, each trailing the one before by Layer Offset.
- **Exit**: whether Uncover carries on through or rewinds. It applies however the wipe is driven.
- **Timing from code**: the durations and eases `Cover()` and `Uncover()` use when called from code, and the inspector preview's Play. A Tweenline clip ignores them and uses its own duration and ease, so an ease is never applied on top of another.

The inspector's Preset menu fills in the classic looks: Wipe, Barn Doors, Zig Zag Wipe, Iris, Star Iris, Clock, Pinwheel, Swirl, Blinds, Sliding Bars, Diamond Tiles, Dots, Random Blocks and Fade.

## Use cases

### Load screen

```csharp
await ScreenWipe.Instance.Cover(look);
await LoadSceneAsync();
await ScreenWipe.Instance.Uncover(otherLook);
```

`Cover` completes at full cover, which is the moment to swap what is behind. `Uncover` takes a look only when the way out differs. Input under the wipe is blocked while anything is covered. Loading art or tips go on a canvas sorted above `ScreenWipe.Instance.SortingOrder`.

To close an iris on a character, set the focus first:

```csharp
ScreenWipe.Instance.Focus.Target = player.transform;
await ScreenWipe.Instance.Cover(irisLook);
```

A cover can also be a prefab with a Tweenline that has `Cover` and `Uncover` animations, for clouds, doors or a mascot: `await ScreenWipe.Instance.Cover(prefab)`.

### Billboard, from an advert to live UI

Put a `Wipe` over the live UI panel, give it the advert's sprite and a look, and set its coverage to 1. `Uncover()` wipes the advert away with the UI running underneath, and `Cover()` brings it back. Nothing is rendered to a texture.

### A full screen overlay that wipes in over a live game

1. Add `WipeReveal` to the overlay's root canvas and pick a look.
2. Add a Tweenline with a Wipe Reveal track. A `Show` animation has a Cover clip and a `Hide` animation has an Uncover clip. Each clip can name its own look, so in and out can differ.
3. Play `Show` and `Hide`. From code without a Tweenline, `await reveal.Cover()` and `await reveal.Uncover()` do the same.

`WipeReveal` owns the canvas's enabled flag at all times, and its render mode, camera and plane distance only while a transition is part way. They are put back when the transition reaches full coverage or the component is disabled or destroyed.

### Tweenline clips

A clip has Start, Duration, Ease, Mode (Cover or Uncover), Amount and an optional Look. There are no from and to numbers. Amount below 1 stops part way: an iris that covers to 0.9, holds on a small hole, then covers the rest is two Cover clips. An ease that overshoots is held at 0 and 1, so Back and Elastic bounce a part way hold but can never turn a cover into an uncover.

Scrubbing a Tweenline in the editor previews a `Wipe` exactly. For a `WipeReveal` it shows the look over the content with the part not revealed yet dimmed, since nothing is borrowed in edit mode.

## Cost

- A `Wipe` is one quad, one pass and one material. Coverage changes only set material values, so the canvas mesh is not rebuilt.
- At coverage 0 a wipe draws nothing and does no per frame work. There is no `Update`. `Cover()` and `Uncover()` run an `Awaitable` loop only while they play.
- The worst case for a colour or sprite wipe is one full screen alpha blend.
- A `WipeReveal` costs nothing while idle. While it moves, the overlay is drawn once into a screen sized texture and one full screen quad samples it. `Texture Scale` lowers the texture's size for low end targets. The camera and canvas it borrows are pooled and disabled between transitions.
- With `Trim` on, a plain straight wipe's quad is cut to the covered part, which saves fill rate but rebuilds that wipe's mesh as it moves. It is on for the Screen Wipe and for reveals, which have a canvas to themselves, and off by default elsewhere.
- The shader has 36 small variants (6 fields, 3 repeats, 2 fills), all `multi_compile` in a Resources shader, so builds include every one.

## Limits and choices

- **Root canvas only for reveals.** Unity can only send a root, screen space canvas to a texture. The inspector and the Tweenline window warn about it.
- **Effects that read the screen** inside a revealed overlay, such as blur behind UI, don't work while it moves.
- **Reveal preview in the editor is approximate.** Borrowing a camera in edit mode risked saving a changed canvas into the scene.
- **No transitions that move the content** (glass, twist, push, page flip) and no scene distortion (mosaic, ripple). They need the screen as a texture.
- **No liquid blobs that merge.** Use a Ramp texture.
- **One ease for all layers.** The clip's ease drives every layer and the offset makes the chase.
- **A silhouette or a fade always rewinds.** Continue has no meaning for them.
- **Part way amounts are meant for Rewind looks.** A Cover that follows a part way Continue uncover jumps.
- **Trim only bounds a plain straight wipe.** Irises, repeats and waves draw the whole rect.
- **A sprite fill** uses the sprite's outer rect in its texture, so tight or rotated atlas packing is not supported.
- **Not maskable.** A wipe ignores Mask and RectMask2D.
- **Up to three layers.** The inspector warns when a look has more.
