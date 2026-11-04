using System.Collections;
using UnityEngine;

public class NightSpawner : MonoBehaviour
{
    [SerializeField] GameObject[] enemyPrefabs;
    [SerializeField] Transform[] forestSpawnPoints;
    [SerializeField] int baseCount = 4;
    [SerializeField] float waveInterval = 25f;

    readonly System.Collections.Generic.List<GameObject> alive = new();
    Coroutine routine;

    void OnEnable()
    {
        if (DayNightCycle.Instance) DayNightCycle.Instance.OnPhaseChanged += OnPhase;
    }

    void OnDisable()
    {
        if (DayNightCycle.Instance) DayNightCycle.Instance.OnPhaseChanged -= OnPhase;
    }

    void OnPhase(TimePhase p)
    {
        if (p == TimePhase.Night) routine = StartCoroutine(Waves());
        else if (p == TimePhase.Dawn) { if (routine != null) StopCoroutine(routine); RetreatAll(); }
    }

    IEnumerator Waves()
    {
        int wave = 0;
        while (true)
        {
            if (forestSpawnPoints == null || forestSpawnPoints.Length == 0 ||
                enemyPrefabs == null || enemyPrefabs.Length == 0) yield break;

            int dayCount = DayNightCycle.Instance ? DayNightCycle.Instance.DayCount : 1;
            int count = baseCount + dayCount * 2 + wave;
            for (int i = 0; i < count; i++)
            {
                var sp = forestSpawnPoints[Random.Range(0, forestSpawnPoints.Length)];
                var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                if (sp && prefab) alive.Add(Instantiate(prefab, sp.position, sp.rotation));
                yield return new WaitForSeconds(0.6f);
            }
            wave++;
            yield return new WaitForSeconds(waveInterval);
        }
    }

    void RetreatAll()
    {
        foreach (var e in alive) if (e) Destroy(e, 3f); // mundur lalu hilang
        alive.Clear();
    }
}

