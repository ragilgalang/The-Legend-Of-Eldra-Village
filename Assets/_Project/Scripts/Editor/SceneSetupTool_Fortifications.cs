#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Pembangun Menara Pengawas Kayu (Watchtower) & Ballista Pertahanan Otomatis.
/// </summary>
public static class WatchtowerBuilder
{
    public static GameObject BuildWatchtower(Vector3 pos, Quaternion rot, Transform parent, string name)
    {
        var towerObj = SceneSetupTool.InstantiateArtModel("EA03_Village_Tover_01a", pos, rot, new Vector3(1.15f, 1.15f, 1.15f), parent);
        if (!towerObj)
        {
            towerObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            towerObj.transform.position = pos + new Vector3(0, 2f, 0);
            towerObj.transform.rotation = rot;
            towerObj.transform.localScale = new Vector3(2f, 4f, 2f);
            SceneSetupTool.ApplyColor(towerObj, new Color(0.5f, 0.4f, 0.3f));
        }
        towerObj.name = name;
        var towerComp = towerObj.GetComponent<Tower>() ?? towerObj.AddComponent<Tower>();

        // Tangga akses ke dek menara: model EA03 sudah memiliki MeshCollider asli pada anak tangga
        SceneSetupTool.AddPorchLantern(towerObj.transform, new Vector3(0, 4.5f, 0), new Color(1f, 0.65f, 0.2f), 12f);

        // Kepala Ballista otomatis di dek atas menara
        var ballistaHead = new GameObject("Ballista_Head");
        ballistaHead.transform.SetParent(towerObj.transform, false);
        ballistaHead.transform.localPosition = new Vector3(0f, 4.2f, 0f);
        ballistaHead.transform.localRotation = Quaternion.identity;

        var mount = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mount.name = "Mount";
        mount.transform.SetParent(ballistaHead.transform, false);
        mount.transform.localPosition = Vector3.zero;
        mount.transform.localScale = new Vector3(0.5f, 0.25f, 0.5f);
        SceneSetupTool.ApplyColor(mount, new Color(0.22f, 0.15f, 0.08f));
        Object.DestroyImmediate(mount.GetComponent<Collider>());

        var bow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bow.name = "BowArms";
        bow.transform.SetParent(ballistaHead.transform, false);
        bow.transform.localPosition = new Vector3(0f, 0.2f, 0.2f);
        bow.transform.localScale = new Vector3(1.4f, 0.12f, 0.18f);
        SceneSetupTool.ApplyColor(bow, new Color(0.35f, 0.22f, 0.12f));
        Object.DestroyImmediate(bow.GetComponent<Collider>());

        var headField = typeof(Tower).GetField("head", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (headField != null) headField.SetValue(towerComp, ballistaHead.transform);

        return towerObj;
    }
}

public static partial class SceneSetupTool
{
    public static void RemovePerimeterFences()
    {
        int count = 0;
        var toRemove = new List<GameObject>();

        // Cari semua objek di scene (aktif maupun non-aktif)
        foreach (var go in Object.FindObjectsOfType<GameObject>(true))
        {
            if (go == null) continue;
            string n = go.name;

            if (n.StartsWith("EA03_Village_Fence") ||
                n == "Palisade_Wall" ||
                n == "Yard_Fence" ||
                n == "Village_Yard_Fences" ||
                n.StartsWith("Yard_"))
            {
                toRemove.Add(go);
            }
        }

        foreach (var go in toRemove)
        {
            if (go != null)
            {
                count++;
                Object.DestroyImmediate(go);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[Eldara] {count} objek pagar jejer-jejer berhasil dihapus dari scene.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  7. BENTENG & PERTAHANAN (8 MENARA PENGAWAS KAYU + BALLISTA OTOMATIS)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupFortifications(Material slavicMat)
    {
        RemovePerimeterFences();

        GameObject towerRoot = GameObject.Find("--- TOWERS ---");
        if (towerRoot) Object.DestroyImmediate(towerRoot);

        towerRoot = new GameObject("--- TOWERS ---");

        // 8 Titik Strategis Menara Pengawas Kayu (pos + rotasi Y menghadap arah pertahanan luar)
        var towerSlots = new (Vector3 pos, float yawDeg, string name)[]
        {
            // 2 Menara Kembar Gerbang Selatan (digeser lebih renggang ke X = +-8.0m agar lorong gerbang lapang & anti-stuck)
            (new Vector3(-8.0f, 0f, -54f), 180f, "Tower_Gate_West"),
            (new Vector3( 8.0f, 0f, -54f), 180f, "Tower_Gate_East"),

            // 4 Menara Sudut Benteng Utama (menghadap diagonal keluar benteng)
            (new Vector3(-64f,  0f,  55f), -45f, "Tower_NorthWest"),
            (new Vector3( 62f,  0f,  50f),  45f, "Tower_NorthEast"),
            (new Vector3(-62f,  0f, -54f),-135f, "Tower_SouthWest"),
            (new Vector3( 62f,  0f, -54f), 135f, "Tower_SouthEast"),

            // 2 Menara Sayap Barat & Timur (menghadap Barat = -90° & Timur = 90°)
            // Tower_WestFlank ditempatkan aman di daratan barat (X = -74m) agar tidak masuk ke alur sungai (X = -55m, lebar 10m)
            (new Vector3(-74f,  0f,   7.5f), -90f, "Tower_WestFlank"),
            (new Vector3( 62f,  0f,   7.5f),  90f, "Tower_EastFlank"),
        };

        foreach (var (pos, yawDeg, tName) in towerSlots)
        {
            WatchtowerBuilder.BuildWatchtower(pos, Quaternion.Euler(0f, yawDeg, 0f), towerRoot.transform, tName);
        }

        // Gerbang Masuk Selatan Megah (Lega 16m)
        var gateArch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gateArch.name = "SouthGate_Crossbeam";
        gateArch.transform.SetParent(towerRoot.transform);
        gateArch.transform.position = new Vector3(0f, 5.2f, -54f);
        gateArch.transform.localScale = new Vector3(17.0f, 0.55f, 0.7f);
        ApplyColor(gateArch, new Color(0.28f, 0.17f, 0.09f));
        Object.DestroyImmediate(gateArch.GetComponent<Collider>()); // Balok udara tidak perlu collider

        AddPorchLantern(towerRoot.transform, new Vector3(-3.8f, 4.7f, -54f), new Color(1f, 0.75f, 0.35f), 14f);
        AddPorchLantern(towerRoot.transform, new Vector3( 3.8f, 4.7f, -54f), new Color(1f, 0.75f, 0.35f), 14f);

        // Dinding Palisade Kayu Penghubung Menara
        void BuildPalisadeLine(Vector3 start, Vector3 end)
        {
            Vector3 diff = end - start;
            float dist = diff.magnitude;
            int count = Mathf.FloorToInt(dist / 2.2f);
            Quaternion rot = Quaternion.LookRotation(diff.normalized) * Quaternion.Euler(0, 90f, 0);
            for (int i = 1; i < count; i++)
            {
                Vector3 p = Vector3.Lerp(start, end, i / (float)count);
                var wall = InstantiateArtModel("EA03_Village_Fence_01a", p, rot, new Vector3(1.1f, 1.4f, 1.1f), towerRoot.transform, slavicMat);
                if (!wall)
                {
                    wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wall.name = "Palisade_Wall";
                    wall.transform.SetParent(towerRoot.transform);
                    wall.transform.position = p + Vector3.up * 1.4f;
                    wall.transform.rotation = rot;
                    wall.transform.localScale = new Vector3(2.2f, 2.8f, 0.35f);
                    ApplyColor(wall, new Color(0.32f, 0.20f, 0.11f));
                }
            }
        }

        // Hapus / nonaktifkan baris berikut di SetupFortifications():
        // BuildPalisadeLine(new Vector3(-62f, 0f, -54f), new Vector3(-7.8f, 0f, -54f));
        // BuildPalisadeLine(new Vector3(  7.8f, 0f, -54f), new Vector3(62f, 0f, -54f));
        // BuildPalisadeLine(new Vector3(-62f, 0f,  50f), new Vector3(-5.5f, 0f,  50f));
        // BuildPalisadeLine(new Vector3(  5.5f, 0f,  50f), new Vector3(62f, 0f,  50f));
        // BuildPalisadeLine(new Vector3(-62f, 0f, -54f), new Vector3(-62f, 0f, -4.5f));
        // BuildPalisadeLine(new Vector3(-62f, 0f,   4.5f), new Vector3(-62f, 0f,  50f));
        // BuildPalisadeLine(new Vector3( 62f, 0f, -54f), new Vector3( 62f, 0f, -4.5f));
        // BuildPalisadeLine(new Vector3( 62f, 0f,   4.5f), new Vector3( 62f, 0f,  50f));

        // Spike Traps di luar gerbang selatan (dipasang trigger murni agar tidak menghalangi pergerakan fisik)
        var trapPositions = new Vector3[]
        {
            new Vector3(-6.5f, 0.05f, -64f),
            new Vector3( 6.5f, 0.05f, -64f),
        };

        for (int i = 0; i < trapPositions.Length; i++)
        {
            var trap = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trap.name = $"SpikeTrap_{i + 1}";
            trap.transform.SetParent(towerRoot.transform);
            trap.transform.position = trapPositions[i];
            trap.transform.localScale = new Vector3(2.5f, 0.12f, 2.5f);
            ApplyColor(trap, new Color(0.25f, 0.25f, 0.35f));
            var col = trap.GetComponent<BoxCollider>();
            if (col) col.isTrigger = true; // Trigger aktif untuk damage musuh tanpa memblokir tabrakan player
            trap.AddComponent<SpikeTrap>();
        }
    }
}
#endif
