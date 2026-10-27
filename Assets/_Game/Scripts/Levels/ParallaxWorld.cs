using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Five painted layers for one world, scrolled at different speeds, plus the movers that make it feel alive.
/// Files are read at runtime from Art/parallax, the same way PropArt loads props.
/// </summary>
public class ParallaxWorld : MonoBehaviour
{
    public static ParallaxWorld Current { get; private set; }

    const float ViewH = 8.44f;

    static readonly string[] Names = { "dawn", "sea", "storm", "sundown", "star" };
    static readonly string[] Layers = { "sky", "far", "mid", "near", "front" };
    static readonly float[] Speeds = { 0.14f, 0.4f, 0.78f, 1.3f, 2.1f };
    static readonly int[] Orders = { 0, 2, 3, 4, 14 };
    static readonly float[] Alphas = { 1f, 1f, 1f, 1f, 0.2f };

    class Band
    {
        public Transform root;
        public float speed, span;
        public readonly List<Transform> tiles = new List<Transform>();
    }

    readonly List<Band> bands = new List<Band>();
    readonly List<SpriteRenderer> lamps = new List<SpriteRenderer>();
    readonly List<Transform> aurora = new List<Transform>();
    SpriteRenderer grade, flash, shot;
    Color gradeA, gradeB;
    int world;
    bool onCamera;
    float flashIn = 2.2f, shotIn = 1.4f;
    int fpsFrames;
    float fpsAcc, fpsMark;

    public static ParallaxWorld Spawn(int world, Transform parent, bool onCamera)
    {
        if (Current) Destroy(Current.gameObject);
        var biome = Object.FindAnyObjectByType<BiomeDirector>();
        if (biome != null && biome.roots != null)
            foreach (var r in biome.roots)
                if (r) r.SetActive(false);
        var go = new GameObject("ParallaxWorld");
        go.transform.SetParent(parent, false);
        if (onCamera && parent != null)
        {
            var cam = parent.GetComponent<Camera>() ?? Camera.main;
            if (cam) go.transform.localPosition = new Vector3(0f, 0f, -cam.transform.position.z);
        }
        var pw = go.AddComponent<ParallaxWorld>();
        pw.onCamera = onCamera;
        pw.Build(world);
        return pw;
    }

    void OnDestroy() { if (Current == this) Current = null; }

    void Build(int w)
    {
        Current = this;
        world = Mathf.Clamp(w, 0, Names.Length - 1);
        string folder = "parallax/" + Names[world] + "/";
        for (int i = 0; i < Layers.Length; i++)
        {
            bool front = i == Layers.Length - 1;
            int order = front && !onCamera ? 42 : Orders[i];
            // the front plate is a few leaves on a full frame; shrink it so they stay small and high
            var spr = PropArt.Paint(folder + Layers[i] + ".png", front ? ViewH * 0.34f : ViewH);
            bool fallback = false;
            if (!spr && i == 0) { spr = GradientSky(world); fallback = true; }
            if (!spr) continue;
            // keyed plates fade at the side edges, so those copies overlap. the sky is opaque.
            MakeBand(spr, Speeds[i], order, Alphas[i], fallback, i != 0);
            if (front) bands[bands.Count - 1].root.localPosition = new Vector3(0f, 2.35f, 0f);
        }
        Grade();
        Life();
    }

    void MakeBand(Sprite spr, float speed, int order, float alpha, bool fallback, bool overlap)
    {
        float width = spr.bounds.size.x;
        // Opaque tiles used to meet on exactly one texel, so bilinear showed the camera clear as a bright line.
        float pad = 3f / Mathf.Max(1f, spr.pixelsPerUnit);
        float span = fallback ? 18f : (overlap ? width * 0.82f : Mathf.Max(0.5f, width - pad));
        int n = Mathf.Max(2, Mathf.CeilToInt(20f / Mathf.Max(0.5f, span)) + 1);
        var root = new GameObject("band").transform;
        root.SetParent(transform, false);
        var band = new Band { root = root, speed = speed, span = span };
        for (int k = -1; k < n; k++)
        {
            var sr = Ui.Spr("tile", root, spr, new Vector2(k * span, 0f), order);
            if (fallback)
                sr.transform.localScale = new Vector3(18f / width, ViewH / spr.bounds.size.y, 1f);
            var c = Color.white;
            c.a = alpha;
            sr.color = c;
            band.tiles.Add(sr.transform);
        }
        bands.Add(band);
    }

    static Sprite GradientSky(int world)
    {
        Color top, bot;
        switch (world)
        {
            case 1: top = new Color(0.45f, 0.78f, 0.95f); bot = new Color(0.95f, 0.85f, 0.55f); break;
            case 2: top = new Color(0.12f, 0.16f, 0.24f); bot = new Color(0.28f, 0.32f, 0.28f); break;
            case 3: top = new Color(0.35f, 0.18f, 0.38f); bot = new Color(0.95f, 0.45f, 0.22f); break;
            case 4: top = new Color(0.04f, 0.05f, 0.14f); bot = new Color(0.16f, 0.12f, 0.32f); break;
            default: top = new Color(0.55f, 0.82f, 0.82f); bot = new Color(0.98f, 0.72f, 0.52f); break;
        }
        var tex = new Texture2D(4, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 32; y++)
        {
            var c = Color.Lerp(bot, top, y / 31f);
            for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 32), new Vector2(0.5f, 0.5f), 32f / ViewH);
    }

    void Grade()
    {
        switch (world)
        {
            case 1: gradeA = new Color(0.55f, 0.8f, 1f, 0.08f); gradeB = new Color(1f, 0.92f, 0.7f, 0.14f); break;
            case 2: gradeA = new Color(0.15f, 0.18f, 0.28f, 0.1f); gradeB = new Color(0.02f, 0.03f, 0.08f, 0.28f); break;
            case 3: gradeA = new Color(1f, 0.62f, 0.28f, 0.1f); gradeB = new Color(0.35f, 0.08f, 0.16f, 0.22f); break;
            case 4: gradeA = new Color(0.05f, 0.06f, 0.16f, 0.18f); gradeB = new Color(0.35f, 0.28f, 0.55f, 0.08f); break;
            default: gradeA = new Color(0.45f, 0.55f, 0.8f, 0.22f); gradeB = new Color(1f, 0.86f, 0.62f, 0.05f); break;
        }
        grade = Ui.Box("Grade", transform, Vector2.zero, new Vector2(20f, 12f), gradeA, onCamera ? 18 : 46);
    }

    void Life()
    {
        switch (world)
        {
            case 0:
                Gulls(4, 1.5f, 6);
                OnBand(1, t => Beam(t, new Vector2(2.4f, -0.7f), 3));
                OnBand(2, t => Mill(t, new Vector2(-3.1f, -1.15f), 4));
                OnBand(3, t => { Smoke(t, new Vector2(1.4f, -0.55f), 5); Flag(t, new Vector2(-2.2f, -0.35f), 5); });
                Drifters("parallax/props/leaf.png", 3, 2.4f, 12, new Color(0.85f, 0.45f, 0.2f, 0.22f), 0.16f);
                break;
            case 1:
                Gulls(5, 1.7f, 6);
                OnBand(2, t => Bobber(t, "parallax/props/boat.png", new Vector2(1.6f, -1.7f), 0.9f, 5, 0.14f, 0.7f));
                Glitter(16, 4);
                Drifters("parallax/props/puff.png", 3, 1.1f, 5, new Color(1f, 1f, 1f, 0.2f), 0.14f);
                break;
            case 2:
                Gulls(3, 2.4f, 6);
                Rain(16, 6.5f, 0.38f, 0.28f, 5);
                Rain(20, 13f, 0.72f, 0.5f, onCamera ? 15 : 44);
                Drifters("parallax/props/leaf.png", 3, 3.4f, onCamera ? 15 : 44, new Color(0.45f, 0.55f, 0.35f, 0.22f), 0.16f);
                flash = Ui.Box("Flash", transform, Vector2.zero, new Vector2(20f, 12f), new Color(0.9f, 0.95f, 1f, 0f), onCamera ? 55 : 70);
                break;
            case 3:
                OnBand(2, t => Bobber(t, "parallax/props/balloon.png", new Vector2(-2f, 1.6f), 1.1f, 5, 0.18f, 0.35f));
                OnBand(3, t => Flag(t, new Vector2(2.4f, -0.2f), 6));
                OnBand(2, t => Smoke(t, new Vector2(3.2f, -0.4f), 5));
                Lamps(10);
                Drifters("parallax/props/puff.png", 3, 0.55f, 12, new Color(1f, 0.82f, 0.4f, 0.2f), 0.12f);
                break;
            default:
                OnBand(1, t => Beam(t, new Vector2(-3.2f, -0.4f), 3));
                Fireflies(12);
                Aurora();
                var streak = PropArt.Paint("parallax/props/star_shot.png", 0.7f, true);
                shot = streak
                    ? Ui.Spr("Star", transform, streak, new Vector2(8f, 3f), 8)
                    : Ui.Box("Star", transform, new Vector2(8f, 3f), new Vector2(0.7f, 0.04f), new Color(1f, 0.95f, 0.8f, 0f), 8);
                shot.color = new Color(1f, 0.95f, 0.8f, 0f);
                break;
        }
    }

    void OnBand(int index, System.Action<Transform> place)
    {
        if (index < 0 || index >= bands.Count) return;
        foreach (var t in bands[index].tiles) place(t);
    }

    void Beam(Transform tile, Vector2 at, int order)
    {
        var pivot = Ui.Root("Lamp", tile);
        pivot.localPosition = at;
        var spr = PropArt.Paint("parallax/props/beam.png", 0.45f, true);
        if (spr)
        {
            var sr = Ui.Spr("Beam", pivot, spr, new Vector2(spr.bounds.extents.x, 0f), order);
            sr.color = new Color(1f, 0.95f, 0.75f, 0.55f);
        }
        else
            Ui.Box("Beam", pivot, new Vector2(1.7f, 0f), new Vector2(3.4f, 0.14f), new Color(1f, 0.95f, 0.72f, 0.42f), order);
        var tower = PropArt.Paint("parallax/props/lighthouse.png", 1.5f);
        if (tower) Ui.Spr("Tower", tile, tower, at + new Vector2(0f, -0.55f), order - 1);
        var sp = pivot.gameObject.AddComponent<Spinner>();
        sp.speed = 0f;
        sp.baseAngle = -18f;
        sp.swingAmp = 36f;
        sp.swingFreq = 0.15f;
        sp.alphaAmp = 0.4f;
        sp.alphaFreq = 0.35f;
    }

    void Mill(Transform tile, Vector2 at, int order)
    {
        var root = Ui.Root("Mill", tile);
        root.localPosition = at;
        var tower = PropArt.Paint("parallax/props/mill.png", 1.35f);
        if (tower) Ui.Spr("Tower", root, tower, new Vector2(0f, 0.15f), order);
        else Ui.Box("Tower", root, new Vector2(0f, 0.1f), new Vector2(0.32f, 1.15f), new Color(0.62f, 0.5f, 0.38f), order);
        var blades = PropArt.Paint("parallax/props/blades.png", 1.15f);
        Transform hub;
        if (blades) hub = Ui.Spr("Blades", root, blades, new Vector2(0.05f, 0.72f), order + 1).transform;
        else
        {
            hub = Ui.Root("Blades", root, new Vector2(0.05f, 0.72f));
            Ui.Box("A", hub, Vector2.zero, new Vector2(1.25f, 0.07f), new Color(0.42f, 0.26f, 0.14f), order + 1);
            Ui.Box("B", hub, Vector2.zero, new Vector2(0.07f, 1.25f), new Color(0.42f, 0.26f, 0.14f), order + 1);
        }
        hub.gameObject.AddComponent<Spinner>().speed = 80f;
    }

    void Flag(Transform tile, Vector2 at, int order)
    {
        var spr = PropArt.Paint("parallax/props/flag.png", 0.55f);
        if (!spr) return;
        var sr = Ui.Spr("Flag", tile, spr, at, order);
        var sp = sr.gameObject.AddComponent<Spinner>();
        sp.speed = 0f;
        sp.swingAmp = 10f;
        sp.swingFreq = 1.6f;
    }

    void Smoke(Transform tile, Vector2 at, int order)
    {
        var puff = PropArt.Paint("parallax/props/puff.png", 0.16f);
        for (int i = 0; i < 3; i++)
        {
            var sr = puff
                ? Ui.Spr("Puff", tile, puff, at, order)
                : Ui.Spr("Puff", tile, Ui.Disc, at, order, 0.04f);
            sr.color = new Color(0.9f, 0.9f, 0.92f, 0.2f);
            var m = sr.gameObject.AddComponent<SkyMote>();
            m.rise = true;
            m.bob = 0.85f;
            m.life = 1.7f;
            m.phase = i * 0.55f;
        }
    }

    void Bobber(Transform tile, string file, Vector2 at, float height, int order, float amp, float freq)
    {
        var spr = PropArt.Paint(file, height);
        if (!spr) return;
        var sr = Ui.Spr("Bob", tile, spr, at, order);
        var m = sr.gameObject.AddComponent<SkyMote>();
        m.bob = amp;
        m.freq = freq;
        m.wrap = false;
    }

    void Gulls(int n, float speed, int order)
    {
        var frames = new Sprite[4];
        for (int i = 0; i < 4; i++)
        {
            frames[i] = PropArt.Paint("parallax/props/bird_" + i + ".png", 0.42f);
            if (!frames[i]) return;
        }
        for (int i = 0; i < n; i++)
        {
            float y = Random.Range(0.4f, 3.1f);
            var sr = Ui.Spr("Gull", transform, frames[0], new Vector2(Random.Range(-7f, 7f), y), order);
            var anim = sr.gameObject.AddComponent<SpriteAnimator>();
            anim.frames = frames;
            anim.fps = 8f;
            anim.phase = Random.value * 4f;
            var m = sr.gameObject.AddComponent<SkyMote>();
            m.speed = speed + i * 0.15f;
            m.bob = 0.14f;
            m.freq = 1.1f;
            m.wrap = true;
        }
    }

    void Drifters(string file, int n, float speed, int order, Color tint, float height)
    {
        var spr = PropArt.Paint(file, height);
        // a missing plate must not become a 1-unit disc (that reads as a lens smudge)
        float scale = spr ? 1f : 0.045f;
        tint.a = Mathf.Min(tint.a, 0.22f);
        for (int i = 0; i < n; i++)
        {
            var sr = Ui.Spr("Drift", transform, spr ? spr : Ui.Disc,
                new Vector2(Random.Range(-8f, 8f), Random.Range(1.2f, 3.6f)), order, scale);
            sr.color = tint;
            var m = sr.gameObject.AddComponent<SkyMote>();
            m.speed = speed * Random.Range(0.7f, 1.3f);
            m.bob = 0.12f;
            m.freq = Random.Range(0.6f, 1.4f);
            m.spin = spr ? Random.Range(-12f, 12f) : 0f;
            m.wrap = true;
        }
    }

    void Glitter(int n, int order)
    {
        for (int i = 0; i < n; i++)
        {
            var sr = Ui.Spr("Glint", transform, Ui.Disc, new Vector2(Random.Range(-7f, 7f), Random.Range(-3.4f, -1.5f)), order, 0.04f);
            sr.color = new Color(1f, 1f, 0.85f, 0.22f);
            var m = sr.gameObject.AddComponent<SkyMote>();
            m.speed = 1.15f;
            m.twinkle = true;
            m.freq = Random.Range(2f, 4f);
            m.wrap = true;
        }
    }

    void Lamps(int n)
    {
        OnBand(2, t =>
        {
            for (int i = 0; i < n; i++)
            {
                var sr = Ui.Spr("Lamp", t, Ui.Disc, new Vector2(-6f + i * 1.3f, -1.35f + Mathf.Sin(i) * 0.25f), 5, 0.045f);
                sr.color = new Color(1f, 0.82f, 0.4f, 0f);
                lamps.Add(sr);
            }
        });
    }

    void Fireflies(int n)
    {
        var fly = PropArt.Paint("parallax/props/firefly.png", 0.32f);
        float scale = fly ? 1f : 0.22f;
        var blob = fly ? fly : SoftDisc();
        for (int i = 0; i < n; i++)
        {
            var sr = Ui.Spr("Fly", transform, blob, new Vector2(Random.Range(-7f, 7f), Random.Range(-2f, 2.5f)), 8, scale);
            sr.color = fly ? Color.white : new Color(0.85f, 1f, 0.55f, 0.55f);
            var m = sr.gameObject.AddComponent<SkyMote>();
            m.speed = 0.35f;
            m.bob = 0.35f;
            m.freq = 0.7f;
            m.twinkle = true;
            m.wrap = true;
        }
    }

    void Aurora()
    {
        // Hard boxes read as a UI glitch. A few wide soft discs sit in the sky like a milky way.
        var blob = SoftDisc();
        for (int i = 0; i < 5; i++)
        {
            var c = i % 2 == 0 ? new Color(0.45f, 0.95f, 0.75f, 0.22f) : new Color(0.62f, 0.45f, 0.95f, 0.16f);
            var sr = Ui.Spr("Aurora", transform, blob, new Vector2(-6f + i * 3.1f, 1.15f + (i % 2) * 0.7f), 1, 1f);
            sr.color = c;
            sr.transform.localScale = new Vector3(4.2f, 1.15f, 1f);
            aurora.Add(sr.transform);
        }
    }

    static Sprite softDisc;
    static Sprite SoftDisc()
    {
        if (softDisc) return softDisc;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        var pix = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                float a = Mathf.Clamp01(1f - (u * u + v * v));
                pix[y * n + x] = new Color(1f, 1f, 1f, a * a);
            }
        tex.SetPixels(pix);
        tex.Apply();
        softDisc = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        return softDisc;
    }

    void Rain(int count, float speed, float len, float alpha, int order)
    {
        for (int i = 0; i < count; i++)
        {
            var sr = Ui.Box("Drop", transform, new Vector2(Random.Range(-9f, 9f), Random.Range(-5f, 5f)), new Vector2(0.028f, len),
                new Color(0.82f, 0.9f, 1f, alpha), order);
            sr.transform.localRotation = Quaternion.Euler(0f, 0f, 18f);
            var m = sr.gameObject.AddComponent<SkyMote>();
            m.speed = speed * 0.25f;
            m.fall = speed;
            m.wrap = true;
        }
    }

    float Progress()
    {
        float time = Mathf.Clamp01(Time.timeSinceLevelLoad / 32f);
        var lv = LevelFlow.Current;
        if (lv == null) return time;
        if ((lv.game == LevelGame.Flappy || lv.game == LevelGame.Boss) && GameState.I != null)
            return Mathf.Clamp01(Mathf.Max(time, GameState.I.Score / (float)Mathf.Max(1, lv.stars[2])));
        return Mathf.Clamp01(Mathf.Max(time, MiniGame.Progress01));
    }

    void Update()
    {
        float dt = Time.deltaTime;
        fpsFrames++;
        fpsAcc += Time.unscaledDeltaTime;
        if (fpsFrames == 240)
            Debug.Log("[Parallax] fps " + (240f / fpsAcc).ToString("0.0"));
        else if (fpsFrames == 480)
            Debug.Log("[Parallax] fps " + (240f / (fpsAcc - fpsMark)).ToString("0.0"));
        if (fpsFrames == 240) fpsMark = fpsAcc;
        for (int i = 0; i < bands.Count; i++)
        {
            var b = bands[i];
            var p = b.root.localPosition;
            p.x -= b.speed * dt;
            if (p.x <= -b.span) p.x += b.span;
            b.root.localPosition = p;
        }
        if (grade) grade.color = Color.Lerp(gradeA, gradeB, Progress());
        for (int i = 0; i < lamps.Count; i++)
        {
            var c = lamps[i].color;
            c.a = Mathf.Clamp01(Progress() * 1.4f) * (0.45f + 0.55f * Mathf.Abs(Mathf.Sin(Time.time * 3f + i)));
            lamps[i].color = c;
        }
        for (int i = 0; i < aurora.Count; i++)
        {
            var p = aurora[i].localPosition;
            p.y = 1.15f + Mathf.Sin(Time.time * 0.7f + i * 0.6f) * 0.45f;
            aurora[i].localPosition = p;
        }
        if (flash)
        {
            flashIn -= dt;
            var c = flash.color;
            if (flashIn <= 0f)
            {
                c.a = 0.72f;
                flashIn = Random.Range(2.4f, 4.2f);
                var bolt = PropArt.Imagen("storm_bolt", 2.2f);
                if (bolt)
                    GameKit.Pop(bolt, new Vector3(Random.Range(-5f, 5f), Random.Range(-1f, 2.5f), 0f), 0.18f, 1f, 0.2f, 56, Color.white);
            }
            else c.a = Mathf.MoveTowards(c.a, 0f, dt * 6f);
            flash.color = c;
        }
        if (shot)
        {
            shotIn -= dt;
            var p = shot.transform.localPosition;
            if (shotIn <= 0f)
            {
                p = new Vector3(Random.Range(2f, 8f), Random.Range(1.2f, 3.6f), 0f);
                shot.transform.localPosition = p;
                shot.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-35f, -15f));
                shotIn = Random.Range(1.6f, 3.2f);
                var c0 = shot.color; c0.a = 0.9f; shot.color = c0;
            }
            p.x -= 9f * dt;
            p.y -= 3.2f * dt;
            shot.transform.localPosition = p;
            var c = shot.color;
            c.a = Mathf.MoveTowards(c.a, 0f, dt * 1.6f);
            shot.color = c;
        }
    }
}

/// <summary>One drifting, bobbing, rising or twinkling sprite. Used only by <see cref="ParallaxWorld"/>.</summary>
public class SkyMote : MonoBehaviour
{
    public float speed, fall, bob, freq = 1f, spin, life = 1.5f, phase;
    public bool wrap = true, rise, twinkle;
    Vector3 origin;
    SpriteRenderer r;
    float age, baseA;

    void Awake()
    {
        r = GetComponent<SpriteRenderer>();
        if (r) baseA = r.color.a;
    }

    void Start() { origin = transform.localPosition; age = phase; }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        var p = transform.localPosition;
        p.x -= speed * dt;
        p.y -= fall * dt;
        if (rise)
        {
            float u = Mathf.Repeat(age / Mathf.Max(0.2f, life), 1f);
            p = origin + new Vector3(Mathf.Sin(age * 3f) * 0.08f, u * bob, 0f);
            if (r)
            {
                var c = r.color;
                c.a = baseA * (1f - u);
                r.color = c;
            }
            transform.localScale = Vector3.one * (0.35f + u * 0.9f);
        }
        else
        {
            p.y = origin.y + Mathf.Sin((age + origin.x) * freq) * bob - (fall > 0f ? 0f : 0f);
            if (fall > 0f) p.y = transform.localPosition.y - fall * dt;
        }
        if (wrap && p.x < -9.2f) p.x += 18.4f;
        if (wrap && fall > 0f && p.y < -5.2f) p.y += 10.4f;
        transform.localPosition = p;
        if (spin != 0f) transform.Rotate(0f, 0f, spin * dt);
        if (twinkle && r && !rise)
        {
            var c = r.color;
            c.a = baseA * (0.25f + 0.75f * Mathf.Abs(Mathf.Sin(age * freq)));
            r.color = c;
        }
    }
}
