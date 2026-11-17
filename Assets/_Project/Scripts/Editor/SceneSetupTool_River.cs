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
            new Vector3(-55f, 0f, 0f),
            width: 10f,
            length: 96f,
            fishCount: 24
        );
    }
}
#endif
