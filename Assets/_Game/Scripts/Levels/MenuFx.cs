using System.Collections.Generic;
using UnityEngine;

/// <summary>Code-only motion for the login screen and the front menu: easing, staggered intro, button feel, panel pop, sky.</summary>
public static class Ease
{
    public static float OutCubic(float t) { t = 1f - Mathf.Clamp01(t); return 1f - t * t * t; }
    public static float OutBack(float t, float s = 1.70158f) { t = Mathf.Clamp01(t) - 1f; return 1f + (s + 1f) * t * t * t + s * t * t; }
    public static float OutBounce(float t)
    {
        t = Mathf.Clamp01(t);
        if (t < 1f / 2.75f) return 7.5625f * t * t;
        if (t < 2f / 2.75f) { t -= 1.5f / 2.75f; return 7.5625f * t * t + 0.75f; }
        if (t < 2.5f / 2.75f) { t -= 2.25f / 2.75f; return 7.5625f * t * t + 0.9375f; }
        t -= 2.625f / 2.75f; return 7.5625f * t * t + 0.984375f;
    }
}

/// <summary>Elements start off-screen (offset) and ease home one after another.</summary>
public class MenuIntro
{
    class Item { public Transform[] ts; public Vector3[] home; public Vector2 off; public float delay, dur; public bool bounce; }
    readonly List<Item> items = new List<Item>();
    readonly float t0 = Time.unscaledTime;

    public float Elapsed => Time.unscaledTime - t0;
    public float EndTime { get; private set; }

    public void Add(Transform t, Vector2 off, float delay, float dur, bool bounce = false) => Add(new[] { t }, off, delay, dur, bounce);

    public void Add(Ui.Button b, Vector2 off, float delay, float dur = 0.5f) => Add(new[] { b.plaque.transform, b.label.transform }, off, delay, dur, false);

    void Add(Transform[] ts, Vector2 off, float delay, float dur, bool bounce)
    {
        var it = new Item { ts = ts, off = off, delay = delay, dur = dur, bounce = bounce, home = new Vector3[ts.Length] };
        for (int i = 0; i < ts.Length; i++) { it.home[i] = ts[i].localPosition; ts[i].localPosition = it.home[i] + (Vector3)off; }
        items.Add(it);
        EndTime = Mathf.Max(EndTime, delay + dur);
    }

    public void Tick()
    {
        float now = Elapsed;
        foreach (var it in items)
        {
            float k = Mathf.Clamp01((now - it.delay) / it.dur);
            float e = it.bounce ? Ease.OutBounce(k) : Ease.OutBack(k, 1.2f);
            for (int i = 0; i < it.ts.Length; i++)
                if (it.ts[i]) it.ts[i].localPosition = it.home[i] + (Vector3)(it.off * (1f - e));
        }
    }
}

/// <summary>Hover / press feel: the plaque and label swell when pointed at or selected and squeeze when pressed.</summary>
public class BtnFx
{
    readonly Ui.Button b;
    readonly Vector3 plaqueBase, labelBase;
    float cur = 1f;

    public BtnFx(Ui.Button b)
    {
        this.b = b;
        plaqueBase = b.plaque.transform.localScale;
        labelBase = b.label.transform.localScale;
    }

    public void Tick(Vector2 pointer, bool held, bool selected)
    {
        bool over = b.Hit(pointer);
        float target = over && held ? 0.93f : (over || selected) ? 1.07f : 1f;
        cur = Mathf.Lerp(cur, target, 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime));
        b.plaque.transform.localScale = plaqueBase * cur;
        b.label.transform.localScale = labelBase * cur;
    }

    public static void TickAll(List<BtnFx> list, int selected = -1)
    {
        bool held = UiInput.Held(out var p);
        for (int i = 0; i < list.Count; i++) list[i].Tick(p, held, i == selected);
    }
}

/// <summary>Panel scale-pop: grows from 60% with an overshoot, then removes itself. The Dim backdrop is kept at full size.</summary>
public class PanelPop : MonoBehaviour
{
    const float Dur = 0.26f;
    float t0;
    Transform dim;
    Vector3 dimBase;

    public static void On(Transform panel)
    {
        if (panel && !panel.GetComponent<PanelPop>()) panel.gameObject.AddComponent<PanelPop>();
    }

    void Awake()
    {
        t0 = Time.unscaledTime;
        dim = transform.Find("Dim");
        if (dim) dimBase = dim.localScale;
        Apply(0f);
    }

    void Update()
    {
        float k = (Time.unscaledTime - t0) / Dur;
        Apply(k);
        if (k >= 1f) Destroy(this);
    }

    void Apply(float k)
    {
        float s = Mathf.LerpUnclamped(0.6f, 1f, Ease.OutBack(k, 2f));
        s = Mathf.Max(0.05f, s);
        transform.localScale = Vector3.one * s;
        if (dim) dim.localScale = dimBase / s;
    }

    void OnDestroy()
    {
        transform.localScale = Vector3.one;
        if (dim) dim.localScale = dimBase;
    }
}

/// <summary>Drifting pixel clouds (parallax by size) and the Poe plane gliding back and forth on a sine path.</summary>
public class MenuSky : MonoBehaviour
{
    class Cloud { public Transform t; public float speed; }
    readonly List<Cloud> clouds = new List<Cloud>();
    Transform plane;
    SpriteRenderer planeSr;
    float planeScale, planeX = -10f, planeDir = 1f, planeY;
    const float Edge = 10.5f;

    static readonly Vector3[] Spots = {   // x, y, size
        new Vector3(-7f, 3.6f, 1.0f), new Vector3(-1.5f, 4.1f, 0.7f), new Vector3(5.5f, 3.2f, 1.2f),
        new Vector3(-4f, -3.1f, 0.8f), new Vector3(2.5f, -3.7f, 1.1f), new Vector3(8f, -2.4f, 0.65f), new Vector3(0.5f, 0.2f, 0.5f)
    };

    public static MenuSky Build(Transform parent, int order, float cloudAlpha, float planeHeight, float planeY, bool withPlane = true)
    {
        var go = Ui.Root("Sky", parent);
        var sky = go.gameObject.AddComponent<MenuSky>();
        foreach (var s in Spots) sky.AddCloud(go, new Vector2(s.x, s.y), s.z, order, cloudAlpha);
        var fly = LevelRuntime.Fly;
        if (withPlane && fly != null && fly.Length > 0 && fly[0])
        {
            var r = Ui.Spr("Plane", go, fly[0], new Vector2(-Edge, planeY), order + 2);
            sky.plane = r.transform; sky.planeSr = r; sky.planeY = planeY;
            sky.planeScale = planeHeight / Mathf.Max(0.01f, fly[0].bounds.size.y);
        }
        return sky;
    }

    void AddCloud(Transform parent, Vector2 pos, float size, int order, float alpha)
    {
        var c = Ui.Root("Cloud", parent, pos);
        var col = new Color(1f, 1f, 1f, alpha);
        Ui.Box("a", c, new Vector2(0f, 0f), new Vector2(3.4f, 0.55f), col, order);
        Ui.Box("b", c, new Vector2(-0.3f, 0.45f), new Vector2(2.3f, 0.45f), col, order);
        Ui.Box("c", c, new Vector2(0.2f, 0.8f), new Vector2(1.2f, 0.35f), col, order);
        c.localScale = Vector3.one * size;
        clouds.Add(new Cloud { t = c, speed = 0.18f + 0.55f * size });
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        foreach (var c in clouds)
        {
            var p = c.t.localPosition;
            p.x += c.speed * dt;
            if (p.x > Edge) p.x = -Edge;
            c.t.localPosition = p;
        }
        if (!plane) return;
        planeX += planeDir * 2.4f * dt;
        if (Mathf.Abs(planeX) > Edge) { planeDir = -planeDir; planeX = Mathf.Sign(planeX) * Edge; }
        float y = planeY + Mathf.Sin(planeX * 0.65f) * 0.55f;
        float slope = Mathf.Cos(planeX * 0.65f) * 0.55f * 0.65f * planeDir;
        plane.localPosition = new Vector3(planeX, y, 0f);
        plane.localScale = new Vector3(planeScale * planeDir, planeScale, 1f);
        plane.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan(slope) * Mathf.Rad2Deg * planeDir * 0.6f);
        var fly = LevelRuntime.Fly;
        planeSr.sprite = fly[(int)(Time.unscaledTime * 8f) % fly.Length] ?? planeSr.sprite;
    }
}
