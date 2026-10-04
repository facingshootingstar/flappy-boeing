using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combat guns. Hold J / Space / LMB for the peashooter; K / RMB spends a card on an EX rocket, or all five on the Super beam.
/// The meter (0..5 cards) fills from damage dealt and from parries.
/// </summary>
public class PlayerWeapons : MonoBehaviour
{
    public static PlayerWeapons I { get; private set; }
    public const int MaxCards = 5;

    public Sprite shotSprite, exSprite, beamSprite;
    public float fireInterval = 0.11f;
    public float chargePerDamage = 0.035f;
    public float exDamage = 20f, exSpeed = 12f;
    public float superTime = 1.2f, superTick = 0.05f, superDamagePerTick = 2.5f;

    float meter, nextShot;
    readonly Stack<PlayerShot> shots = new Stack<PlayerShot>();
    readonly Stack<PlayerShot> rockets = new Stack<PlayerShot>();
    Transform root;
    SpriteRenderer beam;
    bool up;

    public static bool SuperActive { get; private set; }
    public static float Meter => I ? I.meter : 0f;
    public static int Cards => I ? Mathf.FloorToInt(I.meter + 0.0001f) : 0;

    void Awake() { I = this; SuperActive = false; root = new GameObject("PlayerShots").transform; }
    void OnDestroy() { if (I == this) { I = null; SuperActive = false; } }

    public static void Charge(float damage) { if (I && !SuperActive) I.meter = Mathf.Min(MaxCards, I.meter + damage * I.chargePerDamage); }
    public static void AddCard() { if (I) I.meter = Mathf.Min(MaxCards, I.meter + 1f); }

    void Update()
    {
        var gs = GameState.I;
        var pc = PlayerController.I;
        if (gs == null || !gs.Alive || !gs.Combat || pc == null || SuperActive) return;
        if (CombatInput.Ex())
        {
            if (meter >= MaxCards - 0.0001f) StartCoroutine(Super());
            else if (meter >= 1f) FireEx();
        }
        if (CombatInput.Fire() && Time.time >= nextShot)
        {
            nextShot = Time.time + fireInterval;
            Shoot();
        }
    }

    Vector3 Nose => PlayerController.I.transform.position + new Vector3(0.9f, -0.02f, 0);

    void Shoot()
    {
        up = !up;
        var s = Get(shots, shotSprite, new Vector2(0.5f, 0.25f), false);
        s.speed = 16f; s.damage = 1f;
        s.transform.position = Nose + new Vector3(0, up ? 0.05f : -0.05f, 0);
        s.gameObject.SetActive(true);
        var kit = GameKit.I;
        if (kit) GameKit.Pop(kit.muzzle, Nose + new Vector3(0.15f, 0, 0), 0.06f, 1f, 0.3f, 12);
        AudioManager.Sfx("shoot", 0.35f, 0.08f, 0.07f);
    }

    void FireEx()
    {
        meter -= 1f;
        if (GameState.I) GameState.I.ExShots++;
        var s = Get(rockets, exSprite, new Vector2(0.7f, 0.35f), true);
        s.speed = exSpeed; s.damage = exDamage;
        s.transform.position = Nose + new Vector3(0.2f, 0, 0);
        s.gameObject.SetActive(true);
        AudioManager.Sfx("ex", 0.9f);
        CameraFx.Shake(0.06f, 0.12f);
        var kit = GameKit.I;
        if (kit) GameKit.Burst(kit.smoke, Nose - new Vector3(0.6f, 0, 0), 16f, 1.2f, 12);
    }

    PlayerShot Get(Stack<PlayerShot> pool, Sprite sprite, Vector2 box, bool ex)
    {
        if (pool.Count > 0) return pool.Pop();
        var go = new GameObject(ex ? "ExRocket" : "Shot");
        go.transform.SetParent(root, false);
        go.SetActive(false);
        var r = go.AddComponent<SpriteRenderer>(); r.sprite = sprite; r.sortingOrder = 11;
        var c = go.AddComponent<BoxCollider2D>(); c.isTrigger = true; c.size = box;
        var b = go.AddComponent<Rigidbody2D>(); b.bodyType = RigidbodyType2D.Kinematic;
        var s = go.AddComponent<PlayerShot>();
        s.ex = ex;
        s.OnDone = x => { if (!x.gameObject.activeSelf) return; x.gameObject.SetActive(false); pool.Push(x); };
        return s;
    }

    // ---------------------------------------------------------------- super
    IEnumerator Super()
    {
        SuperActive = true;
        meter = 0f;
        if (GameState.I) GameState.I.Supers++;
        AudioManager.Sfx("super", 1f, 0f);
        CameraFx.HitStop(0.15f);
        FilmFilter.Flash(0.6f, 0.2f, Announcer.Yellow);
        if (beam == null)
        {
            var go = new GameObject("SuperBeam");
            beam = go.AddComponent<SpriteRenderer>();
            beam.sprite = beamSprite; beam.sortingOrder = 12;
            beam.drawMode = SpriteDrawMode.Tiled;
        }
        const float length = 15f;
        beam.gameObject.SetActive(true);
        float tick = 0f;
        var hit = new HashSet<Health>();
        for (float t = 0f; t < superTime; t += Time.deltaTime)
        {
            var gs = GameState.I;
            if (gs == null || !gs.Alive) break;
            var origin = Nose;
            float k = t < 0.1f ? t / 0.1f : t > superTime - 0.15f ? (superTime - t) / 0.15f : 1f;
            float h = 0.75f * k * (1f + 0.12f * Mathf.Sin(t * 60f));
            beam.size = new Vector2(length, 0.75f);
            beam.transform.position = origin + new Vector3(length / 2f, 0, 0);
            beam.transform.localScale = new Vector3(1f, h / 0.75f * 1.6f, 1f);
            CameraFx.Shake(0.07f, 0.1f);
            tick -= Time.deltaTime;
            if (tick <= 0f)
            {
                tick = superTick;
                hit.Clear();
                foreach (var c in Physics2D.OverlapBoxAll(beam.transform.position, new Vector2(length, 1.1f), 0f))
                {
                    var b = c.GetComponent<Bullet>();
                    if (b != null) { b.Done(); continue; }
                    var hp = c.GetComponentInParent<Health>();
                    if (hp != null && hp.Vulnerable && !hp.Dead && hit.Add(hp)) hp.Hit(superDamagePerTick);
                }
                var kit = GameKit.I;
                if (kit) GameKit.Burst(kit.hitSpark, origin + new Vector3(Random.Range(1f, 9f), Random.Range(-0.4f, 0.4f), 0), 24f, 1.4f, 13);
            }
            yield return null;
        }
        beam.gameObject.SetActive(false);
        SuperActive = false;
    }
}
