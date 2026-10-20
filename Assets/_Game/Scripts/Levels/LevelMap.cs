using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Candy-Crush style world map: a winding trail of level nodes across four biome skies, scrolled sideways
/// (drag, wheel, arrows). Nodes show stars earned; locked ones are greyed. Click a node (or Space) for its card, then PLAY.
/// </summary>
public class LevelMap : MonoBehaviour
{
    public const float Segment = 15f;      // one world = one 480px sky
    const int Order = 200;

    Transform strip, card, top, mapFar;
    Transform[] drift;
    readonly List<Vector2> nodePos = new List<Vector2>();
    readonly List<Transform> nodes = new List<Transform>();
    float scroll, target, minScroll, maxScroll;
    int selected;
    Transform plane;
    Vector2 planeFrom, planeTo;
    float planeT = 1f;
    readonly List<Transform> chests = new List<Transform>();
    readonly List<Vector2> chestPos = new List<Vector2>();

    bool pressing, dragging;
    Vector2 pressAt;
    float pressScroll, lastX;

    LevelDef cardLevel;
    Ui.Button playBtn, closeBtn, storyBtn, postBtn, menuBtn, panelClose, prevBtn, nextBtn;
    float cardAt;
    Transform panel;
    bool postcardsMode;
    int storyPage;
    readonly List<string> storyShown = new List<string>();
    readonly List<Ui.Button> panelBtns = new List<Ui.Button>();
    readonly List<string> panelIds = new List<string>();

    public static Vector2 NodePos(int index)
    {
        int w = index / LevelDB.PerWorld, j = index % LevelDB.PerWorld;
        // Centres stay in [-1, 1.5]. The star arc and the LAND label need the slack below the menu buttons.
        float x = w * Segment + 1.4f + j * 3.15f;
        float y = 0.2f + 1.05f * Mathf.Sin(index * 0.9f + 0.4f);
        return new Vector2(x, y);
    }

    void Start()
    {
        strip = Ui.Root("MapStrip", transform);
        for (int w = 0; w < LevelDB.Worlds.Length; w++) BuildWorld(w);
        for (int i = 0; i < LevelDB.Count; i++) nodePos.Add(NodePos(i));
        BuildDepth();
        BuildTrail();
        for (int i = 0; i < LevelDB.Count; i++) nodes.Add(BuildNode(LevelDB.All[i], nodePos[i]));
        BuildTop();

        int frontier = LevelFlow.Frontier();
        selected = Mathf.Clamp(LevelFlow.Focus > 0 && LevelFlow.Unlocked(LevelFlow.Focus) ? LevelFlow.Focus : frontier, 1, LevelDB.Count);
        int from = frontier;
        if (LevelFlow.Last != null && LevelFlow.Last.won)
            from = Mathf.Clamp(LevelFlow.Last.level.id, 1, frontier);
        planeFrom = nodePos[from - 1];
        planeTo = nodePos[frontier - 1];
        planeT = from == frontier ? 1f : 0f;
        BuildChests();
        var fly = LevelRuntime.Fly;
        if (fly != null && fly.Length > 0)
        {
            var sr = Ui.Spr("You", strip, fly[0], planeFrom + new Vector2(0.55f, 0.35f), Order + 8, 0.55f);
            sr.gameObject.AddComponent<SpriteAnimator>().frames = fly;
            plane = sr.transform;
        }
        minScroll = Segment * 0.5f;
        maxScroll = Segment * LevelDB.Worlds.Length - Segment * 0.5f;
        scroll = target = Mathf.Clamp(nodePos[selected - 1].x, minScroll, maxScroll);
        Apply();
        if (!CutscenePlayer.IsPlaying) AudioManager.Music("music_flappy", 0.5f);
    }

    // ---------------------------------------------------------------- build
    void BuildWorld(int w)
    {
        float cx = w * Segment + Segment / 2f;
        string[] paintedNames = { "sky_dawn", "sky_sea", "sky_storm", "sky_sundown", "sky_star" };
        var painted = PropArt.Imagen(paintedNames[w], 15f, true);
        var sky = painted ? painted : LevelRuntime.Skies[LevelDB.WorldBiome[w]];
        if (sky)
        {
            var sr = Ui.Spr("Sky" + w, strip, sky, new Vector2(cx, 0f), Order - 20);
            sr.transform.localScale = new Vector3(Segment / sky.bounds.size.x * 1.002f, 8.6f / sky.bounds.size.y, 1f);
        }
        else Ui.Box("Sky" + w, strip, new Vector2(cx, 0f), new Vector2(Segment, 8.6f), LevelDB.WorldColor[w], Order - 20);

        // Hills only when the sky is still a flat biome plate. An Imagine sky already has its own ground.
        if (!painted)
        {
            var ground = Color.Lerp(LevelDB.WorldColor[w], new Color(0.25f, 0.45f, 0.3f), 0.55f);
            for (int k = 0; k < 16; k++)
            {
                float x = w * Segment + k + 0.5f;
                float h = 1.1f + 0.25f * Mathf.Sin((w * Segment + k) * 0.9f);
                Ui.Box("Hill", strip, new Vector2(x, -4.2f + h / 2f), new Vector2(1.02f, h), ground, Order - 18);
            }
        }
        var c = LevelDB.WorldColor[w];
        Ui.Txt(strip, "WORLD " + (w + 1), new Vector2(cx, -2.15f), 0.7f, Announcer.Cream, Order - 10);
        Ui.Txt(strip, LevelDB.Worlds[w], new Vector2(cx, -2.65f), 1.05f, c, Order - 10, PixelText.Align.Center, PixelText.Effect.Wave);
        if (w > 0 && w < LevelDB.GateStars.Length)
        {
            bool gateOpen = LevelFlow.Unlocked(w * LevelDB.PerWorld + 1);
            var arch = Ui.Spr("Gate", strip, PropArt.Gate, new Vector2(w * Segment + 0.35f, -2.15f), Order - 4, 1.05f);
            if (!gateOpen) arch.color = new Color(0.45f, 0.45f, 0.52f, 0.95f);
            Ui.Txt(strip, LevelDB.GateStars[w] + " STARS", new Vector2(w * Segment + 0.35f, -2.9f), 0.65f,
                gateOpen ? Announcer.Yellow : Announcer.Cream, Order - 3);
        }
        // locked worlds get a haze until their first level opens
        if (!LevelFlow.Unlocked(w * LevelDB.PerWorld + 1))
        {
            var haze = Ui.Ink; haze.a = 0.35f;
            Ui.Box("Haze", strip, new Vector2(cx, 0f), new Vector2(Segment, 8.6f), haze, Order - 9);
        }
    }

    void BuildTrail()
    {
        for (int i = 0; i + 1 < nodePos.Count; i++)
        {
            bool open = LevelFlow.Unlocked(i + 2);
            Vector2 a = nodePos[i], b = nodePos[i + 1];
            const int dots = 6;
            for (int k = 1; k < dots; k++)
            {
                float t = k / (float)dots;
                var p = Vector2.Lerp(a, b, t) + new Vector2(0f, Mathf.Sin(t * Mathf.PI) * 0.35f);
                var d = Ui.Spr("Dash", strip, Ui.Disc, p, Order - 5, 1f);
                d.transform.localScale = new Vector3(0.42f, 0.08f, 1f);
                d.color = open ? Announcer.Cream : new Color(0.45f, 0.42f, 0.42f, 0.9f);
            }
        }
    }

    Transform BuildNode(LevelDef lv, Vector2 pos)
    {
        bool open = LevelFlow.Unlocked(lv.id);
        int stars = LevelFlow.Stars(lv.id);
        var n = Ui.Root("Node" + lv.id, strip, pos);
        float size = lv.IsBoss ? 1.35f : 1f;
        var col = LevelDB.WorldColor[lv.world];
        var ring = Ui.Spr("Ring", n, Ui.Disc, Vector2.zero, Order, size * 1.22f);
        ring.color = Ui.Ink;
        var shadow = Ui.Spr("Shadow", n, Ui.Disc, new Vector2(0.06f, -0.1f), Order, size);
        shadow.color = new Color(0f, 0f, 0f, 0.35f);
        var disc = Ui.Spr("Disc", n, Ui.Disc, Vector2.zero, Order + 1, size);
        disc.color = open ? (stars > 0 ? col : Color.Lerp(col, Color.white, 0.45f)) : Ui.Grey(col);

        if (lv.IsBoss)
        {
            var icon = lv.goal >= 0 ? LevelRuntime.BossIcons[lv.goal] : LevelRuntime.BossIcons[2];
            if (icon)
            {
                var sr = Ui.Spr("Boss", n, icon, new Vector2(0f, 0.05f), Order + 2, 0.95f / Mathf.Max(icon.bounds.size.x, icon.bounds.size.y));
                if (!open) sr.color = new Color(0.25f, 0.22f, 0.22f);
            }
            Ui.Txt(n, lv.goal >= 0 ? "BOSS" : "FINAL", new Vector2(0f, -0.95f), 0.8f, open ? Announcer.Red : Announcer.Cream, Order + 3);
            if (!open) Ui.Txt(n, "?", new Vector2(0.55f, 0.42f), 0.9f, Announcer.Cream, Order + 4);
        }
        else
        {
            Ui.Txt(n, open ? lv.id.ToString() : "?", new Vector2(0f, 0.02f), 1.5f, open ? Ui.Ink : Announcer.Cream, Order + 3);
            Ui.Txt(n, Short(lv.game), new Vector2(0f, -0.72f), 0.7f, open ? Color.white : new Color(0.75f, 0.72f, 0.72f), Order + 3);
        }

        // three stars on an arc above the node
        var star = GameKit.I ? GameKit.I.parryStar : Ui.Disc;
        for (int s = 0; s < 3; s++)
        {
            var p = new Vector2((s - 1) * 0.36f, size * 0.55f + 0.12f + (s == 1 ? 0.1f : 0f));
            var sr = Ui.Spr("Star", n, star, p, Order + 4, 0.42f);
            sr.color = s < stars ? Announcer.Yellow : new Color(0.15f, 0.12f, 0.12f, open ? 0.55f : 0.3f);
        }

        return n;
    }

    static string Short(LevelGame g)
    {
        switch (g)
        {
            case LevelGame.Flappy: return "FLAP";
            case LevelGame.Missile: return "DODGE";
            case LevelGame.Stack: return "STACK";
            case LevelGame.Landing: return "LAND";
            case LevelGame.Supply: return "DROP";
            case LevelGame.Bolt: return "BOLT";
            case LevelGame.Refuel: return "FUEL";
            default: return "BOSS";
        }
    }

    PixelText starsText, coinsText;

    void BuildTop()
    {
        top = Ui.Root("MapTop", transform);
        var band = Ui.Ink; band.a = 0.55f;
        Ui.Box("Bar", top, new Vector2(0f, 3.85f), new Vector2(16f, 0.75f), band, Order + 30);
        var star = GameKit.I ? GameKit.I.parryStar : Ui.Disc;
        Ui.Spr("StarIcon", top, star, new Vector2(-6.9f, 3.85f), Order + 31, 0.5f).color = Announcer.Yellow;
        starsText = Ui.Txt(top, LevelFlow.TotalStars() + "/" + LevelDB.MaxStars, new Vector2(-6.5f, 3.85f), 1.1f, Color.white, Order + 31, PixelText.Align.Left);
        Ui.Txt(top, CutsceneDB.Title, new Vector2(0f, 3.85f), 1.15f, Announcer.Yellow, Order + 31, PixelText.Align.Center, PixelText.Effect.Boil);
        coinsText = Ui.Txt(top, SaveSystem.Data.coins + " COINS", new Vector2(7.1f, 3.85f), 1.1f, Announcer.Yellow, Order + 31, PixelText.Align.Right);
        storyBtn = Ui.Btn(top, "STORY", new Vector2(-6.15f, 3.2f), new Vector2(2.15f, 0.48f), Announcer.Cream, Order + 31);
        menuBtn = Ui.Btn(top, "MENU", new Vector2(-6.15f, 2.55f), new Vector2(2.15f, 0.48f), Announcer.Cream, Order + 31);
        postBtn = Ui.Btn(top, "POSTCARDS", new Vector2(5.45f, 3.2f), new Vector2(3.4f, 0.48f), Announcer.Cream, Order + 31);
        Ui.Box("Foot", top, new Vector2(0f, -3.95f), new Vector2(16f, 0.5f), band, Order + 30);
        Ui.Txt(top, "DRAG OR ARROWS: MOVE   SPACE: PLAY   FREE PLAY: 1 CLASSIC  2 ENDLESS  3 BOSS RUSH",
            new Vector2(0f, -3.95f), 0.62f, Announcer.Cream, Order + 31);
    }

    // ---------------------------------------------------------------- input
    void Update()
    {
        if (CutscenePlayer.IsPlaying) return;
        if (panel) { PanelInput(); Animate(); return; }
        if (card) { CardInput(); Animate(); return; }

        if (UiInput.LeftDown()) Select(selected - 1);
        if (UiInput.RightDown()) Select(selected + 1);
        if (UiInput.Confirm()) OpenCard(selected);
        int m = CombatInput.ModeKey();
        if (m > 0) { LevelFlow.FreePlay((RunMode)(m - 1)); return; }

        float wheel = UiInput.Wheel();
        if (wheel != 0f) target = Mathf.Clamp(target - wheel * 2f, minScroll, maxScroll);

        if (UiInput.Down(out var p)) { pressing = true; dragging = false; pressAt = p; pressScroll = target; lastX = p.x; }
        if (pressing && UiInput.Held(out var h))
        {
            if (Mathf.Abs(h.x - pressAt.x) > 0.25f) dragging = true;
            if (dragging) target = Mathf.Clamp(pressScroll - (h.x - pressAt.x), minScroll, maxScroll);
            lastX = h.x;
        }
        if (pressing && UiInput.Up(out var u))
        {
            pressing = false;
            if (!dragging)
            {
                if (menuBtn.Hit(u)) { LevelFlow.Menu(); return; }
                else if (storyBtn.Hit(u)) OpenStories();
                else if (postBtn.Hit(u)) OpenPostcards();
                else Click(u);
            }
        }
        Animate();
    }

    void Animate()
    {
        scroll = Mathf.Lerp(scroll, target, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        Apply();
        float t = Time.unscaledTime;
        for (int i = 0; i < nodes.Count; i++)
        {
            bool sel = i == selected - 1;
            float s = sel ? 1.12f + 0.05f * Mathf.Sin(t * 6f) : 1f;
            nodes[i].localScale = Vector3.one * s;
        }
        if (plane)
        {
            if (planeT < 1f)
            {
                planeT = Mathf.Min(1f, planeT + Time.unscaledDeltaTime * 0.65f);
                float e = planeT * planeT * (3f - 2f * planeT);
                var p = Vector2.Lerp(planeFrom, planeTo, e);
                p.y += Mathf.Sin(e * Mathf.PI) * 0.4f;
                plane.localPosition = p + new Vector2(0.55f, 0.35f);
                if (planeT >= 1f) Arrive();
            }
            else plane.localPosition = planeTo + new Vector2(0.55f, 0.35f + 0.08f * Mathf.Sin(t * 3f));
        }
        if (drift != null)
        {
            float span = Segment * LevelDB.Worlds.Length;
            for (int i = 0; i < drift.Length; i++)
            {
                if (!drift[i]) continue;
                var p = drift[i].localPosition;
                p.x = Mathf.Repeat(t * 0.4f + i * span / drift.Length, span);
                drift[i].localPosition = p;
            }
        }
        for (int c = 0; c < chests.Count; c++)
            if (LevelFlow.ChestReady(c))
                chests[c].localScale = Vector3.one * (0.95f + 0.08f * Mathf.Sin(t * 5f + c));
    }

    void Arrive()
    {
        AudioManager.Sfx("super", 0.65f);
        CameraFx.Shake(0.07f, 0.16f);
        int i = LevelFlow.Frontier() - 1;
        if (i >= 0 && i < nodes.Count) StartCoroutine(Shake(nodes[i]));
    }

    void BuildChests()
    {
        for (int i = 0; i < 5; i++)
        {
            Vector2 p = i + 1 < LevelDB.Worlds.Length
                ? new Vector2((i + 1) * Segment + 0.35f, -2.05f)
                : nodePos[nodePos.Count - 1] + new Vector2(1.7f, -1.15f);
            chestPos.Add(p);
            bool open = LevelFlow.ChestOpen(i);
            var sr = Ui.Spr("Chest" + i, strip, PropArt.Chest, p, Order + 6, 0.85f);
            if (open) sr.color = new Color(0.65f, 0.65f, 0.65f, 0.8f);
            chests.Add(sr.transform);
            Ui.Txt(strip, ((i + 1) * LevelFlow.ChestEvery).ToString(), p + new Vector2(0f, -0.55f), 0.6f,
                open ? Announcer.Cream : Announcer.Yellow, Order + 7);
        }
    }

    void BuildDepth()
    {
        mapFar = Ui.Root("MapFar", transform);
        var farCloud = PropArt.Paint("parallax/props/puff.png", 0.7f);
        if (farCloud)
            for (int i = 0; i < 5; i++)
            {
                var c = Ui.Spr("FarCloud", mapFar, farCloud, new Vector2(-8f + i * 6f, 2.2f + (i % 2) * 0.4f), Order - 30);
                c.color = new Color(1f, 1f, 1f, 0.22f);
            }
        var join = PropArt.Paint("parallax/props/cloud.png", 3.4f);
        for (int w = 1; w < LevelDB.Worlds.Length; w++)
        {
            if (join)
            {
                // Hand-painted cloud cluster on the world edge. The old join was a fog rectangle.
                Ui.Spr("Join", strip, join, new Vector2(w * Segment, 0.55f), Order - 15);
                var b = Ui.Spr("JoinB", strip, join, new Vector2(w * Segment - 1.1f, 1.7f), Order - 14);
                b.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
                var c = Ui.Spr("JoinC", strip, join, new Vector2(w * Segment + 1.15f, -0.85f), Order - 14);
                c.transform.localScale = new Vector3(0.64f, 0.64f, 1f);
            }
            else
            {
                Ui.Box("Seam", strip, new Vector2(w * Segment, 0.2f), new Vector2(3.4f, 9.2f), new Color(0.97f, 0.94f, 0.9f, 0.38f), Order - 15);
                Ui.Box("SeamSoft", strip, new Vector2(w * Segment, 0.6f), new Vector2(5.2f, 8.4f), new Color(0.93f, 0.9f, 0.86f, 0.16f), Order - 16);
            }
        }
        drift = new Transform[4];
        float span = Segment * LevelDB.Worlds.Length;
        for (int i = 0; i < drift.Length; i++)
        {
            var c = Ui.Box("Shade", strip, new Vector2(i * span / drift.Length, -3.35f), new Vector2(2.4f, 0.28f), new Color(0.1f, 0.12f, 0.16f, 0.14f), Order - 8);
            drift[i] = c.transform;
        }
    }

    void Apply()
    {
        if (strip) strip.localPosition = new Vector3(-scroll, 0f, 0f);
        if (mapFar) mapFar.localPosition = new Vector3(-scroll * 0.38f, 0f, 0f);
    }

    void Select(int id)
    {
        if (id < 1 || id > LevelDB.Count || !LevelFlow.Unlocked(id)) return;
        selected = id;
        target = Mathf.Clamp(nodePos[id - 1].x, minScroll, maxScroll);
        AudioManager.Sfx("card", 0.4f);
    }

    void Click(Vector2 world)
    {
        var local = world + new Vector2(scroll, 0f);
        for (int c = 0; c < chestPos.Count; c++)
        {
            if (Vector2.Distance(local, chestPos[c]) > 0.55f) continue;
            if (LevelFlow.ClaimChest(c))
            {
                AudioManager.Sfx("super", 0.85f);
                CameraFx.Shake(0.08f, 0.18f);
                var sr = chests[c].GetComponent<SpriteRenderer>();
                if (sr) sr.color = new Color(0.65f, 0.65f, 0.65f, 0.8f);
                coinsText.Text = SaveSystem.Data.coins + " COINS";
                var pop = Ui.Txt(strip, "+25 COINS", chestPos[c] + new Vector2(0f, 0.7f), 0.75f, Announcer.Yellow, Order + 12);
                Destroy(pop.gameObject, 1.1f);
            }
            else AudioManager.Sfx("hurt", 0.35f);
            return;
        }
        for (int i = 0; i < nodePos.Count; i++)
        {
            float r = LevelDB.All[i].IsBoss ? 0.75f : 0.6f;
            if (Vector2.Distance(local, nodePos[i]) > r) continue;
            if (!LevelFlow.Unlocked(i + 1))
            {
                AudioManager.Sfx("hurt", 0.4f);
                StartCoroutine(Shake(nodes[i]));
                int need = LevelDB.GateBlock(i + 1);
                if (need > 0)
                {
                    var pop = Ui.Txt(strip, "NEED " + need + " STARS", nodePos[i] + new Vector2(0f, 0.7f), 0.7f, Announcer.Yellow, Order + 12);
                    Destroy(pop.gameObject, 1.1f);
                }
                return;
            }
            selected = i + 1;
            OpenCard(i + 1);
            return;
        }
    }

    System.Collections.IEnumerator Shake(Transform t)
    {
        var p = t.localPosition;
        for (float k = 0f; k < 0.3f; k += Time.unscaledDeltaTime)
        {
            t.localPosition = p + new Vector3(Mathf.Sin(k * 60f) * 0.08f * (1f - k / 0.3f), 0f, 0f);
            yield return null;
        }
        t.localPosition = p;
    }

    // ---------------------------------------------------------------- level card
    public void OpenCard(int id)
    {
        var lv = LevelDB.Get(id);
        if (lv == null || !LevelFlow.Unlocked(id)) return;
        cardLevel = lv;
        AudioManager.Sfx("card", 0.7f);
        card = Ui.Root("LevelCard", transform);
        var dim = Ui.Ink; dim.a = 0.55f;
        Ui.Box("Dim", card, Vector2.zero, new Vector2(20f, 12f), dim, Order + 40);
        Ui.Plaque(card, new Vector2(0f, -0.1f), new Vector2(8.4f, 5.8f), Order + 41);
        Ui.Txt(card, "LEVEL " + lv.id, new Vector2(0f, 2.15f), 2.2f, LevelDB.WorldColor[lv.world], Order + 42, PixelText.Align.Center, PixelText.Effect.Boil);
        Ui.Txt(card, lv.GameName, new Vector2(0f, 1.3f), 1.2f, Announcer.Teal, Order + 42);
        Ui.Txt(card, lv.GoalText, new Vector2(0f, 0.7f), 1.2f, Color.white, Order + 42);
        Ui.Txt(card, Hint(lv.game), new Vector2(0f, 0.2f), 0.7f, Announcer.Cream, Order + 42);

        int have = LevelFlow.Stars(id);
        var star = GameKit.I ? GameKit.I.parryStar : Ui.Disc;
        for (int s = 0; s < 3; s++)
        {
            float y = -0.35f - s * 0.45f;
            var icon = Ui.Spr("Star", card, star, new Vector2(-2.6f, y), Order + 42, 0.4f);
            icon.color = s < have ? Announcer.Yellow : new Color(0.3f, 0.25f, 0.22f);
            Ui.Txt(card, (s + 1) + " STAR" + (s > 0 ? "S" : "") + ": " + lv.StarText(s), new Vector2(-2.25f, y), 0.85f,
                s < have ? Announcer.Yellow : Color.white, Order + 42, PixelText.Align.Left);
        }
        playBtn = Ui.Btn(card, "PLAY", new Vector2(0f, -2.2f), new Vector2(3f, 0.95f), Announcer.Yellow, Order + 42);
        closeBtn = Ui.Btn(card, "X", new Vector2(3.75f, 2.4f), new Vector2(0.8f, 0.8f), Announcer.Red, Order + 42);
        cardAt = Time.unscaledTime + 0.15f;
    }

    static string Hint(LevelGame g)
    {
        switch (g)
        {
            case LevelGame.Flappy: return "TAP TO FLAP THROUGH THE GAPS";
            case LevelGame.Boss: return "WASD MOVE  SPACE SHOOT  SHIFT DASH";
            case LevelGame.Missile: return "A/D OR TAP SIDES TO TURN. CRASH MISSILES!";
            case LevelGame.Stack: return "PACK THE MAIL BEFORE THE BOAT SAILS";
            case LevelGame.Supply: return "A/D OR TAP TO CATCH THE CRATE";
            case LevelGame.Bolt: return "POE IS GROUNDED. JUMP THE GAPS.";
            case LevelGame.Refuel: return "STAY LEVEL WITH BUBBA TO TAKE FUEL";
            default: return "HOLD TO CLIMB. LET GO TO SINK. LAND SOFT";
        }
    }

    void CloseCard()
    {
        if (card) Destroy(card.gameObject);
        card = null; cardLevel = null;
    }

    void OpenStories()
    {
        ClosePanel();
        postcardsMode = false;
        storyShown.Clear();
        var ids = CutsceneDB.StoryIds;
        for (int i = 0; i < ids.Length; i++)
            if (CutscenePlayer.Seen(ids[i])) storyShown.Add(ids[i]);
        storyPage = 0;
        AudioManager.Sfx("card", 0.6f);
        BuildStoryPage();
    }

    void BuildStoryPage()
    {
        if (panel) Destroy(panel.gameObject);
        panelBtns.Clear();
        panelIds.Clear();
        prevBtn = nextBtn = null;
        panel = Ui.Root("Stories", transform);
        var dim = Ui.Ink; dim.a = 0.62f;
        Ui.Box("Dim", panel, Vector2.zero, new Vector2(20f, 12f), dim, Order + 40);
        Ui.Plaque(panel, new Vector2(0f, -0.2f), new Vector2(11.4f, 6.5f), Order + 41);
        Ui.Txt(panel, "STORY", new Vector2(0f, 2.5f), 1.5f, Announcer.Yellow, Order + 42, PixelText.Align.Center, PixelText.Effect.Boil);
        panelClose = Ui.Btn(panel, "X", new Vector2(5.05f, 2.55f), new Vector2(0.7f, 0.7f), Announcer.Red, Order + 42);
        if (storyShown.Count == 0)
            Ui.Txt(panel, "NO STORIES YET", new Vector2(0f, 0.2f), 1.1f, Announcer.Cream, Order + 42);
        else
        {
            const int per = 8;
            int start = storyPage * per;
            for (int i = 0; i < per && start + i < storyShown.Count; i++)
            {
                var pos = new Vector2(i % 2 == 0 ? -2.55f : 2.55f, 1.4f - (i / 2) * 0.82f);
                string id = storyShown[start + i];
                panelBtns.Add(Ui.Btn(panel, CutsceneDB.Label(id), pos, new Vector2(4.9f, 0.68f), Announcer.Cream, Order + 42));
                panelIds.Add(id);
            }
            int pages = (storyShown.Count + per - 1) / per;
            if (pages > 1)
            {
                prevBtn = Ui.Btn(panel, "PREV", new Vector2(-2.2f, -2.55f), new Vector2(2.2f, 0.62f), Announcer.Teal, Order + 42);
                nextBtn = Ui.Btn(panel, "NEXT", new Vector2(2.2f, -2.55f), new Vector2(2.2f, 0.62f), Announcer.Teal, Order + 42);
            }
        }
        cardAt = Time.unscaledTime + 0.15f;
    }

    void OpenPostcards()
    {
        ClosePanel();
        postcardsMode = true;
        AudioManager.Sfx("card", 0.6f);
        panel = Ui.Root("Postcards", transform);
        var dim = Ui.Ink; dim.a = 0.62f;
        Ui.Box("Dim", panel, Vector2.zero, new Vector2(20f, 12f), dim, Order + 40);
        Ui.Txt(panel, "POSTCARDS", new Vector2(0f, 3.34f), 1.05f, Announcer.Yellow, Order + 42, PixelText.Align.Center, PixelText.Effect.Boil);
        panelClose = Ui.Btn(panel, "X", new Vector2(6.85f, 2.4f), new Vector2(0.62f, 0.62f), Announcer.Red, Order + 42);
        var have = SaveSystem.Data.postcards;
        for (int i = 0; i < 25; i++)
        {
            var pos = new Vector2((i % 5 - 2) * 2.45f, 2.05f - (i / 5) * 1.12f);
            string pid = CutsceneDB.PostcardId(i + 1);
            bool got = have != null && have.Contains(pid);
            if (got)
            {
                var pic = CutscenePlayer.LoadArt(pid);
                if (pic) Ui.Spr("Pc", panel, pic, pos, Order + 42, CutscenePlayer.Fit(pic, 1.45f));
            }
            else
            {
                Ui.Box("Miss", panel, pos, new Vector2(1.55f, 0.95f), Ui.Ink, Order + 42);
                Ui.Txt(panel, (i + 1).ToString(), pos, 0.65f, Announcer.Cream, Order + 43);
            }
        }
        cardAt = Time.unscaledTime + 0.15f;
    }

    void ClosePanel()
    {
        if (panel) Destroy(panel.gameObject);
        panel = null;
        panelClose = null;
        prevBtn = nextBtn = null;
        panelBtns.Clear();
        panelIds.Clear();
    }

    void PanelInput()
    {
        if (Time.unscaledTime < cardAt) return;
        if (UiInput.Back()) { ClosePanel(); return; }
        if (!UiInput.Up(out var p)) return;
        if (panelClose != null && panelClose.Hit(p)) { ClosePanel(); return; }
        if (!postcardsMode)
        {
            if (prevBtn != null && prevBtn.Hit(p) && storyPage > 0) { storyPage--; BuildStoryPage(); return; }
            if (nextBtn != null && nextBtn.Hit(p) && (storyPage + 1) * 8 < storyShown.Count) { storyPage++; BuildStoryPage(); return; }
            for (int i = 0; i < panelBtns.Count; i++)
            {
                if (!panelBtns[i].Hit(p)) continue;
                string id = panelIds[i];
                ClosePanel();
                CutscenePlayer.Play(id, () => FilmFilter.IrisOpen(0.45f));
                return;
            }
        }
        if (Mathf.Abs(p.x) > 5.8f || Mathf.Abs(p.y) > 3.4f) ClosePanel();
    }

    void CardInput()
    {
        if (Time.unscaledTime < cardAt) return;
        if (UiInput.Back()) { CloseCard(); return; }
        if (UiInput.Confirm()) { LevelFlow.Play(cardLevel.id); return; }
        if (UiInput.Up(out var p))
        {
            if (playBtn.Hit(p)) { AudioManager.Sfx("ready", 0.7f); LevelFlow.Play(cardLevel.id); }
            else if (closeBtn.Hit(p) || Mathf.Abs(p.x) > 4.2f || Mathf.Abs(p.y + 0.1f) > 2.9f) CloseCard();
        }
    }
}
