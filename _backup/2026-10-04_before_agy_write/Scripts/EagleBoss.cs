using System.Collections;
using UnityEngine;
using Kind = BulletPool.Kind;

/// <summary>
/// Tempest Eagle (storm biome).
/// P1: aimed feather fans (every 3rd volley has a pink feather) and a telegraphed horizontal dive.
/// P2 "Storm Call": two storm-cloud minions strike lightning; aimed triples and a teal ring.
/// P3 "Fury": zigzag swoops at the player, feather rain from above, a two-arm feather spiral.
/// </summary>
public class EagleBoss : BossBase
{
    public Sprite[] flap, storm, fury, cloudFrames;
    public Sprite dive, ko, bolt;

    static readonly Vector2 Beak = new Vector2(-1.6f, 0.2f);
    bool drift = true;
    float driftT;
    readonly Minion[] clouds = new Minion[2];
    readonly float[] cloudWait = new float[2];

    protected override void Awake()
    {
        base.Awake();
        displayName = "TEMPEST EAGLE";
    }

    Sprite[] Wings => phase == 0 ? flap : phase == 1 ? storm : fury;

    protected override void Setup()
    {
        Animate(flap, 8f);
        sr.flipX = false;
        drift = true; driftT = 0f;
        for (int i = 0; i < clouds.Length; i++) { clouds[i] = null; cloudWait[i] = 0f; }
    }

    protected override void Tick(float dt)
    {
        if (drift)
        {
            driftT += dt;
            var target = new Vector2(home.x + Mathf.Sin(driftT * 0.7f) * 0.6f, home.y + Mathf.Sin(driftT * 1.1f) * 2.2f);
            pos = Vector2.Lerp(pos, target, 1f - Mathf.Exp(-3f * dt));
        }
        if (phase == 1 && health.Vulnerable)
        {
            for (int i = 0; i < clouds.Length; i++)
            {
                if (Alive(clouds[i])) continue;
                cloudWait[i] -= dt;
                if (cloudWait[i] <= 0f) { clouds[i] = SpawnCloud(i); cloudWait[i] = 9f; }
            }
        }
    }

    protected override void OnInterrupt()
    {
        Animate(Wings, phase == 2 ? 12f : 8f);
        sr.flipX = false;
        drift = true;
    }

    protected override void OnKnockout() { Show(ko); sr.flipX = false; }

    protected override IEnumerator OnPhaseChange(int p)
    {
        Animate(p == 1 ? storm : fury, p == 1 ? 8f : 12f);
        drift = false;
        yield return Ease(home, 0.7f);
        drift = true;
        if (p == 1) for (int i = 0; i < clouds.Length; i++) cloudWait[i] = 0.2f + i * 0.6f;
    }

    protected override IEnumerator Pattern(int p)
    {
        if (p == 0) return P1();
        if (p == 1) return P2();
        return P3();
    }

    // ---------------------------------------------------------------- phase 1
    IEnumerator P1()
    {
        drift = true;
        for (int v = 0; v < 3; v++)
        {
            yield return 0.3f;
            var m = Muzzle(Beak);
            Pool.Fan(Kind.Feather, m, 5, 36f, 5.5f, AngleToPlayer(m), v == 2 ? 2 : -1);
            ShootSfx();
            yield return 0.75f;
        }
        yield return 0.3f;
        yield return Dive();
        yield return 0.6f;
    }

    IEnumerator Dive()
    {
        drift = false;
        float y = Mathf.Clamp(PlayerPos.y, -3.4f, 3.4f);
        yield return Ease(new Vector2(home.x + 0.8f, y), 0.45f);
        AudioManager.Sfx("swoop", 0.7f, 0.05f);
        shake = 0.7f;
        yield return temps.WarnLane(new Vector2(0f, y), 15f, 0f, 0.8f, 1.5f);
        Show(dive);
        AudioManager.Sfx("dash", 0.9f, 0f, 0.03f, 0.7f);
        float ghost = 0f;
        while (pos.x > -11f)
        {
            pos.x -= 16f * Time.deltaTime;
            ghost -= Time.deltaTime;
            if (ghost <= 0f) { ghost = 0.04f; Afterimage(new Color(0.49f, 0.82f, 0.78f, 0.6f)); }
            yield return null;
        }
        Animate(Wings, 8f);
        pos = new Vector2(11f, home.y + 2.5f);
        yield return Ease(home, 1.0f);
        driftT = 0f;
        drift = true;
    }

    // ---------------------------------------------------------------- phase 2
    Minion SpawnCloud(int slot)
    {
        float x0 = slot == 0 ? -3.2f : 0.8f;
        var m = SpawnMinion("StormCloud", cloudFrames, 4f, new Vector2(x0, 6f), 20f, new Vector2(1.3f, 0.8f), 9);
        m.Run(CloudLife(m, x0));
        return m;
    }

    IEnumerator CloudLife(Minion m, float x0)
    {
        yield return m.Ease(new Vector2(x0, 3.2f), 0.9f);
        float t = 0f, next = Random.Range(1.0f, 1.8f);
        while (true)
        {
            float dt = Time.deltaTime;
            t += dt;
            m.pos.x = x0 + Mathf.Sin(t * 0.6f) * 1.6f;
            next -= dt;
            if (next <= 0f)
            {
                yield return Strike(m);
                next = Random.Range(2.6f, 3.6f);
            }
            yield return null;
        }
    }

    IEnumerator Strike(Minion m)
    {
        float x = m.pos.x;
        const float top = 2.9f, bottom = -4.4f;
        yield return m.temps.Warn(new Vector2(x, (top + bottom) / 2f), top - bottom, 90f, 0.8f, 0.45f);
        var r = m.temps.Make("Bolt", bolt, new Vector2(x, (top + bottom) / 2f), 12);
        r.transform.localScale = new Vector3(1f, (top - bottom) / 3f, 1f);
        var c = r.gameObject.AddComponent<BoxCollider2D>();
        c.isTrigger = true; c.size = new Vector2(0.45f, 3f);
        r.gameObject.AddComponent<Hazard>();
        AudioManager.Sfx("thunder", 0.8f, 0.1f, 0.2f);
        FilmFilter.Flash(0.25f, 0.08f);
        CameraFx.Shake(0.1f, 0.2f);
        for (float t = 0f; t < 0.35f; t += Time.deltaTime)
        {
            r.flipX = Mathf.Repeat(t * 20f, 1f) < 0.5f;
            yield return null;
        }
        m.temps.Kill(r);
    }

    IEnumerator P2()
    {
        drift = true;
        for (int v = 0; v < 3; v++)
        {
            yield return 0.35f;
            var m = Muzzle(Beak);
            Pool.Fan(Kind.Aimed, m, 3, 32f, 6.5f, AngleToPlayer(m));
            ShootSfx();
            yield return 0.6f;
        }
        yield return 0.3f;
        yield return temps.Ring(transform.position, 0.45f, Announcer.Teal);
        Pool.Ring(Kind.Teal, transform.position, 14, 4f, Random.value * 360f, 0, 180f, 4);
        ShootSfx();
        yield return 1.3f;
    }

    // ---------------------------------------------------------------- phase 3
    IEnumerator P3()
    {
        yield return Zigzag();
        yield return 0.5f;
        yield return FeatherRain(2.4f);
        yield return 0.4f;
        yield return Spiral(2.4f);
        yield return 0.6f;
    }

    IEnumerator Zigzag()
    {
        drift = false;
        for (int i = 0; i < 3; i++)
        {
            var target = new Vector2(Mathf.Clamp(PlayerPos.x, -6.5f, 3f), Mathf.Clamp(PlayerPos.y, -3.6f, 3.6f));
            AudioManager.Sfx("swoop", 0.6f, 0.1f);
            yield return temps.Ring(target, 0.45f, Announcer.Red);
            var dir = (target - pos).normalized;
            var end = target + dir * 2.5f;
            end.x = Mathf.Clamp(end.x, -6.8f, 6.5f);
            end.y = Mathf.Clamp(end.y, -3.6f, 3.6f);
            Show(dive);
            float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (dir.x > 0f) { sr.flipX = true; rot = Mathf.Clamp(Mathf.DeltaAngle(0f, ang), -60f, 60f); }
            else { sr.flipX = false; rot = Mathf.Clamp(Mathf.DeltaAngle(180f, ang), -60f, 60f); }
            float ghost = 0f;
            while ((pos - end).sqrMagnitude > 0.01f)
            {
                pos = Vector2.MoveTowards(pos, end, 15f * Time.deltaTime);
                ghost -= Time.deltaTime;
                if (ghost <= 0f) { ghost = 0.035f; Afterimage(new Color(0.85f, 0.27f, 0.23f, 0.55f)); }
                yield return null;
            }
            yield return 0.15f;
        }
        rot = 0f; sr.flipX = false;
        Animate(fury, 12f);
        yield return Ease(home, 0.8f);
        driftT = 0f;
        drift = true;
    }

    IEnumerator FeatherRain(float dur)
    {
        drift = false;
        yield return Ease(new Vector2(home.x, 2.4f), 0.5f);
        int n = 0;
        float next = 0f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            next -= Time.deltaTime;
            if (next <= 0f)
            {
                next = 0.09f;
                n++;
                var k = n % 6 == 0 ? Kind.FeatherPink : Kind.Feather;
                Pool.Fire(k, new Vector2(Random.Range(-7.4f, 6f), 4.6f), new Vector2(-1.2f, -5.5f));
            }
            yield return null;
        }
        drift = true;
    }

    IEnumerator Spiral(float dur)
    {
        float a = Random.value * 360f, next = 0f;
        int n = 0;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            next -= Time.deltaTime;
            if (next <= 0f)
            {
                next = 0.07f;
                a += 13f;
                var from = World(Beak * 0.5f);
                for (int arm = 0; arm < 2; arm++)
                {
                    float r = (a + arm * 180f) * Mathf.Deg2Rad;
                    n++;
                    Pool.Fire(n % 8 == 0 ? Kind.FeatherPink : Kind.Feather, from, new Vector2(Mathf.Cos(r), Mathf.Sin(r)) * 4.2f);
                }
                if (n % 4 == 0) ShootSfx();
            }
            yield return null;
        }
    }
}
