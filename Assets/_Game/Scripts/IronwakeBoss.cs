using System.Collections;
using UnityEngine;
using Kind = BulletPool.Kind;

/// <summary>
/// Ironwake, the battleship (sea biome). Rolls on the swell at the bottom of the screen.
/// P1: lobbed shells (gravity arcs, one pink) and aimed triples from the bridge.
/// P2 (damaged, smoking): telegraphed torpedo runs at the player's height, drifting mines (shootable / pink), flak that bursts into rings.
/// P3 (the bow opens into a jaw): crossing double sprinkler and a ram across the lower lane.
/// </summary>
public class IronwakeBoss : BossBase
{
    public Sprite idle, fire, damaged, maw, mawOpen;

    static readonly Vector2 Cannon = new Vector2(-2.0f, 0.7f);
    static readonly Vector2 Bridge = new Vector2(0.4f, 0.9f);
    static readonly Vector2 Bow = new Vector2(-3.0f, -0.3f);
    float roll, smokeT;

    protected override void Awake()
    {
        base.Awake();
        displayName = "IRONWAKE";
        bobAmp = 0.06f; bobFreq = 0.8f;
    }

    Sprite Rest => phase == 0 ? idle : phase == 1 ? damaged : maw;

    protected override void Setup()
    {
        Show(idle);
        roll = 0f;
    }

    protected override void Tick(float dt)
    {
        roll += dt;
        rot = Mathf.Sin(roll * 1.3f) * 1.5f;
        if (phase >= 1)
        {
            smokeT -= dt;
            if (smokeT <= 0f)
            {
                smokeT = 0.35f;
                var b = sr.bounds;
                Puff(new Vector2(Random.Range(b.min.x + 1f, b.max.x - 0.5f), b.max.y - 0.4f), Random.Range(0.8f, 1.3f));
            }
        }
    }

    protected override void OnInterrupt() { Show(Rest); }
    protected override void OnKnockout() { Show(damaged); }

    protected override IEnumerator Intro()
    {
        // steams in from the right along the waterline
        AudioManager.Sfx("roar", 0.7f, 0f, 0.03f, 0.7f);
        yield return Ease(home, 1.8f);
        shake = 0.4f;
        yield return 0.4f;
    }

    protected override IEnumerator OnPhaseChange(int p)
    {
        Show(p == 1 ? damaged : maw);
        var b = sr.bounds;
        for (int i = 0; i < 4; i++)
            if (GameKit.I) GameKit.Burst(GameKit.I.explosion, new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y)), 18f, 1.2f, 21);
        AudioManager.Sfx("explosion", 0.8f, 0f);
        yield return Ease(home, 0.7f);
        if (p == 2) { Show(mawOpen); shake = 0.6f; yield return 0.4f; Show(maw); }
    }

    protected override IEnumerator Pattern(int p)
    {
        if (p == 0) return P1();
        if (p == 1) return P2();
        return P3();
    }

    // ---------------------------------------------------------------- phase 1
    void Lob(Vector2 from, Vector2 target, Kind kind, float g)
    {
        float dx = target.x - from.x, dy = target.y - from.y;
        float T = Mathf.Clamp(Mathf.Abs(dx) / 5f, 1.0f, 1.8f);
        var b = Pool.Fire(kind, from, new Vector2(dx / T, (dy + 0.5f * g * T * T) / T));
        if (b) b.gravity = g;
    }

    IEnumerator P1()
    {
        for (int v = 0; v < 4; v++)
        {
            Show(fire);
            var m = Muzzle(Cannon, 2f);
            Puff(m, 1.2f);
            AudioManager.Sfx("explosion", 0.35f, 0.15f, 0.05f, 1.4f);
            Lob(m, PlayerPos, v == 3 ? Kind.Pink : Kind.Heavy, 4.5f);
            Lob(m, PlayerPos + new Vector2(Random.Range(-2.5f, 1.5f), Random.Range(-1.5f, 2.5f)), Kind.Heavy, 4.5f);
            CameraFx.Shake(0.05f, 0.1f);
            yield return 0.18f;
            Show(idle);
            yield return 0.55f;
        }
        yield return 0.3f;
        for (int i = 0; i < 2; i++)
        {
            var m = Muzzle(Bridge);
            Pool.Fan(Kind.Aimed, m, 3, 30f, 6f, AngleToPlayer(m));
            ShootSfx();
            yield return 0.5f;
        }
        yield return 0.7f;
    }

    // ---------------------------------------------------------------- phase 2
    IEnumerator P2()
    {
        yield return TorpedoRun();
        yield return 0.5f;
        yield return Mines();
        yield return 0.4f;
        yield return Flak();
        yield return 0.8f;
    }

    IEnumerator TorpedoRun()
    {
        for (int i = 0; i < 2; i++)
        {
            float y = Mathf.Clamp(PlayerPos.y, -3.8f, 3.6f);
            yield return temps.Warn(new Vector2(0f, y), 15f, 0f, 0.65f, 0.35f);
            Pool.Fire(Kind.Torpedo, new Vector2(8.5f, y), new Vector2(-9f, 0f));
            AudioManager.Sfx("swoop", 0.6f, 0.1f);
            yield return 0.35f;
        }
    }

    IEnumerator Mines()
    {
        int pink = Random.Range(0, 4);
        for (int i = 0; i < 4; i++)
        {
            Pool.Fire(i == pink ? Kind.MinePink : Kind.Mine, new Vector2(8.2f, Random.Range(-2.6f, 3.4f)), new Vector2(-2f, Random.Range(-0.4f, 0.4f)));
            yield return 0.45f;
        }
    }

    IEnumerator Flak()
    {
        for (int i = 0; i < 3; i++)
        {
            Show(fire);
            var m = Muzzle(Cannon, 2f);
            var b = Pool.Fire(Kind.Flak, m, new Vector2(Random.Range(-6f, -3.5f), Random.Range(3f, 5f)));
            if (b)
            {
                b.gravity = 6f;
                b.lifetime = 0.9f;
                b.onExpire = x =>
                {
                    if (GameKit.I) GameKit.Burst(GameKit.I.explosion, x.transform.position, 20f, 0.7f, 21);
                    AudioManager.Sfx("explosion", 0.35f, 0.15f, 0.05f, 1.3f);
                    BulletPool.I.Ring(Kind.Copper, x.transform.position, 8, 3.5f, Random.value * 45f);
                };
            }
            ShootSfx();
            yield return 0.15f;
            Show(damaged);
            yield return 0.5f;
        }
    }

    // ---------------------------------------------------------------- phase 3
    IEnumerator P3()
    {
        yield return DoubleSpiral(2.6f);
        yield return 0.5f;
        yield return Ram();
        yield return 0.6f;
    }

    IEnumerator DoubleSpiral(float dur)
    {
        Show(mawOpen);
        float next = 0f;
        int n = 0;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            next -= Time.deltaTime;
            if (next <= 0f)
            {
                next = 0.08f;
                var from = World(Bow);
                float sweep = Mathf.Sin(t * 2.2f) * 60f;
                n++;
                float a1 = (160f + sweep) * Mathf.Deg2Rad, a2 = (160f - sweep) * Mathf.Deg2Rad;
                Pool.Fire(n % 10 == 0 ? Kind.Pink : Kind.Copper, from, new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * 4f);
                Pool.Fire(Kind.Teal, from, new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * 4f);
                if (n % 3 == 0) ShootSfx();
            }
            yield return null;
        }
        Show(maw);
    }

    IEnumerator Ram()
    {
        const float laneY = -1.8f;
        yield return Ease(new Vector2(5.6f, laneY), 0.5f);
        Show(mawOpen);
        AudioManager.Sfx("roar", 0.9f, 0f);
        shake = 0.9f;
        yield return temps.WarnLane(new Vector2(0f, laneY), 15f, 0f, 0.9f, 1.8f);
        AudioManager.Sfx("dash", 1f, 0f, 0.03f, 0.6f);
        CameraFx.Shake(0.15f, 0.4f);
        float ghost = 0f;
        while (pos.x > -12f)
        {
            pos.x -= 13f * Time.deltaTime;
            ghost -= Time.deltaTime;
            if (ghost <= 0f) { ghost = 0.05f; Afterimage(new Color(0.49f, 0.82f, 0.78f, 0.5f)); if (GameKit.I) GameKit.Pop(GameKit.I.splash, World(new Vector2(2.5f, -1.2f)), 0.4f, 2f, 0.6f, 12); }
            yield return null;
        }
        Show(maw);
        pos = new Vector2(12f, home.y);
        yield return Ease(home, 1.2f);
    }
}
