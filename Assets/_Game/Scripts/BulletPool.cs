using System.Collections.Generic;
using UnityEngine;

/// <summary>All enemy projectiles. Per-kind look and flags live in Configure(); over the cap, Fire() silently drops the shot.</summary>
public class BulletPool : MonoBehaviour
{
    public static BulletPool I { get; private set; }
    public Sprite copper, teal, aimed, heavy, pink, feather, featherPink, torpedo, mine, brick, flak;
    public int cap = 140;
    readonly Stack<Bullet> free = new Stack<Bullet>();
    readonly List<Bullet> active = new List<Bullet>();

    public enum Kind { Copper, Teal, Aimed, Heavy, Pink, Feather, FeatherPink, Torpedo, Mine, MinePink, Brick, Flak }

    public IReadOnlyList<Bullet> Active => active;

    void Awake() { I = this; }
    void OnDestroy() { if (I == this) I = null; }

    public Bullet Fire(Kind kind, Vector2 pos, Vector2 vel)
    {
        if (active.Count >= cap) return null;
        Bullet b = free.Count > 0 ? free.Pop() : Make();
        b.ResetState();
        b.kind = kind;
        Configure(b, kind);
        b.transform.position = pos;
        b.velocity = vel;
        b.gameObject.SetActive(true);
        active.Add(b);
        return b;
    }

    void Configure(Bullet b, Kind k)
    {
        var sr = b.GetComponent<SpriteRenderer>();
        var col = b.GetComponent<CircleCollider2D>();
        float r = 0.17f;
        switch (k)
        {
            case Kind.Teal: sr.sprite = teal; break;
            case Kind.Aimed: sr.sprite = aimed; break;
            case Kind.Heavy: sr.sprite = heavy; r = 0.3f; break;
            case Kind.Pink: sr.sprite = pink; b.parryable = true; b.spin = 240f; break;
            case Kind.Feather: sr.sprite = feather; r = 0.13f; b.faceVelocity = true; break;
            case Kind.FeatherPink: sr.sprite = featherPink; r = 0.14f; b.faceVelocity = true; b.parryable = true; break;
            case Kind.Torpedo: sr.sprite = torpedo; r = 0.2f; b.faceVelocity = true; b.rotOffset = 180f; b.hp = 3f; break;
            case Kind.Mine: sr.sprite = mine; r = 0.3f; b.hp = 4f; b.spin = 30f; break;
            case Kind.MinePink: sr.sprite = mine; r = 0.3f; b.parryable = true; b.spin = 30f; sr.color = new Color(1f, 0.62f, 0.8f); break;
            case Kind.Brick: sr.sprite = brick; r = 0.2f; b.spin = 160f; break;
            case Kind.Flak: sr.sprite = flak; r = 0.2f; b.spin = 400f; break;
            default: sr.sprite = copper; break;
        }
        col.radius = r;
    }

    Bullet Make()
    {
        var go = new GameObject("Bullet");
        go.transform.SetParent(transform, false);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = 11;
        var c = go.AddComponent<CircleCollider2D>(); c.isTrigger = true;
        var b = go.AddComponent<Bullet>();
        b.OnDone = Release;
        return b;
    }

    void Release(Bullet b)
    {
        if (!b.gameObject.activeSelf) return;
        b.gameObject.SetActive(false);
        active.Remove(b);
        free.Push(b);
    }

    /// <summary>Remove every live bullet; with puffs, each pops into a little smoke burst (phase changes, knockouts).</summary>
    public void ClearAll(bool puffs = false)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var b = active[i];
            if (puffs && GameKit.I && i % 2 == 0) GameKit.Burst(GameKit.I.smoke, b.transform.position, 16f, 1f);
            b.onExpire = null;
            Release(b);
        }
    }

    // ---------------------------------------------------------------- pattern helpers
    /// <summary>n bullets spread around a centre angle (degrees, 180 = straight left).</summary>
    public void Fan(Kind kind, Vector2 from, int n, float spread, float speed, float centre = 180f, int pinkIndex = -1)
    {
        for (int i = 0; i < n; i++)
        {
            float a = (centre + (n == 1 ? 0f : -spread / 2f + spread * i / (n - 1))) * Mathf.Deg2Rad;
            Fire(i == pinkIndex ? PinkOf(kind) : kind, from, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed);
        }
    }

    /// <summary>Full ring with an optional gap (in bullets) facing the gap angle.</summary>
    public void Ring(Kind kind, Vector2 from, int n, float speed, float offset = 0f, int gap = 0, float gapAngle = 180f, int pinkEvery = 0)
    {
        for (int i = 0; i < n; i++)
        {
            float deg = offset + 360f * i / n;
            if (gap > 0 && Mathf.Abs(Mathf.DeltaAngle(deg, gapAngle)) < 360f / n * gap * 0.5f) continue;
            float a = deg * Mathf.Deg2Rad;
            Fire(pinkEvery > 0 && i % pinkEvery == 0 ? PinkOf(kind) : kind, from, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed);
        }
    }

    public Bullet Aimed(Kind kind, Vector2 from, Vector2 target, float speed, float angleOffset = 0f)
    {
        var d = target - from;
        float a = Mathf.Atan2(d.y, d.x) + angleOffset * Mathf.Deg2Rad;
        return Fire(kind, from, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed);
    }

    public static Kind PinkOf(Kind k) => k == Kind.Feather ? Kind.FeatherPink : k == Kind.Mine ? Kind.MinePink : Kind.Pink;
}
