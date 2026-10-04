using System.Collections.Generic;
using UnityEngine;

/// <summary>Bitmap text built from pooled SpriteRenderers using the code-drawn font sheet (9x10 cells).</summary>
public class PixelText : MonoBehaviour
{
    public const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!?.:-+/'%";
    const float Px = 1f / 32f;

    public enum Align { Left, Center, Right }
    public enum Effect { None, Boil, Wave }

    public Sprite[] glyphs;
    [SerializeField] string text = "";
    public Color color = Color.white;
    public int sortingOrder = 110;
    public Align align = Align.Center;
    public Effect effect = Effect.Boil;
    public float advance = 8f;   // pixels
    public float space = 5f;     // pixels

    readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();
    readonly List<Vector3> basePos = new List<Vector3>();
    int used;
    float seed;

    public string Text { get => text; set { if (value != text) { text = value ?? ""; Rebuild(); } } }
    public float Width { get; private set; }

    void Awake() { seed = Random.value * 100f; }
    void OnEnable() { Rebuild(); }

    public void SetColor(Color c) { color = c; for (int i = 0; i < used; i++) pool[i].color = c; }

    public void Rebuild()
    {
        if (glyphs == null || glyphs.Length == 0) glyphs = GameKit.I ? GameKit.I.font : null;
        if (glyphs == null || glyphs.Length == 0) return;
        string s = text.ToUpperInvariant();

        float w = 0f;
        foreach (char ch in s) w += ch == ' ' ? space : advance;
        if (s.Length > 0 && s[s.Length - 1] != ' ') w += 1f;   // last glyph's outline + shadow
        Width = w * Px;
        float x = align == Align.Left ? 0f : align == Align.Center ? -Mathf.Round(w / 2f) : -w;

        used = 0; basePos.Clear();
        foreach (char ch in s)
        {
            int gi = Charset.IndexOf(ch);
            if (ch == ' ' || gi < 0 || gi >= glyphs.Length) { x += ch == ' ' ? space : advance; continue; }
            var sr = Get(used++);
            sr.sprite = glyphs[gi];
            sr.color = color;
            sr.sortingOrder = sortingOrder;
            var p = new Vector3((x + 4.5f) * Px, 0f, 0f);
            sr.transform.localPosition = p;
            basePos.Add(p);
            x += advance;
        }
        for (int i = used; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
    }

    SpriteRenderer Get(int i)
    {
        while (pool.Count <= i)
        {
            var go = new GameObject("g");
            go.transform.SetParent(transform, false);
            pool.Add(go.AddComponent<SpriteRenderer>());
        }
        pool[i].gameObject.SetActive(true);
        return pool[i];
    }

    void Update()
    {
        if (effect == Effect.None || used == 0) return;
        float t = Time.unscaledTime;
        for (int i = 0; i < used; i++)
        {
            var p = basePos[i];
            if (effect == Effect.Boil)
            {
                // hand-drawn line boil: each glyph jitters by a pixel at 8 fps
                int f = Mathf.FloorToInt(t * 8f);
                float h = Mathf.Sin((f * 12.9898f + i * 78.233f + seed) * 0.5f) * 43758.5453f;
                h -= Mathf.Floor(h);
                p.y += (h < 0.33f ? -1 : h > 0.66f ? 1 : 0) * Px;
            }
            else
            {
                p.y += Mathf.Round(Mathf.Sin(t * 6f + i * 0.6f) * 1.6f) * Px;
            }
            pool[i].transform.localPosition = p;
        }
    }
}
