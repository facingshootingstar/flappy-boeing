# Flappy Poeing — agent prompt pack (Unity 2D sprite bible)

Paste the **AGENT BRIEF** into the local agent. It must generate files, not rewrite the design.
Image prompts are English on purpose (pixel models obey them better). Brief around them is Vietnamese so you can steer the agent.

Do not invent a shop, skins, multiplayer, or a second button. Tap is the only verb.

---

## AGENT BRIEF (paste this first)

```
You are generating the full production sprite set for Flappy Poeing, a solo Unity 2D undergraduate game.

Game: one-tap side-scroller. Tap = lift + score. Gravity always pulls down. Facing RIGHT. Side view only.
Identity: Poeing is a light fighter, NOT a bird. Cream fuselage, copper wings and engine, teal glass canopy, orange exhaust.
Wildlife birds are small living pipes. Bullets are copper/teal orbs. Weather is a difficulty layer, not decoration.

OUTPUT
- Root: Assets/_Game/Art/
- Transparent PNG, no mockup frame, no caption, no watermark, no ground shadow blob.
- True pixel art: visible chunky pixels, hard edges, NO anti-alias, NO soft airbrush, NO 3D render, NO photoreal.
- Limited palette from the STYLE LOCK. Dither is allowed. Smooth gradients are not.
- Every gameplay sprite faces right, shares the same light (upper-left), and sits on a consistent baseline.
- Backgrounds are layered parallax strips, seamless on X where noted.
- After each image: nearest-neighbor downscale to the target pixel size in the manifest, then save. Do not leave a hi-res "pixel-looking" image.
- If the generator cannot do transparency, render on flat #FF00FF and chroma-key that exact magenta to alpha. Never leave magenta in the final PNG.
- Name files exactly as the manifest. One sheet or one frame per file, as specified.
- Do not generate audio. Do not generate code. Do not change names.

STYLE LOCK (prepend to every image prompt, do not shorten)
16-bit SNES pixel art, side-scrolling game sprite, chunky visible pixels, hard pixel edges, no anti-aliasing, no outline thicker than 1 pixel, limited palette, cream #F3E0C4 fuselage, copper #C4784A wings and trim, teal glass #3E8E8A, dark ink shadow #2A211C, warm upper-left light, orange exhaust #E36A2F, readable silhouette at small size, transparent background

NEGATIVE (append to every image prompt)
photorealistic, 3D, octane, blur, soft shading, smooth gradient, anti-aliased, painterly, watercolor, text, letters, watermark, logo, UI frame, ground, cast shadow blob, multiple views, turnaround sheet, front view, isometric, bird body on the plane, propeller, second aircraft, cropped wing, magenta fringe

UNITY IMPORT (write a note, do not import unless asked)
- Filter Mode: Point
- Compression: None
- Pixels Per Unit: 32
- Pivot: player and birds = center; bullets = center; bosses = center; backgrounds = center
- Extrude edges 1
```

---

## Palette (do not drift)

| Role | Hex | Use |
|---|---|---|
| Cream | `#F3E0C4` / `#E2C496` | fuselage, bird belly, clouds |
| Copper | `#C4784A` / `#A85A32` | wings, bullets, boss trim |
| Teal glass | `#3E8E8A` / `#7ED0C8` | canopy, sea, safe-lane tint |
| Ink | `#2A211C` | 1px shadow / outline |
| Exhaust | `#E36A2F` / `#F6C15B` | engine, lightning core |
| Dawn sky | `#F6A15A` → `#F3D7B0` → `#8FB7D6` | biome 1 |
| Storm | `#1C2430` / `#3A4658` / `#D7DEE6` | biome 2 |
| Sea | `#1E4E58` / `#C9854A` sun | biome 3 |

---

## Folder manifest

```
Assets/_Game/Art/
  player/
  birds/
  bullets/
  bosses/
  bg/
  fx/
  ui/
  obstacles/
```

Target pixel sizes are the FINAL file size after downscale. Generate larger only if you will nearest-neighbor resize before save.

---

## 1. Player — Poeing

Side-view light jet. Generous visual sprite, gameplay hitbox is fuselage only (do not draw a hitbox). No propeller. Short copper wings, teal canopy, cream body, copper tail, small orange exhaust.

### `player/poeing_fly_sheet.png` — 256×32, 4 frames of 64×32, left to right

```
STYLE LOCK.
Sprite sheet, exactly 4 frames in one horizontal row, each frame 64x32 pixels, equal spacing, no gutters, transparent background.
Light fighter jet side view facing right, cream fuselage, copper wings and tail, teal glass canopy, dark ink 1px shadow, tiny orange exhaust.
Frame 1 neutral level flight. Frame 2 exhaust slightly longer. Frame 3 wing pixel bob up 1px. Frame 4 exhaust flare. Same silhouette every frame. No bank beyond 5 degrees.
NEGATIVE.
```

### `player/poeing_climb_sheet.png` — 128×32, 2 frames of 64×32

```
STYLE LOCK.
Sprite sheet, 2 frames, 64x32 each, horizontal, transparent background.
Same cream-and-copper jet facing right, nose raised about 18 degrees, exhaust brighter and longer, teal canopy visible. Frame 2 exhaust +1 pixel. Do not change the plane design.
NEGATIVE.
```

### `player/poeing_dive_sheet.png` — 128×32, 2 frames of 64×32

```
STYLE LOCK.
Sprite sheet, 2 frames, 64x32 each, horizontal, transparent background.
Same jet facing right, nose down about 16 degrees, exhaust dimmer. Frame 2 a 1px body bob. Same colors and silhouette family.
NEGATIVE.
```

### `player/poeing_hit.png` — 64×32

```
STYLE LOCK.
Single sprite 64x32, transparent background.
Same jet facing right, staggered, small copper debris pixels and a 4-pixel teal glass crack, exhaust snuffed to 2 orange pixels. Still readable as Poeing.
NEGATIVE.
```

---

## 2. Birds — living pipes

Small, same copper/cream family so they read as wildlife, not enemies-with-guns. Side view, facing left OR right as specified (flocks cruise both ways).

### `birds/bird_cruise_sheet.png` — 128×32, 4 frames of 32×32

```
STYLE LOCK.
Sprite sheet, 4 frames, 32x32 each, horizontal, transparent background.
Tiny broad-wing flock bird facing left, cream body, copper wing bars, teal beak, dark ink eye. Frame 1 wings up, frame 2 wings mid, frame 3 wings down, frame 4 wings mid. Compact silhouette, no feet dangling. Looks like a living pipe, chunky and readable.
NEGATIVE.
```

### `birds/bird_dive_sheet.png` — 64×32, 2 frames of 32×32

```
STYLE LOCK.
Sprite sheet, 2 frames, 32x32, transparent background.
Same cream-and-copper flock bird, wings tucked, diving down-left, teal beak forward. Two-frame bob only.
NEGATIVE.
```

### `birds/bird_scatter.png` — 32×32

```
STYLE LOCK.
Single 32x32 sprite, transparent background.
Same flock bird banking away, wings flared, a few cream feather pixels. Readable at 32 pixels.
NEGATIVE.
```

---

## 3. Bullets — danmaku, taught by a thumb

Hard cap in game is a code concern. Art needs 4 families, color-coded, plus a telegraph.

### `bullets/orb_copper.png` — 16×16

```
STYLE LOCK.
Single projectile sprite 16x16, transparent background.
Round copper orb bolt, 1px ink rim, hot #F6C15B core, no motion blur, no trail. Centered.
NEGATIVE.
```

### `bullets/orb_teal.png` — 16×16

```
STYLE LOCK.
Single 16x16 projectile, transparent background.
Round teal glass orb, ink rim, pale #D7FFF8 core. Same size and rim weight as the copper orb.
NEGATIVE.
```

### `bullets/orb_aimed.png` — 16×16

```
STYLE LOCK.
Single 16x16 projectile, transparent background.
Cream orb with a copper 1px ring, brighter than the copper bolt, reads as an aimed shot. Centered, no trail.
NEGATIVE.
```

### `bullets/orb_heavy.png` — 24×24

```
STYLE LOCK.
Single 24x24 projectile, transparent background.
Larger copper orb, double ink rim, yellow core, still a circle. Boss shot. No spikes.
NEGATIVE.
```

### `bullets/telegraph_ring.png` — 32×32

```
STYLE LOCK.
Single 32x32 sprite, transparent background.
Hollow warning ring, 2px copper stroke, empty center, no fill. Pixel circle, not anti-aliased.
NEGATIVE.
```

### `bullets/safe_lane.png` — 8×32

```
STYLE LOCK.
Single vertical sprite 8x32, transparent background.
Faint teal dashed column, 2px wide dashes, signals a guaranteed safe lane. Low contrast, not a solid wall.
NEGATIVE.
```

---

## 4. Bosses — score gates, not menu characters

Every boss is a side-view gameplay sprite, not a portrait. Player still scores during the fight.

### `bosses/eagle_flap_sheet.png` — 384×96, 3 frames of 128×96

```
STYLE LOCK.
Sprite sheet, 3 frames, 128x96 each, horizontal, transparent background.
Tempest Eagle, large storm raptor facing left toward the player, cream chest, copper wing armor, teal eye, ink shadow. Frame 1 wings high, frame 2 wings mid, frame 3 wings low. Majestic but chunky 16-bit, readable silhouette, no lightning drawn on the body.
NEGATIVE.
```

### `bosses/eagle_dive.png` — 128×96

```
STYLE LOCK.
Single 128x96 sprite, transparent background.
Same Tempest Eagle tucked into a dive facing left, wings back, teal eye bright. Same palette as the flap sheet.
NEGATIVE.
```

### `bosses/ironwake_idle.png` — 192×80

```
STYLE LOCK.
Single 192x80 sprite, transparent background.
Ironwake dreadnought, low iron ship facing left, dark hull #2A211C, copper armor plates, teal portholes, two gun barrels, small wake pixels under the hull. Heavy silhouette, not a modern destroyer photo.
NEGATIVE.
```

### `bosses/ironwake_fire.png` — 192×80

```
STYLE LOCK.
Single 192x80 sprite, transparent background.
Same Ironwake dreadnought facing left, gun ports flashed orange, same hull and copper plates. No extra ship.
NEGATIVE.
```

### `bosses/citadel.png` — 160×144

```
STYLE LOCK.
Single 160x144 sprite, transparent background.
Sundown Citadel, floating fortress chunk facing left, cream stone, copper buttresses, teal glass slits, one large cannon, warm sunset rim light on the right edge. Chunky pixel masonry, no landscape background.
NEGATIVE.
```

### `bosses/citadel_gun_sheet.png` — 96×32, 2 frames of 48×32

```
STYLE LOCK.
Sprite sheet, 2 frames, 48x32, transparent background.
Citadel cannon barrel facing left, copper and ink, frame 1 loaded, frame 2 fired with a 4px orange muzzle. Matches the citadel palette.
NEGATIVE.
```

---

## 5. Backgrounds — 3 biomes, parallax

Canvas 480×270, displayed with Point filter, camera orthographic. Each biome is 4 layers. Layers 2–4 tile on X.

Shared suffix for every BG prompt:

```
Pixel-art parallax layer, 480x270, 16-bit, chunky pixels, no characters, no plane, no bird, no bullet, no text, no UI. Seamless left-right edge.
```

### Biome 1 Clear Skies (score 0–29) — learn the tap

- `bg/dawn_sky.png` — full bleed sky, NOT tile: dawn gradient sky only, sun low left, cream to peach to pale blue, dithered bands, no clouds.
- `bg/dawn_clouds_far.png` — tile: sparse pale cream clouds, lots of transparent sky, soft-but-pixel distant.
- `bg/dawn_clouds_mid.png` — tile: larger copper-lit clouds, transparent gaps a plane can read through.
- `bg/dawn_pillars.png` — tile: distant cream cloud pillars, wide gaps, not solid pipes. Decorative. Gameplay obstacles are birds.

```
STYLE LOCK. Clear dawn sky pixel background 480x270, sun low on the left, dithered bands from #F6A15A to #F3D7B0 to #8FB7D6, no clouds, no ground, no text.
NEGATIVE. characters, aircraft, birds, text.
```

```
STYLE LOCK. Seamless horizontal parallax cloud strip 480x270, transparent background, few small cream clouds with copper rims, lots of empty sky, 16-bit, edges wrap cleanly.
NEGATIVE. ground, text, plane.
```

### Biome 2 Thunderfront (score 30–69)

- `bg/storm_sky.png` — ink blue-grey sky, no bolts.
- `bg/storm_clouds.png` — tile: heavy #3A4658 clouds, copper lightning-edge pixels, transparent gaps.
- `bg/storm_rain.png` — tile: diagonal 1px rain streaks, mostly transparent, #D7DEE6.
- `bg/storm_lightning.png` — single bolt prop, transparent, not a full bg: see FX.

```
STYLE LOCK. Storm sky 480x270, dithered #1C2430 to #3A4658, no lightning yet, no plane, oppressive but readable.
NEGATIVE. text, characters.
```

```
STYLE LOCK. Seamless storm cloud layer 480x270, transparent gaps, heavy pixel clouds #3A4658 with cream highlights, copper rim light, wraps on X.
NEGATIVE. text, plane, full opaque wall.
```

### Biome 3 Open Sea (score 70+)

- `bg/sea_sky.png` — low sun, #C9854A near horizon, pale teal above.
- `bg/sea_horizon.png` — tile: far wave line, dark teal.
- `bg/sea_waves.png` — tile: nearer waves, cream foam pixels, transparent above the wave band so the plane can fly in sky.
- `bg/sea_spray.png` — tile: sparse spray pixels, upper half transparent.

```
STYLE LOCK. Open sea sky 480x270, low copper sun on the horizon, dither from #C9854A to #7EB6C4, no waves yet, no text.
NEGATIVE. characters, boat, text.
```

```
STYLE LOCK. Seamless near-wave band 480x270, bottom 40 percent dark teal water #1E4E58 with cream foam pixels, top 60 percent transparent, wraps on X, 16-bit.
NEGATIVE. plane, text, photoreal water.
```

---

## 6. FX

### `fx/exhaust_puff_sheet.png` — 64×16, 4 frames of 16×16

```
STYLE LOCK.
4 frames 16x16 horizontal, transparent background.
Orange-to-cream exhaust puff shrinking each frame. Pixel clusters only, no soft glow.
NEGATIVE.
```

### `fx/hit_spark_sheet.png` — 64×16, 4 frames of 16×16

```
STYLE LOCK.
4 frames 16x16, transparent background.
Copper and cream hit spark, frame 1 tight, frame 4 scattered 1px dots. No blood.
NEGATIVE.
```

### `fx/lightning_bolt.png` — 32×96

```
STYLE LOCK.
Single 32x96 sprite, transparent background.
Vertical-ish jagged lightning, cream core, teal edge pixels, 2px thick, readable, denies a lane. No cloud attached.
NEGATIVE.
```

### `fx/wind_streak.png` — 48×8

```
STYLE LOCK.
Single 48x8 sprite, transparent background.
Horizontal wind shear marks, 1px cream dashes, sparse. Readable as a push, not fog.
NEGATIVE.
```

### `fx/splash.png` — 32×16

```
STYLE LOCK.
Single 32x16 sprite, transparent background.
Sea spray burst, teal and cream pixels. Death-by-water tell.
NEGATIVE.
```

---

## 7. Obstacles that are not birds

Birds are the living pipes. These are sparse biome props, not a second mechanic.

### `obstacles/cloud_pillar.png` — 48×128

```
STYLE LOCK.
Single 48x128 sprite, transparent background.
Vertical cream cloud pillar with copper shadow edge, flat readable sides, gap-friendly, not a metal pipe. Pixel clumps, no soft blur.
NEGATIVE.
```

### `obstacles/sea_rock.png` — 64×48

```
STYLE LOCK.
Single 64x48 sprite, transparent background.
Low sea rock, ink and teal, cream foam at the waterline. Sits on the wave band.
NEGATIVE.
```

---

## 8. UI — pixel, no menus beyond fail state

### `ui/digits_sheet.png` — 160×24, 10 glyphs of 16×24 (`0`–`9`)

```
16-bit pixel numerals 0 through 9, one horizontal sheet, each glyph 16x24, cream fill, 1px ink shadow, monospaced, transparent background, no letters, no punctuation.
NEGATIVE. anti-alias, script font, 3D.
```

### `ui/tap_icon.png` — 24×24

```
16-bit pixel hand-tap mark, cream and copper, 24x24, transparent background, no text.
NEGATIVE.
```

### `ui/banner_gameover.png` — 160×32

```
16-bit pixel plaque, cream panel, copper rim, ink 1px border, empty center for a text mesh, 160x32, transparent outside the plaque. No words baked into the image.
NEGATIVE. letters, words.
```

### `ui/warning_bar.png` — 128×8

```
16-bit horizontal copper bar, 128x8, teal end caps, transparent background. Boss incoming tell. No text.
NEGATIVE.
```

---

## Generation order (tell the agent)

1. Player fly sheet first. Lock that silhouette. Every later player frame must match it.
2. Bird cruise sheet. Lock wildlife palette.
3. Copper orb. Other orbs match its rim weight.
4. Eagle flap sheet, then Ironwake, then Citadel.
5. Dawn layers, storm layers, sea layers.
6. FX and UI last.

If a frame drifts (wrong colors, propeller appears, front view, soft shading), regenerate that file only. Do not "fix" by repainting in a painterly pass.

## Unity hookup note (for you, not the image agent)

- Player Rigidbody2D + CapsuleCollider2D on the fuselage only. Sprite is larger than the capsule on purpose.
- Sort: bg sky 0, far 1, mid 2, pillars/waves 3, gameplay 10, fx 20, ui 100.
- Biome director swaps layer sets at score 30 and 70. Boss prefabs at 30 / 70 / 120.
- Animate sheets with Unity Animation, sample 8 fps fly, 6 fps bird, 12 fps exhaust. Point filter. PPU 32.
- Projectile cap stays in the pooler, not in the art.
```
