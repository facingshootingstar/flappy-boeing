using UnityEngine;

/// <summary>Stay level with the tanker until the hose has been connected for the goal time. Each slip costs points.</summary>
public class SkyRefuel : MiniGame
{
    Transform me, tanker, hose;
    float myY, fill;
    int slips;
    bool linked = true;

    protected override string HintText => "A/D OR TAP TO MATCH THE TANKER";

    protected override void Build()
    {
        var fly = LevelRuntime.Fly;
        var sr = Ui.Spr("Me", root, fly != null && fly.Length > 0 ? fly[0] : Ui.Disc, new Vector2(-2.2f, 0f), Order + 6, 0.7f);
        if (fly != null && fly.Length > 1) sr.gameObject.AddComponent<SpriteAnimator>().frames = fly;
        me = sr.transform;
        tanker = Ui.Spr("Tanker", root, PropArt.Tanker, new Vector2(2.4f, 0f), Order + 6, 1.15f).transform;
        hose = Ui.Box("Hose", root, Vector2.zero, new Vector2(3.4f, 0.08f), Announcer.Teal, Order + 4).transform;
        hose.gameObject.SetActive(false);
        Hud("FUEL 0/" + level.goal, "POINTS 100");
    }

    protected override void Tick(float dt)
    {
        float ty = Mathf.Sin(elapsed * (0.8f + level.hard * 0.7f)) * (1.45f + level.hard * 0.7f);
        tanker.localPosition = new Vector3(2.4f, ty, 0f);

        float dir = Bot ? BotDir(ty) : Steer();
        myY = Mathf.Clamp(myY + (dir * 3.6f + Gust * 0.5f) * dt, -3.1f, 3.1f);
        me.localPosition = new Vector3(-2.2f, myY, 0f);

        bool on = Mathf.Abs(myY - ty) < 0.42f;
        hose.gameObject.SetActive(on);
        if (on)
        {
            hose.localPosition = new Vector3(0.1f, (myY + ty) * 0.5f, 0f);
            hose.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(ty - myY, 4.6f) * Mathf.Rad2Deg);
            fill += dt;
        }
        else
        {
            fill = Mathf.Max(0f, fill - dt * 0.35f);
            if (linked)
            {
                slips++;
                PopText("SLIP!", new Vector2(0f, myY + 0.55f), slips);
            }
        }
        linked = on;
        score = Mathf.Max(0, 100 - slips * 6);

        Hud("FUEL " + Mathf.FloorToInt(fill) + "/" + level.goal, "POINTS " + score);
        if (fill >= level.goal)
        {
            if (slips <= 2) GrantQuota();
            PopText("FULL!", new Vector2(0f, 1.5f), 3);
            End(true);
            return;
        }
        if (elapsed > level.goal * 3.2f) End(false);
    }

    float BotDir(float ty)
    {
        float dy = ty - myY;
        return Mathf.Abs(dy) < 0.05f ? 0f : Mathf.Sign(dy);
    }
}
