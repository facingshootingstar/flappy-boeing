using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot project builder: imports every sprite with the pixel-art settings, slices sheets, and assembles Assets/_Game/Scenes/Main.unity.
/// Run from the menu (Flappy > Build All) or headless: Unity -batchmode -quit -executeMethod SceneBuilder.BuildAll
/// </summary>
public static class SceneBuilder
{
    const string Art = "Assets/_Game/Art/";
    const string ScenePath = "Assets/_Game/Scenes/Main.unity";
    const int PPU = 32;

    // sheet path -> frame size (frame count comes from the texture width)
    static readonly Dictionary<string, Vector2Int> Sheets = new Dictionary<string, Vector2Int>
    {
        { "player/poeing_fly_sheet.png", new Vector2Int(64, 32) },
        { "player/poeing_climb_sheet.png", new Vector2Int(64, 32) },
        { "player/poeing_dive_sheet.png", new Vector2Int(64, 32) },
        { "birds/bird_cruise_sheet.png", new Vector2Int(32, 32) },
        { "birds/bird_dive_sheet.png", new Vector2Int(32, 32) },
        { "bosses/eagle_flap_sheet.png", new Vector2Int(128, 96) },
        { "bosses/citadel_gun_sheet.png", new Vector2Int(48, 32) },
        { "fx/exhaust_puff_sheet.png", new Vector2Int(16, 16) },
        { "fx/hit_spark_sheet.png", new Vector2Int(16, 16) },
        { "ui/digits_sheet.png", new Vector2Int(16, 24) },
        { "ui/font_sheet.png", new Vector2Int(9, 10) },
        { "ui/card_super_sheet.png", new Vector2Int(12, 16) },
        { "fx/explosion_sheet.png", new Vector2Int(48, 48) },
        { "fx/ko_stars_sheet.png", new Vector2Int(40, 16) },
        { "fx/smoke_sheet.png", new Vector2Int(16, 16) },
        { "bg/amb_gull_sheet.png", new Vector2Int(16, 12) },
        { "bg/sea_waves_sheet.png", new Vector2Int(480, 96) },
        { "bosses/eagle_storm_sheet.png", new Vector2Int(128, 96) },
        { "bosses/eagle_fury_sheet.png", new Vector2Int(128, 96) },
        { "bosses/citadel_core_sheet.png", new Vector2Int(96, 96) },
        { "minions/storm_cloud_sheet.png", new Vector2Int(48, 32) },
    };

    // 9-slice borders (left, bottom, right, top) in pixels
    static readonly Dictionary<string, Vector4> Borders = new Dictionary<string, Vector4>
    {
        { "ui/plaque.png", new Vector4(4, 5, 4, 5) },
    };

    [MenuItem("Flappy/Build All")]
    public static void BuildAll()
    {
        ImportSprites();
        BuildScene();
        Debug.Log("[SceneBuilder] done: " + ScenePath);
    }

    // ---------------------------------------------------------------- import
    [MenuItem("Flappy/Import Sprites")]
    public static void ImportSprites()
    {
        AssetDatabase.Refresh();
        var files = Directory.GetFiles(Application.dataPath + "/_Game/Art", "*.png", SearchOption.AllDirectories);
        foreach (var f in files)
        {
            string rel = f.Replace('\\', '/');
            string path = "Assets" + rel.Substring(Application.dataPath.Length);
            string key = path.Substring(Art.Length);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) continue;

            imp.textureType = TextureImporterType.Sprite;
            imp.spritePixelsPerUnit = PPU;
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.maxTextureSize = 2048;
            imp.spriteImportMode = Sheets.ContainsKey(key) ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Center;
            s.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(s);
            imp.spriteBorder = Borders.TryGetValue(key, out var border) ? border : Vector4.zero;
            imp.SaveAndReimport();

            if (Sheets.TryGetValue(key, out var size)) Slice(path, size);
        }
        AssetDatabase.SaveAssets();
        ImportAudio();
    }

    const string AudioDir = "Assets/_Game/Audio";

    /// <summary>PCM keeps the music loops seamless (Vorbis pads the loop point).</summary>
    static void ImportAudio()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as AudioImporter;
            if (imp == null) continue;
            var st = imp.defaultSampleSettings;
            st.loadType = AudioClipLoadType.DecompressOnLoad;
            st.compressionFormat = AudioCompressionFormat.PCM;
            imp.defaultSampleSettings = st;
            imp.forceToMono = true;
            imp.SaveAndReimport();
        }
    }

    static AudioClip[] Clips() => AssetDatabase.FindAssets("t:AudioClip", new[] { AudioDir })
        .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).Where(c => c).ToArray();

    static void Slice(string path, Vector2Int size)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int n = tex.width / size.x;
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();
        string baseName = Path.GetFileNameWithoutExtension(path);
        var rects = new SpriteRect[n];
        for (int i = 0; i < n; i++)
        {
            rects[i] = new SpriteRect
            {
                name = baseName + "_" + i,
                rect = new Rect(i * size.x, 0, size.x, size.y),
                alignment = SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
                spriteID = GUID.Generate(),
            };
        }
        dp.SetSpriteRects(rects);
        var ids = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        ids.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
        dp.Apply();
        imp.SaveAndReimport();
    }

    // ---------------------------------------------------------------- helpers
    static Sprite One(string key)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(Art + key);
        if (s == null) Debug.LogError("[SceneBuilder] missing sprite " + key);
        return s;
    }

    static Sprite[] Frames(string key)
    {
        var all = AssetDatabase.LoadAllAssetsAtPath(Art + key).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
        if (all.Length == 0) Debug.LogError("[SceneBuilder] no frames in " + key);
        return all;
    }

    static GameObject Obj(string name, Transform parent = null, Vector3? pos = null)
    {
        var go = new GameObject(name);
        if (parent) go.transform.SetParent(parent, false);
        go.transform.localPosition = pos ?? Vector3.zero;
        return go;
    }

    static SpriteRenderer Sprite(string name, Sprite sp, int order, Transform parent = null, Vector3? pos = null)
    {
        var go = Obj(name, parent, pos);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp; sr.sortingOrder = order;
        return sr;
    }

    // ---------------------------------------------------------------- scene
    [MenuItem("Flappy/Build Scene")]
    public static void BuildScene()
    {
        Directory.CreateDirectory(Application.dataPath + "/_Game/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // camera: 480x270 reference, ortho size = 270 / PPU / 2
        var camGo = Obj("Main Camera", null, new Vector3(0, 0, -10));
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true; cam.orthographicSize = 270f / PPU / 2f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color32(143, 183, 214, 255);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<CameraFx>();
        camGo.AddComponent<FilmFilter>().shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/_Game/Scripts/Shaders/FilmFilter.shader");

        var game = Obj("Game");
        game.AddComponent<GameState>();
        var kit = game.AddComponent<GameKit>();
        kit.font = Frames("ui/font_sheet.png");
        kit.white = One("ui/white.png"); kit.plaque = One("ui/plaque.png"); kit.heart = One("ui/heart.png");
        kit.warningBar = One("ui/warning_bar.png"); kit.cards = Frames("ui/card_super_sheet.png");
        kit.explosion = Frames("fx/explosion_sheet.png"); kit.hitSpark = Frames("fx/hit_spark_sheet.png");
        kit.smoke = Frames("fx/smoke_sheet.png"); kit.koStars = Frames("fx/ko_stars_sheet.png");
        kit.muzzle = One("fx/muzzle_flash.png"); kit.parryStar = One("fx/parry_star.png");
        kit.splash = One("fx/splash.png"); kit.windStreak = One("fx/wind_streak.png");
        kit.telegraph = One("bullets/telegraph_ring.png");
        game.AddComponent<AudioManager>().clips = Clips();
        Obj("Announcer").AddComponent<Announcer>();

        // ---- backgrounds: one root per biome (0 dawn, 1 storm, 2 sea, 3 dusk), swapped by the boss director under the iris
        var bg = Obj("Background");
        var biome = bg.AddComponent<BiomeDirector>();
        biome.roots = new[] { Dawn(bg.transform), Storm(bg.transform), Sea(bg.transform), Dusk(bg.transform) };

        // ---- player
        var fly = Frames("player/poeing_fly_sheet.png");
        var playerGo = Obj("Poeing", null, new Vector3(-2.5f, 0, 0));
        var psr = playerGo.AddComponent<SpriteRenderer>(); psr.sprite = fly[0]; psr.sortingOrder = 10;
        var rb = playerGo.AddComponent<Rigidbody2D>(); rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var cap = playerGo.AddComponent<CapsuleCollider2D>();
        cap.direction = CapsuleDirection2D.Horizontal; cap.size = new Vector2(1.3f, 0.4f); cap.offset = new Vector2(0.05f, 0f);
        playerGo.AddComponent<SpriteAnimator>().frames = fly;
        var pc = playerGo.AddComponent<PlayerController>();
        pc.fly = fly; pc.climb = Frames("player/poeing_climb_sheet.png"); pc.dive = Frames("player/poeing_dive_sheet.png");
        pc.hit = One("player/poeing_hit.png");
        playerGo.AddComponent<PlayerHealth>();
        var weapons = playerGo.AddComponent<PlayerWeapons>();
        weapons.shotSprite = One("bullets/player_shot.png"); weapons.exSprite = One("bullets/ex_rocket.png"); weapons.beamSprite = One("bullets/super_beam.png");
        var puff = Sprite("Exhaust", null, 9, playerGo.transform, new Vector3(-1.15f, 0, 0));
        var pa = puff.gameObject.AddComponent<SpriteAnimator>(); pa.frames = Frames("fx/exhaust_puff_sheet.png"); pa.fps = 12f;

        // ---- birds
        var birds = Obj("Birds");
        birds.AddComponent<BirdSpawner>().cruise = Frames("birds/bird_cruise_sheet.png");

        // ---- bullets
        var poolGo = Obj("Bullets");
        var pool = poolGo.AddComponent<BulletPool>();
        pool.copper = One("bullets/orb_copper.png"); pool.teal = One("bullets/orb_teal.png");
        pool.aimed = One("bullets/orb_aimed.png"); pool.heavy = One("bullets/orb_heavy.png");
        pool.pink = One("bullets/orb_pink.png"); pool.feather = One("bullets/feather.png"); pool.featherPink = One("bullets/feather_pink.png");
        pool.torpedo = One("minions/torpedo.png"); pool.mine = One("minions/mine.png");
        pool.brick = One("bullets/brick.png"); pool.flak = One("bullets/flak.png");

        // ---- bosses (inactive until the director calls them in)
        var bossRoot = Obj("Bosses");
        var eagle = Boss<EagleBoss>("TempestEagle", bossRoot.transform, Frames("bosses/eagle_flap_sheet.png")[0], new Vector2(3f, 1.5f));
        eagle.flap = Frames("bosses/eagle_flap_sheet.png"); eagle.storm = Frames("bosses/eagle_storm_sheet.png"); eagle.fury = Frames("bosses/eagle_fury_sheet.png");
        eagle.dive = One("bosses/eagle_dive.png"); eagle.ko = One("bosses/eagle_ko.png");
        eagle.cloudFrames = Frames("minions/storm_cloud_sheet.png"); eagle.bolt = One("fx/lightning_bolt.png");
        eagle.home = new Vector2(4.6f, 0.6f); eagle.maxHp = 300f; eagle.index = 0;

        var iron = Boss<IronwakeBoss>("Ironwake", bossRoot.transform, One("bosses/ironwake_idle.png"), new Vector2(5.2f, 1.7f));
        iron.idle = One("bosses/ironwake_idle.png"); iron.fire = One("bosses/ironwake_fire.png"); iron.damaged = One("bosses/ironwake_damaged.png");
        iron.maw = One("bosses/ironwake_maw.png"); iron.mawOpen = One("bosses/ironwake_maw_open.png");
        iron.home = new Vector2(4.3f, -2.3f); iron.maxHp = 380f; iron.index = 1;

        var citadel = Boss<CitadelBoss>("SundownCitadel", bossRoot.transform, One("bosses/citadel.png"), new Vector2(3.6f, 3.8f));
        citadel.tower = One("bosses/citadel.png"); citadel.cracked = One("bosses/citadel_cracked.png");
        citadel.core = Frames("bosses/citadel_core_sheet.png"); citadel.gun = Frames("bosses/citadel_gun_sheet.png");
        citadel.turret = One("minions/turret.png"); citadel.laser = One("bullets/laser.png");
        citadel.home = new Vector2(5.0f, 0.2f); citadel.maxHp = 480f; citadel.index = 2;

        var all = new BossBase[] { eagle, iron, citadel };
        foreach (var b in all) b.player = playerGo.transform;

        var director = Obj("BossDirector").AddComponent<BossDirector>();
        director.bosses = all;
        director.birds = birds.GetComponent<BirdSpawner>();
        director.biomes = biome;

        // ---- HUD (world-space sprites, ortho camera); combat HUD, boss bar and end screens are built at runtime
        var digits = Frames("ui/digits_sheet.png");
        var hud = Obj("HUD").AddComponent<HudController>();
        var score = Obj("Score", hud.transform, new Vector3(0, 3.3f, 0)).AddComponent<DigitDisplay>(); score.digits = digits;
        score.transform.localScale = Vector3.one * 1.6f; score.spacing = 0.6f;
        var tap = Sprite("TapPrompt", One("ui/tap_icon.png"), 100, hud.transform, new Vector3(-0.6f, -1.3f, 0));
        Obj("BossBar", hud.transform).AddComponent<BossBar>();
        hud.score = score; hud.tapPrompt = tap.gameObject; hud.director = director;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    static T Boss<T>(string name, Transform parent, Sprite sp, Vector2 hitbox) where T : BossBase
    {
        var sr = Sprite(name, sp, 10, parent);
        var c = sr.gameObject.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = hitbox;
        sr.gameObject.AddComponent<Hazard>();
        sr.gameObject.AddComponent<Health>();
        sr.gameObject.AddComponent<SpriteAnimator>();
        var boss = sr.gameObject.AddComponent<T>();
        sr.gameObject.SetActive(false);
        return boss;
    }

    // ---------------------------------------------------------------- biomes
    // sorting: sky -10, sky fx -9..-8, parallax 1..4, rain/near clouds 9, gameplay 10, bullets 11, foreground 12
    const float HalfH = 270f / PPU / 2f;

    static SpriteRenderer Layer(Transform root, string key, int order, float speed, Color? tint = null, float fall = 0f, bool always = false)
    {
        var sr = Sprite(Path.GetFileNameWithoutExtension(key), One(key), order, root);
        sr.color = tint ?? Color.white;
        if (speed != 0f || fall != 0f)
        {
            var s = sr.gameObject.AddComponent<Scroller>();
            s.speed = speed; s.fall = fall; s.alwaysRun = always;
        }
        return sr;
    }

    static AmbientSpawner Ambient(Transform root, string name, Sprite[] sprites, int order, Vector2 interval, Vector2 y, Vector2 scale, Vector2 speed,
        Color? tint = null, int prewarm = 0)
    {
        var a = Obj(name, root).AddComponent<AmbientSpawner>();
        a.sprites = sprites; a.sortingOrder = order; a.interval = interval; a.yRange = y; a.scaleRange = scale; a.speedRange = speed;
        a.tint = tint ?? Color.white; a.prewarm = prewarm;
        return a;
    }

    static Spinner Rays(Transform root, Vector2 at, Color tint, float speed, float scale)
    {
        var sr = Sprite("SunRays", One("bg/dawn_sun_rays.png"), -9, root, at);
        sr.color = tint;
        sr.transform.localScale = Vector3.one * scale;
        var sp = sr.gameObject.AddComponent<Spinner>();
        sp.speed = speed; sp.pulseAmp = 0.05f; sp.pulseFreq = 0.25f; sp.alphaAmp = 0.25f; sp.alphaFreq = 0.2f;
        return sp;
    }

    static Sprite[] Clouds() => new[] { One("bg/amb_cloud_1.png"), One("bg/amb_cloud_2.png"), One("bg/amb_cloud_3.png") };

    static GameObject Dawn(Transform parent)
    {
        var root = Obj("Dawn", parent).transform;
        Layer(root, "bg/dawn_sky.png", -10, 0f);
        Rays(root, new Vector2(-4.84f, -2.03f), new Color(1f, 0.9f, 0.8f, 0.6f), 8f, 1.3f);
        Layer(root, "bg/dawn_clouds_far.png", 1, 0.3f);
        Layer(root, "bg/dawn_hills_far.png", 2, 0.6f);
        var balloon = Ambient(root, "Balloons", new[] { One("bg/amb_balloon.png") }, 2, new Vector2(10f, 16f), new Vector2(-0.3f, 2.6f),
            new Vector2(0.8f, 1.1f), new Vector2(0.35f, 0.55f), null, 1);
        balloon.bobAmp = 0.15f; balloon.bobFreq = 0.25f;
        var birds = Ambient(root, "FarBirds", Frames("bg/amb_gull_sheet.png"), 2, new Vector2(3f, 6f), new Vector2(0.8f, 3.4f),
            new Vector2(0.7f, 1f), new Vector2(1.4f, 2.2f), new Color(0.55f, 0.4f, 0.4f, 0.9f));
        birds.animated = true; birds.fps = 6f; birds.bobAmp = 0.1f;
        Ambient(root, "Clouds", Clouds(), 3, new Vector2(3.5f, 6.5f), new Vector2(1.6f, 3.8f), new Vector2(0.6f, 1f), new Vector2(0.8f, 1.3f),
            new Color(1f, 0.9f, 0.85f, 0.75f), 2);
        Layer(root, "bg/dawn_clouds_mid.png", 3, 1.0f);
        Layer(root, "bg/dawn_hills_near.png", 4, 1.8f);
        var wind = Ambient(root, "Wind", new[] { One("fx/wind_streak.png") }, 12, new Vector2(0.5f, 1.3f), new Vector2(-3.5f, 3.8f),
            new Vector2(0.8f, 1.4f), new Vector2(9f, 12f), new Color(1f, 1f, 1f, 0.45f));
        wind.alphaRange = new Vector2(0.5f, 1f);
        return root.gameObject;
    }

    static GameObject Storm(Transform parent)
    {
        var root = Obj("Storm", parent).transform;
        Layer(root, "bg/storm_sky.png", -10, 0f);
        var flash = Sprite("SkyFlash", One("ui/white.png"), -8, root);
        flash.transform.localScale = new Vector3(130f, 75f, 1f);   // 4 px sprite -> covers the view
        flash.color = new Color(0.9f, 0.95f, 1f, 0f);
        var sf = root.gameObject.AddComponent<SkyFlash>();
        sf.flash = flash; sf.bolt = One("bg/storm_lightning.png"); sf.boltOrder = -9;
        Layer(root, "bg/storm_clouds.png", 1, 0.4f);
        Layer(root, "bg/storm_rain_far.png", 2, 1.0f, new Color(1f, 1f, 1f, 0.7f), 6f, true);
        var puffs = Ambient(root, "CloudPuffs", new[] { One("bg/storm_cloud_puff.png") }, 3, new Vector2(2f, 4f), new Vector2(-1.5f, 3.8f),
            new Vector2(1f, 1.6f), new Vector2(1.2f, 2f), new Color(0.85f, 0.9f, 1f, 0.9f), 3);
        puffs.bobAmp = 0.08f; puffs.bobFreq = 0.3f;
        Layer(root, "bg/storm_clouds_mid.png", 3, 0.9f);
        Layer(root, "bg/storm_clouds_near.png", 9, 2.2f);
        Layer(root, "bg/storm_rain.png", 9, 3f, new Color(1f, 1f, 1f, 0.55f), 12f, true);
        root.gameObject.SetActive(false);
        return root.gameObject;
    }

    static GameObject Sea(Transform parent)
    {
        var root = Obj("Sea", parent).transform;
        Layer(root, "bg/sea_sky.png", -10, 0f);
        Rays(root, new Vector2(3.1f, -1.0f), new Color(1f, 0.95f, 0.85f, 0.35f), -5f, 1.1f);
        Ambient(root, "Clouds", Clouds(), 1, new Vector2(4f, 7f), new Vector2(1.8f, 3.8f), new Vector2(0.7f, 1.1f), new Vector2(0.4f, 0.7f),
            new Color(1f, 0.97f, 0.92f, 0.8f), 2);
        Layer(root, "bg/sea_horizon.png", 1, 0.2f);
        var islands = Layer(root, "bg/sea_islands.png", 2, 0.35f);
        // the lighthouse lamp sits at pixel (340, 136) of the island strip: the beam pivots there and sweeps
        var pivot = Obj("BeamPivot", islands.transform, new Vector3(3.125f, -0.03f, 0f));
        var beam = Sprite("Beam", One("bg/lighthouse_beam.png"), 2, pivot.transform, new Vector3(-1f, 0f, 0f));
        beam.color = new Color(1f, 1f, 1f, 0.8f);
        var sp = pivot.AddComponent<Spinner>();
        sp.speed = 0f; sp.baseAngle = 10f; sp.swingAmp = 30f; sp.swingFreq = 0.18f; sp.alphaAmp = 0.3f; sp.alphaFreq = 0.36f;
        var gulls = Ambient(root, "Gulls", Frames("bg/amb_gull_sheet.png"), 3, new Vector2(2f, 4.5f), new Vector2(0.3f, 3.6f),
            new Vector2(0.9f, 1.3f), new Vector2(1.5f, 2.6f), null, 2);
        gulls.animated = true; gulls.fps = 6f; gulls.bobAmp = 0.15f; gulls.bobFreq = 0.6f;
        var wf = Frames("bg/sea_waves_sheet.png");
        var waves = Layer(root, "bg/sea_waves_sheet.png", 3, 1.2f);
        waves.sprite = wf[0];
        waves.transform.localPosition = new Vector3(0f, -HalfH + 1.5f, 0f);
        var wa = waves.gameObject.AddComponent<SpriteAnimator>(); wa.frames = wf; wa.fps = 4f;
        var glitter = Ambient(root, "Glitter", new[] { One("bg/sea_glitter.png") }, 4, new Vector2(0.08f, 0.2f), new Vector2(-3.9f, -1.6f),
            new Vector2(0.6f, 1.1f), new Vector2(1.2f, 1.2f));
        glitter.twinkle = 0.6f; glitter.max = 30;
        Layer(root, "bg/sea_spray.png", 4, 2.5f);
        Layer(root, "bg/sea_foam_near.png", 12, 3.2f);
        root.gameObject.SetActive(false);
        return root.gameObject;
    }

    static GameObject Dusk(Transform parent)
    {
        var root = Obj("Dusk", parent).transform;
        Layer(root, "bg/dusk_sky.png", -10, 0f);
        Rays(root, new Vector2(2.81f, -1.9f), new Color(1f, 0.6f, 0.3f, 0.55f), 6f, 1.5f);
        Layer(root, "bg/dawn_clouds_far.png", 1, 0.3f, new Color(1f, 0.6f, 0.45f, 0.8f));
        var clouds = Ambient(root, "DuskClouds", new[] { One("bg/dusk_cloud.png") }, 1, new Vector2(3f, 6f), new Vector2(0.3f, 3.6f),
            new Vector2(0.9f, 1.4f), new Vector2(0.5f, 0.9f), null, 3);
        clouds.bobAmp = 0.05f; clouds.bobFreq = 0.2f;
        Layer(root, "bg/dawn_hills_far.png", 2, 0.5f, new Color(0.45f, 0.25f, 0.3f));
        var crows = Ambient(root, "Crows", Frames("bg/amb_gull_sheet.png"), 3, new Vector2(3f, 6f), new Vector2(0f, 3.2f),
            new Vector2(0.8f, 1.1f), new Vector2(1.6f, 2.4f), new Color(0.25f, 0.15f, 0.18f, 1f));
        crows.animated = true; crows.fps = 7f; crows.bobAmp = 0.12f;
        Layer(root, "bg/dawn_hills_near.png", 4, 1.6f, new Color(0.2f, 0.12f, 0.14f));
        var embers = Ambient(root, "Embers", new[] { One("bg/sea_glitter.png") }, 12, new Vector2(0.25f, 0.6f), new Vector2(-4f, 3.5f),
            new Vector2(0.5f, 0.9f), new Vector2(2f, 4f), new Color(1f, 0.6f, 0.25f, 0.8f));
        embers.bobAmp = 0.4f; embers.bobFreq = 0.5f;
        root.gameObject.SetActive(false);
        return root.gameObject;
    }
}
