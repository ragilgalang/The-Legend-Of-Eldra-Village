#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menu: Eldara > Perbaiki Pohon Rimba
/// Membangun ulang SEMUA JungleTree yang sudah ada di scene dengan kode terbaru
/// (batang tidak terbalik, tanpa retakan, collider padat). Tata letak desa tidak berubah.
/// </summary>
public static class JungleTreeFixTool
{
    public static void Fix()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("Stop!", "Hentikan Play Mode dulu.", "OK");
            return;
        }

        var trees = Object.FindObjectsOfType<JungleTree>(true);
        foreach (var t in trees)
        {
            Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Perbaiki Pohon Rimba");
            int seed = t.builtSeed != 0
                ? t.builtSeed
                : Mathf.Abs(Mathf.RoundToInt(t.transform.position.x * 131f + t.transform.position.z * 71f)) + 1;
            t.Build(seed);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Eldara] " + trees.Length + " pohon rimba dibangun ulang dengan batang & collider baru.");
        EditorUtility.DisplayDialog("Pohon Diperbaiki", trees.Length + " pohon rimba dibangun ulang.\nSimpan scene (Ctrl+S).", "OK");
    }
}
#endif
