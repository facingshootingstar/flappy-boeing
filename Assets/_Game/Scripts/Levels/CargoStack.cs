using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stack-style cargo loading: a crate slides over the carrier deck, tap to drop it. Whatever overhangs the crate
/// below is sliced off and falls into the sea. Perfect drops snap and (after a combo of 3) grow the crate back.
/// Stack <see cref="LevelDef.goal"/> crates to clear; score = perfect drops.
/// </summary>
public class CargoStack : MiniGame
{
    const float Base = -3.15f, CrateH = 0.5f, MaxW = 3f, PerfectTol = 0.12f;

    class Debris { public Transform t; public Vector2 v; public float spin; }

    Transform stack, moving;
    float topX, topW = MaxW, curX, curW = MaxW, dir = 1f, sway, shift, prevDx = 99f;
    int placed, combo;
    readonly List<Debris> debris = new List<Debris>();

    protected override string HintText => "TAP TO DROP THE CRATE - LINE IT UP!";

    protected override void Build()
    {
        stack = Ui.Root("Stack", root);
        // carrier deck
        Ui.Box("Hull", stack, new Vector2(0f, Base - 0.7f), new Vector2(7f, 1.1f), new Color32(70, 78, 92, 255), Order);
        Ui.Box("DeckEdge", stack, new Vector2(0f, Base - 0.13f), new Vector2(7.2f, 0.12f), Ui.Ink, Order + 1);
        Ui.Box("Deck", stack, new Vector2(0f, Base - 0.06f), new Vector2(MaxW + 0.4f, 0.12f), new Color32(232, 190, 96, 255), Order + 2);
        for (int i = -3; i <= 3; i++)
            Ui.Box("Port", stack, new Vector2(i * 0.95f, Base - 0.65f), new Vector2(0.26f, 0.26f), new Color32(255, 230, 150, 255), Order + 1);
        topX = 0f; topW = MaxW; curW = MaxW;
        NewCrate();
        Hud("CRATES 0/" + level.goal, "PERFECT 0");
    }

    float Speed => 2.4f + level.hard * 2.2f + placed * 0.12f;
    float Limit() => W - 0.35f - curW * 0.5f;   // keep the whole crate on screen
    float TopY => Base + placed * CrateH;

    static Color Hue(int i) => Color.HSVToRGB(Mathf.Repeat(0.08f + i * 0.07f, 1f), 0.55f, 0.95f);

    Transform MakeCrate(string name, float w, Color c, Vector2 pos, Transform parent)
    {
        var t = Ui.Root(name, parent, pos);
        Ui.Box("Edge", t, Vector2.zero, new Vector2(w, CrateH), Ui.Ink, Order + 4);
        Ui.Box("Body", t, new Vector2(0f, -0.02f), new Vector2(w - 0.08f, CrateH - 0.1f), c, Order + 5);
        Ui.Box("Shine", t, new Vector2(0f, CrateH / 2f - 0.1f), new Vector2(w - 0.16f, 0.05f), Color.Lerp(c, Color.white, 0.55f), Order + 6);
        if (w > 0.5f) Ui.Box("Strap", t, new Vector2(0f, -0.02f), new Vector2(0.08f, CrateH - 0.1f), Color.Lerp(c, Ui.Ink, 0.45f), Order + 6);
        return t;
    }

    void NewCrate()
    {
        dir = Random.value < 0.5f ? 1f : -1f;
        curX = -dir * Limit();
        prevDx = curX - topX;
        moving = MakeCrate("Crate", curW, Hue(placed), new Vector2(curX, TopY + CrateH / 2f + 0.9f), stack);
    }

    protected override void Tick(float dt)
    {
        // slide
        float lim = Limit();
        curX += dir * Speed * dt + Gust * dt;
        if (curX > lim) { curX = lim; dir = -1f; } else if (curX < -lim) { curX = -lim; dir = 1f; }
        sway += dt;
        float swayX = Mathf.Sin(sway * 1.3f) * level.hard * 0.25f * Mathf.Min(1f, placed / 6f);
        stack.localPosition = new Vector3(swayX, Mathf.Lerp(stack.localPosition.y, -shift, dt * 4f), 0f);
        float hover = TopY + CrateH / 2f + 0.9f + Mathf.Sin(sway * 5f) * 0.05f;
        moving.localPosition = new Vector3(curX, hover, 0f);

        float dxNow = curX - topX;
        // crossing the stack, not a tiny window: a fast crate used to skip the 0.07 band in one frame
        bool botDrop = Bot && prevDx != 0f && dxNow * prevDx <= 0f;
        prevDx = dxNow;
        if (botDrop || (!Bot && (TapInput.Pressed() || UiInput.Confirm()))) Drop();

        for (int i = debris.Count - 1; i >= 0; i--)
        {
            var d = debris[i];
            d.v.y -= 14f * dt;
            d.t.localPosition += (Vector3)(d.v * dt);
            d.t.Rotate(0f, 0f, d.spin * dt);
            if (d.t.position.y < -6f) { Destroy(d.t.gameObject); debris.RemoveAt(i); }
        }
    }

    void Drop()
    {
        float dx = curX - topX;
        float y = TopY + CrateH / 2f;
        var c = Hue(placed);
        Destroy(moving.gameObject);
        if (Mathf.Abs(dx) >= topW)
        {
            // clean miss: the whole crate falls
            var miss = MakeCrate("Miss", curW, c, new Vector2(curX, y + 0.4f), stack);
            debris.Add(new Debris { t = miss, v = new Vector2(Mathf.Sign(dx) * 1.5f, 0f), spin = Mathf.Sign(dx) * -160f });
            AudioManager.Sfx("hurt", 0.8f, 0f);
            CameraFx.Shake(0.15f, 0.25f);
            End(false);
            return;
        }

        float w, x;
        if (Mathf.Abs(dx) < PerfectTol)
        {
            x = topX; w = curW;
            combo++;
            score++;
            if (combo >= 3) { w = Mathf.Min(MaxW, w + 0.18f); GrantQuota(); }
            AudioManager.Sfx("parry", 0.7f, 0f, 0f, 1f + Mathf.Min(combo, 8) * 0.06f);
            Sparkle(new Vector2(x - w / 2f, y) + (Vector2)stack.localPosition, Announcer.Yellow, 1f);
            Sparkle(new Vector2(x + w / 2f, y) + (Vector2)stack.localPosition, Announcer.Yellow, 1f);
            var p = Ui.Txt(root, combo >= 3 ? "PERFECT X" + combo : "PERFECT!", new Vector2(0f, 1.4f), 1.6f, Announcer.Yellow, Order + 55);
            Destroy(p.gameObject, 0.6f);
        }
        else
        {
            combo = 0;
            w = curW - Mathf.Abs(dx);
            x = topX + dx / 2f;
            float cutW = Mathf.Abs(dx);
            float cutX = x + Mathf.Sign(dx) * (w / 2f + cutW / 2f);
            var cut = MakeCrate("Cut", cutW, c, new Vector2(cutX, y), stack);
            debris.Add(new Debris { t = cut, v = new Vector2(Mathf.Sign(dx) * 1.2f, 1f), spin = Mathf.Sign(dx) * -220f });
            AudioManager.Sfx("hit", 0.6f);
        }
        MakeCrate("Crate" + placed, w, c, new Vector2(x, y), stack);
        CameraFx.Shake(0.05f, 0.1f);
        placed++;
        topX = x; topW = w; curW = w;
        if (placed > 5) shift = (placed - 5) * CrateH;
        Hud("CRATES " + placed + "/" + level.goal, "PERFECT " + score);
        if (placed >= level.goal) { End(true); return; }
        NewCrate();
    }
}
