using System.Collections;
using UnityEngine;

/// <summary>Cuphead-style call-outs: READY? / WALLOP!, A KNOCKOUT!, banners and the controls card. All timing unscaled.</summary>
public class Announcer : MonoBehaviour
{
    public static Announcer I;

    public static readonly Color Cream = new Color32(243, 224, 196, 255);
    public static readonly Color Yellow = new Color32(246, 193, 91, 255);
    public static readonly Color Red = new Color32(216, 68, 58, 255);
    public static readonly Color Pink = new Color32(240, 122, 168, 255);
    public static readonly Color Teal = new Color32(126, 208, 200, 255);

    PixelText big, sub;
    SpriteRenderer card;
    PixelText[] cardLines;
    Coroutine cardRoutine;

    void Awake()
    {
        I = this;
        big = MakeText("Big", new Vector3(0, 0.6f, 0), 3, 130);
        sub = MakeText("Sub", new Vector3(0, -0.5f, 0), 1, 130);
        var cardGo = new GameObject("Card");
        cardGo.transform.SetParent(transform, false);
        card = cardGo.AddComponent<SpriteRenderer>();
        card.sortingOrder = 125;
        cardLines = new PixelText[6];
        for (int i = 0; i < cardLines.Length; i++)
        {
            cardLines[i] = MakeText("Line" + i, Vector3.zero, 1, 126);
            cardLines[i].transform.SetParent(cardGo.transform, false);
            cardLines[i].effect = PixelText.Effect.None;
            cardLines[i].align = PixelText.Align.Left;
        }
        HideAll();
    }

    void OnDestroy() { if (I == this) I = null; }

    PixelText MakeText(string name, Vector3 pos, int scale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * scale;
        var t = go.AddComponent<PixelText>();
        t.sortingOrder = order;
        return t;
    }

    void HideAll()
    {
        big.gameObject.SetActive(false);
        sub.gameObject.SetActive(false);
        card.gameObject.SetActive(false);
    }

    void Show(PixelText t, string s, Color c, float scale, PixelText.Effect fx = PixelText.Effect.Boil)
    {
        t.gameObject.SetActive(true);
        t.effect = fx;
        t.color = c;
        t.transform.localScale = Vector3.one * scale;
        t.Text = s;
        t.Rebuild();
    }

    static IEnumerator Wait(float s) { float t = 0f; while (t < s) { t += Time.unscaledDeltaTime; yield return null; } }

    IEnumerator Pop(Transform tr, float to, float from, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            tr.localScale = Vector3.one * Mathf.Round(Mathf.Lerp(from, to, 1f - (1f - k) * (1f - k)) * 4f) / 4f;
            yield return null;
        }
        tr.localScale = Vector3.one * to;
    }

    // ---------------------------------------------------------------- call-outs
    public IEnumerator ReadyWallop(string bossName)
    {
        Show(big, "READY?", Cream, 3);
        if (!string.IsNullOrEmpty(bossName)) Show(sub, bossName, Yellow, 1, PixelText.Effect.Wave);
        AudioManager.Sfx("ready", 0.9f, 0f);
        yield return Wait(1.15f);
        sub.gameObject.SetActive(false);
        Show(big, "WALLOP!", Yellow, 4);
        AudioManager.Sfx("wallop", 1f, 0f);
        CameraFx.Shake(0.12f, 0.3f);
        yield return Pop(big.transform, 4f, 6f, 0.14f);
        yield return Wait(0.7f);
        big.gameObject.SetActive(false);
    }

    public IEnumerator Knockout()
    {
        CameraFx.SlowMo(0.2f, 1.6f);
        CameraFx.Shake(0.25f, 0.6f);
        FilmFilter.Flash(0.9f, 0.3f);
        AudioManager.Sfx("knockout", 1f, 0f);
        Show(big, "A KNOCKOUT!", Cream, 3);
        yield return Pop(big.transform, 3f, 5f, 0.16f);
        yield return Wait(1.9f);
        big.gameObject.SetActive(false);
    }

    /// <summary>Big centred text with an optional subtitle; returns when it hides (dur &lt;= 0 leaves it up).</summary>
    public IEnumerator Banner(string text, Color c, float dur, int scale = 3, string subtitle = null)
    {
        Show(big, text, c, scale);
        if (!string.IsNullOrEmpty(subtitle)) Show(sub, subtitle, Cream, 1, PixelText.Effect.None);
        yield return Pop(big.transform, scale, scale + 2, 0.14f);
        if (dur <= 0f) yield break;
        yield return Wait(dur);
        big.gameObject.SetActive(false);
        sub.gameObject.SetActive(false);
    }

    public void Hide() { StopAllCoroutines(); cardRoutine = null; HideAll(); }

    // ---------------------------------------------------------------- card
    /// <summary>Plaque with left-aligned lines in the lower part of the screen.</summary>
    public void ShowCard(string[] lines, float dur, Vector3 pos)
    {
        if (cardRoutine != null) StopCoroutine(cardRoutine);
        cardRoutine = StartCoroutine(Card(lines, dur, pos));
    }

    IEnumerator Card(string[] lines, float dur, Vector3 pos)
    {
        var kit = GameKit.I;
        card.sprite = kit ? kit.plaque : null;
        card.drawMode = SpriteDrawMode.Sliced;
        int n = Mathf.Min(lines.Length, cardLines.Length);
        float lineH = 12f / 32f, w = 0f;
        for (int i = 0; i < cardLines.Length; i++)
        {
            bool on = i < n;
            cardLines[i].gameObject.SetActive(on);
            if (!on) continue;
            cardLines[i].color = i == 0 ? Red : Color.white;
            cardLines[i].Text = lines[i];
            cardLines[i].Rebuild();
            w = Mathf.Max(w, cardLines[i].Width);
        }
        float pad = 9f / 32f;
        var size = new Vector2(w + pad * 2f, n * lineH + pad * 2f - 2f / 32f);
        card.size = size;
        for (int i = 0; i < n; i++)
            cardLines[i].transform.localPosition = new Vector3(-size.x / 2f + pad, size.y / 2f - pad - 5f / 32f - i * lineH, 0f);
        card.transform.localPosition = pos;
        card.gameObject.SetActive(true);
        AudioManager.Sfx("card", 0.7f);
        yield return Pop(card.transform, 1f, 0.5f, 0.12f);
        yield return Wait(dur);
        card.gameObject.SetActive(false);
        cardRoutine = null;
    }
}
