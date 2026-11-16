using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Memunculkan 10 NPC desa dalam formasi barisan. Pasang di GameObject kosong,
/// lalu klik kanan komponen > "Spawn 10 NPC".
/// </summary>
public class NpcDesaSpawner : MonoBehaviour
{
    public const string NamaWadah = "NPC_Desa";

    [Tooltip("Putar 180 derajat agar NPC menghadap kamera Scene default (arah -Z).")]
    public bool hadapKamera = true;
    [Tooltip("Pasang animasi napas pada tiap NPC.")]
    public bool animasiNapas = true;
    public bool spawnSaatPlay = true;

    void Start()
    {
        if (spawnSaatPlay && transform.Find(NamaWadah) == null) Spawn();
    }

    [ContextMenu("Spawn 10 NPC")]
    public void Spawn()
    {
        Hapus();
        var wadah = new GameObject(NamaWadah).transform;
        wadah.SetParent(transform, false);

        var daftar = NpcDesaKatalog.Semua();
        foreach (var slot in NpcDesaKatalog.Barisan)
        {
            var go = NpcDesaBuilder.Bangun(daftar[slot.indeks], wadah);
            float x = hadapKamera ? -slot.x : slot.x;
            go.transform.localPosition = new Vector3(x, 0f, slot.z);
            go.transform.localRotation = Quaternion.Euler(0f, hadapKamera ? 180f : 0f, 0f);

            var idle = go.AddComponent<KiWiraIdle>();
            idle.napas = animasiNapas;
            idle.waktuAwal = Random.value * 10f;
        }
    }

    [ContextMenu("Hapus NPC")]
    public void Hapus()
    {
        var lama = transform.Find(NamaWadah);
        if (lama != null) KiWiraUtil.SafeDestroy(lama.gameObject);
    }
}
