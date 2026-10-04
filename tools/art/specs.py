"""Spec for every sprite in Assets/_Game/Art (mirrors flappy-poeing-asset-prompts.md)."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets" / "_Game" / "Art"
RAW = Path(__file__).resolve().parent / "raw"

STYLE = ("16-bit SNES pixel art, side-scrolling game asset, chunky visible pixels, hard pixel edges, "
         "no anti-aliasing, no outline thicker than 1 pixel, limited palette: cream #F3E0C4, copper #C4784A, "
         "teal #3E8E8A, dark ink #2A211C, warm upper-left light, readable silhouette at small size")
NEG = ("photorealistic, 3D, blur, soft shading, smooth gradient, anti-aliased, painterly, text, letters, watermark, "
       "logo, UI frame, ground, cast shadow, multiple views, turnaround sheet, front view, isometric, "
       "propeller, second aircraft, cropped, magenta fringe")
# only the player is an aircraft; everything else must not drift into one
NEG_NOPLANE = ("aircraft, airplane, jet, fighter plane, fuselage, cockpit, engine nozzle, exhaust flame, "
               "jet exhaust, flying machine, vehicle with wings")
KEY = "Flat solid pure magenta #FF00FF background, nothing else in the background."

PLANE = ("Light fighter jet side view facing right, cream fuselage, copper short wings and tail, "
         "teal glass canopy, dark ink 1px outline, small orange #E36A2F exhaust. NOT a bird.")
BIRD = ("Tiny broad-wing flock bird, cream body, copper wing bars, teal beak, dark ink eye, compact silhouette, "
        "no dangling feet.")
EAGLE = ("Tempest Eagle, large storm raptor, cream chest, copper wing armor, teal eye, ink outline, "
         "chunky 16-bit, readable silhouette, wings clearly shaped like feathered wings.")


def S(path, w, h, frames, kind="sprite", ar="1:1", ref=None, frame_prompts=None, **kw):
    return dict(path=path, w=w, h=h, frames=frames, kind=kind, ar=ar, ref=ref,
                frame_prompts=frame_prompts, **kw)


SPECS = [
    # player
    S("player/poeing_fly_sheet.png", 64, 32, 4, ar="3:2", frame_prompts=[
        PLANE + " Frame 1: neutral level flight, tiny exhaust.",
        "Same jet, exhaust slightly longer.",
        "Same jet, wing raised by 1 pixel.",
        "Same jet, exhaust flaring bright."]),
    S("player/poeing_climb_sheet.png", 64, 32, 2, ar="3:2", ref="player/poeing_fly_sheet.png", frame_prompts=[
        "Same jet, nose raised about 18 degrees, exhaust brighter and longer. Do not change the design.",
        "Same jet climbing, exhaust 1 pixel longer."]),
    S("player/poeing_dive_sheet.png", 64, 32, 2, ar="3:2", ref="player/poeing_fly_sheet.png", frame_prompts=[
        "Same jet, nose down about 16 degrees, exhaust dim. Do not change the design.",
        "Same jet diving, body bobbed 1 pixel."]),
    S("player/poeing_hit.png", 64, 32, 1, ar="3:2", ref="player/poeing_fly_sheet.png", frame_prompts=[
        "Same jet staggered, small copper debris pixels, 4-pixel crack on the teal canopy, exhaust snuffed to 2 orange pixels."]),
    # birds
    S("birds/bird_cruise_sheet.png", 32, 32, 4, frame_prompts=[
        BIRD + " Facing left, wings up.", "Same bird, wings mid.", "Same bird, wings down.", "Same bird, wings mid."]),
    S("birds/bird_dive_sheet.png", 32, 32, 2, ref="birds/bird_cruise_sheet.png", frame_prompts=[
        "Same bird, wings tucked, diving down-left, teal beak forward.", "Same bird diving, body bobbed 1 pixel."]),
    S("birds/bird_scatter.png", 32, 32, 1, ref="birds/bird_cruise_sheet.png", frame_prompts=[
        "Same bird banking away, wings flared, a few cream feather pixels."]),
    # bullets
    S("bullets/orb_copper.png", 16, 16, 1, frame_prompts=[
        "Round copper orb projectile, 1px ink rim, hot yellow #F6C15B core, no trail, centered."]),
    S("bullets/orb_teal.png", 16, 16, 1, ref="bullets/orb_copper.png", frame_prompts=[
        "Same orb, teal glass body with pale #D7FFF8 core, same size and rim weight."]),
    S("bullets/orb_aimed.png", 16, 16, 1, ref="bullets/orb_copper.png", frame_prompts=[
        "Same orb shape, cream body with a thin copper ring, brighter, reads as an aimed shot."]),
    S("bullets/orb_heavy.png", 24, 24, 1, ref="bullets/orb_copper.png", frame_prompts=[
        "Larger copper orb, double ink rim, yellow core, still a plain circle, no spikes."]),
    S("bullets/telegraph_ring.png", 32, 32, 1, frame_prompts=[
        "Hollow warning ring, 2 pixel copper stroke, empty transparent center, pixel circle."]),
    S("bullets/safe_lane.png", 8, 32, 1, ar="2:3", frame_prompts=[
        "Faint teal dashed vertical column, 2 pixel wide dashes, low contrast."]),
    # bosses
    S("bosses/eagle_flap_sheet.png", 128, 96, 3, ar="3:2", frame_prompts=[
        EAGLE + " Facing left, side view, wings raised high.", "Same eagle, wings mid-level.", "Same eagle, wings swept low."]),
    S("bosses/eagle_dive.png", 128, 96, 1, ar="3:2", ref="bosses/eagle_flap_sheet.png", frame_prompts=[
        "Same eagle tucked into a dive facing left, wings swept back, teal eye bright."]),
    S("bosses/ironwake_idle.png", 192, 80, 1, ar="16:9", frame_prompts=[
        "Ironwake dreadnought, low iron ship side view facing left, dark hull #2A211C, copper armor plates, teal portholes, two gun barrels, small wake pixels under hull. Heavy silhouette."]),
    S("bosses/ironwake_fire.png", 192, 80, 1, ar="16:9", ref="bosses/ironwake_idle.png", frame_prompts=[
        "Same ship, gun ports flashing orange, identical hull and plates, no extra ship."]),
    S("bosses/citadel.png", 160, 144, 1, frame_prompts=[
        "Sundown Citadel, floating fortress chunk facing left, cream stone, copper buttresses, teal glass slits, one large cannon, warm sunset rim light on the right edge. Chunky pixel masonry."]),
    S("bosses/citadel_gun_sheet.png", 48, 32, 2, ar="3:2", frame_prompts=[
        "Citadel cannon barrel facing left, copper and ink, loaded.", "Same cannon fired, 4 pixel orange muzzle flash."]),
    # boss phases + minions (Cuphead update): 1930s cartoon faces, still 16-bit pixel art
    S("bosses/eagle_storm_sheet.png", 128, 96, 3, ar="3:2", ref="bosses/eagle_flap_sheet.png", frame_prompts=[
        "Same eagle facing left, wings raised, feathers crackling with teal lightning, glowing teal eye, 1930s cartoon pie-cut eye.",
        "Same storm-charged eagle, wings mid-level.", "Same storm-charged eagle, wings swept low."]),
    S("bosses/eagle_fury_sheet.png", 128, 96, 3, ar="3:2", ref="bosses/eagle_flap_sheet.png", frame_prompts=[
        "Same eagle facing left, furious, ruffled torn feathers, angry orange eye, open screaming beak, wings raised.",
        "Same furious eagle, wings mid-level.", "Same furious eagle, wings swept low."]),
    S("bosses/eagle_ko.png", 128, 96, 1, ar="3:2", ref="bosses/eagle_flap_sheet.png", frame_prompts=[
        "Same eagle knocked out, dizzy cartoon spiral eyes, drooping wings, loose feathers."]),
    S("bosses/ironwake_damaged.png", 192, 80, 1, ar="16:9", ref="bosses/ironwake_idle.png", frame_prompts=[
        "Same ship damaged, dented copper plates, small fires and dark smoke puffs, extra deck turret raised."]),
    S("bosses/ironwake_maw.png", 192, 80, 1, ar="16:9", ref="bosses/ironwake_idle.png", frame_prompts=[
        "Same ship, the bow is a closed iron jaw with rivet teeth and two big glowing teal cartoon eyes, angry brow."]),
    S("bosses/ironwake_maw_open.png", 192, 80, 1, ar="16:9", ref="bosses/ironwake_maw.png", frame_prompts=[
        "Same ship with the iron jaw wide open, orange glow inside the mouth, teal eyes glaring."]),
    S("bosses/citadel_cracked.png", 160, 144, 1, ref="bosses/citadel.png", frame_prompts=[
        "Same citadel cracked and crumbling, missing bricks, glowing orange cracks, dust pixels."]),
    S("bosses/citadel_core_sheet.png", 96, 96, 2, frame_prompts=[
        "Glowing fortress heart core, round copper-caged orb with an angry 1930s cartoon face, pie-cut eyes, orange glow.",
        "Same core pulsing larger and brighter, mouth open."]),
    S("minions/storm_cloud_sheet.png", 48, 32, 2, ar="3:2", frame_prompts=[
        "Small dark storm cloud with a grumpy 1930s cartoon face, pie-cut eyes, teal spark underneath.",
        "Same cloud puffing its cheeks, a lightning spark dropping."]),
    S("minions/turret.png", 32, 32, 1, frame_prompts=[
        "Small copper wall turret with one barrel facing left and a single cartoon eye, ink outline."]),
    S("minions/mine.png", 24, 24, 1, frame_prompts=[
        "Round naval sea mine, dark iron with copper spikes, small cartoon face."]),
    S("minions/torpedo.png", 32, 12, 1, ar="3:2", frame_prompts=[
        "Torpedo facing left, copper nose, ink fins, small cartoon angry eye, no flame."]),
    # backgrounds
    S("bg/dawn_sky.png", 480, 270, 1, kind="bg_opaque", ar="16:9", frame_prompts=[
        "Clear dawn sky background, sun low on the left, dithered bands from orange #F6A15A to cream #F3D7B0 to pale blue #8FB7D6, no clouds, no ground."]),
    S("bg/dawn_clouds_far.png", 480, 270, 1, kind="bg_tile", ar="16:9", frame_prompts=[
        "ONLY a few small pale cream clouds with copper rims scattered with lots of empty space. Absolutely nothing else: no vehicles, no airships, no balloons, no ships, no people, no creatures, no sun."]),
    S("bg/dawn_clouds_mid.png", 480, 270, 1, kind="bg_tile", ar="16:9", frame_prompts=[
        "Larger copper-lit cream clouds with fluffy lumpy pixel outlines, clear gaps between them."]),
    S("bg/dawn_pillars.png", 480, 270, 1, kind="bg_tile", ar="16:9", frame_prompts=[
        "Distant cream cloud pillars rising from the bottom, wide gaps between them, soft copper shading, decorative."]),
    S("bg/storm_sky.png", 480, 270, 1, kind="bg_opaque", ar="16:9", frame_prompts=[
        "Storm sky background, dithered dark blue-grey from #1C2430 to #3A4658, no clouds, no lightning, oppressive."]),
    S("bg/storm_clouds.png", 480, 270, 1, kind="bg_tile", ar="16:9", frame_prompts=[
        "ONLY heavy dark storm clouds #3A4658 with cream highlights and copper rim light, clear gaps between clouds. Absolutely nothing else: no vehicles, no airships, no balloons, no ships, no people, no creatures."]),
    S("bg/storm_rain.png", 480, 270, 1, kind="bg_tile", ar="16:9", frame_prompts=[
        "Sparse diagonal 1 pixel rain streaks in pale #D7DEE6 scattered over an otherwise empty background."]),
    S("bg/sea_sky.png", 480, 270, 1, kind="bg_opaque", ar="16:9", frame_prompts=[
        "Open sea sky background, low copper sun #C9854A near the horizon, dithered to pale teal #7EB6C4 above, no waves, no boat."]),
    S("bg/sea_horizon.png", 480, 270, 1, kind="bg_tile", ar="16:9", frame_prompts=[
        "A thin far wave line, dark teal, at the bottom third of the image, empty above."]),
    S("bg/sea_waves.png", 480, 270, 1, kind="bg_tile", ar="16:9", frame_prompts=[
        "Near waves band occupying only the bottom 40 percent, dark teal water #1E4E58 with cream foam pixels, empty above."]),
    S("bg/sea_spray.png", 480, 270, 1, kind="bg_tile", ar="16:9", frame_prompts=[
        "Sparse sea spray pixels, teal and cream dots, only in the lower half, empty above."]),
    # fx
    S("fx/exhaust_puff_sheet.png", 16, 16, 4, frame_prompts=[
        "Orange-to-cream exhaust puff made of pixel clusters, large.", "Same puff, smaller.", "Same puff, smaller.", "Same puff, tiny."]),
    S("fx/hit_spark_sheet.png", 16, 16, 4, frame_prompts=[
        "Copper and cream hit spark, tight burst.", "Same spark, expanding.", "Same spark, scattered.", "Same spark, a few scattered 1px dots."]),
    S("fx/lightning_bolt.png", 32, 96, 1, ar="2:3", frame_prompts=[
        "Vertical jagged lightning bolt, cream core, teal edge pixels, 2 pixels thick, no cloud."]),
    S("fx/wind_streak.png", 48, 8, 1, ar="3:2", frame_prompts=[
        "Horizontal wind shear marks, sparse 1 pixel cream dashes in a row."]),
    S("fx/splash.png", 32, 16, 1, ar="3:2", frame_prompts=[
        "Sea spray burst, teal and cream pixels."]),
    # obstacles
    S("obstacles/cloud_pillar.png", 48, 128, 1, ar="2:3", frame_prompts=[
        "Vertical cream cloud pillar with copper shadow edge, flat readable sides, pixel clumps, not a metal pipe."]),
    S("obstacles/sea_rock.png", 64, 48, 1, ar="3:2", frame_prompts=[
        "Low sea rock, ink and teal, cream foam at the waterline."]),
    # ui (hand icon via grok; digits/banner/warning bar are drawn in code)
    S("ui/tap_icon.png", 24, 24, 1, frame_prompts=[
        "A pointing hand tapping, simple readable hand shape with index finger extended, cream skin tone and copper outline accents, ink outline."]),
    S("ui/digits_sheet.png", 160, 24, 10, kind="code"),
    S("ui/banner_gameover.png", 160, 32, 1, kind="code"),
    S("ui/banner_victory.png", 160, 32, 1, kind="code"),
    S("bullets/player_shot.png", 16, 8, 1, kind="code"),
    S("ui/warning_bar.png", 128, 8, 1, kind="code"),
]

# layers drawn in code (post.draw_code): AI keeps inserting vehicles/characters into empty-environment prompts
CODE_DRAWN = ("bg/dawn_sky.png", "bg/storm_sky.png", "bg/sea_sky.png", "bg/storm_rain.png", "bg/sea_horizon.png",
              "bg/sea_waves.png", "bg/sea_spray.png", "fx/wind_streak.png", "bg/dawn_clouds_far.png",
              "bg/dawn_clouds_mid.png", "bg/dawn_pillars.png", "bg/storm_clouds.png")
for _s in SPECS:
    if _s["path"] in CODE_DRAWN:
        _s["kind"] = "code"

# bg/storm_lightning.png is a copy of fx/lightning_bolt.png (same bolt, see UNITY_IMPORT.txt)
COPIES = {"bg/storm_lightning.png": "fx/lightning_bolt.png"}

PALETTE = """F3E0C4 E2C496 C4784A A85A32 3E8E8A 7ED0C8 2A211C E36A2F F6C15B 1C2430 3A4658 D7DEE6
1E4E58 C9854A F6A15A F3D7B0 8FB7D6 D7FFF8 7EB6C4 F07AA8 FFC3D9 D8443A""".split()

TILE_KINDS = ("bg_tile",)


def frame_path(spec, i):
    return RAW / (spec["path"][:-4] + f"__f{i}.png")


def build_prompt(spec, i):
    fp = spec["frame_prompts"][i]
    transparent = spec["kind"] in ("sprite", "bg_tile")
    neg = NEG if spec["path"].startswith("player/") else f"{NEG}, {NEG_NOPLANE}"
    return f"{STYLE}. {fp} {KEY if transparent else ''} NEGATIVE: {neg}.".replace("  ", " ")
