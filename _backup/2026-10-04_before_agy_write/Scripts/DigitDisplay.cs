using System.Collections.Generic;
using UnityEngine;

/// <summary>Draws a number with the pixel digit sheet (no UI canvas, no text mesh).</summary>
public class DigitDisplay : MonoBehaviour
{
    public Sprite[] digits;
    public int sortingOrder = 100;
    public float spacing = 0.5f;
    readonly List<SpriteRenderer> pool = new List<SpriteRenderer>();

    public void Show(int value)
    {
        string s = value.ToString();
        float w = (s.Length - 1) * spacing;
        for (int i = 0; i < s.Length; i++)
        {
            if (i >= pool.Count)
            {
                var go = new GameObject("d");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = sortingOrder;
                pool.Add(sr);
            }
            pool[i].gameObject.SetActive(true);
            pool[i].sprite = digits[s[i] - '0'];
            pool[i].transform.localPosition = new Vector3(-w / 2f + i * spacing, 0, 0);
        }
        for (int i = s.Length; i < pool.Count; i++) pool[i].gameObject.SetActive(false);
    }
}
