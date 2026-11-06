#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generator 11 Gaya Rumah Kayu Realistis untuk Legenda Desa Eldara (Porting dari Skrip Blender).
/// Menghadirkan 11 gaya arsitektur kayu tradisional dan modern dengan material URP, collider, dan lentera hangat:
/// 1. Cabin (Log Cabin balok kayu tersusun bertingkat, atap pelana, teras depan, cerobong batu)
/// 2. Modern (Villa kayu minimalis kaca lebar, 2 lantai cantilever, dek kolam refleksi)
/// 3. Panggung (Rumah Panggung pilar kayu tinggi 2m menopang lantai dek, tangga berundak)
/// 4. AFrame (A-Frame cabin segitiga curam dari tanah ke puncak, fasad kaca depan penuh & api unggun)
/// 5. Joglo (Joglo Tropis Jawa: 4 soko guru utama jati, atap tumpangsari bertingkat, kolam teratai)
/// 6. Danau (Pondok Danau dermaga kayu menjorok ke perairan dengan perahu kayu kecil)
/// 7. Barn (Barn Modern kayu hitam, pintu geser persilangan X, atap gambrel & silo gandum)
/// 8. Pohon (Rumah Pohon bertengger di dahan pohon raksasa, dek 5m, jembatan gantung)
/// 9. Gadang (Rumah Gadang Minangkabau: tiang miring, atap gonjong lengkung tanduk kerbau menjulang)
/// 10. Minka (Minka Tradisional Jepang: pintu shoji, atap limas hip roof, gerbang torii & kolam koi)
/// 11. Chalet (Chalet Salju Pegunungan: lantai batu alam tebal, balkon ukir kayu, mantel salju)
/// </summary>
public static class ProceduralHouseBuilder
{
    public enum HouseStyle
    {
        Cabin,
        Modern,
        Panggung,
        AFrame,
        Joglo,
        Danau,
        Barn,
        Pohon,
        Gadang,
        Minka,
        Chalet
    }

    private const string FolderHouses = "Assets/_Project/Art/ProceduralHouses";
    private const string FolderMats = "Assets/_Project/Art/HouseMaterials";

    // ─────────────────────────────────────────────────────────────────────
    //  MATERIAL SYSTEM (URP LIT PALETTES)
    // ─────────────────────────────────────────────────────────────────────

    private static Material matWood;
    private static Material matSlat;
    private static Material matBlack;
    private static Material matRedWood;
    private static Material matDeck;
    private static Material matRoof;
    private static Material matRoofThatch;
    private static Material matRoofJapanese;
    private static Material matConcrete;
    private static Material matGlass;
    private static Material matDark;
    private static Material matGold;
    private static Material matShoji;
    private static Material matLamp;
    private static Material matWater;
    private static Material matSnow;
    private static Material matLeaf;
    private static Material matPink;
    private static Material matRed;
    private static Material matMetal;
    private static Material matHay;
    private static Material matFabric;
    private static Material matOrange;
    private static Material matWall;
    private static Material[] matLogs;

    public static void EnsureMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Art"))
            AssetDatabase.CreateFolder("Assets", "_Project/Art");
        if (!AssetDatabase.IsValidFolder(FolderHouses))
            AssetDatabase.CreateFolder("Assets/_Project/Art", "ProceduralHouses");
        if (!AssetDatabase.IsValidFolder(FolderMats))
            AssetDatabase.CreateFolder("Assets/_Project/Art", "HouseMaterials");

        var litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        matWood         = GetOrCreateMat("M_House_Wood", litShader, new Color(0.38f, 0.22f, 0.12f), 0.25f);
        matSlat         = GetOrCreateMat("M_House_Slat", litShader, new Color(0.44f, 0.25f, 0.13f), 0.25f);
        matBlack        = GetOrCreateMat("M_House_Black", litShader, new Color(0.12f, 0.12f, 0.13f), 0.35f);
        matRedWood      = GetOrCreateMat("M_House_RedWood", litShader, new Color(0.52f, 0.22f, 0.11f), 0.25f);
        matDeck         = GetOrCreateMat("M_House_Deck", litShader, new Color(0.46f, 0.28f, 0.15f), 0.30f);
        matRoof         = GetOrCreateMat("M_House_Roof", litShader, new Color(0.22f, 0.15f, 0.11f), 0.20f);
        matRoofThatch   = GetOrCreateMat("M_House_RoofThatch", litShader, new Color(0.65f, 0.48f, 0.22f), 0.15f);
        matRoofJapanese = GetOrCreateMat("M_House_RoofJapanese", litShader, new Color(0.26f, 0.27f, 0.30f), 0.40f);
        matConcrete     = GetOrCreateMat("M_House_Concrete", litShader, new Color(0.60f, 0.60f, 0.58f), 0.15f);
        matDark         = GetOrCreateMat("M_House_Dark", litShader, new Color(0.08f, 0.08f, 0.09f), 0.45f, metallic: 0.3f);
        matGold         = GetOrCreateMat("M_House_Gold", litShader, new Color(0.85f, 0.65f, 0.15f), 0.70f, metallic: 0.85f);
        matShoji        = GetOrCreateMat("M_House_Shoji", litShader, new Color(0.95f, 0.92f, 0.85f), 0.15f);
        matSnow         = GetOrCreateMat("M_House_Snow", litShader, new Color(0.96f, 0.98f, 1.0f), 0.10f);
        matLeaf         = GetOrCreateMat("M_House_Leaf", litShader, new Color(0.18f, 0.45f, 0.12f), 0.15f);
        matPink         = GetOrCreateMat("M_House_Pink", litShader, new Color(0.92f, 0.60f, 0.70f), 0.15f);
        matRed          = GetOrCreateMat("M_House_Red", litShader, new Color(0.68f, 0.12f, 0.08f), 0.30f);
        matMetal        = GetOrCreateMat("M_House_Metal", litShader, new Color(0.72f, 0.74f, 0.76f), 0.75f, metallic: 0.85f);
        matHay          = GetOrCreateMat("M_House_Hay", litShader, new Color(0.78f, 0.62f, 0.25f), 0.10f);
        matFabric       = GetOrCreateMat("M_House_Fabric", litShader, new Color(0.20f, 0.25f, 0.30f), 0.10f);
        matOrange       = GetOrCreateMat("M_House_Orange", litShader, new Color(0.92f, 0.40f, 0.08f), 0.40f);
        matWall         = GetOrCreateMat("M_House_Wall", litShader, new Color(0.90f, 0.86f, 0.78f), 0.15f);

        // Lampu pijar hangat (Emissive)
        matLamp = GetOrCreateMat("M_House_Lamp", litShader, new Color(1.0f, 0.85f, 0.45f), 0.5f);
        matLamp.EnableKeyword("_EMISSION");
        matLamp.SetColor("_EmissionColor", new Color(1.0f, 0.85f, 0.45f) * 4.5f);

        // Kaca transparan URP (Glass)
        matGlass = GetOrCreateMat("M_House_Glass", litShader, new Color(0.65f, 0.80f, 0.90f, 0.35f), 0.95f, isTransparent: true);

        // Air kolam / danau (Water)
        matWater = GetOrCreateMat("M_House_Water", litShader, new Color(0.15f, 0.45f, 0.65f, 0.80f), 0.90f, isTransparent: true);

        // 4 variasi balok kayu log cabin
        matLogs = new Material[4];
        for (int i = 0; i < 4; i++)
        {
            Color logCol = Color.Lerp(new Color(0.35f, 0.20f, 0.10f), new Color(0.50f, 0.30f, 0.16f), i / 3.0f);
            matLogs[i] = GetOrCreateMat($"M_House_Log_{i}", litShader, logCol, 0.20f);
        }

        AssetDatabase.SaveAssets();
    }

    private static Material GetOrCreateMat(string name, Shader shader, Color color, float smoothness, float metallic = 0f, bool isTransparent = false)
    {
        string path = $"{FolderMats}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", metallic);

        if (isTransparent)
        {
            mat.SetFloat("_Surface", 1); // 1 = Transparent in URP Lit
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PRIMITIVE BUILDERS (BOX, CYLINDER, CONE, SPHERE, GABLE, HIP)
    // ─────────────────────────────────────────────────────────────────────

    private static GameObject Box(
        GameObject parent, float w, float h, float d, Material mat,
        Vector3 pos, Vector3 rotEuler = default, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(rotEuler);
        go.transform.localScale = new Vector3(w, h, d);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr)
        {
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
        }

        var col = go.GetComponent<BoxCollider>();
        if (col && !collider) Object.DestroyImmediate(col);

        return go;
    }

    private static GameObject Cylinder(
        GameObject parent, float r, float h, Material mat,
        Vector3 pos, Vector3 rotEuler = default, bool collider = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(rotEuler);
        // Unity primitive cylinder diameter 1, height 2
        go.transform.localScale = new Vector3(r * 2f, h * 0.5f, r * 2f);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr)
        {
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
        }

        var col = go.GetComponent<CapsuleCollider>();
        if (col && !collider) Object.DestroyImmediate(col);

        return go;
    }

    private static GameObject Sphere(
        GameObject parent, float r, Material mat,
        Vector3 pos, Vector3 scaleMultiplier = default)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        Vector3 sm = scaleMultiplier == default ? Vector3.one : scaleMultiplier;
        go.transform.localScale = new Vector3(r * 2f * sm.x, r * 2f * sm.y, r * 2f * sm.z);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr)
        {
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
        }

        var col = go.GetComponent<SphereCollider>();
        if (col) Object.DestroyImmediate(col);

        return go;
    }

    private static GameObject Cone(
        GameObject parent, float r, float h, Material mat,
        Vector3 pos, float ryDeg = 0f, int sides = 16)
    {
        var go = new GameObject("Cone");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, ryDeg, 0);

        string meshKey = $"Cone_{r:F1}_{h:F1}_{sides}";
        string meshPath = $"{FolderHouses}/Mesh_{meshKey}.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (!mesh)
        {
            mesh = CreateConeMesh(r, h, sides);
            mesh.name = meshKey;
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        var col = go.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;

        return go;
    }

    private static Mesh CreateConeMesh(float r, float h, int sides)
    {
        var m = new Mesh();
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs   = new List<Vector2>();
        var tris  = new List<int>();

        Vector3 apex = new Vector3(0, h * 0.5f, 0);
        Vector3 baseCenter = new Vector3(0, -h * 0.5f, 0);

        for (int i = 0; i < sides; i++)
        {
            float a0 = (i / (float)sides) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)sides) * Mathf.PI * 2f;

            Vector3 p0 = new Vector3(Mathf.Sin(a0) * r, -h * 0.5f, Mathf.Cos(a0) * r);
            Vector3 p1 = new Vector3(Mathf.Sin(a1) * r, -h * 0.5f, Mathf.Cos(a1) * r);

            Vector3 sideNorm = Vector3.Cross(p1 - p0, apex - p0).normalized;

            int start = verts.Count;
            verts.Add(apex); norms.Add(sideNorm); uvs.Add(new Vector2(0.5f, 1f));
            verts.Add(p0);   norms.Add(sideNorm); uvs.Add(new Vector2(0f, 0f));
            verts.Add(p1);   norms.Add(sideNorm); uvs.Add(new Vector2(1f, 0f));
            tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);

            // Base bottom cap
            start = verts.Count;
            verts.Add(baseCenter); norms.Add(Vector3.down); uvs.Add(new Vector2(0.5f, 0.5f));
            verts.Add(p1);         norms.Add(Vector3.down); uvs.Add(new Vector2(1f, 0f));
            verts.Add(p0);         norms.Add(Vector3.down); uvs.Add(new Vector2(0f, 0f));
            tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
        }

        m.SetVertices(verts);
        m.SetNormals(norms);
        m.SetUVs(0, uvs);
        m.SetTriangles(tris, 0);
        m.RecalculateBounds();
        return m;
    }

    private static GameObject GableTriangle(
        GameObject parent, float baseWidth, float height, float thickness,
        Material mat, Vector3 pos, float ryDeg = 0f)
    {
        var go = new GameObject("GableTriangle");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, ryDeg, 0);

        string meshKey = $"Gable_{baseWidth:F1}_{height:F1}_{thickness:F2}";
        string meshPath = $"{FolderHouses}/Mesh_{meshKey}.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (!mesh)
        {
            mesh = CreatePrismTriangleMesh(baseWidth, height, thickness);
            mesh.name = meshKey;
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        return go;
    }

    private static Mesh CreatePrismTriangleMesh(float w, float h, float t)
    {
        var m = new Mesh();
        float hw = w * 0.5f;
        float ht = t * 0.5f;

        // 3 front vertices, 3 back vertices
        Vector3[] v = new Vector3[]
        {
            // Front face (+Z)
            new Vector3(-hw, 0, ht), new Vector3(hw, 0, ht), new Vector3(0, h, ht),
            // Back face (-Z)
            new Vector3(-hw, 0, -ht), new Vector3(0, h, -ht), new Vector3(hw, 0, -ht)
        };

        int[] tris = new int[]
        {
            // Front
            0, 1, 2,
            // Back
            3, 4, 5,
            // Bottom
            0, 3, 5, 0, 5, 1,
            // Left slope
            0, 2, 4, 0, 4, 3,
            // Right slope
            1, 5, 4, 1, 4, 2
        };

        m.vertices = v;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private static GameObject HipRoof(
        GameObject parent, float hx, float hz, float ridgeLen, float yBase, float yPeak, Material mat)
    {
        var go = new GameObject("HipRoof");
        go.transform.SetParent(parent.transform, false);

        string meshKey = $"HipRoof_{hx:F1}_{hz:F1}_{ridgeLen:F1}_{yPeak - yBase:F1}";
        string meshPath = $"{FolderHouses}/Mesh_{meshKey}.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
        if (!mesh)
        {
            mesh = CreateHipRoofMesh(hx, hz, ridgeLen, yBase, yPeak);
            mesh.name = meshKey;
            AssetDatabase.CreateAsset(mesh, meshPath);
        }

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;

        var col = go.AddComponent<MeshCollider>();
        col.sharedMesh = mesh;

        return go;
    }

    private static Mesh CreateHipRoofMesh(float hx, float hz, float rx, float y0, float y1)
    {
        var m = new Mesh();
        Vector3[] v = new Vector3[]
        {
            new Vector3(-hx, y0,  hz), // 0 front left
            new Vector3( hx, y0,  hz), // 1 front right
            new Vector3( hx, y0, -hz), // 2 back right
            new Vector3(-hx, y0, -hz), // 3 back left
            new Vector3(-rx, y1,  0),  // 4 ridge left
            new Vector3( rx, y1,  0)   // 5 ridge right
        };

        int[] tris = new int[]
        {
            // Front trapezoid
            0, 1, 5,   0, 5, 4,
            // Back trapezoid
            2, 3, 4,   2, 4, 5,
            // Right triangle
            1, 2, 5,
            // Left triangle
            3, 0, 4,
            // Bottom ceiling
            0, 3, 2,   0, 2, 1
        };

        m.vertices = v;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private static void Window(GameObject parent, float w, float h, Vector3 pos, float ryDeg = 0f)
    {
        float rad = ryDeg * Mathf.Deg2Rad;
        Vector3 off = new Vector3(Mathf.Sin(rad) * 0.05f, 0, Mathf.Cos(rad) * 0.05f);

        // Frame
        Box(parent, w + 0.18f, h + 0.18f, 0.12f, matDark, pos, new Vector3(0, ryDeg, 0), false);
        // Glass
        Box(parent, w, h, 0.04f, matGlass, pos + off, new Vector3(0, ryDeg, 0), false);
    }

    private static void Lantern(GameObject parent, Vector3 pos, Color? customLightColor = null)
    {
        Color lightCol = customLightColor ?? new Color(1.0f, 0.85f, 0.45f);

        // Emissive Sphere
        Sphere(parent, 0.12f, matLamp, pos);

        // Realtime Point Light for atmospheric night glow
        var lightGo = new GameObject("Lantern_PointLight");
        lightGo.transform.SetParent(parent.transform, false);
        lightGo.transform.localPosition = pos;

        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = lightCol;
        light.intensity = 1.35f;
        light.range = 5.5f;
        light.shadows = LightShadows.None;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  MASTER DISPATCHER
    // ─────────────────────────────────────────────────────────────────────

    public static GameObject BuildHouse(
        HouseStyle style, Vector3 worldPos, Quaternion worldRot, Transform parent = null, string customName = null)
    {
        EnsureMaterials();

        string hName = customName ?? $"House_{style}";
        var root = new GameObject(hName);
        if (parent) root.transform.SetParent(parent);
        root.transform.position = worldPos;
        root.transform.rotation = worldRot;

        switch (style)
        {
            case HouseStyle.Cabin:    BuildCabin(root); break;
            case HouseStyle.Modern:   BuildModern(root); break;
            case HouseStyle.Panggung: BuildPanggung(root); break;
            case HouseStyle.AFrame:   BuildAFrame(root); break;
            case HouseStyle.Joglo:    BuildJoglo(root); break;
            case HouseStyle.Danau:    BuildDanau(root); break;
            case HouseStyle.Barn:     BuildBarn(root); break;
            case HouseStyle.Pohon:    BuildPohon(root); break;
            case HouseStyle.Gadang:   BuildGadang(root); break;
            case HouseStyle.Minka:    BuildMinka(root); break;
            case HouseStyle.Chalet:   BuildChalet(root); break;
        }

        return root;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  1. LOG CABIN (Pondok Kayu Klasik Rimba)
    // ─────────────────────────────────────────────────────────────────────
    private static void BuildCabin(GameObject root)
    {
        // Pondasi Batu
        Box(root, 8.7f, 0.55f, 6.7f, matConcrete, new Vector3(0, -0.175f, 0));
        // Badan Interior
        Box(root, 7.7f, 3.0f, 5.7f, matDark, new Vector3(0, 1.5f, 0));

        // Balok Log Kayu Bertumpuk (10 Baris Melingkar)
        float R = 0.17f;
        for (int i = 0; i < 10; i++)
        {
            float yF = 0.15f + 0.3f * i;
            float yS = 0.30f + 0.3f * i;
            Material mF = matLogs[i % 4];
            Material mS = matLogs[(i + 1) % 4];

            // Depan (Z = 3) dan Belakang (Z = -3) memanjang di sumbu X
            Cylinder(root, R, 8.7f, mF, new Vector3(0, yF, 3f), new Vector3(0, 0, 90f), false);
            Cylinder(root, R, 8.7f, mF, new Vector3(0, yF, -3f), new Vector3(0, 0, 90f), false);

            // Samping Kiri (X = -4) dan Kanan (X = 4) memanjang di sumbu Z
            Cylinder(root, R, 6.7f, mS, new Vector3(-4f, yS, 0), new Vector3(90f, 0, 0), false);
            Cylinder(root, R, 6.7f, mS, new Vector3(4f, yS, 0), new Vector3(90f, 0, 0), false);
        }

        // Jendela Kaca Depan & Samping
        Window(root, 1.4f, 1.1f, new Vector3(-2.6f, 1.55f, 3.05f), 0);
        Window(root, 1.4f, 1.1f, new Vector3(2.6f, 1.55f, 3.05f), 0);
        Window(root, 1.4f, 1.1f, new Vector3(-4.05f, 1.55f, -1.6f), -90f);
        Window(root, 1.4f, 1.1f, new Vector3(4.05f, 1.55f, 1.8f), 90f);

        // Pintu Kayu Masuk
        Box(root, 1.0f, 2.15f, 0.1f, matSlat, new Vector3(0, 1.075f, 3.2f), collider: false);
        Box(root, 0.12f, 2.25f, 0.3f, matDark, new Vector3(-0.56f, 1.1f, 3.2f), collider: false);
        Box(root, 0.12f, 2.25f, 0.3f, matDark, new Vector3(0.56f, 1.1f, 3.2f), collider: false);
        Box(root, 1.24f, 0.12f, 0.3f, matDark, new Vector3(0, 2.2f, 3.2f), collider: false);

        // Atap Pelana Miring
        float pitchAngle = Mathf.Atan(2.333f / 3.5f) * Mathf.Rad2Deg;
        Box(root, 8.9f, 0.14f, 4.206f, matRoof, new Vector3(0, 3.833f, 1.75f), new Vector3(pitchAngle, 0, 0));
        Box(root, 8.9f, 0.14f, 4.206f, matRoof, new Vector3(0, 3.833f, -1.75f), new Vector3(-pitchAngle, 0, 0));
        Box(root, 8.95f, 0.16f, 0.34f, matDark, new Vector3(0, 5.03f, 0), collider: false);

        // Gable Segitiga Samping
        GableTriangle(root, 6.2f, 2.05f, 0.15f, matSlat, new Vector3(-3.95f, 3.0f, 0), -90f);
        GableTriangle(root, 6.2f, 2.05f, 0.15f, matSlat, new Vector3(3.95f, 3.0f, 0), 90f);

        // Teras Depan
        Box(root, 8.8f, 0.75f, 2.2f, matDeck, new Vector3(0, -0.025f, 4.1f));
        Box(root, 8.9f, 0.12f, 2.6f, matRoof, new Vector3(0, 2.5f, 4.3f), new Vector3(5.5f, 0, 0));
        for (float x = -4.1f; x <= 4.11f; x += 2.73f)
        {
            Cylinder(root, 0.1f, 2.15f, matLogs[0], new Vector3(x, 1.42f, 5.3f));
        }

        // Tangga Teras
        Box(root, 1.8f, 0.25f, 0.6f, matDeck, new Vector3(0, -0.15f, 5.5f));
        Box(root, 1.8f, 0.15f, 0.6f, matDeck, new Vector3(0, -0.275f, 6.05f));

        // Lentera Teras
        Lantern(root, new Vector3(1.0f, 1.9f, 3.3f));

        // Cerobong Asap Batu
        Box(root, 1.1f, 6.0f, 1.5f, matConcrete, new Vector3(4.75f, 2.6f, 0));
        Box(root, 1.3f, 0.15f, 1.7f, matDark, new Vector3(4.75f, 5.65f, 0));

        // Tumpukan Kayu Bakar di Samping
        for (int r = 0; r < 4; r++)
        {
            for (int c = 0; c < 4 - r; c++)
            {
                Cylinder(root, 0.12f, 1.6f, matLogs[(r + c) % 4],
                    new Vector3(-4.65f, -0.25f + r * 0.21f, 0.7f + c * 0.25f + r * 0.125f),
                    new Vector3(90f, 0, 0), false);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  2. MODERN WOOD VILLA (Villa Kayu Kaca Minimalis)
    // ─────────────────────────────────────────────────────────────────────
    private static void BuildModern(GameObject root)
    {
        // Fondasi & Dek Lantai
        Box(root, 10.2f, 0.4f, 6.2f, matDark, new Vector3(0, -0.2f, 0));
        Box(root, 9.8f, 0.04f, 5.8f, matDeck, new Vector3(0, 0.02f, 0));

        // Dinding Kayu Slat
        Box(root, 10.0f, 3.0f, 0.2f, matSlat, new Vector3(0, 1.5f, -2.9f));
        Box(root, 3.0f, 3.0f, 6.0f, matSlat, new Vector3(-3.5f, 1.5f, 0));
        Box(root, 0.2f, 3.0f, 6.0f, matSlat, new Vector3(4.9f, 1.5f, 0));
        Box(root, 10.2f, 0.3f, 6.5f, matDark, new Vector3(0, 3.15f, 0.1f));

        // Fasad Kaca Besar Lantai 1
        Box(root, 6.8f, 2.9f, 0.04f, matGlass, new Vector3(1.4f, 1.5f, 3.0f), collider: false);
        for (int i = 0; i < 5; i++)
        {
            Box(root, 0.08f, 3.0f, 0.14f, matDark, new Vector3(-2f + i * 1.7f, 1.5f, 3.0f), collider: false);
        }

        // Lantai 2 Cantilever Box
        Box(root, 6.5f, 2.8f, 5.5f, matSlat, new Vector3(-2.25f, 4.7f, 0.75f));
        Box(root, 4.5f, 1.0f, 0.03f, matGlass, new Vector3(-2.55f, 4.75f, 3.55f), collider: false);
        Box(root, 7.2f, 0.25f, 6.2f, matDark, new Vector3(-2.25f, 6.2f, 0.75f));

        // Teras & Kolam Refleksi
        Box(root, 7.4f, 0.3f, 4.0f, matDeck, new Vector3(-2.3f, -0.15f, 5.0f));
        Box(root, 5.4f, 0.3f, 1.2f, matConcrete, new Vector3(4.1f, -0.15f, 3.6f));
        Box(root, 5.4f, 0.3f, 1.0f, matConcrete, new Vector3(4.1f, -0.15f, 7.7f));
        Box(root, 0.6f, 0.3f, 3.0f, matConcrete, new Vector3(1.7f, -0.15f, 5.7f));
        Box(root, 0.4f, 0.3f, 3.0f, matConcrete, new Vector3(6.4f, -0.15f, 5.7f));
        // Air Kolam
        Box(root, 4.2f, 0.02f, 3.0f, matWater, new Vector3(4.1f, -0.08f, 5.7f), collider: false);

        // Lampu Taman & Lentera Minimalis
        Lantern(root, new Vector3(0, 2.5f, -0.2f));
        Lantern(root, new Vector3(-5.6f, 0.3f, 7.8f));
        Lantern(root, new Vector3(7.0f, 0.3f, 8.6f));

        // Stepping Stones
        for (int i = 0; i < 6; i++)
        {
            Cylinder(root, 0.5f, 0.08f, matConcrete,
                new Vector3(Mathf.Sin(i * 1.6f) * 0.5f - 0.2f, -0.15f, 8.6f + i * 1.2f), collider: true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  3. RUMAH PANGGUNG (Panggung Kayu Nusantara)
    // ─────────────────────────────────────────────────────────────────────
    private static void BuildPanggung(GameObject root)
    {
        // 8 Pilar Kayu Tinggi (Tiang Stilt)
        float[] xs = { -3.6f, -1.2f, 1.2f, 3.6f };
        float[] zs = { -2.2f, 3.8f };
        foreach (var x in xs)
        {
            foreach (var z in zs)
            {
                Cylinder(root, 0.18f, 2.5f, matWood, new Vector3(x, 1.0f, z));
            }
        }

        // Lantai Dek Terangkat (Y = 2.0m)
        Box(root, 8.8f, 0.25f, 7.0f, matDeck, new Vector3(0, 2.0f, 0.75f));

        // Dinding Kayu Panggung
        Box(root, 8.0f, 2.4f, 0.15f, matSlat, new Vector3(0, 3.3f, -2.5f));
        Box(root, 0.15f, 2.4f, 5.0f, matSlat, new Vector3(-4.0f, 3.3f, 0));
        Box(root, 0.15f, 2.4f, 5.0f, matSlat, new Vector3(4.0f, 3.3f, 0));
        Box(root, 2.6f, 2.4f, 0.15f, matSlat, new Vector3(-2.7f, 3.3f, 2.5f));
        Box(root, 2.6f, 2.4f, 0.15f, matSlat, new Vector3(2.7f, 3.3f, 2.5f));

        // Jendela Tradisional
        Window(root, 2.8f, 2.2f, new Vector3(0, 3.25f, 2.5f), 0);
        Window(root, 1.2f, 1.0f, new Vector3(-2.7f, 3.5f, 2.6f), 0);
        Window(root, 1.2f, 1.0f, new Vector3(2.7f, 3.5f, 2.6f), 0);

        // Atap Pelana Tinggi
        float roofPitch = Mathf.Atan(2.4f / 3.4f) * Mathf.Rad2Deg;
        Box(root, 9.4f, 0.15f, 4.16f, matRoof, new Vector3(0, 5.8f, 1.7f), new Vector3(roofPitch, 0, 0));
        Box(root, 9.4f, 0.15f, 4.16f, matRoof, new Vector3(0, 5.8f, -1.7f), new Vector3(-roofPitch, 0, 0));
        Box(root, 9.5f, 0.18f, 0.30f, matDark, new Vector3(0, 6.95f, 0), collider: false);

        // Gable Segitiga
        GableTriangle(root, 6.0f, 2.3f, 0.15f, matSlat, new Vector3(-4.1f, 4.5f, 0), -90f);
        GableTriangle(root, 6.0f, 2.3f, 0.15f, matSlat, new Vector3(4.1f, 4.5f, 0), 90f);

        // Tangga Kayu Menuju Teras
        for (int i = 0; i < 6; i++)
        {
            Box(root, 1.6f, 0.12f, 0.45f, matDeck, new Vector3(0, 1.85f - i * 0.38f, 4.5f + i * 0.45f));
        }

        // Pagar Teras
        Box(root, 3.2f, 0.08f, 0.08f, matDark, new Vector3(-2.6f, 3.1f, 4.15f), collider: false);
        Box(root, 3.2f, 0.08f, 0.08f, matDark, new Vector3(2.6f, 3.1f, 4.15f), collider: false);

        // Lentera Gantung Teras
        Lantern(root, new Vector3(-1.5f, 4.0f, 2.7f));
        Lantern(root, new Vector3(1.5f, 4.0f, 2.7f));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  4. A-FRAME CABIN (Kabin Segitiga Curam)
    // ─────────────────────────────────────────────────────────────────────
    private static void BuildAFrame(GameObject root)
    {
        float angleZ = Mathf.Atan2(6.2f, 3.2f) * Mathf.Rad2Deg;

        // Atap Segitiga Miring Kiri & Kanan (Menyentuh Tanah)
        Box(root, 6.98f, 0.2f, 7.6f, matRoof, new Vector3(-1.6f, 3.1f, 0), new Vector3(0, 0, angleZ));
        Box(root, 6.98f, 0.2f, 7.6f, matRoof, new Vector3(1.6f, 3.1f, 0), new Vector3(0, 0, -angleZ));
        Box(root, 0.3f, 0.3f, 7.7f, matDark, new Vector3(0, 6.25f, 0), collider: false);

        // Lantai Dek & Sun Deck Terbuka
        Box(root, 6.4f, 0.3f, 7.4f, matDeck, new Vector3(0, 0, 0));
        Box(root, 6.4f, 0.3f, 3.6f, matDeck, new Vector3(0, 0, 5.5f));

        // Fasad Kaca Penuh Depan
        GableTriangle(root, 6.0f, 5.8f, 0.12f, matGlass, new Vector3(0, 0.15f, 3.7f), 0);
        // Dinding Belakang
        GableTriangle(root, 6.0f, 5.8f, 0.15f, matSlat, new Vector3(0, 0.15f, -3.7f), 180f);

        // Rangka Kayu Hitam Fasad
        Box(root, 0.12f, 5.8f, 0.1f, matDark, new Vector3(0, 3.0f, 3.72f), collider: false);
        for (float y = 1.2f; y <= 4.0f; y += 1.4f)
        {
            Box(root, 6f * (1f - y / 5.8f), 0.1f, 0.1f, matDark, new Vector3(0, 0.15f + y, 3.72f), collider: false);
        }

        // Pagar Teras
        Box(root, 6.4f, 0.08f, 0.08f, matDark, new Vector3(0, 1.0f, 7.2f), collider: false);

        // Cerobong Pipa Logam Modern
        Cylinder(root, 0.12f, 3.2f, matDark, new Vector3(1.8f, 4.2f, -1.0f));

        // Api Unggun Luar (Fire Pit)
        Cylinder(root, 0.85f, 0.25f, matConcrete, new Vector3(0, -0.05f, 9.5f));
        Cone(root, 0.35f, 0.8f, matOrange, new Vector3(0, 0.35f, 9.5f));
        // Bangku Kayu Gelondongan di Sekitar Api
        Box(root, 0.7f, 0.45f, 0.6f, matWood, new Vector3(-1.7f, 0.15f, 9.5f));
        Box(root, 0.7f, 0.45f, 0.6f, matWood, new Vector3(1.7f, 0.15f, 9.5f));

        // Lentera Dek
        Lantern(root, new Vector3(-2.9f, 1.6f, 7.2f));
        Lantern(root, new Vector3(2.9f, 1.6f, 7.2f));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  5. JOGLO TROPIS (Pendopo Tradisional Jawa)
    // ─────────────────────────────────────────────────────────────────────
    private static void BuildJoglo(GameObject root)
    {
        // Umpak / Lantai Batu Utama
        Box(root, 10.4f, 0.5f, 10.4f, matConcrete, new Vector3(0, -0.1f, 0));

        // 8 Tiang Penyangga Teras Luar
        float[] coords = { -4f, 0f, 4f };
        foreach (var x in coords)
        {
            foreach (var z in coords)
            {
                if (x == 0 && z == 0) continue;
                Cylinder(root, 0.16f, 2.8f, matWood, new Vector3(x, 1.55f, z));
                Lantern(root, new Vector3(x * 0.88f, 2.5f, z * 0.88f));
            }
        }

        // 4 SOKO GURU (Tiang Utama Jati di Tengah)
        float[] sg = { -1.5f, 1.5f };
        foreach (var x in sg)
        {
            foreach (var z in sg)
            {
                Cylinder(root, 0.22f, 4.4f, matWood, new Vector3(x, 2.35f, z));
            }
        }

        // Ruang Inti Tengah (Gedhong)
        Box(root, 3.4f, 2.8f, 3.4f, matSlat, new Vector3(0, 1.55f, 0));
        Window(root, 1.2f, 2.0f, new Vector3(0, 1.2f, 1.72f), 0);

        // Atap Joglo Dua Tingkat
        // Tingkat 1: Atap Piramida Lebar Rendah
        Cone(root, 7.35f, 1.4f, matRoofThatch, new Vector3(0, 3.6f, 0), 45f, 4);
        // Tumpangsari Balok Kayu
        Box(root, 5.0f, 1.0f, 5.0f, matSlat, new Vector3(0, 4.1f, 0));
        // Tingkat 2: Atap Piramida Curam Menjulang
        Cone(root, 4.5f, 1.5f, matRoofThatch, new Vector3(0, 5.35f, 0), 45f, 4);
        // Mahkota Puncak (Makutho)
        Cylinder(root, 0.08f, 0.9f, matDark, new Vector3(0, 6.6f, 0), collider: false);
        Sphere(root, 0.14f, matDark, new Vector3(0, 7.1f, 0));

        // Tangga Masuk
        Box(root, 3.0f, 0.25f, 0.6f, matConcrete, new Vector3(0, -0.05f, 5.3f));
        Box(root, 3.0f, 0.15f, 0.6f, matConcrete, new Vector3(0, -0.20f, 5.9f));

        // Kolam Teratai Sisi Kiri & Kanan
        for (int s = -1; s <= 1; s += 2)
        {
            Cylinder(root, 3.8f, 0.08f, matWater, new Vector3(s * 8.5f, -0.2f, 1.5f), collider: false);
            // Daun & Bunga Teratai
            for (int i = 0; i < 5; i++)
            {
                float px = s * 8.5f + Mathf.Sin(i * 1.5f) * 1.8f;
                float pz = 1.5f + Mathf.Cos(i * 1.5f) * 1.8f;
                Cylinder(root, 0.35f, 0.02f, matLeaf, new Vector3(px, -0.16f, pz), collider: false);
                if (i % 2 == 0) Sphere(root, 0.12f, matPink, new Vector3(px, -0.1f, pz));
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  6. PONDOK DANAU (Lakeside Dock Cabin)
    // ─────────────────────────────────────────────────────────────────────
    private static void BuildDanau(GameObject root)
    {
        // Dek Fondasi Tepi Air
        Box(root, 6.4f, 0.4f, 4.8f, matDeck, new Vector3(0, -0.15f, 0));
        Box(root, 6.0f, 0.2f, 4.0f, matDeck, new Vector3(0, 0.05f, 0));

        // Dinding Kabin
        Box(root, 6.0f, 2.6f, 0.2f, matSlat, new Vector3(0, 1.5f, -2.0f));
        Box(root, 0.2f, 2.6f, 4.0f, matSlat, new Vector3(-3.0f, 1.5f, 0));
        Box(root, 0.2f, 2.6f, 4.0f, matSlat, new Vector3(3.0f, 1.5f, 0));
        Box(root, 2.2f, 2.6f, 0.2f, matSlat, new Vector3(-1.9f, 1.5f, 2.0f));
        Box(root, 1.0f, 2.6f, 0.2f, matSlat, new Vector3(2.5f, 1.5f, 2.0f));

        // Pintu Kaca Geser Menghadap Danau
        Window(root, 2.8f, 2.4f, new Vector3(0.6f, 1.4f, 2.0f), 0);

        // Atap Miring Pelana
        Box(root, 7.6f, 0.15f, 5.8f, matRoof, new Vector3(0, 3.1f, 0.2f), new Vector3(0, 0, 7f));
        // Cerobong Asap
        Box(root, 0.7f, 4.4f, 0.7f, matConcrete, new Vector3(-2.2f, 2.4f, -1.4f));

        // Teras Menghadap Air
        Box(root, 6.4f, 0.2f, 2.4f, matDeck, new Vector3(0, 0.05f, 3.2f));
        Box(root, 6.8f, 0.1f, 2.6f, matRoof, new Vector3(0, 2.85f, 3.3f), new Vector3(6f, 0, 0));
        Cylinder(root, 0.1f, 2.7f, matWood, new Vector3(-3.0f, 1.4f, 4.2f));
        Cylinder(root, 0.1f, 2.7f, matWood, new Vector3(3.0f, 1.4f, 4.2f));

        // Dermaga Kayu Panjang (Pier / Jetty)
        Box(root, 1.8f, 0.14f, 9.0f, matDeck, new Vector3(0, -0.05f, 9.2f));
        // 8 Pilar Penyangga Dermaga di Dalam Air
        for (int i = 0; i < 4; i++)
        {
            float z = 6.0f + i * 2.4f;
            Cylinder(root, 0.1f, 1.4f, matWood, new Vector3(-0.85f, -0.5f, z));
            Cylinder(root, 0.1f, 1.4f, matWood, new Vector3(0.85f, -0.5f, z));
        }

        // Permukaan Air Danau
        Box(root, 24f, 0.05f, 20f, matWater, new Vector3(0, -0.2f, 12f), collider: false);

        // Perahu Kayu Kecil (Sampan) di Samping Dermaga
        var boat = Sphere(root, 1.0f, matWood, new Vector3(2.5f, -0.15f, 10.5f), new Vector3(0.6f, 0.35f, 1.8f));
        boat.name = "SampanKayu";

        Lantern(root, new Vector3(1.2f, 2.3f, 2.1f));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  7. BARN MODERN (Lumbung Gandum Modern)
    // ─────────────────────────────────────────────────────────────────────
    private static void BuildBarn(GameObject root)
    {
        // Dinding Kayu Papan Hitam
        Box(root, 0.2f, 3.2f, 9.0f, matBlack, new Vector3(-3.5f, 1.6f, 0));
        Box(root, 0.2f, 3.2f, 9.0f, matBlack, new Vector3(3.5f, 1.6f, 0));
        Box(root, 7.0f, 3.2f, 0.2f, matBlack, new Vector3(0, 1.6f, -4.5f));
        Box(root, 7.0f, 3.2f, 0.2f, matBlack, new Vector3(0, 1.6f, 4.5f));

        // Pintu Gerbang Lumbung Besar dengan Palang X
        Box(root, 3.2f, 2.7f, 0.1f, matSlat, new Vector3(0, 1.4f, 4.62f));
        Box(root, 0.1f, 3.3f, 0.06f, matDark, new Vector3(0, 1.4f, 4.7f), new Vector3(0, 0, 42f), collider: false);
        Box(root, 0.1f, 3.3f, 0.06f, matDark, new Vector3(0, 1.4f, 4.7f), new Vector3(0, 0, -42f), collider: false);

        // Jendela Hayloft Atas
        Window(root, 1.6f, 1.0f, new Vector3(0, 4.3f, 4.56f), 0);

        // Atap Gambrel / Mansard Khas Barn (2 Sudut Kemiringan)
        // Lereng Bawah yang Curam (Angle ~54 deg)
        Box(root, 2.35f, 0.15f, 9.6f, matDark, new Vector3(2.85f, 4.15f, 0), new Vector3(0, 0, -54f));
        Box(root, 2.35f, 0.15f, 9.6f, matDark, new Vector3(-2.85f, 4.15f, 0), new Vector3(0, 0, 54f));
        // Lereng Atas yang Landai (Angle ~28 deg)
        Box(root, 2.6f, 0.15f, 9.6f, matDark, new Vector3(1.1f, 5.65f, 0), new Vector3(0, 0, -28f));
        Box(root, 2.6f, 0.15f, 9.6f, matDark, new Vector3(-1.1f, 5.65f, 0), new Vector3(0, 0, 28f));

        // Gable Dinding Depan & Belakang Gambrel
        GableTriangle(root, 7.0f, 3.0f, 0.15f, matSlat, new Vector3(0, 3.2f, 4.5f), 0);
        GableTriangle(root, 7.0f, 3.0f, 0.15f, matSlat, new Vector3(0, 3.2f, -4.5f), 180f);

        // Silo Logam Gandum di Samping
        Cylinder(root, 1.2f, 7.0f, matMetal, new Vector3(-6.0f, 3.15f, -2.0f));
        Sphere(root, 1.2f, matMetal, new Vector3(-6.0f, 6.65f, -2.0f), new Vector3(1f, 0.6f, 1f));

        // 5 Gulungan Jerami Bulat (Round Hay Bales)
        for (int i = 0; i < 5; i++)
        {
            Cylinder(root, 0.5f, 0.9f, matHay,
                new Vector3(5.5f, 0.45f + (i / 3) * 0.9f, 1f + i * 0.5f), new Vector3(0, 0, 90f));
        }

        Lantern(root, new Vector3(-1.9f, 2.8f, 4.75f));
        Lantern(root, new Vector3(1.9f, 2.8f, 4.75f));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  8. RUMAH POHON (Treehouse Rimba Kanopi)
    // ─────────────────────────────────────────────────────────────────────
    private static void BuildPohon(GameObject root)
    {
        // Batang Pohon Raksasa Penopang (Tinggi 10m)
        Cylinder(root, 1.1f, 10.0f, matWood, new Vector3(0, 4.65f, 0));
        // Dahan-Dahan Besar Menjalar
        Cylinder(root, 0.25f, 3.5f, matWood, new Vector3(-1.4f, 7.5f, -0.5f), new Vector3(0, 0, 45f));
        Cylinder(root, 0.25f, 3.5f, matWood, new Vector3(1.4f, 7.3f, 0.8f), new Vector3(0, 0, -45f));

        // Dedaunan Kanopi Lebat Raksasa
        Sphere(root, 3.4f, matLeaf, new Vector3(0, 11.2f, 0));
        Sphere(root, 2.6f, matLeaf, new Vector3(-2.8f, 10.6f, 0.5f));
        Sphere(root, 2.4f, matLeaf, new Vector3(2.8f, 11.2f, -2.0f));

        // Dek Kayu Bertengger di Ketinggian 5m
        Box(root, 7.0f, 0.3f, 6.0f, matDeck, new Vector3(1.2f, 5.0f, 0));

        // Rumah Kabin Mungil di Atas Dek
        Box(root, 3.0f, 2.2f, 3.0f, matSlat, new Vector3(2.4f, 6.25f, 0));
        Cone(root, 2.9f, 1.6f, matRoofThatch, new Vector3(2.4f, 8.2f, 0), 45f, 4);

        // Jendela Rumah Pohon
        Window(root, 0.9f, 0.9f, new Vector3(2.4f, 6.4f, 1.56f), 0);
        Window(root, 0.9f, 0.9f, new Vector3(3.96f, 6.4f, 0), 90f);

        // Pagar Tali & Kayu Pembatas Dek
        Box(root, 7.0f, 0.08f, 0.08f, matDark, new Vector3(1.2f, 6.05f, 3.0f), collider: false);
        Box(root, 7.0f, 0.08f, 0.08f, matDark, new Vector3(1.2f, 6.05f, -3.0f), collider: false);

        // Lampu Tali Hiasan (Fairy String Lights)
        for (float x = -1.8f; x <= 4.41f; x += 1.4f)
        {
            Lantern(root, new Vector3(x, 6.2f, 3.0f));
        }

        // Tangga Kayu Menuju Dek dari Tanah
        for (float y = 0f; y < 5.0f; y += 0.45f)
        {
            Box(root, 0.6f, 0.06f, 0.08f, matWood, new Vector3(-1.4f, y, 3.4f));
        }

        // Jembatan Gantung Menuju Gardu Pandang Kedua
        Box(root, 6.0f, 0.1f, 1.2f, matDeck, new Vector3(7.7f, 5.0f, 0));
        // Gardu Pandang Kedua
        Box(root, 3.2f, 0.25f, 3.2f, matDeck, new Vector3(11.3f, 5.0f, 0));
        Cylinder(root, 0.5f, 9.0f, matWood, new Vector3(11.3f, 4.2f, 0));
        Sphere(root, 2.2f, matLeaf, new Vector3(11.3f, 9.5f, 0));
        Lantern(root, new Vector3(12.6f, 5.4f, 1.4f));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  9. RUMAH GADANG (Istana Tradisional Minangkabau)
    // ─────────────────────────────────────────────────────────────────────
    private static void BuildGadang(GameObject root)
    {
        // 10 Tiang Tonggak Kayu Berdiri Miring Melengkung
        float[] xs = { -4f, -2f, 0f, 2f, 4f };
        float[] zs = { -1.6f, 1.6f };
        foreach (var x in xs)
        {
            foreach (var z in zs)
            {
                Cylinder(root, 0.18f, 2.2f, matWood, new Vector3(x, 0.75f, z));
            }
        }

        // Lantai Dek Utama yang Luas
        Box(root, 10.6f, 0.25f, 5.4f, matDeck, new Vector3(0, 1.8f, 0));

        // Dinding Merah Kayu Berukir Splay Keluar (Mengembang ke Atas)
        Box(root, 10.0f, 2.4f, 0.15f, matRedWood, new Vector3(0, 3.15f, 2.0f), new Vector3(5.7f, 0, 0));
        Box(root, 10.0f, 2.4f, 0.15f, matRedWood, new Vector3(0, 3.15f, -2.0f), new Vector3(-5.7f, 0, 0));
        Box(root, 0.15f, 2.4f, 4.0f, matRedWood, new Vector3(5.0f, 3.15f, 0), new Vector3(0, 0, -5.7f));
        Box(root, 0.15f, 2.4f, 4.0f, matRedWood, new Vector3(-5.0f, 3.15f, 0), new Vector3(0, 0, 5.7f));

        // Ornamen Ukiran Garis Emas Khas Minang
        for (int i = 0; i < 6; i++)
        {
            Box(root, 0.12f, 2.2f, 0.06f, matGold, new Vector3(-4.2f + i * 1.68f, 3.15f, 2.12f), collider: false);
        }
        Box(root, 10.0f, 0.12f, 0.06f, matGold, new Vector3(0, 2.2f, 2.1f), collider: false);
        Box(root, 10.0f, 0.12f, 0.06f, matGold, new Vector3(0, 4.1f, 2.17f), collider: false);

        // Jendela Megah Fasad Depan
        Window(root, 1.0f, 1.2f, new Vector3(-2.5f, 3.4f, 2.1f), 0);
        Window(root, 1.0f, 1.2f, new Vector3(0f, 3.4f, 2.1f), 0);
        Window(root, 1.0f, 1.2f, new Vector3(2.5f, 3.4f, 2.1f), 0);

        // ATAP GONJONG MELENGKUNG TANDUK KERBAU (Swept Parabolic Horn Roof)
        float ey = 4.2f;
        for (int i = 0; i < 14; i++)
        {
            float x = -6.2f + i * 0.95f;
            // Rumus kelengkungan gonjong: puncak melengkung tajam di ujung
            float h = 6.6f + 1.9f * Mathf.Pow(Mathf.Abs(x) / 6.4f, 2.3f);
            float hNext = 6.6f + 1.9f * Mathf.Pow(Mathf.Abs(x + 0.4f) / 6.4f, 2.3f);
            float hPrev = 6.6f + 1.9f * Mathf.Pow(Mathf.Abs(x - 0.4f) / 6.4f, 2.3f);
            float dh = (hNext - hPrev) / 0.8f;

            float pitch = Mathf.Atan((h - ey) / 3f) * Mathf.Rad2Deg;
            float roll = -Mathf.Atan(dh) * Mathf.Rad2Deg;
            float length = Mathf.Sqrt(9f + (h - ey) * (h - ey));

            Box(root, 1.02f, 0.14f, length, matRoofThatch,
                new Vector3(x, (h + ey) * 0.5f, 1.5f), new Vector3(pitch, 0, roll));
            Box(root, 1.02f, 0.14f, length, matRoofThatch,
                new Vector3(x, (h + ey) * 0.5f, -1.5f), new Vector3(-pitch, 0, roll));
        }

        // Ujung Tanduk Gonjong Emas Menjulang di Sisi Luar
        Cylinder(root, 0.12f, 1.7f, matGold, new Vector3(-6.7f, 8.8f, 0), new Vector3(0, 0, 28f), false);
        Cylinder(root, 0.12f, 1.7f, matGold, new Vector3(6.7f, 8.8f, 0), new Vector3(0, 0, -28f), false);

        // Tangga Masuk Teras
        for (int i = 0; i < 4; i++)
        {
            Box(root, 2.0f, 0.15f, 0.5f, matDeck, new Vector3(0, 1.7f - i * 0.4f, 3.2f + i * 0.5f));
        }

        // Lentera Gerbang
        Lantern(root, new Vector3(-1.4f, 2.4f, 3.0f));
        Lantern(root, new Vector3(1.4f, 2.4f, 3.0f));

        // Lumbung Padi Rangkiang Tradisional di Samping Rumah Gadang
        var rangkiang = new GameObject("Rangkiang_Padi");
        rangkiang.transform.SetParent(root.transform, false);
        rangkiang.transform.localPosition = new Vector3(10.0f, 0, 0);

        for (int a = -1; a <= 1; a += 2)
            for (int b = -1; b <= 1; b += 2)
                Cylinder(rangkiang, 0.12f, 1.3f, matWood, new Vector3(a * 0.9f, 0.3f, b * 0.9f));
        Box(rangkiang, 2.4f, 1.4f, 2.4f, matRedWood, new Vector3(0, 1.7f, 0));
        Cone(rangkiang, 2.3f, 1.3f, matRoofThatch, new Vector3(0, 3.05f, 0), 45f, 4);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  10. MINKA TRADISIONAL (Rumah Tradisional Jepang Zen)
    // ─────────────────────────────────────────────────────────────────────
    private static void BuildMinka(GameObject root)
    {
        // Fondasi Batu Alam Jepang
        Box(root, 10.4f, 0.4f, 7.4f, matConcrete, new Vector3(0, -0.15f, 0));
        // Badan Dinding Plester Putih
        Box(root, 8.4f, 2.4f, 5.4f, matWall, new Vector3(0, 1.4f, 0));

        // Rangka Kayu Ekspos Tiang & Balok (Post-and-Beam Timber Frame)
        for (float x = -4.2f; x <= 4.21f; x += 1.4f)
        {
            Box(root, 0.14f, 2.4f, 0.14f, matWood, new Vector3(x, 1.4f, 2.75f), collider: false);
            Box(root, 0.14f, 2.4f, 0.14f, matWood, new Vector3(x, 1.4f, -2.75f), collider: false);
        }
        for (float z = -2.7f; z <= 2.71f; z += 1.35f)
        {
            Box(root, 0.14f, 2.4f, 0.14f, matWood, new Vector3(4.25f, 1.4f, z), collider: false);
            Box(root, 0.14f, 2.4f, 0.14f, matWood, new Vector3(-4.25f, 1.4f, z), collider: false);
        }

        // Pintu Geser Kertas Shoji Jepang
        for (int i = 0; i < 4; i++)
        {
            Box(root, 2.1f, 2.1f, 0.03f, matShoji, new Vector3(-3.15f + i * 2.1f, 1.4f, 2.78f), collider: false);
        }

        // Teras Dek Engawa Berkeliling
        Box(root, 9.0f, 0.18f, 1.8f, matDeck, new Vector3(0, 0.12f, 3.6f));
        Box(root, 0.18f, 0.18f, 6.4f, matDeck, new Vector3(-4.9f, 0.12f, 0));
        Box(root, 0.18f, 0.18f, 6.4f, matDeck, new Vector3(4.9f, 0.12f, 0));

        // Atap Irimoya / Limas Hip Roof Jepang (Genteng Abu-Abu Tua)
        HipRoof(root, 6.3f, 4.9f, 2.4f, 3.0f, 5.4f, matRoofJapanese);
        Box(root, 4.8f, 0.22f, 0.3f, matDark, new Vector3(0, 5.42f, 0), collider: false);

        // Halaman Pasir / Kerikil Zen
        Box(root, 13.0f, 0.05f, 9.0f, matConcrete, new Vector3(0, -0.2f, 8.5f), collider: false);
        // Batu Zen
        Sphere(root, 0.5f, matConcrete, new Vector3(-3f, -0.1f, 8f), new Vector3(1f, 0.6f, 1f));
        Sphere(root, 0.5f, matConcrete, new Vector3(2f, -0.1f, 11f), new Vector3(1f, 0.6f, 1f));

        // Gerbang Torii Merah di Depan Halaman
        Cylinder(root, 0.18f, 3.6f, matRed, new Vector3(-2.6f, 1.5f, 14.0f));
        Cylinder(root, 0.18f, 3.6f, matRed, new Vector3(2.6f, 1.5f, 14.0f));
        Box(root, 7.0f, 0.25f, 0.3f, matRed, new Vector3(0, 3.4f, 14.0f));
        Box(root, 6.0f, 0.20f, 0.25f, matRed, new Vector3(0, 2.8f, 14.0f));

        // Lentera Batu Jepang (Toro Stone Lantern)
        Cylinder(root, 0.25f, 0.5f, matConcrete, new Vector3(-4.0f, 0.1f, 6.5f));
        Cylinder(root, 0.12f, 0.9f, matConcrete, new Vector3(-4.0f, 0.6f, 6.5f));
        Lantern(root, new Vector3(-4.0f, 1.2f, 6.5f));
        Cone(root, 0.55f, 0.4f, matConcrete, new Vector3(-4.0f, 1.65f, 6.5f), 45f, 4);

        // Kolam Ikan Koi
        Cylinder(root, 2.6f, 0.05f, matWater, new Vector3(6.5f, -0.2f, 7.0f), collider: false);
        // Ikan Koi Oranye Berenang
        for (int i = 0; i < 3; i++)
        {
            Sphere(root, 0.15f, matOrange,
                new Vector3(6.5f + Mathf.Sin(i * 2f) * 1.2f, -0.18f, 7.0f + Mathf.Cos(i * 2f) * 1.2f),
                new Vector3(0.5f, 0.3f, 1.6f));
        }

        // Pohon Sakura Pink (Cherry Blossom)
        Cylinder(root, 0.2f, 3.5f, matWood, new Vector3(-6.5f, 1.4f, 1.0f));
        Sphere(root, 1.4f, matPink, new Vector3(-6.5f, 3.6f, 1.0f));
        Sphere(root, 1.2f, matPink, new Vector3(-5.8f, 4.0f, 1.5f));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  11. CHALET SALJU (Chalet Kayu Pegunungan Alpine)
    // ─────────────────────────────────────────────────────────────────────
    private static void BuildChalet(GameObject root)
    {
        // Lantai Dasar Batu Alam Kokoh (Heavy Stone Base)
        Box(root, 8.0f, 1.6f, 6.0f, matConcrete, new Vector3(0, 0.45f, 0));
        // Lantai Atas Kayu Papan (Slat Wood Upper Floor)
        Box(root, 8.0f, 2.2f, 6.0f, matSlat, new Vector3(0, 2.35f, 0));

        // Pintu Masuk Kayu Tebal
        Box(root, 1.1f, 1.5f, 0.1f, matWood, new Vector3(0, 0.4f, 3.04f));
        Window(root, 1.0f, 0.8f, new Vector3(-2.5f, 0.5f, 3.05f), 0);
        Window(root, 1.0f, 0.8f, new Vector3(2.5f, 0.5f, 3.05f), 0);

        // Jendela Lantai Atas dengan Daun Jendela Merah Alpine
        float[] winXs = { -2.4f, 0f, 2.4f };
        foreach (var x in winXs)
        {
            Window(root, 1.2f, 1.1f, new Vector3(x, 2.5f, 3.05f), 0);
            Box(root, 0.4f, 1.2f, 0.08f, matRed, new Vector3(x - 0.95f, 2.5f, 3.07f), collider: false);
            Box(root, 0.4f, 1.2f, 0.08f, matRed, new Vector3(x + 0.95f, 2.5f, 3.07f), collider: false);
        }

        // Atap Pelana Lebar Rendah Khas Alpen dengan Lapisan Salju Tebal
        float pitchAngle = Mathf.Atan(3.0f / 3.6f) * Mathf.Rad2Deg;
        float slopeLen = Mathf.Sqrt(3.6f * 3.6f + 3.0f * 3.0f);

        // Genteng Atap
        Box(root, 9.4f, 0.2f, slopeLen, matRoof, new Vector3(0, 5.1f, 1.8f), new Vector3(pitchAngle, 0, 0));
        Box(root, 9.4f, 0.2f, slopeLen, matRoof, new Vector3(0, 5.1f, -1.8f), new Vector3(-pitchAngle, 0, 0));
        // Lapisan Salju di Atas Atap
        Box(root, 9.5f, 0.18f, slopeLen + 0.05f, matSnow, new Vector3(0, 5.3f, 1.8f), new Vector3(pitchAngle, 0, 0), collider: false);
        Box(root, 9.5f, 0.18f, slopeLen + 0.05f, matSnow, new Vector3(0, 5.3f, -1.8f), new Vector3(-pitchAngle, 0, 0), collider: false);

        // Gable Samping
        GableTriangle(root, 6.8f, 2.9f, 0.15f, matSlat, new Vector3(-4.05f, 3.45f, 0), -90f);
        GableTriangle(root, 6.8f, 2.9f, 0.15f, matSlat, new Vector3(4.05f, 3.45f, 0), 90f);

        // Cerobong Batu dengan Tutup Salju
        Box(root, 0.9f, 4.4f, 0.9f, matConcrete, new Vector3(2.6f, 5.2f, -1.2f));
        Box(root, 1.1f, 0.2f, 1.1f, matSnow, new Vector3(2.6f, 7.5f, -1.2f));

        // Balkon Kayu Lantai Atas Berukir
        Box(root, 8.4f, 0.18f, 1.6f, matDeck, new Vector3(0, 1.35f, 3.8f));
        Box(root, 8.4f, 0.10f, 0.10f, matWood, new Vector3(0, 2.3f, 4.55f), collider: false);
        // Salju di atas railing balkon
        Box(root, 8.4f, 0.12f, 0.20f, matSnow, new Vector3(0, 2.38f, 4.55f), collider: false);

        // Manusia Salju Lucu (Snowman) di Halaman Depan
        var snowman = new GameObject("Snowman");
        snowman.transform.SetParent(root.transform, false);
        snowman.transform.localPosition = new Vector3(-5.5f, 0, 6.0f);

        Sphere(snowman, 0.70f, matSnow, new Vector3(0, 0.70f, 0));
        Sphere(snowman, 0.50f, matSnow, new Vector3(0, 1.60f, 0));
        Sphere(snowman, 0.34f, matSnow, new Vector3(0, 2.25f, 0));
        // Hidung Wortel Oranye
        Cone(snowman, 0.06f, 0.35f, matOrange, new Vector3(0, 2.25f, 0.40f), 0, 6);
        // Mata Batu Arang
        Sphere(snowman, 0.04f, matDark, new Vector3(-0.1f, 2.35f, 0.32f));
        Sphere(snowman, 0.04f, matDark, new Vector3(0.1f, 2.35f, 0.32f));

        Lantern(root, new Vector3(-1.0f, 2.9f, 4.6f));
        Lantern(root, new Vector3(1.0f, 2.9f, 4.6f));
    }
}
#endif
