using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Loads the hand-painted props in Assets/_Game/Art. Point-filtered. Falls back to a disc if a file is missing.</summary>
public static class PropArt
{
    static Sprite chest, chute, gate, tanker, bolt;
    static readonly Dictionary<string, Sprite> imagen = new Dictionary<string, Sprite>();

    /// <summary>Imagine PNG in Art/imagen. worldSize is the sprite height, or the width when wide. Null if the file is missing.</summary>
    public static Sprite Imagen(string name, float worldSize, bool wide = false)
    {
        string key = name + (wide ? "w" : "h") + worldSize.ToString("0.##");
        if (imagen.TryGetValue(key, out var cached)) return cached;
        string path = Path.Combine(Application.dataPath, "_Game", "Art", "imagen", name + ".png");
        if (!File.Exists(path)) return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        if (!tex.LoadImage(File.ReadAllBytes(path))) return null;
        float ppu = (wide ? tex.width : tex.height) / Mathf.Max(0.01f, worldSize);
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
        imagen[key] = sprite;
        return sprite;
    }

    public static Sprite Chest => chest ? chest : (chest = Load("props/chest.png"));
    public static Sprite Chute => chute ? chute : (chute = Load("props/crate_chute.png"));
    public static Sprite Gate => gate ? gate : (gate = Load("props/gate.png"));
    public static Sprite Tanker => tanker ? tanker : (tanker = Load("props/tanker.png"));
    public static Sprite Bolt => bolt ? bolt : (bolt = Load("fx/lightning_bolt.png"));

    /// <summary>Hand-painted PNG under Art/. worldSize is the height, or the width when wide. Null if the file is missing.</summary>
    public static Sprite Paint(string rel, float worldSize, bool wide = false)
    {
        string key = "p" + rel + (wide ? "w" : "h") + worldSize.ToString("0.##");
        if (imagen.TryGetValue(key, out var cached)) return cached;
        string path = Path.Combine(Application.dataPath, "_Game", "Art", rel.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        if (!tex.LoadImage(File.ReadAllBytes(path))) return null;
        float ppu = (wide ? tex.width : tex.height) / Mathf.Max(0.01f, worldSize);
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu);
        imagen[key] = sprite;
        return sprite;
    }

    /// <summary>poe_fly_0.. or a single poe_climb / poe_dive / poe_hit. Null if any frame is missing.</summary>
    public static Sprite[] PoeSet(string kind, int count, float height)
    {
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            string rel = count == 1 ? "player/poe_" + kind + ".png" : "player/poe_" + kind + "_" + i + ".png";
            frames[i] = Paint(rel, height);
            if (!frames[i]) return null;
        }
        return frames;
    }

    /// <summary>Repeat a strip so its top edge sits on <paramref name="top"/>.</summary>
    public static void TileStrip(Transform parent, Sprite s, float x, float width, float top, int order, string name)
    {
        if (!s || width <= 0.05f) return;
        float tile = Mathf.Max(0.25f, s.bounds.size.x);
        int n = Mathf.Max(1, Mathf.CeilToInt(width / tile));
        float step = width / n;
        float y = top - s.bounds.extents.y;
        for (int i = 0; i < n; i++)
        {
            var sr = Ui.Spr(name, parent, s, new Vector2(x + step * (i + 0.5f), y), order);
            sr.transform.localScale = new Vector3(step / tile, 1f, 1f);
        }
    }

    static Sprite Load(string rel)
    {
        string path = Path.Combine(Application.dataPath, "_Game", "Art", rel.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) return Ui.Disc;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        if (!tex.LoadImage(File.ReadAllBytes(path))) return Ui.Disc;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
    }
}
