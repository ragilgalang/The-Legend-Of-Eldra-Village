#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static partial class SceneSetupTool
{
    // ─────────────────────────────────────────────────────────────────────
    //  B. SUNGAI DESA ELDARA (ELDARA GRAND RIVER) & JEMBATAN KAYU LENGKUNG
    // ─────────────────────────────────────────────────────────────────────
    static void BuildVillageBrookAndBridge(Transform parent, Material waterMat, Material woodMat, Material stoneMat)
    {
        Eldara.EldaraRiverBuilder.BuildEldaraRiver(
            parent,
            new Vector3(-49f, 0f, 0f), // Posisi sungai di sisi barat desa
            width: 14f,                // Lebar sungai 14 meter
            length: 102f,              // Membentang dari selatan ke utara benteng
            fishCount: 24              // 24 ekor ikan 3D berenang aktif
        );
    }
}
#endif
