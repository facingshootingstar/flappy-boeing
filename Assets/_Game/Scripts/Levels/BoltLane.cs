using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Side-scroll mail dash. The storm keeps Poe down, so the pilot runs the sack.
/// Score is letters. Crossing <see cref="LevelDef.goal"/> gaps clears the run.
/// </summary>
public class BoltLane : MiniGame
{
    const float Ground = -1.65f;
    const float Gravity = 32f;
    const float JumpV = 13f;

    class Plat { public float x, w, y; public int island; }
    class Pick { public float x, y; public bool got; public Transform t; }

    Transform world, body;
    Sprite runSpr, jumpSpr;
    readonly List<Plat> plats = new List<Plat>();
    readonly List<Pick> letters = new List<Pick>();
    readonly List<Pick> zaps = new List<Pick>();
    float px, py, vy, cam, coyote, courseEnd, gapW;
    int crossed;
    bool onGround, faceRight = true;

    protected override string HintText => "A/D: RUN   SPACE: JUMP";

    protected override void Build()
    {
        // ponytail: the painted sky stays glued to the screen. A second wide plate would scroll it.
        world = Ui.Root("World", root);
        runSpr = PropArt.Imagen("pilot_run", 1.55f);
        jumpSpr = PropArt.Imagen("pilot_jump", 1.55f) ?? runSpr;
        var plank = PropArt.Imagen("dock_plank", 0.7f);
        var mail = PropArt.Imagen("mail_letter", 0.46f);
        var box = PropArt.Imagen("mail_box", 1.25f);
        var bolt = PropArt.Imagen("storm_bolt", 1.05f) ?? PropArt.Bolt;
        if (!runSpr) runSpr = Ui.Disc;
        if (!jumpSpr) jumpSpr = runSpr;
        if (!plank) plank = Ui.Pixel;
        if (!mail) mail = Ui.Disc;
        if (!box) box = Ui.Disc;

        gapW = Mathf.Lerp(1.85f, 2.7f, level.hard);
        int islands = level.goal + 1;
        int left = level.stars[2];
        float x = 0f;
        for (int i = 0; i < islands; i++)
        {
            bool hazard = i > 0 && i < level.goal && (i % 2 == 0);
            float w = i == 0 ? 9f : hazard ? 11.5f : 8.2f;
            AddPlat(plank, x, w, Ground, i);
            int slots = islands - 1 - i;
            int here = 0;
            if (left > 0 && slots > 0)
                here = slots == 1 ? left : Mathf.Max(1, left / slots);
            here = Mathf.Min(here, left);
            float zapX = x + 4.6f;
            for (int k = 0; k < here; k++)
            {
                float lx = x + w * (0.55f + 0.2f * k);
                if (hazard && Mathf.Abs(lx - zapX) < 1.3f) lx = zapX + 2.4f;
                var sr = Spr(mail, "Letter", lx, Ground + 0.48f, Order + 5);
                letters.Add(new Pick { x = lx, y = Ground + 0.48f, t = sr.transform });
                left--;
            }
            if (hazard)
            {
                var zs = Spr(bolt, "Zap", zapX, Ground + 0.58f, Order + 6);
                zaps.Add(new Pick { x = zapX, y = Ground + 0.58f, t = zs.transform });
            }
            x += w;
            if (i < islands - 1)
            {
                var pitSpr = PropArt.Paint("parallax/props/pit.png", 3.3f);
                if (pitSpr) PropArt.TileStrip(world, pitSpr, x, gapW + 0.15f, Ground + 0.2f, Order + 1, "Pit");
                else Ui.Box("Pit", world, new Vector2(x + gapW * 0.5f, Ground - 1.7f), new Vector2(gapW + 0.15f, 3.5f), new Color(0.05f, 0.04f, 0.07f, 0.94f), Order + 1);
                Ui.Box("Mist", world, new Vector2(x + gapW * 0.5f, Ground - 2.9f), new Vector2(gapW, 0.5f), new Color(0.85f, 0.82f, 0.75f, 0.35f), Order + 2);
                x += gapW;
            }
        }
        courseEnd = x - 0.3f;
        float flag = x - 1.35f;
        Spr(box, "Goal", flag, Ground + BoxLift(box), Order + 5);
        if (left != 0) Debug.LogError("[BoltLane] letters short by " + left);
        if (gapW + 0.95f > 6.1f * (2f * JumpV / Gravity)) Debug.LogError("[BoltLane] gap wider than a jump");

        body = Ui.Spr("Pilot", root, runSpr, Vector2.zero, Order + 8, runSpr == Ui.Disc ? 0.7f : 1f).transform;
        px = 1.7f;
        py = Ground;
        onGround = true;
        coyote = 0.12f;
        Place();
        Hud("GAPS 0/" + level.goal, "MAIL 0");
    }

    static float BoxLift(Sprite s) => s == Ui.Disc ? 0.55f : s.bounds.extents.y;

    SpriteRenderer Spr(Sprite s, string name, float x, float y, int order)
    {
        float sc = s == Ui.Disc || s == PropArt.Bolt ? 0.45f : 1f;
        return Ui.Spr(name, world, s, new Vector2(x, y), order, sc);
    }

    void AddPlat(Sprite plank, float x, float w, float top, int island)
    {
        plats.Add(new Plat { x = x, w = w, y = top, island = island });
        if (island >= 0)
            Ui.Box("Fill", world, new Vector2(x + w * 0.5f, top - 1.45f), new Vector2(w, 2.9f), new Color(0.42f, 0.28f, 0.16f, 1f), Order + 2);
        bool raw = plank == Ui.Pixel;
        float tile = raw ? 1.4f : Mathf.Max(0.4f, plank.bounds.size.x);
        int n = Mathf.Max(1, Mathf.RoundToInt(w / tile));
        float step = w / n;
        float half = raw ? 0.28f : plank.bounds.extents.y;
        for (int i = 0; i < n; i++)
        {
            var sr = Ui.Spr("Plank", world, plank, new Vector2(x + step * (i + 0.5f), top - half * 0.45f), Order + 3, 1f);
            sr.transform.localScale = raw
                ? new Vector3(step, 0.56f, 1f)
                : new Vector3(step / tile, 1f, 1f);
            sr.color = Color.white;
        }
        var grass = PropArt.Paint("parallax/props/grass.png", 0.5f);
        if (grass) PropArt.TileStrip(world, grass, x, w, top + 0.08f, Order + 4, "Grass");
    }

    protected override void Tick(float dt)
    {
        float dir = Bot ? 1f : Steer();
        float spd = Mathf.Lerp(6.1f, 7.3f, level.hard);
        px = Mathf.Clamp(px + dir * spd * dt, 0.45f, courseEnd);
        if (dir < -0.1f) faceRight = false;
        else if (dir > 0.1f) faceRight = true;

        bool hop = Bot ? BotHop() : (coyote > 0f && (UiInput.Confirm() || UiInput.UpDown() || UiInput.Down(out _)));
        if (hop && coyote > 0f)
        {
            vy = JumpV;
            coyote = 0f;
            onGround = false;
            AudioManager.Sfx("flap", 0.45f, 0.06f, 0.08f);
        }
        vy -= Gravity * dt;
        float next = py + vy * dt;
        if (Feet(next, out var stood))
        {
            onGround = true;
            py = stood.y;
            vy = 0f;
            coyote = 0.12f;
            if (stood.island > crossed) crossed = stood.island;
        }
        else
        {
            onGround = false;
            py = next;
            coyote = Mathf.Max(0f, coyote - dt);
        }

        if (py < Ground - 2.6f)
        {
            Boom(new Vector2(px - cam, py), 1.1f);
            body.gameObject.SetActive(false);
            End(false);
            return;
        }
        foreach (var z in zaps)
        {
            if (Mathf.Abs(px - z.x) < 0.46f && py < z.y + 0.35f && py + 1.25f > z.y - 0.7f)
            {
                Boom(new Vector2(px - cam, py + 0.5f), 1.05f);
                body.gameObject.SetActive(false);
                End(false);
                return;
            }
        }
        foreach (var m in letters)
        {
            if (m.got) continue;
            if (Mathf.Abs(px - m.x) > 0.62f || Mathf.Abs(py + 0.75f - m.y) > 1.15f) continue;
            m.got = true;
            score++;
            m.t.gameObject.SetActive(false);
            PopText("MAIL!", new Vector2(m.x - cam, m.y + 0.4f), score);
            Sparkle(new Vector2(m.x - cam, m.y), Announcer.Yellow, 0.7f);
            if (score >= 3) GrantQuota();
        }

        cam = Mathf.Lerp(cam, Mathf.Max(0f, px - 1.5f), 1f - Mathf.Exp(-8f * dt));
        world.localPosition = new Vector3(-cam, 0f, 0f);
        Place();
        Hud("GAPS " + Mathf.Min(crossed, level.goal) + "/" + level.goal, "MAIL " + score);
        if (crossed >= level.goal) End(true);
    }

    bool Feet(float next, out Plat stood)
    {
        stood = null;
        float best = -999f;
        foreach (var p in plats)
        {
            if (px < p.x + 0.12f || px > p.x + p.w - 0.12f) continue;
            if (py < p.y - 0.05f || next > p.y + 0.12f || vy > 0.35f) continue;
            if (p.y > best) { best = p.y; stood = p; }
        }
        return stood != null;
    }

    bool BotHop()
    {
        if (coyote <= 0f) return false;
        foreach (var z in zaps)
            if (z.x - px < 1.35f && z.x - px > 0.9f) return true;
        foreach (var p in plats)
        {
            if (p.island < 0 || px < p.x || px > p.x + p.w) continue;
            float end = p.x + p.w;
            return end - px < 0.9f && end - px > 0.4f;
        }
        return false;
    }

    void Place()
    {
        var sr = body.GetComponent<SpriteRenderer>();
        sr.sprite = onGround || coyote > 0.02f ? runSpr : jumpSpr;
        body.localPosition = new Vector3(px - cam, py + (sr.sprite == Ui.Disc ? 0.45f : 0.5f), 0f);
        body.localScale = new Vector3(faceRight ? 1f : -1f, 1f, 1f);
    }
}
