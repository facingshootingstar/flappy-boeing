using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Go-Plane style: the plane never stops, the player only turns. Heat-seeking missiles chase it; trick them into
/// colliding with each other (+1 each) and grab stars (+1). Survive <see cref="LevelDef.goal"/> seconds to clear.
/// Screen edges wrap for the plane, not for missiles.
/// </summary>
public class MissileDodge : MiniGame
{
    const float PlaneSpeed = 4.3f, TurnRate = 210f, HitR = 0.42f;

    class Missile { public Transform t; public float ang, life, puff; }

    Transform plane, pickup;
    float heading;
    readonly List<Missile> missiles = new List<Missile>();
    float spawnAt = 1.2f;

    protected override string HintText => "HOLD A/D OR TAP LEFT/RIGHT TO TURN";

    protected override void Build()
    {
        var fly = LevelRuntime.Fly;
        var sr = Ui.Spr("Plane", root, fly != null && fly.Length > 0 ? fly[0] : Ui.Disc, new Vector2(-3f, 0f), Order + 5, 0.75f);
        if (fly != null && fly.Length > 1) sr.gameObject.AddComponent<SpriteAnimator>().frames = fly;
        plane = sr.transform;
        pickup = Ui.Spr("Star", root, GameKit.I ? GameKit.I.parryStar : Ui.Disc, RandomSpot(), Order + 4, 0.7f).transform;
        pickup.GetComponent<SpriteRenderer>().color = Announcer.Yellow;
        Hud("TIME 0/" + level.goal, "POINTS 0");
    }

    static Vector2 RandomSpot() => new Vector2(Random.Range(-6f, 6f), Random.Range(-2.8f, 2.8f));

    protected override void Tick(float dt)
    {
        float progress = Mathf.Clamp01(elapsed / level.goal);

        // ---- plane
        float turn = Bot ? BotSteer() : -Steer();      // right = clockwise
        heading += turn * TurnRate * dt;
        var dir = new Vector2(Mathf.Cos(heading * Mathf.Deg2Rad), Mathf.Sin(heading * Mathf.Deg2Rad));
        Vector2 p = (Vector2)plane.localPosition + dir * PlaneSpeed * dt;
        if (p.x > W + 0.6f) p.x = -W - 0.6f; else if (p.x < -W - 0.6f) p.x = W + 0.6f;
        if (p.y > H + 0.4f) p.y = -H - 0.4f; else if (p.y < -H - 0.4f) p.y = H + 0.4f;
        plane.localPosition = p;
        plane.localRotation = Quaternion.Euler(0f, 0f, heading);
        plane.localScale = new Vector3(0.75f, dir.x < 0f ? -0.75f : 0.75f, 1f);   // keep the cockpit up

        // ---- pickup
        if (Vector2.Distance(p, pickup.localPosition) < 0.6f)
        {
            score++;
            Sparkle(pickup.localPosition, Announcer.Yellow, 1.2f);
            AudioManager.Sfx("score", 0.6f, 0f);
            pickup.localPosition = RandomSpot();
        }
        pickup.localRotation = Quaternion.Euler(0f, 0f, elapsed * 90f);

        // ---- spawn
        int cap = 2 + Mathf.RoundToInt(level.hard * 3f + progress * 3f);
        spawnAt -= dt;
        if (spawnAt <= 0f && missiles.Count < cap)
        {
            spawnAt = Mathf.Lerp(3f, 1.15f, level.hard) * (1f - 0.3f * progress);
            Spawn(p);
        }

        // ---- missiles (easy levels are slower than the plane; hard levels catch up)
        float mSpeed = Mathf.Lerp(3.7f, 5.3f, level.hard), mTurn = Mathf.Lerp(75f, 140f, level.hard);
        for (int i = missiles.Count - 1; i >= 0; i--)
        {
            var m = missiles[i];
            Vector2 mp = m.t.localPosition;
            float want = Mathf.Atan2(p.y - mp.y, p.x - mp.x) * Mathf.Rad2Deg;
            m.ang = Mathf.MoveTowardsAngle(m.ang, want, mTurn * dt);
            mp += new Vector2(Mathf.Cos(m.ang * Mathf.Deg2Rad), Mathf.Sin(m.ang * Mathf.Deg2Rad)) * mSpeed * dt;
            m.t.localPosition = mp;
            m.t.localRotation = Quaternion.Euler(0f, 0f, m.ang);
            m.life += dt;
            m.puff -= dt;
            if (m.puff <= 0f && GameKit.I && GameKit.I.smoke != null && GameKit.I.smoke.Length > 0)
            {
                m.puff = 0.07f;
                GameKit.Pop(GameKit.I.smoke[0], mp - new Vector2(Mathf.Cos(m.ang * Mathf.Deg2Rad), Mathf.Sin(m.ang * Mathf.Deg2Rad)) * 0.35f,
                    0.45f, 0.3f, 0.8f, Order + 1, new Color(1f, 1f, 1f, 0.6f));
            }
            if (m.life > 9f) { Pop(i, false); continue; }   // out of fuel
            if (m.life < 0.55f) continue;                   // arming: a fresh spawn cannot hit
            if (Vector2.Distance(mp, p) < HitR)
            {
                Boom(p, 1.4f);
                plane.gameObject.SetActive(false);
                AudioManager.Sfx("death", 0.8f, 0f);
                End(false);
                return;
            }
        }
        // missile vs missile
        for (int a = missiles.Count - 1; a >= 0; a--)
            for (int b = a - 1; b >= 0; b--)
            {
                if (Vector2.Distance(missiles[a].t.localPosition, missiles[b].t.localPosition) > 0.38f) continue;
                Pop(a, true); Pop(b, true);
                score += 2;
                a = Mathf.Min(a, missiles.Count);
                break;
            }

        int secs = Mathf.FloorToInt(elapsed);
        Hud("TIME " + Mathf.Min(secs, level.goal) + "/" + level.goal, "POINTS " + score);
        if (elapsed >= level.goal) End(true);
    }

    void Spawn(Vector2 plane)
    {
        // from a screen edge, preferably away from the plane
        Vector2 at;
        int tries = 0;
        do
        {
            int side = Random.Range(0, 4);
            at = side switch
            {
                0 => new Vector2(-W - 0.8f, Random.Range(-H, H)),
                1 => new Vector2(W + 0.8f, Random.Range(-H, H)),
                2 => new Vector2(Random.Range(-W, W), H + 0.8f),
                _ => new Vector2(Random.Range(-W, W), -H - 0.8f),
            };
        } while (Vector2.Distance(at, plane) < 5f && ++tries < 8);
        var sprite = LevelRuntime.Rocket ? LevelRuntime.Rocket : LevelRuntime.Torpedo;
        var sr = Ui.Spr("Missile", root, sprite ? sprite : Ui.Disc, at, Order + 3, sprite ? 1f : 0.3f);
        sr.color = new Color(1f, 0.55f, 0.5f);
        float ang = Mathf.Atan2(plane.y - at.y, plane.x - at.x) * Mathf.Rad2Deg;
        missiles.Add(new Missile { t = sr.transform, ang = ang });
        AudioManager.Sfx("swoop", 0.35f);
    }

    void Pop(int i, bool boom)
    {
        var m = missiles[i];
        if (boom) Boom(m.t.localPosition, 0.9f);
        else if (GameKit.I) GameKit.Burst(GameKit.I.smoke, m.t.localPosition, 14f, 0.6f, Order + 2);
        Destroy(m.t.gameObject);
        missiles.RemoveAt(i);
    }

    /// <summary>Debug bot: try left / straight / right and keep the heading whose near future is farthest from every missile.</summary>
    float BotSteer()
    {
        Vector2 p = plane.localPosition;
        float best = float.NegativeInfinity;
        float pick = 0f;
        for (int i = -1; i <= 1; i++)
        {
            float turn = i;
            float h = heading + turn * TurnRate * 0.4f;
            var dir = new Vector2(Mathf.Cos(h * Mathf.Deg2Rad), Mathf.Sin(h * Mathf.Deg2Rad));
            Vector2 fut = p + dir * PlaneSpeed * 0.4f;
            if (fut.x > W) fut.x -= W * 2f; else if (fut.x < -W) fut.x += W * 2f;
            if (fut.y > H) fut.y -= H * 2f; else if (fut.y < -H) fut.y += H * 2f;
            float safety = 9f;
            foreach (var m in missiles)
            {
                Vector2 mp = m.t.localPosition;
                float ang = m.ang;
                const float step = 0.12f;
                for (float t = 0f; t < 0.48f; t += step)
                {
                    float aim = Mathf.Atan2(fut.y - mp.y, fut.x - mp.x) * Mathf.Rad2Deg;
                    ang = Mathf.MoveTowardsAngle(ang, aim, 90f * step);
                    mp += new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad)) * 3.8f * step;
                }
                float d = Vector2.Distance(fut, mp);
                if (d < safety) safety = d;
            }
            float s = safety * 3f - Vector2.Distance(fut, pickup.localPosition) * 0.08f;
            if (s > best) { best = s; pick = turn; }
        }
        return pick;
    }
}
