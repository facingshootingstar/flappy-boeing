using UnityEngine;

/// <summary>World-space boss health bar along the top of the screen, with tick marks at the phase thresholds.
/// Visible only while a boss is fighting. Damage leaves a lagging pale "ghost" segment.</summary>
public class BossBar : MonoBehaviour
{
    public float width = 6f, y = 3.92f;
    Transform fill, ghost, root;
    Transform[] ticks = new Transform[0];
    SpriteRenderer fillR;
    Sprite spr;
    Health shown;
    float ghostFrac = 1f, flash;

    static readonly Color FillColor = new Color32(227, 106, 47, 255);

    void Awake()
    {
        var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        var px = new Color[16]; for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        spr = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4f);   // 1 unit wide, left pivot
        root = new GameObject("BossBarRoot").transform;
        root.SetParent(transform, false);
        root.localPosition = new Vector3(-width / 2f, y, 0);
        Make("Border", new Color32(42, 33, 28, 255), 100, new Vector3(-0.08f, 0, 0), new Vector3(width + 0.16f, 0.36f, 1));
        Make("Back", new Color32(92, 62, 52, 255), 101, Vector3.zero, new Vector3(width, 0.2f, 1));
        ghost = Make("Ghost", new Color32(243, 224, 196, 255), 102, Vector3.zero, new Vector3(width, 0.2f, 1));
        fill = Make("Fill", FillColor, 103, Vector3.zero, new Vector3(width, 0.2f, 1));
        fillR = fill.GetComponent<SpriteRenderer>();
        root.gameObject.SetActive(false);
    }

    Transform Make(string n, Color c, int order, Vector3 pos, Vector3 scale)
    {
        var go = new GameObject(n);
        go.transform.SetParent(root, false);
        go.transform.localPosition = pos; go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = spr; sr.color = c; sr.sortingOrder = order;
        return go.transform;
    }

    void Rebind(Health b)
    {
        shown = b;
        foreach (var t in ticks) if (t) Destroy(t.gameObject);
        var th = b != null ? b.phaseThresholds : new float[0];
        ticks = new Transform[th.Length];
        for (int i = 0; i < th.Length; i++)
            ticks[i] = Make("Tick", new Color32(42, 33, 28, 255), 104, new Vector3(width * th[i] - 0.03f, 0, 0), new Vector3(0.06f, 0.36f, 1));
        ghostFrac = 1f;
        if (b != null) b.Damaged += OnDamaged;
    }

    void OnDamaged(float dmg) { flash = 0.06f; }

    void Update()
    {
        var b = Health.Boss;
        var boss = b ? b.GetComponent<BossBase>() : null;
        bool show = b != null && b.gameObject.activeInHierarchy && !b.Dead && boss != null && boss.Fighting;
        if (b != shown)
        {
            if (shown != null) shown.Damaged -= OnDamaged;
            Rebind(b);
        }
        if (root.gameObject.activeSelf != show) root.gameObject.SetActive(show);
        if (!show) return;
        float f = b.Fraction;
        ghostFrac = Mathf.Max(f, Mathf.MoveTowards(ghostFrac, f, Time.deltaTime * 0.25f));
        fill.localScale = new Vector3(width * f, 0.2f, 1);
        ghost.localScale = new Vector3(width * ghostFrac, 0.2f, 1);
        flash -= Time.deltaTime;
        fillR.color = flash > 0f ? Color.white : FillColor;
        for (int i = 0; i < ticks.Length; i++)
            ticks[i].GetComponent<SpriteRenderer>().color = b.Phase > i ? new Color32(243, 224, 196, 255) : new Color32(42, 33, 28, 255);
    }

    void OnDestroy() { if (shown != null) shown.Damaged -= OnDamaged; }
}
