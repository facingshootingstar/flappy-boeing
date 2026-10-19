using UnityEngine;

/// <summary>
/// Glide the plane down onto the runway: gravity pulls, holding gives lift. Touch down softly and close to the
/// target mark for up to 100 points. Off the runway or too hard = crash. Overshooting the runway scores 0.
/// Make <see cref="LevelDef.goal"/> landings to clear.
/// </summary>
public class PerfectLanding : MiniGame
{
    const float Ground = -3.2f, Gravity = -2.2f, Lift = 4f, MaxSink = 1.4f, PlaneHalf = 0.22f;

    Transform plane, runway, target;
    float vx, vy, rwA, rwB, targetX, pause;
    int landings;
    bool flying;

    protected override string HintText => "HOLD SPACE OR TAP TO PULL UP - LAND SOFT!";

    protected override void Build()
    {
        var fly = LevelRuntime.Fly;
        var sr = Ui.Spr("Plane", root, fly != null && fly.Length > 0 ? fly[0] : Ui.Disc, Vector2.zero, Order + 8, 0.7f);
        if (fly != null && fly.Length > 1) sr.gameObject.AddComponent<SpriteAnimator>().frames = fly;
        plane = sr.transform;

        Ui.Box("Grass", root, new Vector2(0f, Ground - 0.55f), new Vector2(16f, 1.1f), new Color32(96, 150, 78, 255), Order);
        Ui.Box("GrassEdge", root, new Vector2(0f, Ground - 0.02f), new Vector2(16f, 0.06f), Ui.Ink, Order + 1);
        runway = Ui.Root("Runway", root);
        target = Ui.Root("Target", root);
        Ui.Box("Pole", target, new Vector2(0f, 0.35f), new Vector2(0.06f, 0.7f), Ui.Ink, Order + 3);
        Ui.Box("Flag", target, new Vector2(0.18f, 0.58f), new Vector2(0.32f, 0.22f), Announcer.Red, Order + 3);
        Ui.Box("Mark", target, new Vector2(0f, -0.02f), new Vector2(0.5f, 0.1f), Announcer.Yellow, Order + 4);
        NextApproach();
        Hud("LANDING 1/" + level.goal, "POINTS 0");
    }

    void NextApproach()
    {
        // runway shrinks with difficulty, target wanders
        foreach (Transform c in runway) Destroy(c.gameObject);
        float len = Mathf.Lerp(5.5f, 3.4f, level.hard);
        rwB = Random.Range(5.2f, 6.6f);
        rwA = rwB - len;
        Ui.Box("Strip", runway, new Vector2((rwA + rwB) / 2f, Ground - 0.06f), new Vector2(len, 0.16f), new Color32(78, 76, 84, 255), Order + 2);
        for (float x = rwA + 0.3f; x < rwB - 0.2f; x += 0.6f)
            Ui.Box("Dash", runway, new Vector2(x, Ground - 0.05f), new Vector2(0.3f, 0.04f), Color.white, Order + 3);
        targetX = Random.Range(rwA + len * 0.3f, rwA + len * 0.6f);
        target.localPosition = new Vector3(targetX, Ground, 0f);

        plane.gameObject.SetActive(true);
        plane.localPosition = new Vector3(-6.5f, Random.Range(2.2f, 3.1f), 0f);
        plane.localRotation = Quaternion.identity;
        vx = 2.4f + level.hard * 0.5f;
        vy = 0f;
        flying = true;
    }

    protected override void Tick(float dt)
    {
        if (!flying)
        {
            pause -= dt;
            if (pause <= 0f)
            {
                if (landings >= level.goal) { End(true); return; }
                NextApproach();
            }
            return;
        }

        bool lift = Bot ? BotLift() : (Hold() || UiInput.Held(out _));
        float wind = Mathf.Sin(elapsed * 1.9f) * Mathf.Sin(elapsed * 0.7f + 1f) * level.hard * 1.6f;
        vy += (Gravity + (lift ? Lift : 0f) + wind) * dt;
        vy = Mathf.Clamp(vy, -4f, 2.2f);
        Vector2 p = (Vector2)plane.localPosition + new Vector2(vx, vy) * dt;
        p.y = Mathf.Min(p.y, H - 0.3f);
        plane.localPosition = p;
        plane.localRotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(vy * 9f, -25f, 18f));
        if (lift && GameKit.I && GameKit.I.smoke != null && GameKit.I.smoke.Length > 0 && Random.value < 0.3f)
            GameKit.Pop(GameKit.I.smoke[0], p + new Vector2(-0.4f, -0.05f), 0.3f, 0.2f, 0.6f, Order + 7, new Color(1f, 1f, 1f, 0.5f));

        if (p.x > W + 0.3f)
        {
            Popup("GO AROUND! +0", p.x - 1.5f, Announcer.Cream);
            AudioManager.Sfx("swoop", 0.5f, 0f);
            Landed(0);
            return;
        }
        if (p.y - PlaneHalf > Ground) { Hud("LANDING " + (landings + 1) + "/" + level.goal, "POINTS " + score); return; }

        // touchdown
        bool onRunway = p.x >= rwA && p.x <= rwB;
        if (!onRunway || vy < -MaxSink)
        {
            Boom(new Vector2(p.x, Ground + 0.2f), 1.3f);
            plane.gameObject.SetActive(false);
            AudioManager.Sfx("death", 0.8f, 0f);
            Popup(onRunway ? "TOO HARD!" : "MISSED THE RUNWAY!", Mathf.Clamp(p.x, -5f, 5f), Announcer.Red);
            End(false);
            return;
        }
        float soft = 1f - Mathf.Abs(vy) / MaxSink;
        float aim = 1f - Mathf.Min(1f, Mathf.Abs(p.x - targetX) / 2.5f);
        int pts = Mathf.RoundToInt(50f * soft + 50f * aim);
        plane.localPosition = new Vector3(p.x, Ground + PlaneHalf, 0f);
        plane.localRotation = Quaternion.identity;
        if (GameKit.I) GameKit.Burst(GameKit.I.smoke, new Vector2(p.x - 0.2f, Ground + 0.1f), 16f, 0.5f, Order + 9);
        AudioManager.Sfx(pts >= 85 ? "parry" : "score", 0.8f, 0f);
        CameraFx.Shake(0.06f, 0.12f);
        if (pts >= 85) Sparkle(new Vector2(p.x, Ground + 0.5f), Announcer.Yellow, 1.4f);
        Popup((pts >= 85 ? "PERFECT! +" : pts >= 60 ? "NICE! +" : "OK +") + pts, Mathf.Clamp(p.x, -5f, 5f), Announcer.Yellow);
        Landed(pts);
    }

    void Landed(int pts)
    {
        score += pts;
        landings++;
        flying = false;
        pause = 1.3f;
        Hud("LANDING " + Mathf.Min(landings + 1, level.goal) + "/" + level.goal, "POINTS " + score);
    }

    void Popup(string s, float x, Color c)
    {
        var t = Ui.Txt(root, s, new Vector2(x, 0.4f), 1.4f, c, Order + 55, PixelText.Align.Center, PixelText.Effect.Boil);
        Destroy(t.gameObject, 1.2f);
    }

    /// <summary>Debug bot: aim the glide path at the target, flare near the ground.</summary>
    bool BotLift()
    {
        Vector2 p = plane.localPosition;
        float t = Mathf.Max(0.3f, (targetX - p.x) / vx);
        float want = (Ground + PlaneHalf - p.y) / t;
        float h = p.y - Ground;
        if (h < 1.0f) want = Mathf.Max(want, -0.5f);
        if (targetX - p.x < 0f) want = -0.4f;
        return vy < want;
    }
}
