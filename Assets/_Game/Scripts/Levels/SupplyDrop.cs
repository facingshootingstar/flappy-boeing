using System.Collections.Generic;
using UnityEngine;

/// <summary>Catch falling supply crates in the bay. Three misses fails. A centered catch is worth more points.</summary>
public class SupplyDrop : MiniGame
{
    class Drop { public Transform t; public float x, y; }

    Transform bay;
    readonly List<Drop> drops = new List<Drop>();
    float bayX, nextIn = 0.4f;
    int caught, missed;

    protected override string HintText => "A/D OR TAP TO CATCH THE CRATE";

    protected override void Build()
    {
        bay = Ui.Root("Bay", root, new Vector2(0f, -2.7f));
        Ui.Box("Hull", bay, new Vector2(0f, -0.28f), new Vector2(2.1f, 0.4f), new Color32(70, 78, 92, 255), Order + 3);
        Ui.Box("Deck", bay, Vector2.zero, new Vector2(1.8f, 0.28f), new Color32(232, 190, 96, 255), Order + 4);
        var fly = LevelRuntime.Fly;
        var sr = Ui.Spr("Plane", bay, fly != null && fly.Length > 0 ? fly[0] : Ui.Disc, new Vector2(0f, 0.38f), Order + 5, 0.62f);
        if (fly != null && fly.Length > 1) sr.gameObject.AddComponent<SpriteAnimator>().frames = fly;
        Hud("CRATES 0/" + level.goal, "POINTS 0");
    }

    protected override void Tick(float dt)
    {
        float dir = Bot ? BotDir() : Steer();
        bayX = Mathf.Clamp(bayX + dir * (5.4f + level.hard * 1.5f) * dt, -6f, 6f);
        bay.localPosition = new Vector3(bayX, -2.7f, 0f);

        nextIn -= dt;
        if (nextIn <= 0f && caught < level.goal && caught + missed + drops.Count < level.goal + 2)
        {
            nextIn = Mathf.Lerp(1.45f, 0.85f, level.hard);
            float x = Random.Range(-5.6f, 5.6f);
            var t = Ui.Spr("Drop", root, PropArt.Chute, new Vector2(x, 4.1f), Order + 6, 1f).transform;
            drops.Add(new Drop { t = t, x = x, y = 4.1f });
        }

        float fall = 2.3f + level.hard * 1.7f + caught * 0.06f;
        for (int i = drops.Count - 1; i >= 0; i--)
        {
            var d = drops[i];
            d.y -= fall * dt;
            d.x = Mathf.Clamp(d.x + Gust * dt, -6.4f, 6.4f);
            d.t.localPosition = new Vector3(d.x, d.y, 0f);
            if (d.y > -2.4f) continue;
            bool hit = Mathf.Abs(d.x - bayX) < 0.95f;
            Destroy(d.t.gameObject);
            drops.RemoveAt(i);
            if (!hit)
            {
                missed++;
                Boom(new Vector2(d.x, -3.1f), 0.7f);
                if (missed >= 3) { End(false); return; }
                continue;
            }
            caught++;
            bool center = Mathf.Abs(d.x - bayX) < 0.34f;
            score += center ? 10 : 4;
            PopText(center ? "PERFECT!" : "CAUGHT!", new Vector2(d.x, -1.6f), center ? caught : 1);
            Hud("CRATES " + caught + "/" + level.goal, "POINTS " + score);
            if (caught >= level.goal) { End(true); return; }
        }
        Hud("CRATES " + caught + "/" + level.goal, "POINTS " + score);
    }

    float BotDir()
    {
        if (drops.Count == 0) return 0f;
        Drop low = drops[0];
        foreach (var d in drops) if (d.y < low.y) low = d;
        float dx = low.x - bayX;
        return Mathf.Abs(dx) < 0.1f ? 0f : Mathf.Sign(dx);
    }
}
