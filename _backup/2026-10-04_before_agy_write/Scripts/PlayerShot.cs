using UnityEngine;

/// <summary>Player bullet: flies right, damages any Health (boss, minion) or shootable bullet. Pooled by PlayerWeapons.</summary>
public class PlayerShot : MonoBehaviour
{
    public float speed = 16f;
    public float damage = 1f;
    /// <summary>EX rocket: explodes on contact, splash damage.</summary>
    public bool ex;
    public System.Action<PlayerShot> OnDone;

    void Update()
    {
        transform.Translate(Vector3.right * speed * Time.deltaTime, Space.World);
        if (transform.position.x > 8.5f) OnDone?.Invoke(this);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var b = other.GetComponent<Bullet>();
        if (b != null)
        {
            if (b.TakeHit(damage)) Impact(other.transform.position);
            return;
        }
        var h = other.GetComponentInParent<Health>();
        if (h == null || !h.Vulnerable || h.Dead) return;
        h.Hit(damage);
        PlayerWeapons.Charge(ex ? 0f : damage);
        Impact(transform.position, h);
    }

    void Impact(Vector3 at, Health direct = null)
    {
        var kit = GameKit.I;
        if (ex)
        {
            // splash: shoots down nearby shootable bullets and chips other targets (minions next to the boss)
            foreach (var c in Physics2D.OverlapCircleAll(at, 1.2f))
            {
                var b = c.GetComponent<Bullet>();
                if (b != null) { b.TakeHit(damage * 0.5f); continue; }
                var h = c.GetComponentInParent<Health>();
                if (h != null && h != direct && h.Vulnerable && !h.Dead) h.Hit(damage * 0.5f);
            }
            if (kit) GameKit.Burst(kit.explosion, at, 18f, 1.3f, 21);
            AudioManager.Sfx("explosion", 0.8f);
            CameraFx.Shake(0.12f, 0.25f);
        }
        else
        {
            if (kit) GameKit.Burst(kit.hitSpark, at + new Vector3(0.25f, 0, 0), 24f, 1f, 21);
            AudioManager.Sfx("hit", 0.35f, 0.12f, 0.05f);
        }
        OnDone?.Invoke(this);
    }
}
