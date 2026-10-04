using UnityEngine;

/// <summary>Turns, swings and pulses a decoration: the sun rays, a lighthouse beam.</summary>
public class Spinner : MonoBehaviour
{
    public float speed = 10f;                    // continuous rotation, deg/s
    public float baseAngle;
    public float swingAmp, swingFreq = 0.25f;    // back-and-forth sweep (deg, Hz)
    public float pulseAmp, pulseFreq = 0.5f;     // scale breathing
    public float alphaAmp, alphaFreq = 0.5f;     // brightness breathing (all child sprites)
    SpriteRenderer[] rs;
    float[] a0;
    Vector3 s0;
    float angle, t;

    void Awake()
    {
        rs = GetComponentsInChildren<SpriteRenderer>();
        a0 = new float[rs.Length];
        for (int i = 0; i < rs.Length; i++) a0[i] = rs[i].color.a;
        s0 = transform.localScale;
        angle = baseAngle;
        t = Random.value * 10f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        t += dt;
        angle += speed * dt;
        float a = angle + (swingAmp != 0f ? Mathf.Sin(t * swingFreq * Mathf.PI * 2f) * swingAmp : 0f);
        transform.localRotation = Quaternion.Euler(0f, 0f, a);
        if (pulseAmp != 0f) transform.localScale = s0 * (1f + Mathf.Sin(t * pulseFreq * Mathf.PI * 2f) * pulseAmp);
        if (alphaAmp != 0f)
        {
            float k = 1f + Mathf.Sin(t * alphaFreq * Mathf.PI * 2f) * alphaAmp;
            for (int i = 0; i < rs.Length; i++) { var c = rs[i].color; c.a = Mathf.Clamp01(a0[i] * k); rs[i].color = c; }
        }
    }
}
