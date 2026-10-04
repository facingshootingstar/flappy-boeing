using System.Collections;
using UnityEngine;
using Kind = BulletPool.Kind;

/// <summary>
/// Sundown Citadel (dusk biome), a flying fortress.
/// P1: two wall guns trade aimed heavy shots, the main cannon fires rings with a gap toward the player.
/// P2 (cracked, guns blown off): two shootable turret minions, bricks falling from the sky, copper fans.
/// P3: the shell is gone and the caged core fights: three-arm spiral, a laser sweep that leaves the top or bottom safe, pink fans.
/// </summary>
public class CitadelBoss : BossBase
{
    public Sprite tower, cracked, turret, laser;
    public Sprite[] core, gun;

    static readonly Vector2 BigCannon = new Vector2(-2.1f, 0f);
    static readonly Vector2[] GunAt = { new Vector2(-2.0f, 1.45f), new Vector2(-1.9f, -1.55f) };
    static readonly Vector2[] TurretAt = { new Vector2(2.6f, 3.2f), new Vector2(2.6f, -3.0f) };
    const float CoreScale = 1.4f;

    SpriteRenderer[] guns;
    readonly float[] gunFlash = new float[2];
    readonly Minion[] turrets = new Minion[2];
    readonly float[] turretWait = new float[2];
    BoxCollider2D box;
    bool sweepTop;
    float driftT;

    protected override void Awake()
    {
        base.Awake();
        displayName = "SUNDOWN CITADEL";
        bobAmp = 0.05f; bobFreq = 0.6f;
        box = GetComponent<BoxCollider2D>();
    }

    Vector2 Mouth => phase == 2 ? Vector2.zero : BigCannon;

    protected override void Setup()
    {
        Show(tower);
        transform.localScale = Vector3.one;
        if (box) { box.size = new Vector2(3.6f, 3.8f); box.offset = Vector2.zero; }
        if (guns == null)
        {
            guns = new SpriteRenderer[2];
            for (int i = 0; i < 2; i++)
            {
                var go = new GameObject("Gun" + i);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = GunAt[i];
                guns[i] = go.AddComponent<SpriteRenderer>();
                guns[i].sortingOrder = 11;
            }
        }
        foreach (var g in guns) { g.gameObject.SetActive(true); g.sprite = gun[0]; }
        for (int i = 0; i < 2; i++) { turrets[i] = null; turretWait[i] = 0f; gunFlash[i] = 0f; }
        driftT = 0f;
    }

    protected override void Tick(float dt)
    {
        for (int i = 0; i < 2; i++)
        {
            if (gunFlash[i] <= 0f) continue;
            gunFlash[i] -= dt;
            if (gunFlash[i] <= 0f && guns[i]) guns[i].sprite = gun[0];
        }
        if (phase == 1 && health.Vulnerable)
        {
            for (int i = 0; i < 2; i++)
            {
                if (Alive(turrets[i])) continue;
                turretWait[i] -= dt;
                if (turretWait[i] <= 0f) { turrets[i] = SpawnTurret(i); turretWait[i] = 10f; }
            }
        }
        if (phase == 2)
        {
            driftT += dt;
            var target = new Vector2(home.x - 0.4f, home.y + Mathf.Sin(driftT * 0.8f) * 1.6f);
            pos = Vector2.Lerp(pos, target, 1f - Mathf.Exp(-2f * dt));
        }
    }

    protected override void OnInterrupt()
    {
        if (phase == 2) Animate(core, 4f);
        else Show(phase == 0 ? tower : cracked);
    }

    protected override void OnKnockout()
    {
        if (phase == 2) Show(core[core.Length - 1]);
        else Show(cracked);
    }

    protected override IEnumerator Intro()
    {
        // drifts down from the sky
        pos = home + new Vector2(1.5f, 7f);
        yield return Ease(home, 1.8f);
        AudioManager.Sfx("roar", 0.8f, 0f, 0.03f, 0.8f);
        shake = 0.5f;
        yield return 0.5f;
    }

    protected override IEnumerator OnPhaseChange(int p)
    {
        if (p == 1)
        {
            Show(cracked);
            for (int i = 0; i < 2; i++)
            {
                if (GameKit.I) GameKit.Burst(GameKit.I.explosion, guns[i].transform.position, 16f, 1.4f, 21);
                guns[i].gameObject.SetActive(false);
            }
            AudioManager.Sfx("explosion", 0.9f, 0f);
            yield return Ease(home, 0.6f);
            for (int i = 0; i < 2; i++) turretWait[i] = 0.1f + i * 0.5f;
        }
        else
        {
            // the shell bursts and the core is exposed
            var b = sr.bounds;
            for (int i = 0; i < 8; i++)
            {
                if (GameKit.I) GameKit.Burst(GameKit.I.explosion, new Vector2(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y)), 16f, 1.5f, 21);
                if (i % 2 == 0) AudioManager.Sfx("explosion", 0.6f, 0.15f, 0.05f);
                yield return 0.08f;
            }
            if (Pool) for (int i = 0; i < 10; i++)
                {
                    var bk = Pool.Fire(Kind.Brick, (Vector2)transform.position + Random.insideUnitCircle, new Vector2(Random.Range(-4f, 1f), Random.Range(2f, 5f)));
                    if (bk) bk.gravity = 7f;
                }
            Animate(core, 4f);
            transform.localScale = Vector3.one * CoreScale;
            if (box) box.size = Vector2.one * (2.6f / CoreScale);
            health.CacheRenderers();
            driftT = 0f;
            yield return Ease(home + new Vector2(-0.4f, 0f), 0.6f);
        }
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
        for (int i = 0; i < 6; i++)
        {
            int g = i % 2;
            guns[g].sprite = gun[1];
            gunFlash[g] = 0.1f;
            var m = World(GunAt[g] + new Vector2(-0.7f, 0f));
            Pool.Aimed(Kind.Heavy, m, PlayerPos, 5.2f);
            Puff(m, 0.8f);
            ShootSfx();
            yield return 0.55f;
        }
        yield return 0.2f;
        for (int r = 0; r < 2; r++)
        {
            var c = World(BigCannon);
            yield return temps.Ring(c, 0.5f, Announcer.Yellow);
            Muzzle(BigCannon, 2.4f);
            Pool.Ring(Kind.Copper, c, 18, 3.6f, r * 10f, 3, AngleToPlayer(c), 6);
            AudioManager.Sfx("explosion", 0.4f, 0.1f, 0.05f, 1.5f);
            CameraFx.Shake(0.08f, 0.15f);
            yield return 0.9f;
        }
        yield return 0.4f;
    }

    // ---------------------------------------------------------------- phase 2
    Minion SpawnTurret(int slot)
    {
        var m = SpawnMinion("Turret", new[] { turret }, 1f, transform.position, 25f, new Vector2(0.9f, 0.9f), 10);
        m.bobAmp = 0.12f;
        m.Run(TurretLife(m, TurretAt[slot]));
        return m;
    }

    IEnumerator TurretLife(Minion m, Vector2 at)
    {
        yield return m.Ease(at, 0.7f);
        while (true)
        {
            yield return Random.Range(1.3f, 1.9f);
            var from = m.pos + new Vector2(-0.5f, 0f);
            if (GameKit.I) GameKit.Pop(GameKit.I.muzzle, from, 0.08f, 1.2f, 0.5f, 12);
            Pool.Aimed(Kind.Copper, from, PlayerPos, 5f);
            ShootSfx();
        }
    }

    IEnumerator P2()
    {
        yield return BrickFall(2.6f);
        yield return 0.3f;
        for (int i = 0; i < 3; i++)
        {
            var m = Muzzle(BigCannon, 2f);
            Pool.Fan(Kind.Copper, m, 5, 50f, 4.5f, AngleToPlayer(m));
            ShootSfx();
            yield return 0.6f;
        }
        yield return 0.6f;
    }

    IEnumerator BrickFall(float dur)
    {
        var pending = new System.Collections.Generic.List<Vector2>();   // x, fire time
        float t = 0f, next = 0f;
        int n = 0;
        while (t < dur || pending.Count > 0)
        {
            t += Time.deltaTime;
            if (t < dur && t >= next)
            {
                next += 0.32f;
                n++;
                float x = n % 2 == 0 ? Mathf.Clamp(PlayerPos.x + Random.Range(-0.5f, 0.5f), -7f, 3f) : Random.Range(-7f, 3f);
                Puff(new Vector2(x, 4.1f), 1.1f);
                pending.Add(new Vector2(x, t + 0.45f));
            }
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (t < pending[i].y) continue;
                var b = Pool.Fire(Kind.Brick, new Vector2(pending[i].x, 4.7f), new Vector2(0f, -1f));
                if (b) b.gravity = 7f;
                pending.RemoveAt(i);
            }
            yield return null;
        }
    }

    // ---------------------------------------------------------------- phase 3
    IEnumerator P3()
    {
        yield return Spiral3(2.8f);
        yield return 0.4f;
        yield return LaserSweep(sweepTop);
        sweepTop = !sweepTop;
        yield return 0.3f;
        for (int i = 0; i < 2; i++)
        {
            var c = (Vector2)transform.position;
            Pool.Fan(Kind.Copper, c, 7, 70f, 4.5f, AngleToPlayer(c), 3);
            ShootSfx();
            yield return 0.55f;
        }
        yield return 0.6f;
    }

    IEnumerator Spiral3(float dur)
    {
        float a = Random.value * 360f, next = 0f;
        int n = 0;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            next -= Time.deltaTime;
            if (next <= 0f)
            {
                next = 0.07f;
                a += 9f;
                var c = (Vector2)transform.position;
                for (int arm = 0; arm < 3; arm++)
                {
                    float r = (a + arm * 120f) * Mathf.Deg2Rad;
                    n++;
                    Pool.Fire(n % 9 == 0 ? Kind.Pink : Kind.Copper, c, new Vector2(Mathf.Cos(r), Mathf.Sin(r)) * 3.8f);
                }
                if (n % 6 == 0) ShootSfx();
            }
            yield return null;
        }
    }

    IEnumerator LaserSweep(bool fromTop)
    {
        // fromTop: starts high and sweeps down, the bottom stays safe; otherwise the mirror
        float a0 = fromTop ? 180f - 28f : 180f + 28f;
        float a1 = fromTop ? 180f + 8f : 180f - 8f;
        const float length = 14f;
        var c = (Vector2)transform.position;
        var d0 = new Vector2(Mathf.Cos(a0 * Mathf.Deg2Rad), Mathf.Sin(a0 * Mathf.Deg2Rad));
        var d1 = new Vector2(Mathf.Cos(a1 * Mathf.Deg2Rad), Mathf.Sin(a1 * Mathf.Deg2Rad));
        AudioManager.Sfx("roar", 0.6f, 0f, 0.03f, 1.4f);
        yield return temps.Blink(0.9f, 0.9f, temps.Bar(c + d0 * length / 2f, length, a0), temps.Bar(c + d1 * length / 2f, length, a1, 0.15f));

        var pivot = temps.Make("LaserPivot", null, c, 12, a0);
        var beamGo = new GameObject("Laser");
        beamGo.transform.SetParent(pivot.transform, false);
        beamGo.transform.localPosition = new Vector3(length / 2f, 0f, 0f);
        var beam = beamGo.AddComponent<SpriteRenderer>();
        beam.sprite = laser; beam.sortingOrder = 12;
        beam.drawMode = SpriteDrawMode.Tiled;
        beam.size = new Vector2(length, 0.75f);
        var col = beamGo.AddComponent<BoxCollider2D>();
        col.isTrigger = true; col.size = new Vector2(length, 0.4f);
        beamGo.AddComponent<Hazard>();
        AudioManager.Sfx("super", 0.5f, 0f, 0.1f, 1.3f);
        const float dur = 1.6f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            pivot.transform.position = transform.position;
            pivot.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(a0, a1, k));
            beamGo.transform.localScale = new Vector3(1f, 1f + 0.15f * Mathf.Sin(t * 50f), 1f);
            CameraFx.Shake(0.04f, 0.08f);
            yield return null;
        }
        temps.Kill(pivot);
    }
}
