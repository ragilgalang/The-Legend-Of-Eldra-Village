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
        // Panggil EldaraRiverBuilder untuk sungai megah + jembatan + dermaga nelayan + kawanan ikan 3D
        Eldara.EldaraRiverBuilder.BuildEldaraRiver(parent, new Vector3(-37f, 0f, 0f), 14f, 108f, 24);
    }
}
#endif
