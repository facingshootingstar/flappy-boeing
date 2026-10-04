using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared boss life cycle: fly-in intro, a pattern loop per phase, phase transitions (bullets clear, roar, flash),
/// and the knockout outro (explosions, then a fall off screen). Patterns are IEnumerators stepped by a Routine,
/// so a phase change or KO stops them instantly and every telegraph/hazard they made is cleaned up.
/// </summary>
[RequireComponent(typeof(Health), typeof(SpriteAnimator))]
public abstract class BossBase : MonoBehaviour
{
    public string displayName = "BOSS";
    public float maxHp = 300f;
    public Vector2 home = new Vector2(4.8f, 0.5f);
    public float[] phaseAt = { 0.66f, 0.33f };
    public int index;
    public Transform player;

    protected Health health;
    protected SpriteRenderer sr;
    protected SpriteAnimator anim;
    protected Collider2D body;
    protected Vector2 pos;
    protected float rot;
    protected float bobAmp = 0.12f, bobFreq = 1.5f;
    protected int phase;
    protected float phaseTime;
    protected float shake;
    protected readonly Temps temps = new Temps();
    protected readonly List<Minion> minions = new List<Minion>();

    Routine main;
    bool pendingDeath, koing;
    int pendingPhase = -1;
    float bobT;

    public bool Finished { get; private set; }
    public bool Fighting { get; private set; }
    public bool Koing => koing;
    public Health Health => health;

    protected BulletPool Pool => BulletPool.I;
    protected Vector2 PlayerPos => player ? (Vector2)player.position : new Vector2(-5f, 0f);

    protected virtual void Awake()
    {
        health = GetComponent<Health>();
        sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<SpriteAnimator>();
        body = GetComponent<Collider2D>();
        health.isBoss = true;
        health.PhaseChanged += p => pendingPhase = p;
        health.Died += () => pendingDeath = true;
    }

    protected virtual void OnDestroy() { temps.Clear(); }

    // ---------------------------------------------------------------- director entry point
    public IEnumerator Fight()
    {
        Finished = false; koing = false; pendingDeath = false; pendingPhase = -1;
        phase = 0; phaseTime = 0f; rot = 0f; shake = 0f;
        if (player == null && PlayerController.I) player = PlayerController.I.transform;
        gameObject.SetActive(true);
        health.phaseThresholds = phaseAt;
        health.Begin(maxHp);
        health.Vulnerable = false;
        if (body) body.enabled = true;
        pos = home + new Vector2(8f, 0f);
        anim.enabled = true;
        Setup();
        transform.position = pos;
        health.CacheRenderers();
        main = new Routine(FightSeq());
        Fighting = true;
        Debug.Log("[Boss] " + displayName + " phase 1");
        while (!Finished) yield return null;
        Fighting = false;
    }

    IEnumerator FightSeq()
    {
        yield return Intro();
        health.Vulnerable = true;
        yield return Loop();
    }

    IEnumerator Loop()
    {
        while (true)
        {
            yield return Pattern(phase);
            yield return null;   // a pattern that yields nothing still gives the frame back
        }
    }

    IEnumerator PhaseSeq(int p)
    {
        phase = p; phaseTime = 0f;
        health.Vulnerable = false;
        if (GameState.I) GameState.I.SetBoss(index, p);
        Debug.Log("[Boss] " + displayName + " phase " + (p + 1));
        if (Pool) Pool.ClearAll(true);
        KillMinions();
        CameraFx.HitStop(0.12f);
        FilmFilter.Flash(0.55f, 0.15f);
        AudioManager.Sfx("roar", 1f, 0f);
        CameraFx.Shake(0.2f, 0.7f);
        shake = 0.7f;
        yield return OnPhaseChange(p);
        yield return 0.35f;
        health.Vulnerable = true;
        yield return Loop();
    }

    IEnumerator KnockoutSeq()
    {
        health.Vulnerable = false;
        if (body) body.enabled = false;
        if (Pool) Pool.ClearAll(true);
        KillMinions();
        OnKnockout();
        Debug.Log("[Boss] " + displayName + " KO");
        if (Announcer.I) Announcer.I.StartCoroutine(Announcer.I.Knockout());
        var b = sr.bounds;
        float next = 0f;
        for (float t = 0f; t < 2.2f; t += Time.deltaTime)
        {
            shake = 0.15f;
            next -= Time.deltaTime;
            if (next <= 0f)
            {
                next = 0.13f;
                var at = new Vector3(Random.Range(b.min.x, b.max.x), Random.Range(b.min.y, b.max.y), 0f);
                if (GameKit.I) GameKit.Burst(GameKit.I.explosion, at, 18f, Random.Range(0.8f, 1.4f), sr.sortingOrder + 2);
                AudioManager.Sfx("explosion", 0.45f, 0.15f, 0.08f);
            }
            yield return null;
        }
        float vy = 2f;
        while (pos.y > -9.5f)
        {
            float dt = Time.deltaTime;
            vy -= 16f * dt;
            pos += new Vector2(0.8f * dt, vy * dt);
            rot += 30f * dt;
            yield return null;
        }
        gameObject.SetActive(false);
        Finished = true;
    }

    // ---------------------------------------------------------------- per-frame
    protected virtual void Update()
    {
        if (main == null) return;
        var gs = GameState.I;
        if (gs != null && !gs.Alive && !gs.Victory && !koing) return;   // freeze while the death screen is up
        float dt = Time.deltaTime;
        if (pendingDeath && !koing)
        {
            pendingDeath = false; pendingPhase = -1;
            Interrupt();
            koing = true;
            main = new Routine(KnockoutSeq());
        }
        else if (pendingPhase >= 0 && !koing)
        {
            int p = pendingPhase; pendingPhase = -1;
            Interrupt();
            main = new Routine(PhaseSeq(p));
        }
        phaseTime += dt;
        bobT += dt;
        if (shake > 0f) shake -= dt;
        if (!koing) Tick(dt);
        main.Step(dt);
    }

    protected virtual void LateUpdate()
    {
        if (main == null) return;
        Vector2 p = pos;
        if (!koing) p.y += Mathf.Sin(bobT * bobFreq * Mathf.PI * 2f) * bobAmp;
        if (shake > 0f) p += Random.insideUnitCircle * 0.08f;
        transform.position = p;
        transform.rotation = Quaternion.Euler(0f, 0f, rot);
    }

    void Interrupt()
    {
        main?.Stop();
        temps.Clear();
        rot = 0f;
        OnInterrupt();
    }

    // ---------------------------------------------------------------- subclass hooks
    protected abstract void Setup();
    protected abstract IEnumerator Pattern(int phase);
    protected virtual void Tick(float dt) { }
    /// <summary>Pattern stopped mid-way (phase change / KO): restore sprites, flips, flags.</summary>
    protected virtual void OnInterrupt() { }
    protected virtual void OnKnockout() { }

    protected virtual IEnumerator Intro()
    {
        yield return Ease(home, 1.5f);
        AudioManager.Sfx("roar", 0.8f, 0f);
        shake = 0.5f;
        yield return 0.6f;
    }

    protected virtual IEnumerator OnPhaseChange(int p)
    {
        yield return Ease(home, 0.7f);
    }

    // ---------------------------------------------------------------- movement helpers
    protected IEnumerator MoveTo(Vector2 target, float speed)
    {
        while ((pos - target).sqrMagnitude > 0.0004f)
        {
            pos = Vector2.MoveTowards(pos, target, speed * Time.deltaTime);
            yield return null;
        }
    }

    protected IEnumerator Ease(Vector2 target, float dur)
    {
        Vector2 from = pos;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            pos = Vector2.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }
        pos = target;
    }

    // ---------------------------------------------------------------- fx helpers
    protected Vector2 World(Vector2 local) => (Vector2)transform.TransformPoint(local);

    protected Vector2 Muzzle(Vector2 local, float scale = 1.5f)
    {
        var w = World(local);
        if (GameKit.I) GameKit.Pop(GameKit.I.muzzle, w, 0.08f, scale, 0.5f, 12);
        return w;
    }

    protected float AngleToPlayer(Vector2 from)
    {
        var d = PlayerPos - from;
        return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
    }

    protected void Afterimage(Color c)
    {
        var fx = GameKit.Pop(sr.sprite, transform.position, 0.25f, transform.localScale.x, 0f, sr.sortingOrder - 1, c, rot);
        if (fx) fx.GetComponent<SpriteRenderer>().flipX = sr.flipX;
    }

    protected static void Puff(Vector2 at, float scale = 1f)
    {
        if (GameKit.I) GameKit.Burst(GameKit.I.smoke, at, 14f, scale, 21);
    }

    protected static void ShootSfx() { AudioManager.Sfx("shoot", 0.3f, 0.1f, 0.06f, 0.6f); }

    /// <summary>Show a static sprite (stops the frame animator).</summary>
    protected void Show(Sprite s)
    {
        anim.enabled = false;
        sr.sprite = s;
    }

    /// <summary>Loop a sheet.</summary>
    protected void Animate(Sprite[] frames, float fps)
    {
        anim.enabled = true;
        anim.Play(frames, fps);
    }

    // ---------------------------------------------------------------- minions
    protected Minion SpawnMinion(string name, Sprite[] frames, float fps, Vector2 at, float hp, Vector2 hitbox, int order = 10)
    {
        minions.RemoveAll(m => m == null);
        var m2 = Minion.Spawn(name, frames, fps, at, hp, hitbox, order);
        minions.Add(m2);
        return m2;
    }

    protected static bool Alive(Minion m) => m != null && !m.Dying;

    protected void KillMinions()
    {
        foreach (var m in minions) if (m != null) m.Remove(true);
        minions.Clear();
    }
}

/// <summary>Objects a pattern spawns (warnings, bolts, lasers). Cleared together when the pattern is interrupted.</summary>
public class Temps
{
    readonly List<GameObject> list = new List<GameObject>();

    public SpriteRenderer Make(string name, Sprite sp, Vector2 at, int order, float rotDeg = 0f)
    {
        var go = new GameObject(name);
        go.transform.position = at;
        go.transform.rotation = Quaternion.Euler(0f, 0f, rotDeg);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = sp;
        r.sortingOrder = order;
        list.Add(go);
        return r;
    }

    public void Kill(Component c)
    {
        if (c == null) return;
        list.Remove(c.gameObject);
        Object.Destroy(c.gameObject);
    }

    public void Clear()
    {
        foreach (var g in list) if (g) Object.Destroy(g);
        list.Clear();
    }

    /// <summary>A warning stripe (centre, length and thickness in world units).</summary>
    public SpriteRenderer Bar(Vector2 at, float length, float angle, float thickness = 0.25f, bool mark = true)
    {
        var r = Make("Warn", GameKit.I ? GameKit.I.warningBar : null, at, 15, angle);
        r.transform.localScale = new Vector3(length / 4f, thickness / 0.25f, 1f);
        if (mark) Telegraph.Mark(r.gameObject, length * 0.5f, Mathf.Max(thickness * 0.5f, 0.7f));
        return r;
    }

    /// <summary>Blink the bars faster and faster, then remove them.</summary>
    public IEnumerator Blink(float dur, float alpha, params SpriteRenderer[] bars)
    {
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float rate = t > dur * 0.6f ? 18f : 8f;
            float a = alpha * (Mathf.Repeat(t * rate, 1f) < 0.5f ? 1f : 0.25f);
            foreach (var b in bars) if (b) b.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        foreach (var b in bars) Kill(b);
    }

    public IEnumerator Warn(Vector2 at, float length, float angle, float dur, float thickness = 0.25f, float alpha = 0.6f)
    {
        AudioManager.Sfx("card", 0.35f, 0f, 0.1f, 1.6f);
        yield return Blink(dur, alpha, Bar(at, length, angle, thickness));
    }

    /// <summary>
    /// Telegraph for a body charging down a lane: a faint fill exactly as wide as the attacker's hitbox,
    /// framed by two crisp edge lines, so "outside the lines" really is safe.
    /// </summary>
    public IEnumerator WarnLane(Vector2 at, float length, float angle, float dur, float width)
    {
        AudioManager.Sfx("card", 0.35f, 0f, 0.1f, 1.6f);
        var fill = Bar(at, length, angle, width, false);
        Telegraph.Mark(fill.gameObject, length * 0.5f, width * 0.5f);
        var n = (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector3.up) * (width * 0.5f);
        var edgeA = Bar(at + n, length, angle, 0.12f, false);
        var edgeB = Bar(at - n, length, angle, 0.12f, false);
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float rate = t > dur * 0.6f ? 18f : 8f;
            float k = Mathf.Repeat(t * rate, 1f) < 0.5f ? 1f : 0.3f;
            fill.color = new Color(1f, 1f, 1f, 0.2f * k);
            edgeA.color = edgeB.color = new Color(1f, 1f, 1f, 0.75f * k);
            yield return null;
        }
        Kill(fill); Kill(edgeA); Kill(edgeB);
    }

    /// <summary>Shrinking ring on the spot that is about to be hit.</summary>
    public IEnumerator Ring(Vector2 at, float dur, Color c)
    {
        var r = Make("Telegraph", GameKit.I ? GameKit.I.telegraph : null, at, 15);
        Telegraph.Mark(r.gameObject, 0f, 1.1f);
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            r.transform.localScale = Vector3.one * Mathf.Lerp(1.8f, 0.8f, k);
            r.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.4f, 1f, k));
            yield return null;
        }
        Kill(r);
    }
}
