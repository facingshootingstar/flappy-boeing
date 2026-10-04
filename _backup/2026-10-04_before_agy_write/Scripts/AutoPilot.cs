using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Debug bot (attached at runtime through UnityMCP, never by the scene builder).
/// Flappy: taps to hold the jet at the centre of the next bird gap.
/// Combat: always fires; each frame scores nine move directions by predicting every bullet's path (plus static hazards,
/// lasers, bolts, the boss body) over the next 0.6 s and picks the safest one near a preferred firing spot;
/// dashes through imminent hits, dashes into pink bullets to parry, and spends cards (EX, then a Super at five).
/// </summary>
[DefaultExecutionOrder(-100)]
public class AutoPilot : MonoBehaviour
{
    public Transform player;
    public bool god;
    public float timeScale = 1f;
    public bool autoRestart = false;
    public bool useEx = true;

    bool logged, saveForSuper = true;
    float nextLog;
    readonly List<Collider2D> statics = new List<Collider2D>();
    readonly List<Vector2> staticVel = new List<Vector2>();
    readonly Dictionary<Collider2D, Vector2> lastCentre = new Dictionary<Collider2D, Vector2>();
    struct Shot { public Vector2 p, v; public float g, r; }
    readonly List<Shot> shots = new List<Shot>();
    float staticsAt;

    static readonly Vector2[] Dirs =
    {
        Vector2.zero, Vector2.up, Vector2.down, Vector2.left, Vector2.right,
        new Vector2(0.707f, 0.707f), new Vector2(-0.707f, 0.707f), new Vector2(0.707f, -0.707f), new Vector2(-0.707f, -0.707f),
    };
    static readonly float[] Steps = { 0.08f, 0.16f, 0.26f, 0.38f, 0.5f, 0.62f };
    const float HalfW = 0.45f, HalfH = 0.2f;

    void Start() { CameraFx.BaseScale = timeScale; GameState.God = god; }
    void OnDestroy()
    {
        CameraFx.BaseScale = 1f; GameState.God = false;
        CombatInput.MoveOverride = null; CombatInput.FireOverride = false; CombatInput.DashOverride = false; CombatInput.ExOverride = false;
    }

    void Update()
    {
        var gs = GameState.I;
        if (gs == null || player == null) return;
        if (!gs.Alive)
        {
            CombatInput.MoveOverride = Vector2.zero;
            CombatInput.FireOverride = false;
            if (!logged)
            {
                logged = true;
                Debug.Log("[AutoPilot] " + (gs.Victory ? "won" : "died") + " score=" + gs.Score + " boss=" + gs.BossIndex + " phase=" + gs.BossPhase + " t=" + Time.time.ToString("F1"));
            }
            if (autoRestart) TapInput.InjectedFrame = Time.frameCount;
            return;
        }
        if (gs.Combat) { Combat(); return; }

        float target = 0f, bestX = float.MaxValue;
        foreach (var g in FindObjectsByType<ScoreGate>())
        {
            float dx = g.transform.position.x - player.position.x;
            if (dx > -2.2f && dx < bestX) { bestX = dx; target = g.transform.position.y; }
        }
        if (!gs.Started || player.position.y < target - 0.55f) TapInput.InjectedFrame = Time.frameCount;
    }

    // ---------------------------------------------------------------- combat
    void Combat()
    {
        CombatInput.FireOverride = true;
        var pc = PlayerController.I;
        if (pc == null) return;
        Vector2 p = player.position;

        if (Time.time > staticsAt)
        {
            staticsAt = Time.time + 0.1f;
            statics.Clear();
            foreach (var h in FindObjectsByType<Hazard>())
            {
                if (h is Bullet) continue;
                var c = h.GetComponent<Collider2D>();
                if (c && c.enabled && c.gameObject.activeInHierarchy) statics.Add(c);
            }
        }

        // hazard bodies move (dives, rams, swoops): extrapolate them with their measured velocity
        staticVel.Clear();
        float dt = Time.deltaTime;
        for (int i = 0; i < statics.Count; i++)
        {
            var c = statics[i];
            Vector2 now = c ? (Vector2)c.bounds.center : Vector2.zero, v = Vector2.zero;
            if (c && dt > 0f && lastCentre.TryGetValue(c, out var was)) v = Vector2.ClampMagnitude((now - was) / dt, 25f);
            if (c) lastCentre[c] = now;
            staticVel.Add(v);
        }

        shots.Clear();
        var pool = BulletPool.I;
        if (pool)
        {
            var act = pool.Active;
            for (int i = 0; i < act.Count; i++)
            {
                var b = act[i];
                if (!b || !b.gameObject.activeInHierarchy) continue;
                var cc = b.GetComponent<CircleCollider2D>();
                float r = (cc ? cc.radius : 0.17f) * b.transform.lossyScale.x;
                Vector2 bpos = b.transform.position;
                if ((bpos - p).sqrMagnitude > 64f) continue;
                shots.Add(new Shot { p = bpos, v = b.velocity, g = b.gravity, r = r });
            }
        }

        // preferred spot: left side, level with the boss so shots connect
        var boss = Health.Boss;
        float prefY = boss && boss.gameObject.activeInHierarchy ? Mathf.Clamp(boss.transform.position.y, -3.4f, 3.4f) : 0f;
        var pref = new Vector2(-4.6f, prefY);

        int best = 0; float bestCost = float.MaxValue, bestFirstHit = float.MaxValue;
        for (int d = 0; d < Dirs.Length; d++)
        {
            float firstHit;
            float cost = Danger(p, Dirs[d] * pc.moveSpeed, pc, out firstHit);
            var end = Clamp(p + Dirs[d] * pc.moveSpeed * 0.3f, pc);
            cost += 0.25f * Vector2.Distance(end, pref);
            if (cost < bestCost) { bestCost = cost; best = d; bestFirstHit = firstHit; }
        }
        var move = Dirs[best];
        if (best == 0)
        {
            var to = pref - p;
            if (to.sqrMagnitude > 0.04f && Danger(p, to.normalized * pc.moveSpeed * 0.4f, pc, out _) < 0.01f) move = to.normalized * 0.4f;
        }
        CombatInput.MoveOverride = move;

        // dash: through an unavoidable hit, or into a pink bullet to parry
        if (pc.DashReadyIn <= 0f && !pc.Dashing)
        {
            if (bestFirstHit < 0.12f) { CombatInput.DashOverride = true; Log("[AutoPilot] panic dash"); }
            else
            {
                var pink = NearestPink(p);
                if (pink && Vector2.Distance(pink.transform.position, p) < 1.1f)
                {
                    CombatInput.MoveOverride = ((Vector2)pink.transform.position - p).normalized;
                    CombatInput.DashOverride = true;
                }
            }
        }

        // cards
        if (useEx && !PlayerWeapons.SuperActive && boss && boss.Vulnerable)
        {
            int cards = PlayerWeapons.Cards;
            if (cards >= PlayerWeapons.MaxCards) { CombatInput.ExOverride = true; saveForSuper = false; Debug.Log("[AutoPilot] super"); }
            else if (!saveForSuper && cards >= 1) { CombatInput.ExOverride = true; if (cards == 1) saveForSuper = true; }
        }
    }

    void Log(string s) { if (Time.time > nextLog) { nextLog = Time.time + 1f; Debug.Log(s); } }

    static Vector2 Clamp(Vector2 q, PlayerController pc)
    {
        q.x = Mathf.Clamp(q.x, pc.combatX.x, pc.combatX.y);
        q.y = Mathf.Clamp(q.y, -pc.combatY, pc.combatY);
        return q;
    }

    float Danger(Vector2 p, Vector2 vel, PlayerController pc, out float firstHit)
    {
        firstHit = float.MaxValue;
        float cost = 0f;
        for (int s = 0; s < Steps.Length; s++)
        {
            float t = Steps[s];
            var q = Clamp(p + vel * t, pc);
            float w = 1.5f - t;    // nearer in time matters more
            for (int i = 0; i < shots.Count; i++)
            {
                var b = shots[i];
                Vector2 bp = b.p + b.v * t + new Vector2(0f, -0.5f * b.g * t * t);
                float dx = Mathf.Abs(bp.x - q.x) - HalfW - b.r, dy = Mathf.Abs(bp.y - q.y) - HalfH - b.r;
                float gap = Mathf.Max(dx, dy);
                if (gap < 0f) { cost += 10f * w; if (t < firstHit) firstHit = t; }
                else if (gap < 0.35f) cost += (0.35f - gap) * w;
            }
            for (int i = 0; i < statics.Count; i++)
            {
                var c = statics[i];
                if (!c) continue;
                var bb = c.bounds;
                bb.center += (Vector3)(staticVel[i] * t);
                float dx = Mathf.Max(bb.min.x - q.x, q.x - bb.max.x) - HalfW;
                float dy = Mathf.Max(bb.min.y - q.y, q.y - bb.max.y) - HalfH;
                float gap = Mathf.Max(dx, dy);
                if (gap < 0f) { cost += 10f * w; if (t < firstHit) firstHit = t; }
                else if (gap < 0.5f) cost += (0.5f - gap) * w;
            }
        }
        // telegraphed attacks (dive lanes, bolts, swoop targets): keep out, but they never trigger a panic dash
        var tel = Telegraph.All;
        for (int i = 0; i < tel.Count; i++)
        {
            var q = Clamp(p + vel * 0.3f, pc);
            float gap = tel[i].Gap(q) - HalfH;
            if (gap < 0f) cost += 8f;
            else if (gap < 0.4f) cost += (0.4f - gap) * 2f;
        }
        // stay off the walls
        float edge = Mathf.Min(p.y + pc.combatY, pc.combatY - p.y);
        if (edge < 0.4f && vel.y * Mathf.Sign(p.y) > 0f) cost += 0.6f;
        return cost;
    }

    static Bullet NearestPink(Vector2 p)
    {
        var pool = BulletPool.I;
        if (!pool) return null;
        Bullet best = null; float bd = float.MaxValue;
        var act = pool.Active;
        for (int i = 0; i < act.Count; i++)
        {
            var b = act[i];
            if (!b || !b.parryable || !b.gameObject.activeInHierarchy) continue;
            float d = ((Vector2)b.transform.position - p).sqrMagnitude;
            if (d < bd) { bd = d; best = b; }
        }
        return best;
    }
}
