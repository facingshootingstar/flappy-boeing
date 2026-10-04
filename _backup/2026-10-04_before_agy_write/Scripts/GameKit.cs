using System.Collections.Generic;
using UnityEngine;

/// <summary>Shared UI/FX sprites (filled by the scene builder) and a pool of one-shot animated effects.</summary>
public class GameKit : MonoBehaviour
{
    public static GameKit I;

    [Header("UI")]
    public Sprite[] font;
    public Sprite white, plaque, heart, warningBar;
    public Sprite[] cards;              // 0 = back, 1 = face

    [Header("FX")]
    public Sprite[] explosion, hitSpark, smoke, koStars;
    public Sprite muzzle, parryStar, splash, windStreak, telegraph;

    readonly Stack<OneShotFx> free = new Stack<OneShotFx>();
    Transform root;

    void Awake() { I = this; root = new GameObject("FxPool").transform; }
    void OnDestroy() { if (I == this) I = null; }

    /// <summary>Play frames once at a world position.</summary>
    public static OneShotFx Burst(Sprite[] frames, Vector3 pos, float fps = 18f, float scale = 1f, int order = 20, Color? tint = null, float rot = 0f)
    {
        if (I == null || frames == null || frames.Length == 0) return null;
        var fx = I.free.Count > 0 ? I.free.Pop() : I.Make();
        fx.transform.position = new Vector3(pos.x, pos.y, 0f);
        fx.transform.localScale = Vector3.one * scale;
        fx.transform.rotation = Quaternion.Euler(0, 0, rot);
        fx.gameObject.SetActive(true);
        fx.Play(frames, fps, order, tint ?? Color.white);
        return fx;
    }

    /// <summary>Single sprite that fades and grows over its life.</summary>
    public static OneShotFx Pop(Sprite sprite, Vector3 pos, float life = 0.25f, float scale = 1f, float grow = 0.6f, int order = 20, Color? tint = null, float rot = 0f)
    {
        if (sprite == null) return null;
        var fx = Burst(new[] { sprite }, pos, 1f / life, scale, order, tint, rot);
        if (fx) { fx.grow = grow; fx.fade = true; }
        return fx;
    }

    OneShotFx Make()
    {
        var go = new GameObject("fx");
        go.transform.SetParent(root, false);
        go.AddComponent<SpriteRenderer>();
        return go.AddComponent<OneShotFx>();
    }

    internal void Release(OneShotFx fx) { fx.gameObject.SetActive(false); free.Push(fx); }
}

public class OneShotFx : MonoBehaviour
{
    public Vector2 velocity;
    public float grow;
    public bool fade;
    SpriteRenderer sr;
    Sprite[] frames;
    float fps, t, baseScale;
    Color tint;

    public void Play(Sprite[] f, float rate, int order, Color c)
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        frames = f; fps = rate; t = 0f; tint = c; grow = 0f; fade = false; velocity = Vector2.zero;
        baseScale = transform.localScale.x;
        sr.sprite = f[0]; sr.sortingOrder = order; sr.color = c;
    }

    void Update()
    {
        t += Time.deltaTime;
        int i = (int)(t * fps);
        if (i >= frames.Length) { GameKit.I?.Release(this); if (GameKit.I == null) Destroy(gameObject); return; }
        sr.sprite = frames[i];
        if (velocity != Vector2.zero) transform.position += (Vector3)(velocity * Time.deltaTime);
        if (grow != 0f || fade)
        {
            float k = t * fps / frames.Length;
            transform.localScale = Vector3.one * baseScale * (1f + grow * k);
            var c = tint; c.a *= 1f - k; sr.color = c;
        }
    }
}
