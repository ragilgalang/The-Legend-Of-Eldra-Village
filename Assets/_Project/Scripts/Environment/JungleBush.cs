using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Semak rimba prosedural: auto-rebuild jika mesh hilang saat domain reload / Play Mode.
/// </summary>
public class JungleBush : MonoBehaviour
{
    public int seed;

    void Start()
    {
        var mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Rebuild();
        }
    }

    public void Rebuild()
    {
        int s = seed != 0 ? seed : Mathf.Abs(Mathf.RoundToInt(transform.position.x * 127f + transform.position.z * 61f)) + 1;
        Random.State old = Random.state;
        Random.InitState(s);

        var parts = new List<CombineInstance>();
        Mesh lumpy = JungleTree.LumpySphere();
        if (lumpy == null) { Random.state = old; return; }

        int n = Random.Range(3, 6);
        for (int i = 0; i < n; i++)
        {
            Vector3 p = new Vector3(Random.Range(-1.1f, 1.1f), Random.Range(0.3f, 0.9f), Random.Range(-1.1f, 1.1f));
            float sz = Random.Range(1.0f, 1.9f);
            parts.Add(new CombineInstance
            {
                mesh = lumpy,
                transform = Matrix4x4.TRS(p, Quaternion.Euler(0, Random.value * 360f, 0), new Vector3(sz, sz * 0.6f, sz))
            });
        }

        var mf = GetComponent<MeshFilter>();
        if (mf == null) mf = gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = JungleTree.Combine(parts);

        var mr = GetComponent<MeshRenderer>();
        if (mr == null) mr = gameObject.AddComponent<MeshRenderer>();
        if (mr.sharedMaterial == null)
        {
            Color c = Color.Lerp(new Color(0.10f, 0.40f, 0.12f), new Color(0.25f, 0.55f, 0.18f), Random.value);
            mr.sharedMaterial = JungleTree.MakeMaterial(c, 0.1f);
        }

        Random.state = old;
    }
}
