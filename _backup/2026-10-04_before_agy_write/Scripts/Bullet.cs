using System;
using UnityEngine;

/// <summary>Enemy projectile (pooled by BulletPool). Optional gravity, lifetime with an expire callback, parry and hit points.</summary>
public class Bullet : Hazard
{
    public BulletPool.Kind kind;
    public Vector2 velocity;
    public float gravity;
    public bool faceVelocity;
    public float rotOffset;       // degrees added when facing velocity (sprite drawn pointing left = 180)
    public float spin;
    public bool parryable;
    public float hp;              // > 0: the player can shoot it down
    public float lifetime = -1f;
    public Action<Bullet> onExpire;
    public Action<Bullet> OnDone;

    float age;
    SpriteRenderer sr;

    public float Age => age;
    public bool Shootable => hp > 0f;

    public void ResetState()
    {
        age = 0f; gravity = 0f; spin = 0f; faceVelocity = false; rotOffset = 0f; parryable = false; hp = 0f; lifetime = -1f; onExpire = null;
        transform.rotation = Quaternion.identity;
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        sr.color = Color.white;
    }

    void Update()
    {
        var gs = GameState.I;
        if (gs != null && !gs.Alive) return;
        float dt = Time.deltaTime;
        age += dt;
        velocity.y -= gravity * dt;
        transform.position += (Vector3)(velocity * dt);
        if (faceVelocity && velocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg + rotOffset);
        else if (spin != 0f) transform.Rotate(0, 0, spin * dt);

        if (lifetime > 0f && age >= lifetime)
        {
            var cb = onExpire; onExpire = null;
            cb?.Invoke(this);
            Done();
            return;
        }
        var p = transform.position;
        if (p.x < -9.5f || p.x > 10f || p.y < -6.5f || p.y > 8f) Done();
    }

    public void Done() { OnDone?.Invoke(this); }

    /// <summary>Player shot hit this bullet. Returns true if it absorbed the shot.</summary>
    public bool TakeHit(float dmg)
    {
        if (!Shootable) return false;
        hp -= dmg;
        if (hp <= 0f)
        {
            GameKit.Burst(GameKit.I ? GameKit.I.explosion : null, transform.position, 20f, 0.6f);
            AudioManager.Sfx("explosion", 0.5f, 0.1f);
            var cb = onExpire; onExpire = null;
            cb?.Invoke(this);
            Done();
        }
        else if (sr) sr.color = new Color(1f, 0.6f, 0.6f);
        return true;
    }

    void LateUpdate() { if (sr && sr.color != Color.white && Shootable) sr.color = Color.Lerp(sr.color, Color.white, 0.3f); }
}
