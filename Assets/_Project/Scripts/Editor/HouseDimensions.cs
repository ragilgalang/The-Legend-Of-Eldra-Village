using UnityEngine;

/// <summary>
/// Pusat semua ukuran yang berhubungan dengan kenyamanan player di dalam rumah.
/// Ubah angka di sini, lalu jalankan ulang Eldara > Setup Prototype Scene;
/// semua pintu, tinggi dinding minimum, dan tangga di ProceduralHouseBuilder mengikuti otomatis.
/// </summary>
public static class HouseDimensions
{
    // ── Ukuran player (samakan dengan CharacterController di SetupPlayer) ──
    public const float PlayerHeight = 2.0f;
    public const float PlayerRadius = 0.5f;
    public const float PlayerStepOffset = 0.3f;   // stepOffset default CharacterController

    // ── Kelegaan ──
    /// <summary>Jarak kosong di atas kepala player saat lewat pintu (naikkan jika masih terasa mepet / kamera menabrak).</summary>
    public const float HeadClearance = 0.7f;
    /// <summary>Jarak kosong di kiri-kanan badan player saat lewat pintu.</summary>
    public const float SideClearance = 0.35f;

    // ── Pintu ──
    public const float DoorHeight = PlayerHeight + HeadClearance;                  // 2.7 m
    public const float DoorWidth  = PlayerRadius * 2f + SideClearance * 2f;        // 1.7 m

    // ── Dinding ──
    /// <summary>Balok minimal di atas lubang pintu/jendela agar tembok tetap menyatu dengan atap.</summary>
    public const float LintelMin = 0.12f;
    /// <summary>Tinggi dinding minimum agar pintu standar muat.</summary>
    public const float MinWallHeight = DoorHeight + LintelMin;                     // 2.82 m

    // ── Tangga ──
    /// <summary>Tinggi maksimum satu anak tangga (harus di bawah PlayerStepOffset).</summary>
    public const float MaxStairRise = 0.22f;
    public const float StairRun     = 0.30f;
    public const float StairWidth   = 1.6f;

    /// <summary>Jumlah anak tangga yang dibutuhkan untuk naik setinggi totalHeight.</summary>
    public static int StairSteps(float totalHeight)
        => Mathf.Max(1, Mathf.CeilToInt(totalHeight / MaxStairRise));
}
