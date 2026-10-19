using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>World-space UI building blocks (ortho camera, 32 PPU) shared by the level map, result screen and mini games.</summary>
public static class Ui
{
    public static readonly Color Ink = new Color32(42, 33, 28, 255);
    static Sprite pixel, disc;

    public static Sprite Pixel
    {
        get
        {
            if (pixel) return pixel;
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return pixel;
        }
    }

    /// <summary>1-unit pixel-art disc: ink outline, white body (tint it), soft top highlight.</summary>
    public static Sprite Disc
    {
        get
        {
            if (disc) return disc;
            const int n = 32;
            var tex = new Texture2D(n, n) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var c = new Vector2(n / 2f - 0.5f, n / 2f - 0.5f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    Color col = Color.clear;
                    if (d < n / 2f) col = new Color(0.22f, 0.17f, 0.15f);                 // outline (stays dark under tint)
                    if (d < n / 2f - 2.2f) col = new Color(0.82f, 0.82f, 0.82f);         // body
                    if (d < n / 2f - 2.2f && y > n * 0.55f && d > n / 2f - 6f) col = Color.white; // rim light
                    tex.SetPixel(x, y, col);
                }
            tex.Apply();
            disc = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
            return disc;
        }
    }

    public static Transform Root(string name, Transform parent = null, Vector2? pos = null)
    {
        var t = new GameObject(name).transform;
        if (parent) t.SetParent(parent, false);
        t.localPosition = pos ?? Vector2.zero;
        return t;
    }

    public static SpriteRenderer Box(string name, Transform parent, Vector2 pos, Vector2 size, Color c, int order)
    {
        var r = Spr(name, parent, Pixel, pos, order);
        r.transform.localScale = new Vector3(size.x, size.y, 1f);
        r.color = c;
        return r;
    }

    public static SpriteRenderer Spr(string name, Transform parent, Sprite s, Vector2 pos, int order, float scale = 1f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * scale;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = s; r.sortingOrder = order;
        return r;
    }

    public static SpriteRenderer Plaque(Transform parent, Vector2 pos, Vector2 size, int order)
    {
        var r = Spr("Plaque", parent, GameKit.I ? GameKit.I.plaque : Pixel, pos, order);
        if (GameKit.I) { r.drawMode = SpriteDrawMode.Sliced; r.size = size; }
        else { r.transform.localScale = new Vector3(size.x, size.y, 1f); r.color = Ink; }
        return r;
    }

    public static PixelText Txt(Transform parent, string s, Vector2 pos, float scale, Color c, int order,
        PixelText.Align align = PixelText.Align.Center, PixelText.Effect fx = PixelText.Effect.None)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * scale;
        var t = go.AddComponent<PixelText>();
        t.sortingOrder = order; t.align = align; t.effect = fx; t.color = c;
        t.Text = s;
        t.Rebuild();
        return t;
    }

    /// <summary>Plaque button with a label; hit-test it with <see cref="Button.Hit"/>.</summary>
    public static Button Btn(Transform parent, string label, Vector2 pos, Vector2 size, Color c, int order)
    {
        var b = new Button { center = (Vector2)parent.TransformPoint(pos), size = size };
        b.plaque = Plaque(parent, pos, size, order);
        b.label = Txt(parent, label, pos + new Vector2(0f, 0.02f), 1.3f, c, order + 1);
        return b;
    }

    public class Button
    {
        public Vector2 center, size;
        public SpriteRenderer plaque;
        public PixelText label;
        public bool Hit(Vector2 world) => Mathf.Abs(world.x - center.x) <= size.x / 2f && Mathf.Abs(world.y - center.y) <= size.y / 2f;
    }

    public static Color Grey(Color c, float k = 0.35f)
    {
        float g = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
        return Color.Lerp(new Color(g, g, g, c.a), c, k) * new Color(0.75f, 0.75f, 0.75f, 1f);
    }
}

/// <summary>Pointer + menu keys for UI screens (either input backend). Tests inject clicks in world space.</summary>
public static class UiInput
{
    public static Vector2? InjectedClick;
    static int injectedFrame = -1;
    static Vector2 injectedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { InjectedClick = null; injectedFrame = -1; }

    static void PumpInjected()
    {
        if (InjectedClick.HasValue) { injectedAt = InjectedClick.Value; injectedFrame = Time.frameCount; InjectedClick = null; }
    }

    static Vector2 ToWorld(Vector2 screen)
    {
        var cam = Camera.main;
        return cam ? (Vector2)cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z)) : Vector2.zero;
    }

    public static Vector2 Position
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            var t = Touchscreen.current;
            if (t != null && t.primaryTouch.press.isPressed) return ToWorld(t.primaryTouch.position.ReadValue());
            var m = Mouse.current;
            return m != null ? ToWorld(m.position.ReadValue()) : Vector2.zero;
#else
            return ToWorld(Input.mousePosition);
#endif
        }
    }

    public static bool Down(out Vector2 world)
    {
        PumpInjected();
        if (injectedFrame == Time.frameCount) { world = injectedAt; return true; }
        world = Position;
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current; var t = Touchscreen.current;
        return (m != null && m.leftButton.wasPressedThisFrame) || (t != null && t.primaryTouch.press.wasPressedThisFrame);
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    public static bool Held(out Vector2 world)
    {
        world = Position;
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current; var t = Touchscreen.current;
        return (m != null && m.leftButton.isPressed) || (t != null && t.primaryTouch.press.isPressed);
#else
        return Input.GetMouseButton(0);
#endif
    }

    /// <summary>Release this frame; an injected click counts as press + release on the same frame.</summary>
    public static bool Up(out Vector2 world)
    {
        PumpInjected();
        if (injectedFrame == Time.frameCount) { world = injectedAt; return true; }
        world = Position;
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current; var t = Touchscreen.current;
        return (m != null && m.leftButton.wasReleasedThisFrame) || (t != null && t.primaryTouch.press.wasReleasedThisFrame);
#else
        return Input.GetMouseButtonUp(0);
#endif
    }

    public static float Wheel()
    {
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current; return m != null ? m.scroll.ReadValue().y / 120f : 0f;
#else
        return Input.mouseScrollDelta.y;
#endif
    }

#if ENABLE_INPUT_SYSTEM
    static bool K(Key a, Key b = Key.None)
    {
        var k = Keyboard.current;
        return k != null && (k[a].wasPressedThisFrame || (b != Key.None && k[b].wasPressedThisFrame));
    }
    public static bool LeftDown() => K(Key.LeftArrow, Key.A);
    public static bool RightDown() => K(Key.RightArrow, Key.D);
    public static bool Confirm() => K(Key.Enter, Key.Space);
    public static bool Back() => K(Key.Escape, Key.Backspace);
    public static bool RetryKey() => K(Key.R);
    public static bool NextKey() => K(Key.N);
#else
    public static bool LeftDown() => Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
    public static bool RightDown() => Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
    public static bool Confirm() => Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
    public static bool Back() => Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
    public static bool RetryKey() => Input.GetKeyDown(KeyCode.R);
    public static bool NextKey() => Input.GetKeyDown(KeyCode.N);
#endif
}
