using System.Collections.Generic;
using UnityEngine;

/// <summary>Switch lanes under falling bolts. Score counts bolts passed in the neighbouring lane. One hit fails.</summary>
public class BoltLane : MiniGame
{
    static readonly float[] LaneX = { -2.6f, 0f, 2.6f };
    const float PlaneY = -2.4f;

    class Bolt { public Transform t; public int lane; public float y, hold; public bool armed; }

    Transform plane;
    readonly List<Bolt> bolts = new List<Bolt>();
    int lane = 1, passed, edge;
    float nextIn = 0.4f;

    protected override string HintText => "A/D OR TAP TO SWITCH LANE";

    protected override void Build()
    {
        for (int i = 0; i < 3; i++)
        {
            Ui.Box("Lane", root, new Vector2(LaneX[i], -0.35f), new Vector2(1.45f, 7.5f), new Color(0.02f, 0.03f, 0.08f, 0.35f), Order);
            Ui.Box("Tick", root, new Vector2(LaneX[i], -3.45f), new Vector2(0.85f, 0.1f), Announcer.Cream, Order + 1);
        }
        var fly = LevelRuntime.Fly;
        var sr = Ui.Spr("Plane", root, fly != null && fly.Length > 0 ? fly[0] : Ui.Disc, new Vector2(LaneX[1], PlaneY), Order + 6, 0.65f);
        if (fly != null && fly.Length > 1) sr.gameObject.AddComponent<SpriteAnimator>().frames = fly;
        plane = sr.transform;
        Hud("BOLTS 0/" + level.goal, "CLOSE 0");
    }

    protected override void Tick(float dt)
    {
        if (Bot)
        {
            int want = BotWant();
            if (want != lane) lane += want > lane ? 1 : -1;
        }
        else
        {
            float s = Steer();
            int d = s < 0f ? -1 : s > 0f ? 1 : 0;
            if (d != 0 && d != edge) lane = Mathf.Clamp(lane + d, 0, 2);
            edge = d;
        }
        plane.localPosition = new Vector3(LaneX[lane], PlaneY, 0f);

        int cap = level.hard < 0.45f ? 1 : 2;
        nextIn -= dt;
        if (nextIn <= 0f && bolts.Count < cap && passed + bolts.Count < level.goal)
        {
            nextIn = Mathf.Lerp(1.25f, 0.72f, level.hard);
            Spawn();
        }

        float speed = Mathf.Lerp(3.5f, 5.4f, level.hard);
        for (int i = bolts.Count - 1; i >= 0; i--)
        {
            var b = bolts[i];
            if (b.hold > 0f)
            {
                b.hold -= dt;
                b.t.localPosition = new Vector3(LaneX[b.lane], b.y, 0f);
                continue;
            }
            if (!b.armed)
            {
                b.armed = true;
                b.t.GetComponent<SpriteRenderer>().color = Color.white;
            }
            b.y -= speed * dt;
            b.t.localPosition = new Vector3(LaneX[b.lane], b.y, 0f);
            if (b.y > PlaneY + 0.35f) continue;
            if (b.lane == lane)
            {
                Boom(new Vector2(LaneX[lane], PlaneY), 1.2f);
                plane.gameObject.SetActive(false);
                End(false);
                return;
            }
            if (Mathf.Abs(b.lane - lane) == 1)
            {
                score++;
                PopText("CLOSE!", new Vector2(LaneX[b.lane], -1.15f), score);
                if (score >= 3) GrantQuota();
            }
            Destroy(b.t.gameObject);
            bolts.RemoveAt(i);
            passed++;
            Hud("BOLTS " + passed + "/" + level.goal, "CLOSE " + score);
            if (passed >= level.goal) { End(true); return; }
        }
        Hud("BOLTS " + passed + "/" + level.goal, "CLOSE " + score);
    }

    void Spawn()
    {
        int pick = Random.Range(0, 3);
        var sr = Ui.Spr("Bolt", root, PropArt.Bolt, new Vector2(LaneX[pick], 3.55f), Order + 5, 0.42f);
        sr.color = new Color(1f, 1f, 1f, 0.4f);
        bolts.Add(new Bolt { t = sr.transform, lane = pick, y = 3.55f, hold = 0.4f });
        AudioManager.Sfx("thunder", 0.28f, 0.04f, 0.08f);
    }

    int BotWant()
    {
        float nearest = 99f;
        bool threat = false;
        foreach (var b in bolts)
        {
            if (b.lane != lane || b.y < PlaneY) continue;
            threat = true;
            if (b.y < nearest) nearest = b.y;
        }
        if (!threat || nearest > 2.6f) return lane;
        for (int step = 1; step <= 2; step++)
        {
            int left = lane - step, right = lane + step;
            if (left >= 0 && Safe(left)) return left;
            if (right <= 2 && Safe(right)) return right;
        }
        return lane;
    }

    bool Safe(int ln)
    {
        foreach (var b in bolts)
            if (b.lane == ln && b.y > PlaneY && b.y < 3.2f) return false;
        return true;
    }
}
