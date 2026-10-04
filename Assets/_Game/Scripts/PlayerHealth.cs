using System;
using UnityEngine;

/// <summary>
/// Combat hit points (the Flappy phase stays one-hit). A hit costs 1 HP and gives 1.5 s of blinking i-frames.
/// Dashing is invulnerable, and dashing into a pink (parryable) bullet is a parry: +1 super card, hit-stop, refunded dash.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth I { get; private set; }
    public static event Action<int> HpChanged;
    public static event Action Parried;

    public int maxHp = 3;
    public float invulnTime = 1.5f;

    PlayerController pc;
    SpriteRenderer sr;
    float invulnUntil;

    public int Hp { get; private set; }
    public bool Invulnerable => Time.time < invulnUntil || pc.Dashing || PlayerWeapons.SuperActive;

    void Awake()
    {
        I = this;
        pc = GetComponent<PlayerController>();
        sr = GetComponent<SpriteRenderer>();
        Hp = maxHp;
    }

    void OnDestroy() { if (I == this) { I = null; HpChanged = null; Parried = null; } }

    public void Touch(Collider2D other)
    {
        var b = other.GetComponent<Bullet>();
        if (b != null && b.parryable && pc.Dashing) { Parry(b); return; }
        if (other.GetComponent<Hazard>() == null || Invulnerable) return;
        var osr = other.GetComponent<SpriteRenderer>();
        Hurt(other.name + (osr && osr.sprite ? "/" + osr.sprite.name : ""));
        if (b != null) b.Done();
    }

    void Parry(Bullet b)
    {
        var at = b.transform.position;
        b.Done();
        pc.ResetDash();
        PlayerWeapons.AddCard();
        if (GameState.I) GameState.I.Parries++;
        CameraFx.HitStop(0.1f);
        CameraFx.Shake(0.08f, 0.15f);
        AudioManager.Sfx("parry", 1f, 0f);
        var kit = GameKit.I;
        if (kit) GameKit.Pop(kit.parryStar, at, 0.3f, 1.2f, 0.8f, 21, Announcer.Pink);
        Debug.Log("[Parry] cards=" + PlayerWeapons.Cards);
        Parried?.Invoke();
    }

    void Hurt(string by)
    {
        var gs = GameState.I;
        if (gs == null || !gs.Alive) return;
        gs.HitsTaken++;
        invulnUntil = Time.time + invulnTime;
        if (!GameState.God) Hp--;
        Debug.Log("[Player] hp=" + Hp + " by=" + by + " at=" + (Vector2)transform.position);
        HpChanged?.Invoke(Hp);
        if (Hp <= 0) { pc.Die(); return; }
        AudioManager.Sfx("hurt", 0.9f);
        CameraFx.Shake(0.18f, 0.35f);
        CameraFx.HitStop(0.08f);
        FilmFilter.Flash(0.35f, 0.12f, Announcer.Red);
        var kit = GameKit.I;
        if (kit) GameKit.Burst(kit.hitSpark, transform.position, 20f, 1.6f, 21, Announcer.Red);
    }

    void LateUpdate()
    {
        var gs = GameState.I;
        bool blink = gs != null && gs.Alive && Time.time < invulnUntil;
        var c = sr.color;
        c.a = blink && Mathf.Repeat(Time.time * 14f, 1f) < 0.5f ? 0.25f : 1f;
        sr.color = c;
    }
}
