using System.IO;
using UnityEngine;

/// <summary>Loads the hand-painted props in Assets/_Game/Art. Point-filtered. Falls back to a disc if a file is missing.</summary>
public static class PropArt
{
    static Sprite chest, chute, gate, tanker, bolt;

    public static Sprite Chest => chest ? chest : (chest = Load("props/chest.png"));
    public static Sprite Chute => chute ? chute : (chute = Load("props/crate_chute.png"));
    public static Sprite Gate => gate ? gate : (gate = Load("props/gate.png"));
    public static Sprite Tanker => tanker ? tanker : (tanker = Load("props/tanker.png"));
    public static Sprite Bolt => bolt ? bolt : (bolt = Load("fx/lightning_bolt.png"));

    static Sprite Load(string rel)
    {
        string path = Path.Combine(Application.dataPath, "_Game", "Art", rel.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) return Ui.Disc;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        if (!tex.LoadImage(File.ReadAllBytes(path))) return Ui.Disc;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
    }
}
