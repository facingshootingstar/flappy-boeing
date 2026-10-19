using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Plays a <see cref="Cutscene"/> full screen: Ken Burns, typewriter caption, iris / fade / page slide.
/// Hold Space or a touch for 0.6s to skip. A tap finishes the line, or advances when the line is done.
/// MiniGame.Bot and SkipAll call onDone immediately so automated runs never sit in a cutscene.
/// </summary>
public class CutscenePlayer : MonoBehaviour
{
    public static CutscenePlayer I { get; private set; }
    public static bool SkipAll;
    public static bool IsPlaying => I != null;

    const float HoldSkip = 0.6f;
    const int Order = 230;

    static readonly Dictionary<string, Sprite> artCache = new Dictionary<string, Sprite>();

    string id;
    Action done;
    bool ended, restored, scratchSaved, titleShot, textDone, holding, armed, jump, abort;
    float prevScale = 1f, prevScratch, holdT, shakeUntil, shakeAmp, typeDur;
    int shown, total;
    string[] wrapped = new string[0];
    Coroutine beat;

    Transform artT, extra;
    SpriteRenderer frame, veil, plaque, ring;
    PixelText[] lines;
    PixelText hint;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { I = null; SkipAll = false; artCache.Clear(); }

    public static bool Seen(string id)
    {
        var list = SaveSystem.Data.seenCutscenes;
        return !string.IsNullOrEmpty(id) && list != null && list.Contains(id);
    }

    public static void Mark(string id)
    {
        if (string.IsNullOrEmpty(id) || MiniGame.Bot || SkipAll) return;
        var list = SaveSystem.Data.seenCutscenes;
        if (list == null) SaveSystem.Data.seenCutscenes = list = new List<string>();
        if (list.Contains(id)) return;
        list.Add(id);
        SaveSystem.Save();
    }

    /// <summary>ponytail: a second Play while one is up calls onDone now. Chain from the first callback if two must follow.</summary>
    public static void Play(string id, Action onDone)
    {
        if (MiniGame.Bot || SkipAll || !Application.isPlaying) { onDone?.Invoke(); return; }
        var cs = CutsceneDB.Get(id);
        if (cs == null || cs.shots == null || cs.shots.Length == 0) { onDone?.Invoke(); return; }
        if (I != null) { onDone?.Invoke(); return; }
        var go = new GameObject("Cutscene");
        go.AddComponent<CutscenePlayer>().Begin(cs, onDone);
    }

    public void PlayBeat(IEnumerator routine)
    {
        if (beat != null) StopCoroutine(beat);
        beat = StartCoroutine(routine);
    }

    public void Nudge(float amp, float dur) { shakeAmp = amp; shakeUntil = Time.unscaledTime + dur; }

    public void Banner(string text)
    {
        if (extra == null || string.IsNullOrEmpty(text)) return;
        Ui.Txt(extra, text, new Vector2(0f, 0.2f), 1.25f, Announcer.Yellow, Order + 16, PixelText.Align.Center, PixelText.Effect.Boil);
    }

    void Awake() { I = this; }

    void OnDestroy()
    {
        Restore();
        if (I == this) I = null;
    }

    void Begin(Cutscene cs, Action onDone)
    {
        id = cs.id;
        done = onDone;
        prevScale = CameraFx.BaseScale;
        CameraFx.BaseScale = 0f;
        CameraFx.ClearTime();
        if (FilmFilter.I) { prevScratch = FilmFilter.I.scratch; scratchSaved = true; }
        var cam = Camera.main;
        if (cam)
        {
            transform.SetParent(cam.transform, false);
            transform.localPosition = new Vector3(0f, 0f, -cam.transform.position.z);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
        Build();
        StartCoroutine(Run(cs));
    }

    void Build()
    {
        Ui.Box("Back", transform, Vector2.zero, new Vector2(20f, 12f), Ui.Ink, Order);
        artT = Ui.Root("Art", transform);
        frame = Ui.Spr("Frame", artT, null, Vector2.zero, Order + 2);
        veil = Ui.Box("Veil", transform, Vector2.zero, new Vector2(20f, 12f), new Color(0f, 0f, 0f, 0f), Order + 6);
        plaque = Ui.Plaque(transform, new Vector2(0f, -3.45f), new Vector2(14.2f, 1.35f), Order + 8);
        plaque.enabled = false;
        lines = new PixelText[3];
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i] = Ui.Txt(transform, "", Vector2.zero, 0.68f, Announcer.Cream, Order + 10);
            lines[i].gameObject.SetActive(false);
        }
        extra = Ui.Root("Extra", transform);
        hint = Ui.Txt(transform, "HOLD TO SKIP", new Vector2(0f, 3.82f), 0.48f, Announcer.Cream, Order + 12);
        ring = Ui.Spr("Hold", transform, Ui.Disc, new Vector2(6.15f, 3.82f), Order + 12, 0.16f);
        ring.color = Announcer.Yellow;
        ring.enabled = false;
    }

    IEnumerator Run(Cutscene cs)
    {
        for (int i = 0; i < cs.shots.Length && !abort; i++)
        {
            var shot = cs.shots[i];
            Show(shot);
            float t = 0f;
            while (t < shot.dur && !abort && !jump)
            {
                t += Time.unscaledDeltaTime;
                Apply(shot, t);
                Reveal(textDone ? total : Mathf.Min(total, Mathf.CeilToInt(total * Mathf.Clamp01(t / typeDur))));
                ReadInput();
                yield return null;
            }
            jump = false;
            if (abort) break;
            if (shot.outT == Transition.Iris)
            {
                var close = FilmFilter.IrisClose(0.4f);
                if (close != null) yield return close;
            }
        }
        bool leaveShut = !abort && cs.shots.Length > 0 && cs.shots[cs.shots.Length - 1].outT == Transition.Iris;
        if (!leaveShut) FilmFilter.IrisSet(FilmFilter.IrisOpenRadius);
        End();
    }

    void Show(Shot shot)
    {
        if (beat != null) { StopCoroutine(beat); beat = null; }
        shakeUntil = 0f;
        holdT = 0f;
        holding = false;
        armed = false;
        jump = false;
        textDone = false;
        shown = -1;
        if (FilmFilter.I && scratchSaved) FilmFilter.I.scratch = prevScratch;
        for (int i = extra.childCount - 1; i >= 0; i--) Destroy(extra.GetChild(i).gameObject);

        frame.sprite = LoadArt(shot.art);
        frame.enabled = frame.sprite != null;
        titleShot = shot.art != null && (shot.art.StartsWith("card") || shot.art == "intro_logo");
        wrapped = Wrap(shot.text, titleShot ? 40 : 34);
        total = 0;
        for (int i = 0; i < wrapped.Length; i++) total += wrapped[i].Length;
        typeDur = Mathf.Max(0.35f, shot.dur * 0.62f);
        if (total == 0) textDone = true;
        LayoutText();
        Reveal(0);
        Veil(shot.inT == Transition.Fade ? 1f : 0f);
        if (shot.inT == Transition.Iris)
        {
            FilmFilter.IrisSet(0f);
            FilmFilter.IrisOpen(0.5f);
        }
        if (!string.IsNullOrEmpty(shot.music)) AudioManager.Music(shot.music, 0.6f);
        if (!string.IsNullOrEmpty(shot.sfx)) AudioManager.Sfx(shot.sfx, 0.75f);
        shot.custom?.Invoke();
    }

    void LayoutText()
    {
        int n = wrapped.Length;
        bool caption = n > 0 && !titleShot;
        plaque.enabled = caption;
        if (caption) FitPlaque(n);
        float scale = titleShot ? Mathf.Min(1.05f, 12.2f / Mathf.Max(1f, (n > 0 ? wrapped[0].Length : 1) * 0.25f)) : 0.68f;
        float gap = titleShot ? 0.7f : 0.44f;
        float mid = titleShot ? 0.15f : -3.45f;
        var color = titleShot ? Announcer.Yellow : Announcer.Cream;
        var fx = titleShot ? PixelText.Effect.Boil : PixelText.Effect.None;
        for (int i = 0; i < lines.Length; i++)
        {
            bool on = i < n;
            lines[i].gameObject.SetActive(on);
            if (!on) continue;
            lines[i].transform.localScale = Vector3.one * scale;
            lines[i].transform.localPosition = new Vector3(0f, mid + gap * (n - 1) * 0.5f - i * gap, 0f);
            lines[i].SetColor(color);
            lines[i].effect = fx;
        }
    }

    void FitPlaque(int n)
    {
        float h = 0.95f + 0.5f * (n - 1);
        if (plaque.drawMode == SpriteDrawMode.Sliced)
        {
            plaque.size = new Vector2(14.2f, h);
            plaque.transform.localScale = Vector3.one;
        }
        else plaque.transform.localScale = new Vector3(14.2f, h, 1f);
        plaque.transform.localPosition = new Vector3(0f, -3.45f, 0f);
    }

    void Apply(Shot shot, float t)
    {
        float k = shot.dur <= 0f ? 1f : Mathf.Clamp01(t / shot.dur);
        float z = Mathf.Lerp(shot.zoomFrom <= 0f ? 1.08f : shot.zoomFrom, shot.zoomTo <= 0f ? 1.16f : shot.zoomTo, k);
        var pan = Vector2.Lerp(shot.panFrom, shot.panTo, k);
        // ponytail: PageTurn is a sideways slide. A mesh curl can replace the offset.
        float slide = 0f;
        if (shot.inT == Transition.PageTurn) slide += Mathf.Lerp(9f, 0f, Mathf.Clamp01(t / 0.45f));
        if (shot.outT == Transition.PageTurn) slide += Mathf.Lerp(0f, -9f, Mathf.Clamp01((t - (shot.dur - 0.4f)) / 0.4f));
        if (Time.unscaledTime < shakeUntil) pan += UnityEngine.Random.insideUnitCircle * shakeAmp;
        artT.localPosition = new Vector3(pan.x + slide, pan.y, 0f);
        artT.localScale = new Vector3(z, z, 1f);
        float a = 0f;
        if (shot.inT == Transition.Fade) a = Mathf.Clamp01(1f - t / 0.4f);
        if (shot.outT == Transition.Fade) a = Mathf.Max(a, Mathf.Clamp01((t - (shot.dur - 0.35f)) / 0.35f));
        Veil(a);
    }

    void Reveal(int n)
    {
        if (n == shown) return;
        shown = n;
        if (n >= total) textDone = true;
        int left = n;
        for (int i = 0; i < lines.Length; i++)
        {
            if (wrapped == null || i >= wrapped.Length) break;
            int take = Mathf.Clamp(left, 0, wrapped[i].Length);
            left -= take;
            lines[i].Text = take <= 0 ? "" : wrapped[i].Substring(0, take);
        }
    }

    void ReadInput()
    {
        bool tap = TapDown();
        bool held = HoldDown();
        if (tap)
        {
            holding = true;
            holdT = 0f;
            if (!textDone) { textDone = true; armed = true; Reveal(total); }
            else armed = false;
        }
        if (holding && held)
        {
            holdT += Time.unscaledDeltaTime;
            ring.enabled = true;
            // ponytail: skip meter is a growing disc. A stroked arc replaces it if players read the disc as a button.
            ring.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.5f, Mathf.Clamp01(holdT / HoldSkip));
            if (holdT >= HoldSkip) abort = true;
        }
        else ring.enabled = false;
        if (holding && !held)
        {
            if (!armed && textDone) jump = true;
            holding = false;
            holdT = 0f;
        }
    }

    void Veil(float a)
    {
        var c = Ui.Ink;
        c.a = a;
        veil.color = c;
    }

    void End()
    {
        if (ended) return;
        ended = true;
        Restore();
        Mark(id);
        var cb = done;
        done = null;
        if (I == this) I = null;
        Destroy(gameObject);
        cb?.Invoke();
    }

    void Restore()
    {
        if (restored) return;
        restored = true;
        CameraFx.BaseScale = prevScale;
        if (FilmFilter.I && scratchSaved) FilmFilter.I.scratch = prevScratch;
    }

    static bool TapDown()
    {
        if (UiInput.Confirm()) return true;
        return UiInput.Down(out _);
    }

    static bool HoldDown()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null && k.spaceKey.isPressed) return true;
#else
        if (Input.GetKey(KeyCode.Space)) return true;
#endif
        return UiInput.Held(out _);
    }

    static string[] Wrap(string text, int max)
    {
        if (string.IsNullOrEmpty(text)) return new string[0];
        var words = text.Split(' ');
        var list = new List<string>();
        string cur = "";
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length == 0) continue;
            string next = cur.Length == 0 ? words[i] : cur + " " + words[i];
            if (cur.Length > 0 && next.Length > max) { list.Add(cur); cur = words[i]; }
            else cur = next;
        }
        if (cur.Length > 0) list.Add(cur);
        return list.ToArray();
    }

    public static Sprite LoadArt(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (artCache.TryGetValue(name, out var cached)) return cached;
        string path = Path.Combine(Application.dataPath, "_Game", "Art", "story", name + ".png");
        if (!File.Exists(path)) return null;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        if (!tex.LoadImage(File.ReadAllBytes(path))) return null;
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
        artCache[name] = sprite;
        return sprite;
    }
}
