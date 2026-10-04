using System.Collections.Generic;
using UnityEngine;

/// <summary>Background life: props drifting across the sky (clouds, gulls, a balloon, wind streaks) with random height, scale and speed,
/// or, with twinkle > 0, short-lived sparkles anywhere in a band (sea glitter). Children of this object, so they hide with the biome.</summary>
public class AmbientSpawner : MonoBehaviour
{
    public Sprite[] sprites;              // one picked at random, or the frames when animated
    public bool animated;
    public float fps = 8f;
    public Vector2 interval = new Vector2(2f, 4f);
    public Vector2 yRange = new Vector2(0f, 3f);
    public Vector2 scaleRange = Vector2.one;
    public Vector2 speedRange = new Vector2(1f, 2f);
    public Vector2 alphaRange = Vector2.one;
    public Color tint = Color.white;
    public int sortingOrder = 2;
    public float bobAmp, bobFreq = 1f;
    public bool randomFlip;
    public float twinkle;                 // > 0: lifetime of a sparkle spawned in view
    public int prewarm;                   // items already on screen when the biome appears
    public bool alwaysRun;
    public int max = 24;

    class Item { public Transform t; public SpriteRenderer r; public float speed, y0, phase, age, alpha, half; }
    readonly List<Item> items = new List<Item>();
    float timer;
    bool warmed;

    void OnEnable()
    {
        timer = Random.Range(interval.x, interval.y) * 0.5f;
        if (!warmed) { warmed = true; for (int i = 0; i < prewarm; i++) Spawn(Random.Range(-7f, 7f)); }
    }

    void Update()
    {
        var gs = GameState.I;
        if (!alwaysRun && gs != null && (!gs.Alive || !gs.Started)) return;
        float dt = Time.deltaTime;
        timer -= dt;
        if (timer <= 0f)
        {
            timer = Random.Range(interval.x, interval.y);
            if (items.Count < max) Spawn(twinkle > 0f ? Random.Range(-7.5f, 7.5f) : float.NaN);
        }
        for (int i = items.Count - 1; i >= 0; i--)
        {
            var it = items[i];
            it.age += dt;
            var p = it.t.localPosition;
            p.x -= it.speed * dt;
            p.y = it.y0 + (bobAmp > 0f ? Mathf.Sin((it.age + it.phase) * bobFreq * Mathf.PI * 2f) * bobAmp : 0f);
            it.t.localPosition = p;
            if (twinkle > 0f)
            {
                var c = it.r.color; c.a = it.alpha * Mathf.Sin(Mathf.Clamp01(it.age / twinkle) * Mathf.PI); it.r.color = c;
            }
            bool gone = twinkle > 0f ? it.age >= twinkle : p.x < -8f - it.half;
            if (gone) { Destroy(it.t.gameObject); items.RemoveAt(i); }
        }
    }

    void Spawn(float x)
    {
        if (sprites == null || sprites.Length == 0) return;
        var go = new GameObject("Amb");
        go.transform.SetParent(transform, false);
        var r = go.AddComponent<SpriteRenderer>();
        r.sortingOrder = sortingOrder;
        if (animated)
        {
            var a = go.AddComponent<SpriteAnimator>();
            a.frames = sprites; a.fps = fps; a.phase = Random.value * sprites.Length;
            r.sprite = sprites[0];
        }
        else r.sprite = sprites[Random.Range(0, sprites.Length)];
        float s = Random.Range(scaleRange.x, scaleRange.y);
        go.transform.localScale = new Vector3(randomFlip && Random.value < 0.5f ? -s : s, s, 1f);
        var it = new Item
        {
            t = go.transform, r = r,
            speed = Random.Range(speedRange.x, speedRange.y),
            y0 = Random.Range(yRange.x, yRange.y),
            phase = Random.value * 10f,
            alpha = Random.Range(alphaRange.x, alphaRange.y),
            half = r.sprite.bounds.extents.x * s,
        };
        var c = tint; c.a *= twinkle > 0f ? 0f : it.alpha; r.color = c;
        it.alpha *= tint.a;
        go.transform.localPosition = new Vector3(float.IsNaN(x) ? 8f + it.half : x, it.y0, 0f);
        items.Add(it);
    }
}
