using System.Collections.Generic;
using UnityEngine;

/// <summary>Parallax strip: copies of the sprite tiled side by side (and stacked when falling), scrolling left / down and wrapping.
/// Sprite must tile on X (and on Y when fall > 0). Children (e.g. a lighthouse beam) are cloned onto every copy.</summary>
public class Scroller : MonoBehaviour
{
    public float speed = 1f;      // leftward, units/s
    public float fall = 0f;       // downward, units/s (rain)
    public bool alwaysRun;        // keep moving before the run starts and after death (weather)
    Vector2 size, origin;
    int nx, ny;
    readonly List<Transform> tiles = new List<Transform>();

    void Start()
    {
        var sr = GetComponent<SpriteRenderer>();
        size = Vector2.Scale(sr.sprite.bounds.size, transform.lossyScale);
        origin = transform.position;
        nx = speed != 0f ? 2 : 1;
        ny = fall != 0f ? 2 : 1;
        tiles.Add(transform);
        for (int ix = 0; ix < nx; ix++)
            for (int iy = 0; iy < ny; iy++)
            {
                if (ix == 0 && iy == 0) continue;
                var go = new GameObject(name + "_twin");
                go.transform.SetParent(transform.parent, false);
                go.transform.position = transform.position + new Vector3(size.x * ix, size.y * iy, 0);
                go.transform.localScale = transform.localScale;
                var t = go.AddComponent<SpriteRenderer>();
                t.sprite = sr.sprite; t.sortingOrder = sr.sortingOrder; t.sortingLayerID = sr.sortingLayerID; t.color = sr.color;
                go.AddComponent<TwinSync>().Source = sr;
                foreach (Transform c in transform) Instantiate(c.gameObject, go.transform, false);
                tiles.Add(go.transform);
            }
    }

    void Update()
    {
        var gs = GameState.I;
        if (!alwaysRun && gs != null && (!gs.Alive || !gs.Started)) return;
        var d = new Vector3(-speed, -fall, 0f) * Time.deltaTime;
        foreach (var t in tiles)
        {
            var p = t.position + d;
            if (nx > 1 && p.x <= origin.x - size.x) p.x += size.x * nx;
            if (ny > 1 && p.y <= origin.y - size.y) p.y += size.y * ny;
            t.position = p;
        }
    }
}

/// <summary>Keeps a scroller's twin showing the same sprite (animated layers such as the sea swell change the source sprite).</summary>
public class TwinSync : MonoBehaviour
{
    public SpriteRenderer Source;
    SpriteRenderer me;
    void Awake() { me = GetComponent<SpriteRenderer>(); }
    void LateUpdate() { if (Source != null) { me.sprite = Source.sprite; me.color = Source.color; } }
}
