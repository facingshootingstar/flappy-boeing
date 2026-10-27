using UnityEngine;

/// <summary>
/// Poe. Flappy phase (score under 30): tap = lift, gravity always pulls down, one hit kills.
/// Combat (boss gauntlet): WASD / arrows move freely, Shift / L dashes (short invulnerability, dash into pink = parry).
/// Shooting lives in PlayerWeapons, hit points and parry in PlayerHealth. Collider is the fuselage only.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteAnimator))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController I { get; private set; }

    public float lift = 7.2f;
    public float gravityScale = 2.6f;
    public float maxFall = -9f;
    public Sprite[] fly, climb, dive;
    public Sprite hit;
    public float ceiling = 4.4f, floor = -4.4f;

    [Header("Combat")]
    public float moveSpeed = 6.5f;
    public Vector2 combatX = new Vector2(-6.8f, 2.0f);
    public float combatY = 3.9f;
    public float dashSpeed = 18f, dashTime = 0.18f, dashCooldown = 0.45f;

    Rigidbody2D rb;
    SpriteAnimator anim;
    SpriteRenderer sr;
    CapsuleCollider2D cap;
    PlayerHealth health;
    Vector3 baseScale;
    bool combat;
    float dashT, dashReady, ghostT;
    Vector2 dashDir;

    public bool Combat => combat;
    public bool Dashing => dashT > 0f;
    public float DashReadyIn => Mathf.Max(0f, dashReady - Time.time);
    public SpriteRenderer Renderer => sr;

    void Awake()
    {
        I = this;
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<SpriteAnimator>();
        sr = GetComponent<SpriteRenderer>();
        cap = GetComponent<CapsuleCollider2D>();
        health = GetComponent<PlayerHealth>();
        baseScale = transform.localScale;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
    }

    void Start()
    {
        // The scene sheet is the old pixel plane. The animator reapplies it every frame until Play() swaps frames.
        const float PoeH = 1.4f;
        var set = PropArt.PoeSet("fly", 4, PoeH);
        if (set != null)
        {
            fly = set;
            climb = PropArt.PoeSet("climb", 1, PoeH) ?? fly;
            dive = PropArt.PoeSet("dive", 1, PoeH) ?? fly;
            var hitSet = PropArt.PoeSet("hit", 1, PoeH);
            if (hitSet != null) hit = hitSet[0];
            sr.sortingOrder = 16;
        }
        if (fly != null && fly.Length > 0) anim.Play(fly, 12f);
        if (MiniGame.Bot && GameState.I != null && !GameState.I.Started)
        {
            var ap = GetComponent<AutoPilot>();
            if (!ap) ap = gameObject.AddComponent<AutoPilot>();
            if (!ap.player) ap.player = transform;
            GameState.I.Begin();
            rb.gravityScale = gravityScale;
        }
    }

    void OnDestroy() { if (I == this) I = null; }
    void OnEnable() { GameState.CombatStarted += EnterCombat; }
    void OnDisable() { GameState.CombatStarted -= EnterCombat; }

    void EnterCombat()
    {
        combat = true;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        transform.rotation = Quaternion.identity;
        if (cap) cap.size = new Vector2(0.8f, 0.3f);   // small hitbox for danmaku
    }

    /// <summary>Director moves the jet between bosses (behind the iris).</summary>
    public void Place(Vector2 p)
    {
        transform.position = p;
        rb.linearVelocity = Vector2.zero;
        dashT = 0f;
    }

    void Update()
    {
        var gs = GameState.I;
        if (gs == null || !gs.Alive) return;
        if (combat) { CombatUpdate(); return; }
        if (!gs.Started)
        {
            if (fly != null && fly.Length > 0) anim.Play(fly, 12f);
            transform.position = new Vector3(transform.position.x, Mathf.Sin(Time.time * 3f) * 0.25f, 0);
            if (TapInput.Pressed()) { gs.Begin(); rb.gravityScale = gravityScale; Lift(); }
            return;
        }
        if (TapInput.Pressed()) Lift();
        var v = rb.linearVelocity;
        v.y = Mathf.Max(v.y, maxFall);
        rb.linearVelocity = v;
        anim.Play(v.y > 1.5f ? climb : v.y < -2.5f ? dive : fly, v.y > 1.5f ? 12f : 14f);
        if (transform.position.y > ceiling || transform.position.y < floor) Die();
    }

    void CombatUpdate()
    {
        float dt = Time.deltaTime;
        Vector2 m = CombatInput.Move();
        if (PlayerWeapons.SuperActive) { rb.linearVelocity = Vector2.zero; m = Vector2.zero; }
        else if (dashT > 0f)
        {
            dashT -= dt;
            rb.linearVelocity = dashDir * dashSpeed;
            ghostT -= dt;
            if (ghostT <= 0f)
            {
                ghostT = 0.03f;
                GameKit.Pop(sr.sprite, transform.position, 0.2f, 1f, 0f, 9, new Color(0.49f, 0.82f, 0.78f, 0.7f));
            }
        }
        else
        {
            rb.linearVelocity = m * moveSpeed;
            if (CombatInput.Dash() && Time.time >= dashReady) StartDash(m);
        }
        var p = transform.position;
        p.x = Mathf.Clamp(p.x, combatX.x, combatX.y);
        p.y = Mathf.Clamp(p.y, -combatY, combatY);
        transform.position = p;
        anim.Play(m.y > 0.3f ? climb : m.y < -0.3f ? dive : fly, 10f);
    }

    void StartDash(Vector2 m)
    {
        dashDir = m.sqrMagnitude > 0.01f ? m.normalized : Vector2.right;
        dashT = dashTime;
        dashReady = Time.time + dashTime + dashCooldown;
        ghostT = 0f;
        AudioManager.Sfx("dash", 0.6f, 0.08f);
        var kit = GameKit.I;
        if (kit) GameKit.Pop(kit.windStreak, transform.position - (Vector3)dashDir * 0.6f, 0.2f, 1f, 0.4f, 9,
            Color.white, Mathf.Atan2(dashDir.y, dashDir.x) * Mathf.Rad2Deg);
    }

    /// <summary>Parry refunds the dash so chains feel snappy.</summary>
    public void ResetDash() { dashReady = Time.time; }

    void LateUpdate()
    {
        var gs = GameState.I;
        if (gs == null || combat) return;
        if (!gs.Alive || !gs.Started)
        {
            transform.rotation = Quaternion.identity;
            transform.localScale = baseScale;
            return;
        }
        float vy = rb.linearVelocity.y;
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(vy * 3.4f, -28f, 34f));
        float sy = Mathf.Clamp(1f + vy * 0.02f, 0.84f, 1.18f);
        transform.localScale = new Vector3(baseScale.x / sy, baseScale.y * sy, baseScale.z);
    }

    void Lift()
    {
        var v = rb.linearVelocity; v.y = lift; rb.linearVelocity = v;
        AudioManager.Sfx("flap", 0.5f, 0.08f);
        if (GameKit.I != null && GameKit.I.smoke != null && GameKit.I.smoke.Length > 0)
            GameKit.Pop(GameKit.I.smoke[0], transform.position + new Vector3(-0.72f, -0.32f, 0f), 0.32f, 0.55f, 0.9f, 9,
                new Color(1f, 0.96f, 0.9f, 0.75f));
    }

    void OnTriggerEnter2D(Collider2D other) { Touch(other); }
    void OnTriggerStay2D(Collider2D other) { if (combat) Touch(other); }   // boss bodies keep hurting after i-frames
    void OnCollisionEnter2D(Collision2D c) { Touch(c.collider); }

    void Touch(Collider2D other)
    {
        var gs = GameState.I;
        if (gs == null || !gs.Alive) return;
        if (combat) { if (health) health.Touch(other); return; }
        if (other.GetComponent<Hazard>() != null) Die();
    }

    /// <summary>Final blow: Flappy hit, or the last combat HP.</summary>
    public void Die()
    {
        var gs = GameState.I;
        if (gs == null || !gs.Alive || GameState.God) return;
        gs.Kill();
        AudioManager.Sfx("death", 0.9f, 0f);
        AudioManager.Sfx("hurt"); CameraFx.Shake(0.25f, 0.5f); CameraFx.HitStop(0.12f);
        anim.enabled = false;
        if (hit) sr.sprite = hit;
        sr.color = Color.white;
        rb.gravityScale = gravityScale;
        rb.linearVelocity = new Vector2(0, 2f);
    }
}
