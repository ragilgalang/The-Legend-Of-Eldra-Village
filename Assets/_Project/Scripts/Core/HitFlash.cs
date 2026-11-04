using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class HitFlash : MonoBehaviour
{
    [SerializeField] Renderer[] renderers;
    [SerializeField] Color flashColor = new Color(1f, 0.15f, 0.15f);
    [SerializeField] float duration = 0.12f;

    MaterialPropertyBlock block;
    float lastHp;
    Coroutine routine;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor"); // URP Lit

    void Awake()
    {
        block = new MaterialPropertyBlock();
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>();

        var h = GetComponent<Health>();
        lastHp = h.max;
        h.OnChanged += (cur, max) =>
        {
            if (cur < lastHp) Flash();
            lastHp = cur;
        };
    }

    void Flash()
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Do());
    }

    IEnumerator Do()
    {
        Set(flashColor);
        yield return new WaitForSeconds(duration);
        foreach (var r in renderers) r.SetPropertyBlock(null);   // kembali ke warna asli
    }

    void Set(Color c)
    {
        block.SetColor(BaseColor, c);
        foreach (var r in renderers) r.SetPropertyBlock(block);
    }
}

