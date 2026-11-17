#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generator 11 Gaya Rumah Kayu untuk Legenda Desa Eldara — SEMUA BISA DIMASUKI.
///
/// REVISI AKSES & KELEGAAN KEPALA:
///  - Pintu minimal 1.7 m lebar x 2.7 m tinggi (Door() otomatis memaksa ukuran minimum ini).
///    Player = CharacterController tinggi 2 m, radius 0.5 m, jadi sebelumnya pintu 1.0-1.2 m x 2.2 m
///    terlalu sempit/mepet kepala.
///  - Tinggi dinding, atap, lantai-2, lampu, dan atap teras dinaikkan agar kepala/kamera tidak menabrak.
///  - Tangga Panggung & Gadang diganti tangga padat (rise &lt;= 0.2 m; sebelumnya 0.38-0.40 m,
///    melebihi stepOffset CharacterController sehingga TIDAK BISA dinaiki).
///  - Treehouse: pagar dek tidak lagi menutup ujung tangga, kabin digeser keluar dari batang pohon.
///  - Modern: sekat kamar & fasad pintu dilebarkan, tangga ke lantai 2 dilebarkan (1.0 -> 1.6 m).
///  - A-Frame: pintu fasad dilebarkan & dipertinggi.
///
/// Gaya: 1 Cabin, 2 Modern, 3 Panggung, 4 AFrame, 5 Joglo, 6 Danau, 7 Barn, 8 Pohon, 9 Gadang, 10 Minka, 11 Chalet.
/// Konvensi: depan rumah = +Z (pintu menghadap +Z).
/// </summary>
public static partial class ProceduralHouseBuilder
{
    public enum HouseStyle
    {
        Cabin, Modern, Panggung, AFrame, Joglo, Danau, Barn, Pohon, Gadang, Minka, Chalet, BalaiDesa
    }

    private const string FolderHouses = "Assets/_Project/Art/ProceduralHouses";
    private const string FolderMats = "Assets/_Project/Art/HouseMaterials";

    // Ukuran minimum pintu agar nyaman untuk player (tinggi 2 m, diameter 1 m)
    // (nilai diatur di HouseDimensions.cs)
    public const float DoorMinW = HouseDimensions.DoorWidth;
    public const float DoorMinH = HouseDimensions.DoorHeight;

    // ─────────────────────────────────────────────────────────────────────
    //  MATERIAL
    // ─────────────────────────────────────────────────────────────────────
    private static Material matWood, matSlat, matBlack, matRedWood, matDeck, matRoof, matRoofThatch,
        matRoofJapanese, matConcrete, matGlass, matDark, matGold, matShoji, matLamp, matWater, matSnow,
        matLeaf, matPink, matRed, matMetal, matHay, matFabric, matOrange, matWall, matClay, matBatik;
    private static Material[] matLogs;

    public static void EnsureMaterials()
    {
        EnsureFolder(FolderHouses);
        EnsureFolder(FolderMats);

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
        matClay         = GetOrCreateMat("M_House_Clay", litShader, new Color(0.62f, 0.34f, 0.20f), 0.20f);   // tanah liat (kendi, tungku)
        matBatik        = GetOrCreateMat("M_House_Batik", litShader, new Color(0.13f, 0.17f, 0.36f), 0.10f);  // kain batik indigo

        matLamp = GetOrCreateMat("M_House_Lamp", litShader, new Color(1.0f, 0.85f, 0.45f), 0.5f);
        matLamp.EnableKeyword("_EMISSION");
        matLamp.SetColor("_EmissionColor", new Color(1.0f, 0.85f, 0.45f) * 4.5f);

        matGlass = GetOrCreateMat("M_House_Glass", litShader, new Color(0.65f, 0.80f, 0.90f, 0.35f), 0.95f, isTransparent: true);
        matWater = GetOrCreateMat("M_House_Water", litShader, new Color(0.15f, 0.45f, 0.65f, 0.80f), 0.90f, isTransparent: true);

        matLogs = new Material[4];
        for (int i = 0; i < 4; i++)
        {
            Color logCol = Color.Lerp(new Color(0.35f, 0.20f, 0.10f), new Color(0.50f, 0.30f, 0.16f), i / 3.0f);
            matLogs[i] = GetOrCreateMat($"M_House_Log_{i}", litShader, logCol, 0.20f);
        }

        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = System.IO.Path.GetFileName(path);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
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
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetShaderPassEnabled("ShadowCaster", false);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PRIMITIF DASAR
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

        string meshKey = $"Cone_{r:F2}_{h:F2}_{sides}";
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
        Material mat, Vector3 pos, float ryDeg = 0f, bool collider = true)
    {
        var go = new GameObject("GableTriangle");
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, ryDeg, 0);

        string meshKey = $"Gable_{baseWidth:F2}_{height:F2}_{thickness:F2}";
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

        if (collider)
        {
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
        }
        return go;
    }

    private static Mesh CreatePrismTriangleMesh(float w, float h, float t)
    {
        var m = new Mesh();
        float hw = w * 0.5f;
        float ht = t * 0.5f;

        Vector3[] v = new Vector3[]
        {
            new Vector3(-hw, 0, ht), new Vector3(hw, 0, ht), new Vector3(0, h, ht),
            new Vector3(-hw, 0, -ht), new Vector3(0, h, -ht), new Vector3(hw, 0, -ht)
        };

        int[] tris = new int[]
        {
            0, 1, 2,
            3, 4, 5,
            0, 3, 5, 0, 5, 1,
            0, 2, 4, 0, 4, 3,
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

        string meshKey = $"HipRoof_{hx:F2}_{hz:F2}_{ridgeLen:F2}_{yBase:F2}_{yPeak:F2}";
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
            new Vector3(-hx, y0,  hz), new Vector3( hx, y0,  hz),
            new Vector3( hx, y0, -hz), new Vector3(-hx, y0, -hz),
            new Vector3(-rx, y1,  0),  new Vector3( rx, y1,  0)
        };

        int[] tris = new int[]
        {
            0, 1, 5,   0, 5, 4,
            2, 3, 4,   2, 4, 5,
            1, 2, 5,
            3, 0, 4,
            0, 3, 2,   0, 2, 1      // langit-langit (terlihat dari dalam rumah)
        };

        m.vertices = v;
        m.triangles = tris;
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    private static GameObject Group(GameObject parent, string name, Vector3 pos, float yawDeg)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = pos;
        g.transform.localRotation = Quaternion.Euler(0, yawDeg, 0);
        return g;
    }

    // Jendela: rangka tipis + kaca (tanpa collider) — bagian dalam rumah tetap terlihat.
    private static void Window(GameObject parent, float w, float h, Vector3 pos, float ryDeg = 0f, Material pane = null)
    {
        var g = Group(parent, "Window", pos, ryDeg);
        Material p = pane ? pane : matGlass;
        const float b = 0.07f;
        Box(g, w + b * 2f, b, 0.14f, matDark, new Vector3(0, h * 0.5f + b * 0.5f, 0), default, false);
        Box(g, w + b * 2f, b, 0.14f, matDark, new Vector3(0, -h * 0.5f - b * 0.5f, 0), default, false);
        Box(g, b, h, 0.14f, matDark, new Vector3(-w * 0.5f - b * 0.5f, 0, 0), default, false);
        Box(g, b, h, 0.14f, matDark, new Vector3(w * 0.5f + b * 0.5f, 0, 0), default, false);
        Box(g, 0.04f, h, 0.06f, matDark, Vector3.zero, default, false);
        Box(g, w, 0.04f, 0.06f, matDark, Vector3.zero, default, false);
        Box(g, w, h, 0.03f, p, Vector3.zero, default, false);
    }

    private static void Lantern(GameObject parent, Vector3 pos, Color? customLightColor = null)
    {
        Color lightCol = customLightColor ?? new Color(1.0f, 0.85f, 0.45f);
        Sphere(parent, 0.12f, matLamp, pos);

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
    //  DINDING BERLUBANG (pintu & jendela) + RUANGAN
    // ─────────────────────────────────────────────────────────────────────
    private struct WallOpening
    {
        public float center, width, sill, head;
        public bool glass;
        public Material pane;
        public WallOpening(float center, float width, float sill, float head, bool glass = false, Material pane = null)
        { this.center = center; this.width = width; this.sill = sill; this.head = head; this.glass = glass; this.pane = pane; }
    }

    // Pintu: lubang dari lantai. Ukuran SELALU minimal DoorMinW x DoorMinH supaya player (2 m x 1 m) lega.
    private static WallOpening Door(float c, float w, float floorY, float h = HouseDimensions.DoorHeight)
    {
        w = Mathf.Max(w, DoorMinW);
        h = Mathf.Max(h, DoorMinH);
        return new WallOpening(c, w, floorY, floorY + h, false);
    }

    // Jendela berkaca: ambang sillH di atas lantai, tinggi h
    private static WallOpening Win(float c, float w, float floorY, float sillH = 0.95f, float h = 1.15f, Material pane = null)
        => new WallOpening(c, w, floorY + sillH, floorY + sillH + h, true, pane);

    private static void WallPiece(GameObject root, bool alongX, float fixedPos,
        float a, float b, float y0, float y1, float t, Material mat)
    {
        float len = b - a, h = y1 - y0;
        if (len < 0.01f || h < 0.01f) return;
        float mid = (a + b) * 0.5f;
        Box(root,
            alongX ? len : t, h, alongX ? t : len,
            mat,
            new Vector3(alongX ? mid : fixedPos, (y0 + y1) * 0.5f, alongX ? fixedPos : mid));
    }

    // alongX=true: tembok memanjang di sumbu X pada z=fixedPos (center lubang = koordinat X).
    // alongX=false: memanjang di sumbu Z pada x=fixedPos (center lubang = koordinat Z).
    private static void WallRun(GameObject root, bool alongX, float fixedPos, float min, float max,
        float y0, float y1, float t, Material mat, WallOpening[] ops)
    {
        if (ops == null) ops = new WallOpening[0];
        System.Array.Sort(ops, (p, q) => p.center.CompareTo(q.center));
        float cur = min;
        foreach (var op in ops)
        {
            // Sisakan minimal 12 cm balok di atas lubang agar tembok tetap menyatu dengan atap
            float head = Mathf.Min(op.head, y1 - HouseDimensions.LintelMin);
            float l = op.center - op.width * 0.5f;
            float r = op.center + op.width * 0.5f;
            WallPiece(root, alongX, fixedPos, cur, l, y0, y1, t, mat);
            WallPiece(root, alongX, fixedPos, l, r, y0, op.sill, t, mat);
            WallPiece(root, alongX, fixedPos, l, r, head, y1, t, mat);
            cur = r;
            if (op.glass)
            {
                float yc = (op.sill + head) * 0.5f;
                Vector3 wp = alongX ? new Vector3(op.center, yc, fixedPos) : new Vector3(fixedPos, yc, op.center);
                Window(root, op.width, head - op.sill, wp, alongX ? 0f : 90f, op.pane);
            }
        }
        WallPiece(root, alongX, fixedPos, cur, max, y0, y1, t, mat);
    }

    private static void Room(GameObject root, float cx, float cz, float hx, float hz,
        float y0, float y1, float t, Material mat,
        WallOpening[] front, WallOpening[] back, WallOpening[] left, WallOpening[] right,
        Material post = null)
    {
        if (y1 - y0 < HouseDimensions.MinWallHeight)
            Debug.LogWarning($"[Eldara] Ruangan '{root.name}' terlalu rendah ({y1 - y0:F2} m < {HouseDimensions.MinWallHeight:F2} m): kepala player bisa mepet.");

        WallRun(root, true,  cz + hz, cx - hx - t * 0.5f, cx + hx + t * 0.5f, y0, y1, t, mat, front);
        WallRun(root, true,  cz - hz, cx - hx - t * 0.5f, cx + hx + t * 0.5f, y0, y1, t, mat, back);
        WallRun(root, false, cx - hx, cz - hz + t * 0.5f, cz + hz - t * 0.5f, y0, y1, t, mat, left);
        WallRun(root, false, cx + hx, cz - hz + t * 0.5f, cz + hz - t * 0.5f, y0, y1, t, mat, right);

        if (post)
            foreach (float sx in new[] { -1f, 1f })
                foreach (float sz in new[] { -1f, 1f })
                    Box(root, t + 0.1f, y1 - y0 + 0.04f, t + 0.1f, post,
                        new Vector3(cx + sx * hx, (y0 + y1) * 0.5f, cz + sz * hz), default, false);
    }

    // Dinding balok gelondongan (log) dengan lubang: baris yang menabrak lubang dipotong.
    private static void LogWall(GameObject root, bool alongX, float fixedPos, float min, float max,
        float floorY, int rows, float R, WallOpening[] ops)
    {
        System.Array.Sort(ops, (p, q) => p.center.CompareTo(q.center));
        for (int i = 0; i < rows; i++)
        {
            float yC = floorY + R + 0.3f * i;
            Material m = matLogs[(i + (alongX ? 0 : 1)) % 4];
            float cur = min;
            foreach (var op in ops)
            {
                bool cut = yC + R > op.sill && yC - R < op.head;
                if (!cut) continue;
                float l = op.center - op.width * 0.5f, r = op.center + op.width * 0.5f;
                LogSeg(root, alongX, fixedPos, cur, l, yC, R, m);
                cur = r;
            }
            LogSeg(root, alongX, fixedPos, cur, max, yC, R, m);
        }
        foreach (var op in ops)
        {
            if (!op.glass) continue;
            float yc = (op.sill + op.head) * 0.5f;
            Vector3 wp = alongX ? new Vector3(op.center, yc, fixedPos) : new Vector3(fixedPos, yc, op.center);
            Window(root, op.width, op.head - op.sill, wp, alongX ? 0f : 90f, op.pane);
        }
    }

    private static void LogSeg(GameObject root, bool alongX, float fixedPos, float a, float b, float yC, float R, Material m)
    {
        float len = b - a;
        if (len < 0.05f) return;
        float mid = (a + b) * 0.5f;
        Cylinder(root, R, len, m,
            new Vector3(alongX ? mid : fixedPos, yC, alongX ? fixedPos : mid),
            alongX ? new Vector3(0, 0, 90f) : new Vector3(90f, 0, 0));
    }

    // Tangga balok padat. start = dasar anak tangga pertama; naik ke arah +Z lokal setelah diputar yaw.
    // PENTING: rise harus <= 0.25 m agar bisa dinaiki CharacterController (stepOffset default 0.3).
    private static void Stairs(GameObject root, Vector3 start, float yawDeg, int steps, float rise, float run, float width, Material mat)
    {
        var g = Group(root, "Stairs", start, yawDeg);
        for (int i = 0; i < steps; i++)
        {
            float hgt = rise * (i + 1);
            Box(g, width, hgt, run, mat, new Vector3(0, hgt * 0.5f, run * (i + 0.5f)));
        }
    }

    // Tangga otomatis: jumlah anak tangga dihitung dari tinggi total & HouseDimensions.MaxStairRise.
    // topEdge = titik (di y = 0) tempat ujung atas tangga menyentuh lantai atas.
    // yaw = arah naik (0 = naik ke +Z, 180 = naik ke -Z, dst., dalam ruang lokal rumah).
    private static void StairsTo(GameObject root, Vector3 topEdge, float yawDeg, float totalHeight, float width, Material mat)
    {
        int steps = HouseDimensions.StairSteps(totalHeight);
        float rise = totalHeight / steps;
        float run = HouseDimensions.StairRun;
        Vector3 climbDir = Quaternion.Euler(0, yawDeg, 0) * Vector3.forward;
        Vector3 start = topEdge - climbDir * (steps * run);
        Stairs(root, start, yawDeg, steps, rise, run, width, mat);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  FURNITUR TRADISIONAL  (pos = titik di lantai; yaw memutar, depan = +Z lokal)
    // ─────────────────────────────────────────────────────────────────────
    private static void Dipan(GameObject root, Vector3 pos, float yaw)
    {
        var g = Group(root, "Dipan", pos, yaw);
        foreach (float lx in new[] { -0.9f, 0.9f })
            foreach (float lz in new[] { -0.45f, 0.45f })
                Box(g, 0.1f, 0.36f, 0.1f, matWood, new Vector3(lx, 0.18f, lz), default, false);
        Box(g, 2.0f, 0.08f, 1.1f, matDeck, new Vector3(0, 0.40f, 0));
        Box(g, 1.92f, 0.12f, 1.02f, matBatik, new Vector3(0, 0.50f, 0), default, false);          // kasur berkain batik
        Box(g, 0.5f, 0.14f, 0.7f, matWall, new Vector3(-0.68f, 0.62f, 0), default, false);        // bantal
        Box(g, 0.75f, 0.08f, 1.02f, matRed, new Vector3(0.55f, 0.58f, 0), default, false);        // selimut
        Box(g, 0.08f, 0.95f, 1.14f, matRedWood, new Vector3(-1.0f, 0.7f, 0), default, false);     // papan kepala
        Box(g, 0.09f, 0.08f, 1.0f, matGold, new Vector3(-1.0f, 1.2f, 0), default, false);         // ukiran emas
    }

    private static void MejaMakan(GameObject root, Vector3 pos, float yaw, float w = 1.3f, float d = 0.8f)
    {
        var g = Group(root, "MejaMakan", pos, yaw);
        Box(g, w, 0.08f, d, matWood, new Vector3(0, 0.76f, 0));
        foreach (float lx in new[] { -1f, 1f })
            foreach (float lz in new[] { -1f, 1f })
                Box(g, 0.08f, 0.72f, 0.08f, matWood, new Vector3(lx * (w * 0.5f - 0.08f), 0.36f, lz * (d * 0.5f - 0.08f)), default, false);
        Box(g, w - 0.2f, 0.05f, 0.05f, matWood, new Vector3(0, 0.3f, 0), default, false);
    }

    private static void Dingklik(GameObject root, Vector3 pos)
    {
        var g = Group(root, "Dingklik", pos, 0f);
        Cylinder(g, 0.22f, 0.06f, matWood, new Vector3(0, 0.44f, 0), default, false);
        foreach (float lx in new[] { -0.12f, 0.12f })
            foreach (float lz in new[] { -0.12f, 0.12f })
                Box(g, 0.06f, 0.42f, 0.06f, matWood, new Vector3(lx, 0.21f, lz), default, false);
    }

    private static void Lemari(GameObject root, Vector3 pos, float yaw)
    {
        var g = Group(root, "LemariUkir", pos, yaw);
        Box(g, 1.2f, 1.9f, 0.55f, matRedWood, new Vector3(0, 0.95f, 0));
        Box(g, 1.3f, 0.12f, 0.62f, matWood, new Vector3(0, 1.96f, 0), default, false);
        Box(g, 0.02f, 1.7f, 0.02f, matDark, new Vector3(0, 0.95f, 0.285f), default, false);
        foreach (float sx in new[] { -0.3f, 0.3f })
        {
            Box(g, 0.42f, 0.8f, 0.02f, matWood, new Vector3(sx, 1.2f, 0.285f), default, false);
            Box(g, 0.34f, 0.04f, 0.025f, matGold, new Vector3(sx, 1.65f, 0.29f), default, false);
        }
        Sphere(g, 0.04f, matGold, new Vector3(-0.07f, 0.95f, 0.3f));
        Sphere(g, 0.04f, matGold, new Vector3(0.07f, 0.95f, 0.3f));
    }

    private static void Kendi(GameObject root, Vector3 pos, float s)
    {
        Sphere(root, 0.13f * s, matClay, pos + Vector3.up * (0.13f * s));
        Cylinder(root, 0.05f * s, 0.14f * s, matClay, pos + Vector3.up * (0.30f * s), default, false);
        Cylinder(root, 0.065f * s, 0.03f * s, matClay, pos + Vector3.up * (0.385f * s), default, false);
    }

    private static void RakDapur(GameObject root, Vector3 pos, float yaw)
    {
        var g = Group(root, "RakDapur", pos, yaw);
        foreach (float by in new[] { 1.35f, 1.8f })
            Box(g, 1.5f, 0.05f, 0.28f, matWood, new Vector3(0, by, 0.14f), default, false);
        foreach (float sx in new[] { -0.7f, 0.7f })
            Box(g, 0.05f, 0.5f, 0.05f, matWood, new Vector3(sx, 1.58f, 0.04f), default, false);
        Kendi(g, new Vector3(-0.45f, 1.375f, 0.14f), 1f);
        Kendi(g, new Vector3(-0.1f, 1.375f, 0.14f), 0.8f);
        Cylinder(g, 0.12f, 0.1f, matMetal, new Vector3(0.4f, 1.43f, 0.14f), default, false);
        Cylinder(g, 0.1f, 0.08f, matClay, new Vector3(0.3f, 1.87f, 0.14f), default, false);
        Cylinder(g, 0.08f, 0.1f, matClay, new Vector3(-0.3f, 1.88f, 0.14f), default, false);
    }

    // Pawon: tungku tanah liat dengan periuk
    private static void Tungku(GameObject root, Vector3 pos, float yaw)
    {
        var g = Group(root, "TungkuPawon", pos, yaw);
        Box(g, 1.0f, 0.55f, 0.7f, matClay, new Vector3(0, 0.275f, 0));
        Box(g, 0.96f, 0.06f, 0.66f, matDark, new Vector3(0, 0.58f, 0), default, false);
        Cylinder(g, 0.2f, 0.28f, matMetal, new Vector3(-0.22f, 0.75f, 0), default, false);
        Cylinder(g, 0.16f, 0.22f, matClay, new Vector3(0.25f, 0.72f, 0), default, false);
        Box(g, 0.4f, 0.22f, 0.04f, matDark, new Vector3(0, 0.2f, 0.36f), default, false);
        Box(g, 0.28f, 0.08f, 0.02f, matLamp, new Vector3(0, 0.12f, 0.365f), default, false);   // bara menyala
        for (int i = 0; i < 3; i++)
            Cylinder(g, 0.06f, 0.5f, matWood, new Vector3(0.75f, 0.07f + i * 0.1f, 0.0f + (i % 2) * 0.05f), new Vector3(90f, 0, 0), false);
    }

    private static void Gentong(GameObject root, Vector3 pos)
    {
        Sphere(root, 0.32f, matClay, pos + Vector3.up * 0.34f, new Vector3(1f, 1.05f, 1f));
        Cylinder(root, 0.17f, 0.12f, matClay, pos + Vector3.up * 0.68f, default, false);
    }

    private static void Bakul(GameObject root, Vector3 pos)
    {
        Cylinder(root, 0.25f, 0.28f, matHay, pos + Vector3.up * 0.14f, default, false);
        Cylinder(root, 0.2f, 0.04f, matLeaf, pos + Vector3.up * 0.29f, default, false);
    }

    private static void Peti(GameObject root, Vector3 pos, float yaw)
    {
        var g = Group(root, "PetiKayu", pos, yaw);
        Box(g, 0.9f, 0.5f, 0.5f, matWood, new Vector3(0, 0.25f, 0));
        Box(g, 0.94f, 0.08f, 0.54f, matDeck, new Vector3(0, 0.54f, 0), default, false);
        foreach (float sx in new[] { -0.3f, 0.3f })
            Box(g, 0.06f, 0.52f, 0.52f, matMetal, new Vector3(sx, 0.26f, 0), default, false);
    }

    private static void Tikar(GameObject root, Vector3 pos, float yaw, float w, float d)
    {
        var g = Group(root, "TikarAnyaman", pos, yaw);
        Box(g, w, 0.025f, d, matHay, new Vector3(0, 0.0125f, 0), default, false);
        foreach (float sz in new[] { -1f, 1f })
            Box(g, w, 0.03f, 0.07f, matBatik, new Vector3(0, 0.015f, sz * (d * 0.5f - 0.035f)), default, false);
        foreach (float sx in new[] { -1f, 1f })
            Box(g, 0.07f, 0.03f, d - 0.14f, matBatik, new Vector3(sx * (w * 0.5f - 0.035f), 0.015f, 0), default, false);
    }

    private static void KainBatik(GameObject root, Vector3 center, float yaw, float w, float h)
    {
        var g = Group(root, "KainBatik", center, yaw);
        Box(g, w, h, 0.04f, matBatik, Vector3.zero, default, false);
        Box(g, w, 0.05f, 0.05f, matGold, new Vector3(0, h * 0.5f - 0.03f, 0), default, false);
        Box(g, w, 0.05f, 0.05f, matGold, new Vector3(0, -h * 0.5f + 0.03f, 0), default, false);
        Box(g, w * 0.25f, w * 0.25f, 0.045f, matGold, Vector3.zero, new Vector3(0, 0, 45f), false);
    }

    private static void Pelita(GameObject root, Vector3 pos)
    {
        Cylinder(root, 0.05f, 0.06f, matClay, pos + Vector3.up * 0.03f, default, false);
        Sphere(root, 0.035f, matLamp, pos + Vector3.up * 0.1f);
    }

    private static void Bale(GameObject root, Vector3 pos, float yaw, float w, float d)
    {
        var g = Group(root, "BaleBale", pos, yaw);
        Box(g, w, 0.12f, d, matDeck, new Vector3(0, 0.45f, 0));
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                Box(g, 0.12f, 0.4f, 0.12f, matWood, new Vector3(sx * (w * 0.5f - 0.1f), 0.2f, sz * (d * 0.5f - 0.1f)), default, false);
        Tikar(g, new Vector3(0, 0.51f, 0), 0f, w - 0.2f, d - 0.2f);
    }

    private static void Lesung(GameObject root, Vector3 pos)
    {
        Cylinder(root, 0.3f, 0.5f, matWood, pos + Vector3.up * 0.25f, default, false);
        Cylinder(root, 0.2f, 0.04f, matDark, pos + Vector3.up * 0.5f, default, false);
        Cylinder(root, 0.04f, 1.3f, matWood, pos + new Vector3(0.38f, 0.65f, 0.1f), new Vector3(0, 0, 10f), false);
    }

    private static void GamelanSet(GameObject root, Vector3 pos, float yaw)
    {
        var g = Group(root, "Gamelan", pos, yaw);
        foreach (float sx in new[] { -0.95f, 0.95f })
            Box(g, 0.1f, 1.6f, 0.1f, matRedWood, new Vector3(sx, 0.8f, 0), default, false);
        Box(g, 2.0f, 0.1f, 0.1f, matRedWood, new Vector3(0, 1.55f, 0), default, false);
        Cylinder(g, 0.3f, 0.05f, matGold, new Vector3(-0.5f, 1.0f, 0), new Vector3(90f, 0, 0), false);
        Cylinder(g, 0.38f, 0.05f, matGold, new Vector3(0.1f, 0.95f, 0), new Vector3(90f, 0, 0), false);
        Cylinder(g, 0.24f, 0.05f, matGold, new Vector3(0.65f, 1.05f, 0), new Vector3(90f, 0, 0), false);
        Box(g, 1.6f, 0.2f, 0.4f, matWood, new Vector3(0, 0.3f, 1.0f));
        for (int i = 0; i < 6; i++)
            Box(g, 0.2f, 0.02f, 0.34f, matGold, new Vector3(-0.65f + i * 0.26f, 0.41f, 1.0f), default, false);
        Cylinder(g, 0.14f, 0.5f, matWood, new Vector3(-0.9f, 0.3f, 1.9f), new Vector3(0, 0, 90f), false);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PENATA FURNITUR OTOMATIS (public: dipakai juga oleh BuildModularHouse)
    //  cx,cz,hx,hz = pusat & setengah ukuran ruang (diukur ke as tembok); y = permukaan lantai.
    //  Depan ruang = +Z. doorX = posisi X pintu agar meja tidak menghalangi.
    // ─────────────────────────────────────────────────────────────────────
    public const int FBed = 1, FWardrobe = 2, FKitchen = 4, FDining = 8, FChest = 16, FMat = 32, FAll = 63;

    public static void FurnishInterior(GameObject root, float cx, float cz, float hx, float hz,
        float y, float doorX, int flags, float ceilingY = -1f)
    {
        EnsureMaterials();
        if (ceilingY < 0f) ceilingY = y + 2.8f;

        float x0 = cx - hx + 0.2f, x1 = cx + hx - 0.2f, zb = cz - hz + 0.15f;
        float avail = x1 - x0;
        float bedEnd = x0, kitStart = x1;
        bool diningRight = false;

        // Belakang-kiri: dipan + kain batik
        if ((flags & FBed) != 0 && avail >= 2.4f)
        {
            Dipan(root, new Vector3(x0 + 1.0f, y, zb + 0.6f), 0f);
            KainBatik(root, new Vector3(x0 + 1.0f, y + 1.6f, zb), 0f, 1.3f, 0.9f);
            bedEnd = x0 + 2.0f;
        }

        // Belakang-kanan: dapur (pawon, rak, gentong, bakul)
        if ((flags & FKitchen) != 0 && avail >= 3.0f)
        {
            Tungku(root, new Vector3(x1 - 0.5f, y, zb + 0.35f), 0f);
            RakDapur(root, new Vector3(x1 - 0.7f, y, zb), 0f);
            Gentong(root, new Vector3(x1 - 1.5f, y, zb + 0.4f));
            kitStart = x1 - 1.95f;
            if (avail >= 4.4f)
            {
                Bakul(root, new Vector3(x1 - 2.05f, y, zb + 0.3f));
                kitStart = x1 - 2.35f;
            }
        }

        // Lemari ukir di antara dipan dan dapur
        if ((flags & FWardrobe) != 0 && kitStart - bedEnd >= 1.5f)
            Lemari(root, new Vector3((bedEnd + kitStart) * 0.5f, y, zb + 0.28f), 0f);

        // Meja makan + dingklik (hanya di ruang lebar, dan tidak di jalur pintu)
        if ((flags & FDining) != 0 && avail >= 4.4f)
        {
            float tx = Mathf.Abs(doorX - (x0 + 0.9f)) > 1.9f ? x0 + 0.9f : x1 - 0.9f;
            diningRight = tx > cx;
            float tz = cz + 0.35f;
            MejaMakan(root, new Vector3(tx, y, tz), 0f, 1.3f, 0.8f);
            Dingklik(root, new Vector3(tx - 0.35f, y, tz + 0.62f));
            Dingklik(root, new Vector3(tx + 0.35f, y, tz - 0.62f));
            Pelita(root, new Vector3(tx, y + 0.8f, tz));
            Kendi(root, new Vector3(tx + 0.42f, y + 0.8f, tz + 0.15f), 0.7f);
        }

        // Peti kayu di dinding samping
        if ((flags & FChest) != 0)
            Peti(root, new Vector3(diningRight ? x0 + 0.25f : x1 - 0.25f, y, cz + hz * 0.45f), 90f);

        // Tikar anyaman di tengah
        if ((flags & FMat) != 0)
            Tikar(root, new Vector3(cx + hx * 0.2f, y, cz + 0.1f), 0f, 1.8f, 1.2f);

        // Lentera gantung (dinaikkan agar tidak di depan wajah)
        Box(root, 0.02f, 0.5f, 0.02f, matDark, new Vector3(cx, ceilingY - 0.3f, cz - 0.3f), default, false);
        Lantern(root, new Vector3(cx, ceilingY - 0.6f, cz - 0.3f));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  DISPATCHER
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
            case HouseStyle.BalaiDesa: BuildBalaiDesa(root); break;
        }
        return root;
    }

    // ═════════════════════════════════════════════════════════════════════
    //  1. LOG CABIN
    // ═════════════════════════════════════════════════════════════════════
    private static void BuildCabin(GameObject root)
    {
        const float fy = 0.16f;   // permukaan lantai dalam
        Box(root, 8.7f, 0.55f, 6.7f, matConcrete, new Vector3(0, -0.175f, 0));
        Box(root, 7.7f, 0.08f, 5.7f, matDeck, new Vector3(0, 0.12f, 0));

        float R = 0.17f;
        var front = new[] { Door(0f, 1.7f, fy, 2.7f), Win(-2.6f, 1.4f, fy, 0.9f, 1.1f), Win(2.6f, 1.4f, fy, 0.9f, 1.1f) };
        var back  = new[] { Win(-1.8f, 1.4f, fy, 0.9f, 1.1f) };
        var left  = new[] { Win(-1.6f, 1.4f, fy, 0.9f, 1.1f) };
        var right = new[] { Win(1.8f, 1.4f, fy, 0.9f, 1.1f) };
        LogWall(root, true,  3f,  -4.35f, 4.35f, fy, 10, R, front);
        LogWall(root, true, -3f,  -4.35f, 4.35f, fy, 10, R, back);
        LogWall(root, false, -4f, -3.35f, 3.35f, fy, 10, R, left);
        LogWall(root, false,  4f, -3.35f, 3.35f, fy, 10, R, right);

        // Bingkai pintu (lebar 1.7 m, tinggi 2.7 m)
        Box(root, 0.12f, 2.8f, 0.34f, matDark, new Vector3(-0.91f, 1.56f, 3f), default, false);
        Box(root, 0.12f, 2.8f, 0.34f, matDark, new Vector3(0.91f, 1.56f, 3f), default, false);
        Box(root, 1.94f, 0.12f, 0.34f, matDark, new Vector3(0, 2.92f, 3f), default, false);
        // Daun pintu terbuka ke dalam, menempel di sisi kiri (tanpa collider)
        Box(root, 1.0f, 2.6f, 0.07f, matSlat, new Vector3(-0.88f, 1.46f, 2.5f), new Vector3(0, 90f, 0), false);

        // Atap pelana
        float pitchAngle = Mathf.Atan(2.333f / 3.5f) * Mathf.Rad2Deg;
        Box(root, 8.9f, 0.14f, 4.206f, matRoof, new Vector3(0, 3.833f, 1.75f), new Vector3(pitchAngle, 0, 0));
        Box(root, 8.9f, 0.14f, 4.206f, matRoof, new Vector3(0, 3.833f, -1.75f), new Vector3(-pitchAngle, 0, 0));
        Box(root, 8.95f, 0.16f, 0.34f, matDark, new Vector3(0, 5.03f, 0), default, false);
        GableTriangle(root, 6.2f, 2.05f, 0.15f, matSlat, new Vector3(-3.95f, 3.0f, 0), -90f);
        GableTriangle(root, 6.2f, 2.05f, 0.15f, matSlat, new Vector3(3.95f, 3.0f, 0), 90f);

        // Balok melintang di dalam
        foreach (float bx in new[] { -2.6f, 0f, 2.6f })
            Box(root, 0.2f, 0.2f, 6.0f, matWood, new Vector3(bx, 3.15f, 0), default, false);

        // Teras depan setinggi lantai
        Box(root, 8.8f, 0.2f, 2.2f, matDeck, new Vector3(0, 0.06f, 4.1f));
        // Atap teras dinaikkan & hampir datar agar kepala tidak mepet
        Box(root, 8.9f, 0.12f, 2.6f, matRoof, new Vector3(0, 2.95f, 4.3f), new Vector3(3f, 0, 0));
        for (float x = -4.1f; x <= 4.11f; x += 2.73f)
            Cylinder(root, 0.1f, 2.85f, matLogs[0], new Vector3(x, 1.58f, 5.3f));
        Box(root, 1.8f, 0.1f, 0.6f, matDeck, new Vector3(0, 0.05f, 5.5f));
        Lantern(root, new Vector3(1.2f, 2.3f, 3.4f));

        // Cerobong batu (di luar dinding kanan)
        Box(root, 1.1f, 6.0f, 1.5f, matConcrete, new Vector3(4.75f, 2.6f, 0));
        Box(root, 1.3f, 0.15f, 1.7f, matDark, new Vector3(4.75f, 5.65f, 0), default, false);

        // Kayu bakar
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4 - r; c++)
                Cylinder(root, 0.12f, 1.6f, matLogs[(r + c) % 4],
                    new Vector3(-4.65f, 0.1f + r * 0.21f, 0.7f + c * 0.25f + r * 0.125f), new Vector3(90f, 0, 0), false);

        FurnishInterior(root, 0f, 0f, 3.85f, 2.85f, fy, 0f, FAll, 3.05f);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  2. MODERN WOOD VILLA (lantai 1 + lantai 2 lewat tangga luar & balkon)
    // ═════════════════════════════════════════════════════════════════════
    private static void BuildModern(GameObject root)
    {
        const float fy = 0.04f;
        Box(root, 10.2f, 0.4f, 6.2f, matDark, new Vector3(0, -0.2f, 0));
        Box(root, 9.8f, 0.04f, 5.8f, matDeck, new Vector3(0, 0.02f, 0));

        // Lantai 1: ruang tamu/dapur (kanan) + kamar (kiri, sekat di x=-2)
        Room(root, 0f, 0f, 4.9f, 2.9f, fy, 3.0f, 0.2f, matSlat,
            new[] { new WallOpening(1.45f, 6.9f, fy, 3.0f) },       // fasad kaca (diisi panel di bawah)
            new[] { Win(-3.2f, 1.4f, fy, 0.9f, 1.4f), Win(1.0f, 1.6f, fy, 0.9f, 1.4f), Win(3.6f, 1.2f, fy, 0.9f, 1.4f) },
            new[] { Win(-1.0f, 1.4f, fy, 0.9f, 1.4f) },
            new[] { Win(0f, 1.6f, fy, 0.9f, 1.4f) });
        // Sekat kamar: pintu 1.7 m (sebelumnya 1.0 m -> kamar tidak bisa dimasuki)
        WallRun(root, false, -2.0f, -2.8f, 2.8f, fy, 3.0f, 0.15f, matSlat, new[] { Door(0.6f, 1.7f, fy, 2.7f) });

        // Fasad kaca dengan celah pintu lebar 1.8 m (x 0.6 .. 2.4)
        Box(root, 2.6f, 2.8f, 0.04f, matGlass, new Vector3(-0.7f, 1.44f, 2.9f));
        Box(root, 2.5f, 2.8f, 0.04f, matGlass, new Vector3(3.65f, 1.44f, 2.9f));
        foreach (float mx in new[] { -2.0f, 0.6f, 2.4f, 4.9f })
            Box(root, 0.08f, 3.0f, 0.14f, matDark, new Vector3(mx, 1.5f, 2.9f), default, false);
        Box(root, 1.8f, 0.1f, 0.14f, matDark, new Vector3(1.5f, 2.86f, 2.9f), default, false);

        // Pelat lantai 2 / atap lantai 1
        Box(root, 10.7f, 0.3f, 6.5f, matDark, new Vector3(-0.25f, 3.15f, 0.1f));

        // Lantai 2 (kamar tidur utama) — dinding dinaikkan ke 6.2 agar pintu 2.7 m lega
        Room(root, -2.25f, 0.75f, 3.25f, 2.75f, 3.3f, 6.2f, 0.2f, matSlat,
            new[] { Door(-2.25f, 1.7f, 3.3f, 2.7f), Win(-4.3f, 1.2f, 3.3f, 0.9f, 1.3f), Win(-0.2f, 1.2f, 3.3f, 0.9f, 1.3f) },
            new[] { Win(-2.25f, 2.0f, 3.3f, 0.9f, 1.3f) },
            new[] { Win(0.75f, 1.6f, 3.3f, 0.9f, 1.3f) },
            new[] { Win(0.75f, 1.6f, 3.3f, 0.9f, 1.3f) });
        Box(root, 7.2f, 0.25f, 6.2f, matDark, new Vector3(-2.25f, 6.32f, 0.75f));

        // Balkon depan lantai 2 (dilebarkan agar menyambung dengan tangga) + pagar
        Box(root, 9.0f, 0.3f, 1.8f, matDeck, new Vector3(-3.2f, 3.15f, 4.4f));
        Box(root, 9.0f, 0.06f, 0.06f, matDark, new Vector3(-3.2f, 4.2f, 5.25f), default, false);
        for (int i = 0; i < 6; i++)
            Box(root, 0.06f, 0.9f, 0.06f, matDark, new Vector3(-7.6f + i * 1.7f, 3.75f, 5.25f), default, false);
        // Tangga luar menuju balkon: lebar 1.6 m (sebelumnya 1.0 m = tidak muat), 16 anak tangga, tinggi total 3.3 m
        StairsTo(root, new Vector3(-6.5f, 0f, 3.5f), 0f, 3.3f, HouseDimensions.StairWidth, matDeck);

        // Teras & kolam refleksi
        Box(root, 7.4f, 0.3f, 4.0f, matDeck, new Vector3(-2.3f, -0.15f, 5.0f));
        Box(root, 5.4f, 0.3f, 1.2f, matConcrete, new Vector3(4.1f, -0.15f, 3.6f));
        Box(root, 5.4f, 0.3f, 1.0f, matConcrete, new Vector3(4.1f, -0.15f, 7.7f));
        Box(root, 0.6f, 0.3f, 3.0f, matConcrete, new Vector3(1.7f, -0.15f, 5.7f));
        Box(root, 0.4f, 0.3f, 3.0f, matConcrete, new Vector3(6.4f, -0.15f, 5.7f));
        Box(root, 4.2f, 0.02f, 3.0f, matWater, new Vector3(4.1f, -0.08f, 5.7f), default, false);

        Lantern(root, new Vector3(-5.6f, 0.3f, 7.8f));
        Lantern(root, new Vector3(7.0f, 0.3f, 8.6f));
        for (int i = 0; i < 6; i++)
            Cylinder(root, 0.5f, 0.08f, matConcrete, new Vector3(Mathf.Sin(i * 1.6f) * 0.5f - 0.2f, -0.15f, 8.6f + i * 1.2f));

        // Furnitur: kamar bawah, ruang tamu-dapur, kamar atas
        FurnishInterior(root, -3.45f, 0f, 1.45f, 2.9f, fy, -2.0f, FBed, 2.95f);
        FurnishInterior(root, 1.45f, 0f, 3.4f, 2.9f, fy, 1.5f, FKitchen | FDining | FWardrobe | FChest | FMat, 2.95f);
        FurnishInterior(root, -2.25f, 0.75f, 3.2f, 2.7f, 3.3f, -2.25f, FBed | FWardrobe | FDining | FChest | FMat, 6.15f);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  3. RUMAH PANGGUNG NUSANTARA
    // ═════════════════════════════════════════════════════════════════════
    private static void BuildPanggung(GameObject root)
    {
        const float fy = 2.125f;   // permukaan lantai panggung

        float[] xs = { -3.6f, -1.2f, 1.2f, 3.6f };
        float[] zs = { -2.2f, 3.8f };
        foreach (var x in xs)
            foreach (var z in zs)
                Cylinder(root, 0.18f, 2.5f, matWood, new Vector3(x, 1.0f, z));

        Box(root, 8.8f, 0.25f, 7.0f, matDeck, new Vector3(0, 2.0f, 0.75f));

        Room(root, 0f, 0f, 4.0f, 2.5f, fy, 5.15f, 0.15f, matSlat,
            new[] { Door(0f, 1.7f, fy, 2.7f), Win(-2.7f, 1.2f, fy, 0.9f, 1.1f), Win(2.7f, 1.2f, fy, 0.9f, 1.1f) },
            new[] { Win(-1.5f, 1.2f, fy, 0.9f, 1.1f), Win(1.5f, 1.2f, fy, 0.9f, 1.1f) },
            new[] { Win(0f, 1.4f, fy, 0.9f, 1.1f) },
            new[] { Win(0f, 1.4f, fy, 0.9f, 1.1f) }, matWood);

        // Atap pelana tinggi
        float roofPitch = Mathf.Atan(2.4f / 3.4f) * Mathf.Rad2Deg;
        Box(root, 9.4f, 0.15f, 4.16f, matRoof, new Vector3(0, 5.8f, 1.7f), new Vector3(roofPitch, 0, 0));
        Box(root, 9.4f, 0.15f, 4.16f, matRoof, new Vector3(0, 5.8f, -1.7f), new Vector3(-roofPitch, 0, 0));
        Box(root, 9.5f, 0.18f, 0.30f, matDark, new Vector3(0, 6.95f, 0), default, false);
        GableTriangle(root, 6.0f, 2.3f, 0.15f, matSlat, new Vector3(-4.1f, 4.5f, 0), -90f);
        GableTriangle(root, 6.0f, 2.3f, 0.15f, matSlat, new Vector3(4.1f, 4.5f, 0), 90f);

        // Tangga kayu padat ke veranda: 10 anak tangga x 0.2125 m = 2.125 m (dulu 0.38 m/anak = tidak bisa dinaiki).
        // Mulai dari ujung bawah (z = 7.25) naik ke arah rumah (-Z) sampai tepi dek (z = 4.25).
        StairsTo(root, new Vector3(0f, 0f, 4.25f), 180f, fy, 1.8f, matDeck);

        // Pagar veranda (celah di tengah untuk tangga)
        Box(root, 3.2f, 0.08f, 0.08f, matDark, new Vector3(-2.6f, 3.1f, 4.15f), default, false);
        Box(root, 3.2f, 0.08f, 0.08f, matDark, new Vector3(2.6f, 3.1f, 4.15f), default, false);
        foreach (float px in new[] { -4.1f, -1.1f, 1.1f, 4.1f })
            Box(root, 0.08f, 1.0f, 0.08f, matDark, new Vector3(px, 2.6f, 4.15f), default, false);

        Lantern(root, new Vector3(-1.5f, 4.0f, 2.8f));
        Lantern(root, new Vector3(1.5f, 4.0f, 2.8f));

        FurnishInterior(root, 0f, 0f, 3.9f, 2.4f, fy, 0f, FAll, 5.0f);
        Lesung(root, new Vector3(3.3f, fy, 3.4f));     // lesung & alu di veranda
        Bakul(root, new Vector3(-3.3f, fy, 3.5f));
    }

    // ═════════════════════════════════════════════════════════════════════
    //  4. A-FRAME CABIN
    // ═════════════════════════════════════════════════════════════════════
    private static void BuildAFrame(GameObject root)
    {
        float angleZ = Mathf.Atan2(6.2f, 3.2f) * Mathf.Rad2Deg;

        // Atap miring segitiga
        Box(root, 6.98f, 0.2f, 7.6f, matRoof, new Vector3(-1.6f, 3.1f, 0), new Vector3(0, 0, angleZ));
        Box(root, 6.98f, 0.2f, 7.6f, matRoof, new Vector3( 1.6f, 3.1f, 0), new Vector3(0, 0, -angleZ));
        Box(root, 0.35f, 0.35f, 7.7f, matDark, new Vector3(0, 6.25f, 0), default, false);

        // Lantai interior & dek teras depan
        Box(root, 6.4f, 0.3f, 7.4f, matDeck, new Vector3(0, 0, 0));          // lantai dalam (y atas = 0.15)
        Box(root, 6.4f, 0.3f, 3.6f, matDeck, new Vector3(0, 0, 5.5f));       // dek depan (z = 3.7 s/d 7.3)

        // Pagar samping dek depan (kiri & kanan terpasang kokoh dengan tiang, bagian depan terbuka untuk jalan)
        // Sisi Kiri
        Cylinder(root, 0.08f, 1.1f, matDark, new Vector3(-3.1f, 0.65f, 3.8f));
        Cylinder(root, 0.08f, 1.1f, matDark, new Vector3(-3.1f, 0.65f, 5.5f));
        Cylinder(root, 0.08f, 1.1f, matDark, new Vector3(-3.1f, 0.65f, 7.2f));
        Box(root, 0.1f, 0.1f, 3.5f, matDark, new Vector3(-3.1f, 1.1f, 5.5f), default, false);

        // Sisi Kanan
        Cylinder(root, 0.08f, 1.1f, matDark, new Vector3(3.1f, 0.65f, 3.8f));
        Cylinder(root, 0.08f, 1.1f, matDark, new Vector3(3.1f, 0.65f, 5.5f));
        Cylinder(root, 0.08f, 1.1f, matDark, new Vector3(3.1f, 0.65f, 7.2f));
        Box(root, 0.1f, 0.1f, 3.5f, matDark, new Vector3(3.1f, 1.1f, 5.5f), default, false);

        // Lentera sudut teras terpasang di atas tiang kayu (tidak melayang!)
        Lantern(root, new Vector3(-3.1f, 1.35f, 7.2f));
        Lantern(root, new Vector3( 3.1f, 1.35f, 7.2f));

        // ── Fasad Depan (Bebas Glitch, Pintu Terbuka Lebar) ──
        float zf = 3.7f;

        // Kusen pintu kayu elegan (lebar bukaan 1.9m, tinggi 2.85m -> leluasa dimasuki)
        Box(root, 0.15f, 2.85f, 0.22f, matDark, new Vector3(-0.95f, 1.55f, zf));
        Box(root, 0.15f, 2.85f, 0.22f, matDark, new Vector3( 0.95f, 1.55f, zf));
        Box(root, 2.05f, 0.15f, 0.22f, matDark, new Vector3(0f, 2.95f, zf));

        // Dinding kaca & kisi samping kiri pintu (x = -2.8 s/d -0.95)
        Box(root, 1.7f, 2.6f, 0.06f, matGlass, new Vector3(-1.85f, 1.45f, zf + 0.02f), default, false);
        WallPiece(root, true, zf, -2.75f, -0.95f, 0.15f, 0.55f, 0.14f, matSlat);
        Box(root, 0.08f, 2.4f, 0.14f, matDark, new Vector3(-1.85f, 1.45f, zf), default, false);
        Box(root, 1.7f, 0.08f, 0.14f, matDark, new Vector3(-1.85f, 1.45f, zf), default, false);

        // Dinding kaca & kisi samping kanan pintu (x = 0.95 s/d 2.8)
        Box(root, 1.7f, 2.6f, 0.06f, matGlass, new Vector3(1.85f, 1.45f, zf + 0.02f), default, false);
        WallPiece(root, true, zf, 0.95f, 2.75f, 0.15f, 0.55f, 0.14f, matSlat);
        Box(root, 0.08f, 2.4f, 0.14f, matDark, new Vector3(1.85f, 1.45f, zf), default, false);
        Box(root, 1.7f, 0.08f, 0.14f, matDark, new Vector3(1.85f, 1.45f, zf), default, false);

        // Dinding penutup segitiga atas (y = 2.95 s/d 6.1)
        GableTriangle(root, 3.8f, 3.0f, 0.16f, matSlat, new Vector3(0, 3.05f, zf), 0f);

        // Jendela segitiga loteng atas dekoratif
        Box(root, 0.9f, 0.9f, 0.06f, matGlass, new Vector3(0, 4.35f, zf + 0.02f), default, false);
        Box(root, 0.96f, 0.08f, 0.10f, matDark, new Vector3(0, 4.35f, zf + 0.04f), default, false);
        Box(root, 0.08f, 0.96f, 0.10f, matDark, new Vector3(0, 4.35f, zf + 0.04f), default, false);

        // Lentera hangat di atas pintu depan
        Lantern(root, new Vector3(0f, 3.3f, zf + 0.35f));

        // Dinding belakang kayu (solid, ber-collider)
        GableTriangle(root, 6.0f, 5.8f, 0.18f, matSlat, new Vector3(0, 0.15f, -3.7f), 180f);

        // Cerobong asap
        Cylinder(root, 0.14f, 3.2f, matDark, new Vector3(1.8f, 4.2f, -1.0f));

        // Api unggun halaman depan
        Cylinder(root, 0.85f, 0.25f, matConcrete, new Vector3(0, -0.05f, 9.5f));
        Cone(root, 0.35f, 0.8f, matOrange, new Vector3(0, 0.35f, 9.5f));
        Box(root, 0.7f, 0.45f, 0.6f, matWood, new Vector3(-1.7f, 0.15f, 9.5f));
        Box(root, 0.7f, 0.45f, 0.6f, matWood, new Vector3( 1.7f, 0.15f, 9.5f));

        // Ruang interior yang nyaman dan rapi
        FurnishInterior(root, 0f, 0f, 2.0f, 3.3f, 0.15f, 0f, FBed | FKitchen | FMat, 2.9f);
        Dingklik(root, new Vector3(-1.0f, 0.15f, 1.7f));
        Dingklik(root, new Vector3(-1.5f, 0.15f, 2.2f));
    }

    // ═════════════════════════════════════════════════════════════════════
    //  5. JOGLO (pendopo terbuka + gedhong hollow yang bisa dimasuki)
    // ═════════════════════════════════════════════════════════════════════
    private static void BuildJoglo(GameObject root)
    {
        const float fy = 0.15f;
        Box(root, 10.4f, 0.5f, 10.4f, matConcrete, new Vector3(0, -0.1f, 0));

        // Tiang pendopo dipertinggi (4.0 m) agar atap tidak mepet kepala/kamera
        float[] coords = { -4f, 0f, 4f };
        foreach (var x in coords)
            foreach (var z in coords)
            {
                if (x == 0 && z == 0) continue;
                Cylinder(root, 0.16f, 4.0f, matWood, new Vector3(x, 2.15f, z));
                Lantern(root, new Vector3(x * 0.88f, 3.2f, z * 0.88f));
            }

        // Soko guru: tiang utama di sudut gedhong (tidak lagi menjorok ke dalam ruangan)
        float[] sg = { -2.0f, 2.0f };
        foreach (var x in sg)
            foreach (var z in sg)
                Cylinder(root, 0.22f, 5.6f, matWood, new Vector3(x, 2.95f, z));

        // Gedhong (ruang inti) — berongga, 4 x 4 m, tinggi dinding 3.95 m, pintu 1.7 x 2.7 m
        Room(root, 0f, 0f, 2.0f, 2.0f, fy, 4.1f, 0.2f, matSlat,
            new[] { Door(0f, 1.7f, fy, 2.7f) },
            new[] { Win(0f, 1.0f, fy, 1.0f, 1.0f) },
            new[] { Win(0f, 1.0f, fy, 1.0f, 1.0f) },
            new[] { Win(0f, 1.0f, fy, 1.0f, 1.0f) });

        // Atap tumpang (dinaikkan 1.25 m)
        Cone(root, 7.35f, 1.4f, matRoofThatch, new Vector3(0, 4.85f, 0), 45f, 4);
        Box(root, 5.0f, 1.0f, 5.0f, matSlat, new Vector3(0, 5.35f, 0), default, false);
        Cone(root, 4.5f, 1.5f, matRoofThatch, new Vector3(0, 6.6f, 0), 45f, 4);
        Cylinder(root, 0.08f, 0.9f, matDark, new Vector3(0, 7.85f, 0), default, false);
        Sphere(root, 0.14f, matDark, new Vector3(0, 8.35f, 0));

        Box(root, 3.0f, 0.25f, 0.6f, matConcrete, new Vector3(0, -0.05f, 5.3f));
        Box(root, 3.0f, 0.15f, 0.6f, matConcrete, new Vector3(0, -0.20f, 5.9f));

        // Kolam teratai
        for (int s = -1; s <= 1; s += 2)
        {
            Cylinder(root, 3.8f, 0.08f, matWater, new Vector3(s * 8.5f, -0.2f, 1.5f), default, false);
            for (int i = 0; i < 5; i++)
            {
                float px = s * 8.5f + Mathf.Sin(i * 1.5f) * 1.8f;
                float pz = 1.5f + Mathf.Cos(i * 1.5f) * 1.8f;
                Cylinder(root, 0.35f, 0.02f, matLeaf, new Vector3(px, -0.16f, pz), default, false);
                if (i % 2 == 0) Sphere(root, 0.12f, matPink, new Vector3(px, -0.1f, pz));
            }
        }

        // Isi gedhong: lemari ukir, peti, tikar, kain batik, pelita
        Lemari(root, new Vector3(0f, fy, -1.55f), 0f);
        Peti(root, new Vector3(1.5f, fy, -0.6f), 90f);
        Tikar(root, new Vector3(-0.2f, fy, 0.3f), 0f, 1.6f, 1.2f);
        KainBatik(root, new Vector3(-1.87f, fy + 1.6f, 0f), 90f, 1.2f, 1.0f);
        Pelita(root, new Vector3(-0.2f, fy, 0.3f));
        Dingklik(root, new Vector3(-1.0f, fy, 1.0f));

        // Isi pendopo: gamelan, bale-bale, tikar besar, lesung
        GamelanSet(root, new Vector3(-2.9f, fy, 2.6f), 0f);
        Bale(root, new Vector3(2.9f, fy, 2.7f), 0f, 2.4f, 1.6f);
        Tikar(root, new Vector3(0f, fy, 3.0f), 0f, 3.0f, 1.4f);
        Lesung(root, new Vector3(3.3f, fy, -2.8f));
        Dingklik(root, new Vector3(-3.0f, fy, -2.7f));
        Dingklik(root, new Vector3(-2.3f, fy, -2.9f));
        Gentong(root, new Vector3(-3.4f, fy, -1.2f));
    }

    // ═════════════════════════════════════════════════════════════════════
    //  6. PONDOK DANAU
    // ═════════════════════════════════════════════════════════════════════
    private static void BuildDanau(GameObject root)
    {
        const float fy = 0.15f;
        Box(root, 6.4f, 0.4f, 4.8f, matDeck, new Vector3(0, -0.15f, 0));
        Box(root, 6.0f, 0.2f, 4.0f, matDeck, new Vector3(0, 0.05f, 0));

        // Dinding dinaikkan ke 3.3 m (sebelumnya 2.8 m) agar pintu 2.7 m lega
        Room(root, 0f, 0f, 3.0f, 2.0f, fy, 3.3f, 0.2f, matSlat,
            new[] { Door(0.6f, 1.7f, fy, 2.7f), Win(-1.9f, 1.2f, fy, 0.9f, 1.1f) },
            new[] { Win(-1.0f, 1.2f, fy, 0.9f, 1.1f), Win(1.5f, 1.2f, fy, 0.9f, 1.1f) },
            new[] { Win(0f, 1.2f, fy, 0.9f, 1.1f) },
            new[] { Win(0f, 1.2f, fy, 0.9f, 1.1f) }, matWood);

        // Atap pelana pendek
        float rise = 1.6f, half = 2.6f, baseY = 3.3f;
        float pitch = Mathf.Atan(rise / half) * Mathf.Rad2Deg;
        float slope = Mathf.Sqrt(half * half + rise * rise);
        float midY = baseY + rise * 0.5f + 0.09f;
        Box(root, 6.6f, 0.15f, slope, matRoof, new Vector3(0, midY, half * 0.5f), new Vector3(pitch, 0, 0));
        Box(root, 6.6f, 0.15f, slope, matRoof, new Vector3(0, midY, -half * 0.5f), new Vector3(-pitch, 0, 0));
        Box(root, 6.7f, 0.14f, 0.3f, matDark, new Vector3(0, baseY + rise + 0.02f, 0), default, false);
        GableTriangle(root, half * 2f, rise, 0.15f, matSlat, new Vector3(-3.05f, baseY, 0), -90f);
        GableTriangle(root, half * 2f, rise, 0.15f, matSlat, new Vector3(3.05f, baseY, 0), 90f);

        Box(root, 0.7f, 5.6f, 0.7f, matConcrete, new Vector3(-2.2f, 2.8f, -2.35f));   // cerobong (di luar)

        // Teras & dermaga kayu kompak tepi sungai
        Box(root, 6.4f, 0.2f, 2.4f, matDeck, new Vector3(0, 0.05f, 3.2f));
        Box(root, 6.8f, 0.1f, 2.6f, matRoof, new Vector3(0, 3.5f, 3.3f), new Vector3(6f, 0, 0));
        Cylinder(root, 0.1f, 3.4f, matWood, new Vector3(-3.0f, 1.85f, 4.2f));
        Cylinder(root, 0.1f, 3.4f, matWood, new Vector3(3.0f, 1.85f, 4.2f));
        Box(root, 1.8f, 0.14f, 4.2f, matDeck, new Vector3(0, -0.05f, 4.8f));
        for (int i = 0; i < 2; i++)
        {
            float z = 3.6f + i * 2.0f;
            Cylinder(root, 0.1f, 1.4f, matWood, new Vector3(-0.85f, -0.5f, z));
            Cylinder(root, 0.1f, 1.4f, matWood, new Vector3(0.85f, -0.5f, z));
        }
        var boat = Sphere(root, 1.0f, matWood, new Vector3(2.2f, 0.15f, 4.5f), new Vector3(0.6f, 0.35f, 1.8f));
        boat.name = "SampanKayu";
        Lantern(root, new Vector3(1.2f, 3.0f, 2.9f));

        FurnishInterior(root, 0f, 0f, 2.9f, 1.9f, fy, 0.6f, FAll, 3.25f);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  7. BARN (lumbung/kandang): pintu geser terbuka, kandang kuda, gerobak
    // ═════════════════════════════════════════════════════════════════════
    private static void BuildBarn(GameObject root)
    {
        const float fy = 0.1f;
        Box(root, 7.2f, 0.1f, 9.2f, matDeck, new Vector3(0, 0.05f, 0));

        Room(root, 0f, 0f, 3.5f, 4.5f, fy, 3.2f, 0.2f, matBlack,
            new[] { Door(0f, 3.2f, fy, 2.9f) },
            null,
            new[] { Win(-2.0f, 1.2f, fy, 1.1f, 0.9f), Win(2.0f, 1.2f, fy, 1.1f, 0.9f) },
            new[] { Win(-2.0f, 1.2f, fy, 1.1f, 0.9f), Win(2.0f, 1.2f, fy, 1.1f, 0.9f) });

        // Dua daun pintu geser dibuka ke samping (tanpa collider)
        foreach (float s in new[] { -1f, 1f })
        {
            Box(root, 1.6f, 2.8f, 0.1f, matSlat, new Vector3(s * 2.4f, 1.5f, 4.65f), default, false);
            Box(root, 0.1f, 3.2f, 0.06f, matDark, new Vector3(s * 2.4f, 1.5f, 4.72f), new Vector3(0, 0, 42f), false);
            Box(root, 0.1f, 3.2f, 0.06f, matDark, new Vector3(s * 2.4f, 1.5f, 4.72f), new Vector3(0, 0, -42f), false);
        }
        Window(root, 1.6f, 1.0f, new Vector3(0, 4.3f, 4.56f), 0);

        // Atap gambrel
        Box(root, 2.35f, 0.15f, 9.6f, matDark, new Vector3(2.85f, 4.15f, 0), new Vector3(0, 0, -54f));
        Box(root, 2.35f, 0.15f, 9.6f, matDark, new Vector3(-2.85f, 4.15f, 0), new Vector3(0, 0, 54f));
        Box(root, 2.6f, 0.15f, 9.6f, matDark, new Vector3(1.1f, 5.65f, 0), new Vector3(0, 0, -28f));
        Box(root, 2.6f, 0.15f, 9.6f, matDark, new Vector3(-1.1f, 5.65f, 0), new Vector3(0, 0, 28f));
        GableTriangle(root, 7.0f, 3.0f, 0.15f, matSlat, new Vector3(0, 3.2f, 4.5f), 0);
        GableTriangle(root, 7.0f, 3.0f, 0.15f, matSlat, new Vector3(0, 3.2f, -4.5f), 180f);

        // Silo & jerami bulat di luar
        Cylinder(root, 1.2f, 7.0f, matMetal, new Vector3(-6.0f, 3.15f, -2.0f));
        Sphere(root, 1.2f, matMetal, new Vector3(-6.0f, 6.65f, -2.0f), new Vector3(1f, 0.6f, 1f));
        for (int i = 0; i < 5; i++)
            Cylinder(root, 0.5f, 0.9f, matHay, new Vector3(5.5f, 0.45f + (i / 3) * 0.9f, 1f + i * 0.5f), new Vector3(0, 0, 90f));

        // Interior: 3 kandang kuda di sisi kiri (sekat + palung)
        foreach (float dz in new[] { -1.95f, 0.55f, 3.05f })
            Box(root, 1.6f, 1.3f, 0.08f, matWood, new Vector3(-2.7f, 0.75f, dz));
        foreach (float tz in new[] { -3.2f, -0.7f, 1.8f })
        {
            Box(root, 0.5f, 0.4f, 1.4f, matWood, new Vector3(-3.2f, 0.3f, tz));
            Box(root, 0.4f, 0.1f, 1.2f, matHay, new Vector3(-3.2f, 0.52f, tz), default, false);
        }

        // Interior: gerobak, tumpukan jerami, meja kerja, rak perkakas
        Box(root, 1.6f, 0.2f, 2.4f, matWood, new Vector3(2.4f, 0.7f, -2.0f));
        foreach (float wx in new[] { -1f, 1f })
            Cylinder(root, 0.45f, 0.1f, matWood, new Vector3(2.4f + wx * 0.95f, 0.55f, -2.0f), new Vector3(0, 0, 90f));
        for (int i = 0; i < 6; i++)
            Box(root, 1.0f, 0.8f, 0.8f, matHay, new Vector3(2.9f - (i % 2) * 0.1f, 0.5f + (i / 3) * 0.8f, 1.2f + (i % 3) * 0.85f));
        Box(root, 1.8f, 0.08f, 0.7f, matWood, new Vector3(0, 0.9f, -3.9f));
        foreach (float lx in new[] { -0.8f, 0.8f })
            Box(root, 0.08f, 0.85f, 0.08f, matWood, new Vector3(lx, 0.47f, -3.9f), default, false);
        Box(root, 2.0f, 0.08f, 0.1f, matWood, new Vector3(0, 1.8f, -4.3f), default, false);
        Gentong(root, new Vector3(0.9f, fy, -3.4f));
        Peti(root, new Vector3(-0.9f, fy, -3.6f), 0f);

        Box(root, 0.03f, 0.6f, 0.03f, matDark, new Vector3(0, 3.0f, 0), default, false);
        Lantern(root, new Vector3(0, 2.7f, 0));
        Lantern(root, new Vector3(-1.9f, 2.8f, 4.75f));
        Lantern(root, new Vector3(1.9f, 2.8f, 4.75f));
    }

    // ═════════════════════════════════════════════════════════════════════
    //  8. RUMAH POHON (tangga balok panjang menuju dek, kabin berongga)
    // ═════════════════════════════════════════════════════════════════════
    private static void BuildPohon(GameObject root)
    {
        const float fy = 5.15f;
        const float cx = 3.1f;     // pusat kabin (digeser menjauh dari batang pohon, r = 1.1 m)

        Cylinder(root, 1.1f, 10.0f, matWood, new Vector3(0, 4.65f, 0));
        Cylinder(root, 0.25f, 3.5f, matWood, new Vector3(-1.4f, 7.5f, -0.5f), new Vector3(0, 0, 45f), false);
        Cylinder(root, 0.25f, 3.5f, matWood, new Vector3(1.4f, 7.3f, 0.8f), new Vector3(0, 0, -45f), false);

        Sphere(root, 3.4f, matLeaf, new Vector3(0, 11.2f, 0));
        Sphere(root, 2.6f, matLeaf, new Vector3(-2.8f, 10.6f, 0.5f));
        Sphere(root, 2.4f, matLeaf, new Vector3(2.8f, 11.2f, -2.0f));

        // Dek diperlebar (x -2.3 .. 5.3, z -3 .. 3)
        Box(root, 7.6f, 0.3f, 6.0f, matDeck, new Vector3(1.5f, 5.0f, 0));

        // Kabin 3.6 x 3.6 m, tinggi dinding 2.85 m, pintu 1.7 x 2.7 m
        Room(root, cx, 0f, 1.8f, 1.8f, fy, 8.0f, 0.15f, matSlat,
            new[] { Door(cx, 1.7f, fy, 2.7f) },
            new[] { Win(cx, 0.9f, fy, 0.9f, 0.9f) },
            new[] { Win(0f, 0.9f, fy, 0.9f, 0.9f) },
            new[] { Win(0f, 0.9f, fy, 0.9f, 0.9f) }, matWood);
        Cone(root, 3.0f, 1.6f, matRoofThatch, new Vector3(cx, 8.9f, 0), 45f, 4);

        // Pagar dek. Pagar depan DIBERI CELAH (x 2.2 .. 4.0) tepat di ujung tangga —
        // sebelumnya pagar menutup ujung tangga sehingga rumah pohon tidak bisa dimasuki.
        Box(root, 4.5f, 0.08f, 0.08f, matDark, new Vector3(-0.05f, 6.05f, 3.0f), default, false);
        Box(root, 1.3f, 0.08f, 0.08f, matDark, new Vector3(4.65f, 6.05f, 3.0f), default, false);
        Box(root, 7.6f, 0.08f, 0.08f, matDark, new Vector3(1.5f, 6.05f, -3.0f), default, false);
        for (float x = -1.8f; x <= 4.41f; x += 2.1f)
            Lantern(root, new Vector3(x, 6.2f, 3.0f));

        // Tangga balok 25 anak tangga (lebar 1.6 m): dari tanah (z=10.4) naik ke tepi dek (z=3)
        StairsTo(root, new Vector3(cx, 0f, 3.0f), 180f, fy, HouseDimensions.StairWidth, matDeck);

        // Jembatan & gardu kedua
        Box(root, 6.0f, 0.1f, 1.2f, matDeck, new Vector3(8.3f, 5.0f, 0));
        Box(root, 3.2f, 0.25f, 3.2f, matDeck, new Vector3(11.3f, 5.0f, 0));
        Cylinder(root, 0.5f, 9.0f, matWood, new Vector3(11.3f, 4.2f, 0));
        Sphere(root, 2.2f, matLeaf, new Vector3(11.3f, 9.5f, 0));
        Lantern(root, new Vector3(12.6f, 5.4f, 1.4f));

        // Isi kabin
        FurnishInterior(root, cx, 0f, 1.7f, 1.7f, fy, cx, FBed | FChest | FMat, 7.9f);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  9. RUMAH GADANG (Minangkabau)
    // ═════════════════════════════════════════════════════════════════════
    private static void BuildGadang(GameObject root)
    {
        const float fy = 1.925f;

        float[] xs = { -4f, -2f, 0f, 2f, 4f };
        float[] zs = { -1.6f, 1.6f };
        foreach (var x in xs)
            foreach (var z in zs)
                Cylinder(root, 0.18f, 2.2f, matWood, new Vector3(x, 0.75f, z));

        Box(root, 10.6f, 0.25f, 5.4f, matDeck, new Vector3(0, 1.8f, 0));

        // Dinding kayu merah berongga: pintu tengah + 4 jendela di antara tiang ukiran emas
        Room(root, 0f, 0f, 5.0f, 2.0f, fy, 4.95f, 0.15f, matRedWood,
            new[] { Door(0f, 1.7f, fy, 2.7f), Win(-1.68f, 1.0f, fy, 0.9f, 1.3f), Win(1.68f, 1.0f, fy, 0.9f, 1.3f),
                    Win(-3.36f, 1.0f, fy, 0.9f, 1.3f), Win(3.36f, 1.0f, fy, 0.9f, 1.3f) },
            new[] { Win(-3.0f, 1.0f, fy, 0.9f, 1.3f), Win(0f, 1.0f, fy, 0.9f, 1.3f), Win(3.0f, 1.0f, fy, 0.9f, 1.3f) },
            new[] { Win(0f, 1.0f, fy, 0.9f, 1.3f) },
            new[] { Win(0f, 1.0f, fy, 0.9f, 1.3f) }, matWood);

        // Ukiran garis emas di fasad (di antara pintu/jendela)
        for (int i = 0; i < 6; i++)
            Box(root, 0.12f, 2.2f, 0.06f, matGold, new Vector3(-4.2f + i * 1.68f, 3.15f, 2.1f), default, false);
        Box(root, 10.0f, 0.12f, 0.06f, matGold, new Vector3(0, 4.55f, 2.1f), default, false);

        // Atap gonjong tanduk kerbau
        float ey = 4.2f;
        for (int i = 0; i < 14; i++)
        {
            float x = -6.2f + i * 0.95f;
            float h = 6.6f + 1.9f * Mathf.Pow(Mathf.Abs(x) / 6.4f, 2.3f);
            float hNext = 6.6f + 1.9f * Mathf.Pow(Mathf.Abs(x + 0.4f) / 6.4f, 2.3f);
            float hPrev = 6.6f + 1.9f * Mathf.Pow(Mathf.Abs(x - 0.4f) / 6.4f, 2.3f);
            float dh = (hNext - hPrev) / 0.8f;

            float pitch = Mathf.Atan((h - ey) / 3f) * Mathf.Rad2Deg;
            float roll = -Mathf.Atan(dh) * Mathf.Rad2Deg;
            float length = Mathf.Sqrt(9f + (h - ey) * (h - ey));

            Box(root, 1.02f, 0.14f, length, matRoofThatch, new Vector3(x, (h + ey) * 0.5f, 1.5f), new Vector3(pitch, 0, roll));
            Box(root, 1.02f, 0.14f, length, matRoofThatch, new Vector3(x, (h + ey) * 0.5f, -1.5f), new Vector3(-pitch, 0, roll));
        }
        Cylinder(root, 0.12f, 1.7f, matGold, new Vector3(-6.7f, 8.8f, 0), new Vector3(0, 0, 28f), false);
        Cylinder(root, 0.12f, 1.7f, matGold, new Vector3(6.7f, 8.8f, 0), new Vector3(0, 0, -28f), false);

        // Tangga masuk padat: 10 anak tangga x 0.1925 m = 1.925 m (dulu 0.4 m/anak + celah melayang = tidak bisa dinaiki).
        // Dari z = 5.7 naik ke tepi dek (z = 2.7).
        StairsTo(root, new Vector3(0f, 0f, 2.7f), 180f, fy, 2.0f, matDeck);

        Lantern(root, new Vector3(-1.4f, 3.4f, 2.9f));
        Lantern(root, new Vector3(1.4f, 3.4f, 2.9f));

        // Rangkiang (lumbung padi) di samping
        var rangkiang = new GameObject("Rangkiang_Padi");
        rangkiang.transform.SetParent(root.transform, false);
        rangkiang.transform.localPosition = new Vector3(10.0f, 0, 0);
        for (int a = -1; a <= 1; a += 2)
            for (int b = -1; b <= 1; b += 2)
                Cylinder(rangkiang, 0.12f, 1.3f, matWood, new Vector3(a * 0.9f, 0.3f, b * 0.9f));
        Box(rangkiang, 2.4f, 1.4f, 2.4f, matRedWood, new Vector3(0, 1.7f, 0));
        Cone(rangkiang, 2.3f, 1.3f, matRoofThatch, new Vector3(0, 3.05f, 0), 45f, 4);

        // Isi rumah gadang
        FurnishInterior(root, 0f, 0f, 5.0f, 2.0f, fy, 0f, FAll, 4.9f);
        KainBatik(root, new Vector3(0f, fy + 1.9f, -1.9f), 0f, 2.4f, 1.1f);
        Tikar(root, new Vector3(1.4f, fy, 1.0f), 0f, 2.6f, 1.0f);
        Lemari(root, new Vector3(-1.2f, fy, -1.62f), 0f);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  10. MINKA TRADISIONAL JEPANG (tatami, irori, tokonoma, futon)
    // ═════════════════════════════════════════════════════════════════════
    private static void BuildMinka(GameObject root)
    {
        const float fy = 0.22f;
        Box(root, 10.4f, 0.4f, 7.4f, matConcrete, new Vector3(0, -0.15f, 0));
        Box(root, 8.2f, 0.12f, 5.2f, matDeck, new Vector3(0, 0.16f, 0));

        // Engawa
        Box(root, 9.0f, 0.18f, 1.8f, matDeck, new Vector3(0, 0.13f, 3.6f));
        Box(root, 0.18f, 0.18f, 6.4f, matDeck, new Vector3(-4.9f, 0.13f, 0));
        Box(root, 0.18f, 0.18f, 6.4f, matDeck, new Vector3(4.9f, 0.13f, 0));

        // Dinding plester (tinggi 3.5 m) dengan pintu geser terbuka & jendela shoji
        Room(root, 0f, 0f, 4.2f, 2.7f, fy, 3.5f, 0.2f, matWall,
            new[] { Win(-2.9f, 1.8f, fy, 0.15f, 2.0f, matShoji), Door(0f, 1.8f, fy, 2.7f), Win(2.9f, 1.8f, fy, 0.15f, 2.0f, matShoji) },
            new[] { Win(-2.5f, 1.8f, fy, 0.9f, 1.2f, matShoji), Win(2.5f, 1.8f, fy, 0.9f, 1.2f, matShoji) },
            new[] { Win(0f, 2.0f, fy, 0.9f, 1.2f, matShoji) },
            new[] { Win(0f, 2.0f, fy, 0.9f, 1.2f, matShoji) });

        // Rangka kayu ekspos (tidak di jalur pintu)
        for (float x = -4.2f; x <= 4.21f; x += 1.4f)
        {
            if (Mathf.Abs(x) > 0.1f) Box(root, 0.14f, 3.28f, 0.14f, matWood, new Vector3(x, 1.86f, 2.75f), default, false);
            Box(root, 0.14f, 3.28f, 0.14f, matWood, new Vector3(x, 1.86f, -2.75f), default, false);
        }
        for (float z = -2.7f; z <= 2.71f; z += 1.35f)
        {
            Box(root, 0.14f, 3.28f, 0.14f, matWood, new Vector3(4.25f, 1.86f, z), default, false);
            Box(root, 0.14f, 3.28f, 0.14f, matWood, new Vector3(-4.25f, 1.86f, z), default, false);
        }

        // Pintu shoji yang digeser terbuka (menumpuk di sisi pintu)
        Box(root, 0.9f, 2.1f, 0.03f, matShoji, new Vector3(-1.35f, 1.3f, 2.83f), default, false);
        Box(root, 0.9f, 2.1f, 0.03f, matShoji, new Vector3(1.35f, 1.3f, 2.83f), default, false);

        // Atap irimoya; langit-langit di y=3.5 menutup ruangan
        HipRoof(root, 6.3f, 4.9f, 2.4f, 3.5f, 5.9f, matRoofJapanese);
        Box(root, 4.8f, 0.22f, 0.3f, matDark, new Vector3(0, 5.95f, 0), default, false);

        // Taman zen, toro lentera batu, kolam koi, sakura
        Box(root, 13.0f, 0.05f, 9.0f, matConcrete, new Vector3(0, -0.2f, 8.5f), default, false);
        Sphere(root, 0.5f, matConcrete, new Vector3(-3f, -0.1f, 8f), new Vector3(1f, 0.6f, 1f));
        Sphere(root, 0.5f, matConcrete, new Vector3(2f, -0.1f, 11f), new Vector3(1f, 0.6f, 1f));
        Cylinder(root, 0.25f, 0.5f, matConcrete, new Vector3(-4.0f, 0.1f, 6.5f));
        Cylinder(root, 0.12f, 0.9f, matConcrete, new Vector3(-4.0f, 0.6f, 6.5f));
        Lantern(root, new Vector3(-4.0f, 1.2f, 6.5f));
        Cone(root, 0.55f, 0.4f, matConcrete, new Vector3(-4.0f, 1.65f, 6.5f), 45f, 4);
        Cylinder(root, 2.6f, 0.05f, matWater, new Vector3(6.5f, -0.2f, 7.0f), default, false);
        for (int i = 0; i < 3; i++)
            Sphere(root, 0.15f, matOrange, new Vector3(6.5f + Mathf.Sin(i * 2f) * 1.2f, -0.18f, 7.0f + Mathf.Cos(i * 2f) * 1.2f), new Vector3(0.5f, 0.3f, 1.6f));
        Cylinder(root, 0.2f, 3.5f, matWood, new Vector3(-6.5f, 1.4f, 1.0f));
        Sphere(root, 1.4f, matPink, new Vector3(-6.5f, 3.6f, 1.0f));
        Sphere(root, 1.2f, matPink, new Vector3(-5.8f, 4.0f, 1.5f));

        // ── Interior ──
        // Tatami: 4 kolom x 2 baris besar + 1 baris sempit di depan
        for (int c = 0; c < 4; c++)
        {
            float tx = -2.7f + c * 1.8f;
            Tikar(root, new Vector3(tx, fy, -1.5f), 0f, 1.76f, 1.76f);
            Tikar(root, new Vector3(tx, fy, 0.3f), 0f, 1.76f, 1.76f);
            Tikar(root, new Vector3(tx, fy, 1.95f), 0f, 1.76f, 0.9f);
        }

        // Irori (tungku lantai) + ketel gantung (tanpa collider agar tidak menghalangi jalan)
        Box(root, 1.3f, 0.1f, 1.3f, matDark, new Vector3(0f, fy + 0.05f, -0.6f), default, false);
        Box(root, 1.0f, 0.02f, 1.0f, matConcrete, new Vector3(0f, fy + 0.11f, -0.6f), default, false);
        Box(root, 0.3f, 0.02f, 0.3f, matOrange, new Vector3(0f, fy + 0.125f, -0.6f), default, false);
        Box(root, 0.02f, 2.6f, 0.02f, matDark, new Vector3(0f, fy + 1.9f, -0.6f), default, false);
        Cylinder(root, 0.2f, 0.3f, matDark, new Vector3(0f, fy + 0.8f, -0.6f), default, false);

        // Chabudai (meja rendah) + zabuton (bantal duduk)
        var cg = Group(root, "Chabudai", new Vector3(-2.7f, fy, 1.0f), 0f);
        Box(cg, 1.1f, 0.06f, 0.8f, matWood, new Vector3(0, 0.34f, 0), default, false);
        foreach (float lx in new[] { -0.45f, 0.45f })
            foreach (float lz in new[] { -0.3f, 0.3f })
                Box(cg, 0.06f, 0.32f, 0.06f, matWood, new Vector3(lx, 0.16f, lz), default, false);
        Box(cg, 0.5f, 0.08f, 0.5f, matRed, new Vector3(-0.9f, 0.04f, 0), default, false);
        Box(cg, 0.5f, 0.08f, 0.5f, matRed, new Vector3(0.9f, 0.04f, 0), default, false);
        Box(cg, 0.5f, 0.08f, 0.5f, matRed, new Vector3(0, 0.04f, 0.65f), default, false);
        Pelita(root, new Vector3(-2.7f, fy + 0.37f, 1.0f));

        // Tokonoma: ceruk dengan gulungan lukisan & vas
        Box(root, 1.8f, 0.12f, 0.7f, matWood, new Vector3(2.5f, fy + 0.06f, -2.15f));
        KainBatik(root, new Vector3(2.5f, fy + 1.5f, -2.45f), 0f, 0.7f, 1.5f);
        Kendi(root, new Vector3(3.0f, fy + 0.12f, -2.15f), 0.8f);

        // Tansu (lemari) & peti
        Lemari(root, new Vector3(-2.3f, fy, -2.3f), 0f);
        Peti(root, new Vector3(-3.6f, fy, -2.3f), 0f);

        // Futon di tatami depan-kanan
        Box(root, 1.0f, 0.1f, 1.9f, matBatik, new Vector3(3.1f, fy + 0.08f, 0.9f), default, false);
        Box(root, 0.6f, 0.12f, 0.35f, matWall, new Vector3(3.1f, fy + 0.17f, 1.55f), default, false);

        Box(root, 0.02f, 0.5f, 0.02f, matDark, new Vector3(0f, 3.2f, 0.8f), default, false);
        Lantern(root, new Vector3(0f, 2.85f, 0.8f));
    }

    // ═════════════════════════════════════════════════════════════════════
    //  11. CHALET SALJU PEGUNUNGAN
    // ═════════════════════════════════════════════════════════════════════
    private static void BuildChalet(GameObject root)
    {
        const float fy = 0.30f;
        Box(root, 8.5f, 0.30f, 6.5f, matConcrete, new Vector3(0, 0.15f, 0));
        Box(root, 7.6f, 0.06f, 5.6f, matDeck, new Vector3(0, 0.33f, 0));

        Room(root, 0f, 0f, 4.0f, 3.0f, fy, 3.6f, 0.25f, matSlat,
            new[] { Door(0f, 1.7f, fy, 2.7f), Win(-2.3f, 1.3f, fy, 0.9f, 1.2f), Win(2.3f, 1.3f, fy, 0.9f, 1.2f) },
            new[] { Win(-1.5f, 1.3f, fy, 0.9f, 1.2f) },
            new[] { Win(-1.4f, 1.2f, fy, 0.9f, 1.2f), Win(1.4f, 1.2f, fy, 0.9f, 1.2f) },
            new[] { Win(-1.4f, 1.2f, fy, 0.9f, 1.2f), Win(1.4f, 1.2f, fy, 0.9f, 1.2f) }, matWood);

        // Bingkai pintu (1.7 x 2.7 m) + daun pintu terbuka menempel di dinding dalam
        Box(root, 0.10f, 2.8f, 0.34f, matWood, new Vector3(-0.90f, 1.70f, 3f), default, false);
        Box(root, 0.10f, 2.8f, 0.34f, matWood, new Vector3(0.90f, 1.70f, 3f), default, false);
        Box(root, 1.90f, 0.12f, 0.34f, matDark, new Vector3(0, 3.06f, 3f), default, false);
        Box(root, 1.0f, 2.65f, 0.07f, matWood, new Vector3(-0.88f, 1.62f, 2.4f), new Vector3(0, 90f, 0), false);

        // Teras + tangga
        Box(root, 8.0f, 0.14f, 1.9f, matDeck, new Vector3(0, 0.23f, 3.95f));
        Box(root, 1.8f, 0.15f, 0.5f, matDeck, new Vector3(0, 0.075f, 5.15f));
        foreach (float px in new[] { -4.0f, -2.5f, -0.9f, 0.9f, 2.5f, 4.0f })
            Box(root, 0.1f, 0.95f, 0.1f, matWood, new Vector3(px, 0.78f, 4.75f), default, false);
        Box(root, 3.1f, 0.08f, 0.12f, matWood, new Vector3(-2.45f, 1.25f, 4.75f), default, false);
        Box(root, 3.1f, 0.08f, 0.12f, matWood, new Vector3(2.45f, 1.25f, 4.75f), default, false);
        Box(root, 3.1f, 0.05f, 0.06f, matWood, new Vector3(-2.45f, 0.80f, 4.75f), default, false);
        Box(root, 3.1f, 0.05f, 0.06f, matWood, new Vector3(2.45f, 0.80f, 4.75f), default, false);
        Cylinder(root, 0.11f, 2.3f, matWood, new Vector3(-3.9f, 1.45f, 4.55f));
        Cylinder(root, 0.11f, 2.3f, matWood, new Vector3(3.9f, 1.45f, 4.55f));

        // Atap curam + mantel salju
        float rise = 3.0f, half = 4.6f, baseY = 2.6f;
        float pitch = Mathf.Atan(rise / half) * Mathf.Rad2Deg;
        float slope = Mathf.Sqrt(half * half + rise * rise);
        float midY = baseY + rise * 0.5f + 0.09f;
        Box(root, 9.2f, 0.18f, slope, matRoof, new Vector3(0, midY, half * 0.5f), new Vector3(pitch, 0, 0));
        Box(root, 9.2f, 0.18f, slope, matRoof, new Vector3(0, midY, -half * 0.5f), new Vector3(-pitch, 0, 0));
        float cos = Mathf.Cos(pitch * Mathf.Deg2Rad), sin = Mathf.Sin(pitch * Mathf.Deg2Rad);
        float off = 0.15f;
        Box(root, 9.3f, 0.14f, slope - 0.3f, matSnow,
            new Vector3(0, midY + off * cos, half * 0.5f + off * sin), new Vector3(pitch, 0, 0), false);
        Box(root, 9.3f, 0.14f, slope - 0.3f, matSnow,
            new Vector3(0, midY + off * cos, -half * 0.5f - off * sin), new Vector3(-pitch, 0, 0), false);
        Box(root, 9.3f, 0.16f, 0.30f, matDark, new Vector3(0, baseY + rise + 0.02f, 0), default, false);
        Box(root, 9.4f, 0.20f, 0.55f, matSnow, new Vector3(0, baseY + rise + 0.18f, 0), default, false);
        GableTriangle(root, half * 2f, rise, 0.15f, matSlat, new Vector3(-4.0f, baseY, 0), -90f);
        GableTriangle(root, half * 2f, rise, 0.15f, matSlat, new Vector3(4.0f, baseY, 0), 90f);

        foreach (float bx in new[] { -2.8f, 0f, 2.8f })
            Box(root, 0.22f, 0.22f, 6.0f, matWood, new Vector3(bx, 3.45f, 0), default, false);

        // Perapian batu + cerobong (sudut belakang kanan)
        Box(root, 1.0f, 6.2f, 1.0f, matConcrete, new Vector3(2.2f, 3.4f, -2.3f));
        Box(root, 1.8f, 0.12f, 1.3f, matConcrete, new Vector3(2.2f, 0.36f, -1.45f), default, false);
        Box(root, 0.55f, 0.55f, 0.06f, matDark, new Vector3(2.2f, 0.75f, -1.78f), default, false);
        Box(root, 0.3f, 0.12f, 0.03f, matLamp, new Vector3(2.2f, 0.52f, -1.79f), default, false);
        Box(root, 1.2f, 0.12f, 1.2f, matDark, new Vector3(2.2f, 6.55f, -2.3f), default, false);
        Box(root, 1.1f, 0.08f, 1.1f, matSnow, new Vector3(2.2f, 6.65f, -2.3f), default, false);

        Lantern(root, new Vector3(-3.9f, 2.2f, 4.3f));
        Lantern(root, new Vector3(3.9f, 2.2f, 4.3f));
        Sphere(root, 1.0f, matSnow, new Vector3(-5.0f, 0.05f, 2.5f), new Vector3(1.6f, 0.25f, 1.2f));
        Sphere(root, 1.0f, matSnow, new Vector3(5.2f, 0.05f, -1.5f), new Vector3(1.4f, 0.22f, 1.5f));

        // Furnitur tradisional (perapian berfungsi sebagai dapur)
        FurnishInterior(root, 0f, 0f, 3.9f, 2.9f, fy, 0f, FBed | FWardrobe | FDining | FChest | FMat, 3.4f);
    }

    // ═════════════════════════════════════════════════════════════════════
    //  12. BALAI DESA AGUNG ELDARA (Grand Civic Council Hall)
    //  Gedung balairung adat megah pengganti model FBX administratif lama.
    //  100% solid, anti-glitch, pintu lapang 2.8m, tangga landai, interior lengkap.
    // ═════════════════════════════════════════════════════════════════════
    private static void BuildBalaiDesa(GameObject root)
    {
        const float fy = 0.35f;

        // 1. PONDASI BATU AGUNG & UNDAKAN TANGGA DEPAN
        Box(root, 15.2f, 0.40f, 13.2f, matConcrete, new Vector3(0, 0.15f, 0));
        Box(root, 14.6f, 0.12f, 12.6f, matDeck, new Vector3(0, 0.36f, 0));

        // Tangga batu depan bertingkat 3 (landai, undakan hanya 0.11m -> sangat mulus didaki)
        Box(root, 6.4f, 0.11f, 1.2f, matConcrete, new Vector3(0, 0.055f, 7.2f));
        Box(root, 5.6f, 0.11f, 1.0f, matConcrete, new Vector3(0, 0.165f, 6.6f));
        Box(root, 4.8f, 0.11f, 0.8f, matConcrete, new Vector3(0, 0.275f, 6.1f));

        // Pipi tangga batu di sisi kiri dan kanan
        Box(root, 0.45f, 0.55f, 1.8f, matDark, new Vector3(-3.4f, 0.25f, 6.7f));
        Box(root, 0.45f, 0.55f, 1.8f, matDark, new Vector3( 3.4f, 0.25f, 6.7f));
        Cone(root, 0.35f, 0.4f, matGold, new Vector3(-3.4f, 0.65f, 7.3f), default, 4);
        Cone(root, 0.35f, 0.4f, matGold, new Vector3( 3.4f, 0.65f, 7.3f), default, 4);

        // 2. SERAMBI KEHORMATAN (FRONT VERANDA / PORTICO)
        float[] pillarXs = new float[] { -6.2f, -3.8f, -1.6f, 1.6f, 3.8f, 6.2f };
        foreach (float px in pillarXs)
        {
            Box(root, 0.55f, 0.25f, 0.55f, matConcrete, new Vector3(px, 0.45f, 5.6f));
            Cylinder(root, 0.18f, 3.6f, matLogs[1], new Vector3(px, 2.3f, 5.6f));
            Cylinder(root, 0.21f, 0.12f, matGold, new Vector3(px, 3.9f, 5.6f), default, false);
        }

        Lantern(root, new Vector3(-3.8f, 3.2f, 5.85f));
        Lantern(root, new Vector3(-1.6f, 3.2f, 5.85f));
        Lantern(root, new Vector3( 1.6f, 3.2f, 5.85f));
        Lantern(root, new Vector3( 3.8f, 3.2f, 5.85f));

        Box(root, 14.8f, 0.30f, 0.40f, matDark, new Vector3(0, 4.15f, 5.6f));
        Box(root, 15.2f, 0.15f, 2.8f, matRoof, new Vector3(0, 4.35f, 5.0f), new Vector3(10f, 0, 0));

        // 3. DINDING BALAIRUNG & PINTU MASUK AGUNG
        float zFront = 3.6f;
        Box(root, 0.18f, 3.3f, 0.35f, matDark, new Vector3(-1.45f, 1.95f, zFront));
        Box(root, 0.18f, 3.3f, 0.35f, matDark, new Vector3( 1.45f, 1.95f, zFront));
        Box(root, 3.20f, 0.22f, 0.35f, matDark, new Vector3(0f, 3.55f, zFront));
        Box(root, 3.10f, 0.08f, 0.38f, matGold, new Vector3(0f, 3.65f, zFront), default, false);

        Box(root, 1.25f, 3.1f, 0.08f, matDark, new Vector3(-1.35f, 1.85f, zFront - 0.7f), new Vector3(0, 75f, 0), false);
        Box(root, 1.25f, 3.1f, 0.08f, matDark, new Vector3( 1.35f, 1.85f, zFront - 0.7f), new Vector3(0, -75f, 0), false);

        var frontOpenings = new[] { Door(0f, 2.8f, fy, 3.3f), Win(-4.0f, 1.8f, fy, 1.2f, 1.4f), Win(4.0f, 1.8f, fy, 1.2f, 1.4f) };
        var backOpenings  = new[] { Win(-4.0f, 1.8f, fy, 1.2f, 1.4f), Win(4.0f, 1.8f, fy, 1.2f, 1.4f) };
        var sideOpeningsL = new[] { Win(-2.5f, 1.8f, fy, 1.2f, 1.4f), Win(2.0f, 1.8f, fy, 1.2f, 1.4f) };
        var sideOpeningsR = new[] { Win(-2.5f, 1.8f, fy, 1.2f, 1.4f), Win(2.0f, 1.8f, fy, 1.2f, 1.4f) };

        LogWall(root, true,  zFront, -6.5f, 6.5f, fy, 12, 0.16f, frontOpenings);
        LogWall(root, true, -5.6f,   -6.5f, 6.5f, fy, 12, 0.16f, backOpenings);
        LogWall(root, false, -6.5f,  -5.6f, zFront, fy, 12, 0.16f, sideOpeningsL);
        LogWall(root, false,  6.5f,  -5.6f, zFront, fy, 12, 0.16f, sideOpeningsR);

        // 4. INTERIOR BALAIRUNG AGUNG (GREAT COUNCIL HALL)
        Box(root, 2.6f, 0.02f, 8.8f, matRed, new Vector3(0, 0.44f, -1.0f), default, false);
        Box(root, 0.1f, 0.03f, 8.8f, matGold, new Vector3(-1.32f, 0.44f, -1.0f), default, false);
        Box(root, 0.1f, 0.03f, 8.8f, matGold, new Vector3( 1.32f, 0.44f, -1.0f), default, false);

        // Meja musyawarah panjang di tengah
        Box(root, 1.6f, 0.82f, 4.8f, matWood, new Vector3(0, 0.85f, -0.6f));
        for (int i = 0; i < 4; i++)
        {
            float cz = -2.4f + i * 1.2f;
            Box(root, 0.55f, 0.50f, 0.55f, matWood, new Vector3(-1.35f, 0.65f, cz));
            Box(root, 0.08f, 0.65f, 0.55f, matDark, new Vector3(-1.60f, 1.15f, cz), default, false);
            Box(root, 0.55f, 0.50f, 0.55f, matWood, new Vector3( 1.35f, 0.65f, cz));
            Box(root, 0.08f, 0.65f, 0.55f, matDark, new Vector3( 1.60f, 1.15f, cz), default, false);
        }

        // Panggung kehormatan & Tahta Kepala Desa
        Box(root, 3.8f, 0.22f, 2.2f, matWood, new Vector3(0, 0.54f, -4.2f));
        Box(root, 1.1f, 0.60f, 0.9f, matDark, new Vector3(0, 0.88f, -4.4f));
        Box(root, 1.1f, 1.10f, 0.12f, matDark, new Vector3(0, 1.65f, -4.8f), default, false);
        Box(root, 1.15f, 0.10f, 0.16f, matGold, new Vector3(0, 2.22f, -4.8f), default, false);
        Box(root, 2.8f, 2.2f, 0.04f, matBatik, new Vector3(0, 2.4f, -5.5f), default, false);
        Box(root, 0.12f, 2.2f, 0.06f, matGold, new Vector3(-1.42f, 2.4f, -5.5f), default, false);
        Box(root, 0.12f, 2.2f, 0.06f, matGold, new Vector3( 1.42f, 2.4f, -5.5f), default, false);

        // Perapian batu besar di dinding barat
        Box(root, 1.4f, 4.6f, 2.6f, matConcrete, new Vector3(-6.6f, 2.4f, -1.0f));
        Box(root, 0.8f, 0.8f, 1.4f, matDark, new Vector3(-6.2f, 0.8f, -1.0f), default, false);
        Box(root, 0.5f, 0.2f, 0.8f, matOrange, new Vector3(-6.1f, 0.55f, -1.0f), default, false);
        Lantern(root, new Vector3(-5.7f, 1.5f, -1.0f));

        // Lentera gantung aula
        Lantern(root, new Vector3(0, 3.8f,  1.5f));
        Lantern(root, new Vector3(0, 3.8f, -1.2f));
        Lantern(root, new Vector3(0, 3.8f, -3.8f));

        // 5. ATAP AGUNG DUA TINGKAT
        float roofBaseY = fy + 3.84f;
        HipRoof(root, 7.8f, 6.2f, 3.8f, roofBaseY, roofBaseY + 2.2f, matRoof);
        Box(root, 15.8f, 0.25f, 0.35f, matDark, new Vector3(0, roofBaseY + 0.1f, -1.0f), default, false);

        float crownBaseY = roofBaseY + 1.8f;
        Box(root, 5.2f, 0.9f, 4.2f, matDark, new Vector3(0, crownBaseY + 0.45f, -1.0f));
        Box(root, 4.4f, 0.45f, 0.08f, matGold, new Vector3(0, crownBaseY + 0.45f, 1.12f), default, false);
        Box(root, 4.4f, 0.45f, 0.08f, matGold, new Vector3(0, crownBaseY + 0.45f, -3.12f), default, false);

        HipRoof(root, 3.4f, 2.8f, 1.6f, crownBaseY + 0.9f, crownBaseY + 2.4f, matRoof);
        Box(root, 2.2f, 0.20f, 0.30f, matDark, new Vector3(0, crownBaseY + 2.42f, -1.0f), default, false);
        Cone(root, 0.35f, 0.70f, matGold, new Vector3(-1.1f, crownBaseY + 2.8f, -1.0f), default, 4);
        Cone(root, 0.35f, 0.70f, matGold, new Vector3( 1.1f, crownBaseY + 2.8f, -1.0f), default, 4);
        Cone(root, 0.40f, 0.90f, matGold, new Vector3( 0f,   crownBaseY + 2.9f, -1.0f), default, 4);
    }
}
#endif
