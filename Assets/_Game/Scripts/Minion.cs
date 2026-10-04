using System.Collections;
using UnityEngine;

/// <summary>Small shootable helper (storm cloud, turret). Owns a Routine and its own temps; explodes when its Health runs out.</summary>
public class Minion : MonoBehaviour
{
    public Health health;
    public Vector2 pos;
    public float bobAmp = 0.1f, bobFreq = 1.2f;
    public readonly Temps temps = new Temps();

    SpriteRenderer sr;
    Routine routine;
    float bobT, popT;
    Vector3 baseScale = Vector3.one;

    public bool Dying { get; private set; }
    public SpriteRenderer Renderer => sr;

    public static Minion Spawn(string name, Sprite[] frames, float fps, Vector2 at, float hp, Vector2 hitbox, int order = 10)
    {
        var go = new GameObject(name);
        go.transform.position = at;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = frames != null && frames.Length > 0 ? frames[0] : null;
        r.sortingOrder = order;
        if (frames != null && frames.Length > 1)
        {
            var an = go.AddComponent<SpriteAnimator>();
            an.frames = frames; an.fps = fps; an.phase = Random.value * frames.Length;
        }
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true; col.size = hitbox;
        go.AddComponent<Hazard>();
        var h = go.AddComponent<Health>();
        var m = go.AddComponent<Minion>();
        m.health = h; m.sr = r; m.pos = at; m.bobT = Random.value * 10f;
        h.Begin(hp);
        h.Died += m.Die;
        return m;
    }

    public void Run(IEnumerator e) { routine = new Routine(e); }

    void Update()
    {
        var gs = GameState.I;
        if (gs != null && !gs.Alive) return;
        float dt = Time.deltaTime;
        bobT += dt; popT += dt;
        if (routine != null && !routine.Step(dt)) routine = null;
    }

    void LateUpdate()
    {
        float k = Mathf.Clamp01(popT / 0.25f);
        float s = k < 1f ? 1f + 0.4f * Mathf.Sin(k * Mathf.PI) - 0.4f * (1f - k) : 1f;
        transform.localScale = baseScale * s;
        transform.position = pos + Vector2.up * Mathf.Sin(bobT * bobFreq * Mathf.PI * 2f) * bobAmp;
    }

    public IEnumerator Ease(Vector2 target, float dur)
    {
        Vector2 from = pos;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            pos = Vector2.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t / dur));
            yield return null;
        }
        pos = target;
    }

    void Die()
    {
        Debug.Log("[Minion] " + name + " destroyed");
        Remove(true);
    }

    public void Remove(bool explode)
    {
        if (Dying) return;
        Dying = true;
        routine = null;
        temps.Clear();
        if (explode)
        {
            if (GameKit.I) GameKit.Burst(GameKit.I.explosion, transform.position, 18f, 1.2f, 21);
            AudioManager.Sfx("explosion", 0.6f, 0.1f);
        }
        Destroy(gameObject);
    }

    void OnDestroy() { temps.Clear(); }
}
