using UnityEngine;

/// <summary>Screen shake, hit-stop and slow-mo. Owns Time.timeScale: everything else sets BaseScale instead.</summary>
[DefaultExecutionOrder(-50)]
public class CameraFx : MonoBehaviour
{
    public static CameraFx I;
    /// <summary>Global speed multiplier (the debug bot runs the game at 3x through this).</summary>
    public static float BaseScale = 1f;

    Vector3 home;
    float shakeAmp, shakeDur, shakeEnd;
    float stopUntil;
    float slowScale = 1f, slowUntil;

    void Awake() { I = this; home = transform.localPosition; Time.timeScale = BaseScale; }
    void OnDestroy() { if (I == this) I = null; }

    public static void Shake(float amp, float dur)
    {
        if (I == null) return;
        float now = Time.unscaledTime;
        float current = now < I.shakeEnd ? I.shakeAmp * (I.shakeEnd - now) / I.shakeDur : 0f;
        if (amp < current) return;
        I.shakeAmp = amp; I.shakeDur = dur; I.shakeEnd = now + dur;
    }

    public static void HitStop(float dur)
    {
        if (I == null) return;
        I.stopUntil = Mathf.Max(I.stopUntil, Time.unscaledTime + dur);
    }

    public static void SlowMo(float scale, float dur)
    {
        if (I == null) return;
        I.slowScale = scale; I.slowUntil = Time.unscaledTime + dur;
    }

    public static void ClearTime() { if (I != null) { I.stopUntil = 0f; I.slowUntil = 0f; } }

    void LateUpdate()
    {
        float now = Time.unscaledTime;
        Vector3 off = Vector3.zero;
        if (now < shakeEnd)
        {
            float a = shakeAmp * (shakeEnd - now) / shakeDur;
            var r = Random.insideUnitCircle * a;
            off = new Vector3(Mathf.Round(r.x * 32f) / 32f, Mathf.Round(r.y * 32f) / 32f, 0f);
        }
        transform.localPosition = home + off;

        float ts = BaseScale;
        if (now < slowUntil) ts *= slowScale;
        if (now < stopUntil) ts = 0f;
        Time.timeScale = ts;
    }
}
