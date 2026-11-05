#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

public static class AssetMaterialSetup
{
    private const string SlavicArtFolder = "Assets/_Project/Art/SlavicWorld";
    private const string MedievalArtFolder = "Assets/_Project/Art/MedievalVillage";

    [MenuItem("Eldara/1. Setup Asset Materials")]
    public static void SetupAllMaterials()
    {
        SetupSlavicMaterial();
        SetupMedievalMaterials();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Material Setup Selesai!",
            "Material URP untuk SlavicWorld dan MedievalVillage berhasil dibuat dan dihubungkan ke texture!\n\n" +
            "Sekarang kamu bisa jalankan:\nEldara > Setup Prototype Scene\nuntuk melihat desa dengan model 3D nyata!",
            "Mantap!"
        );
    }

    public static Material SetupSlavicMaterial()
    {
        string matFolder = $"{SlavicArtFolder}/Materials";
        if (!AssetDatabase.IsValidFolder(matFolder))
        {
            if (!AssetDatabase.IsValidFolder(SlavicArtFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "SlavicWorld");
            AssetDatabase.CreateFolder(SlavicArtFolder, "Materials");
        }

        string matPath = $"{matFolder}/M_Slavic_Main.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        // Texture
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{SlavicArtFolder}/Textures/EA03_FREE_Slavica.png");
        if (tex)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_Smoothness", 0.15f);
            mat.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(mat);
        }

        return mat;
    }

    public static Material SetupTreeMaterial()
    {
        string matFolder = $"{SlavicArtFolder}/Materials";
        if (!AssetDatabase.IsValidFolder(matFolder))
        {
            if (!AssetDatabase.IsValidFolder(SlavicArtFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "SlavicWorld");
            AssetDatabase.CreateFolder(SlavicArtFolder, "Materials");
        }

        string matPath = $"{matFolder}/M_Slavic_Tree_Lush.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{SlavicArtFolder}/Textures/EA03_FREE_Slavica.png");
        if (tex)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
            // Dedaunan alami bersifat matte (smoothness rendah) agar tidak berkilap seperti plastik
            mat.SetFloat("_Smoothness", 0.02f);
            mat.SetFloat("_Metallic", 0.0f);
            mat.SetColor("_BaseColor", new Color(1.02f, 1.08f, 0.95f, 1.0f));
            EditorUtility.SetDirty(mat);
        }

        return mat;
    }

    public static void SetupMedievalMaterials()
    {
        string matFolder = $"{MedievalArtFolder}/Materials";
        if (!AssetDatabase.IsValidFolder(matFolder))
        {
            if (!AssetDatabase.IsValidFolder(MedievalArtFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "MedievalVillage");
            AssetDatabase.CreateFolder(MedievalArtFolder, "Materials");
        }

        CreatePbrMat(matFolder, "M_Medieval_Plaster", "T_Plaster");
        CreatePbrMat(matFolder, "M_Medieval_Brick", "T_Brick");
        CreatePbrMat(matFolder, "M_Medieval_UnevenBrick", "T_UnevenBrick");
        CreatePbrMat(matFolder, "M_Medieval_WoodTrim", "T_WoodTrim");
        CreatePbrMat(matFolder, "M_Medieval_RockTrim", "T_RockTrim");
        CreatePbrMat(matFolder, "M_Medieval_RoundTiles", "T_RoundTiles");

        // Material Dedaunan Realistis dengan Alpha Cutout & Double-Sided
        SetupFoliageMaterial("M_Foliage_Oak", "T_RealisticLeaf_Oak");
        SetupFoliageMaterial("M_Foliage_Pine", "T_RealisticLeaf_Pine");
    }

    public static Material SetupFoliageMaterial(string matName, string texName)
    {
        string matFolder = $"{MedievalArtFolder}/Materials";
        if (!AssetDatabase.IsValidFolder(matFolder))
        {
            if (!AssetDatabase.IsValidFolder(MedievalArtFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "MedievalVillage");
            AssetDatabase.CreateFolder(MedievalArtFolder, "Materials");
        }

        string matPath = $"{matFolder}/{matName}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        string texPath = $"{MedievalArtFolder}/Textures/{texName}.png";
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        if (tex)
        {
            var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (importer && !importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            mat.SetTexture("_BaseMap", tex);
            mat.SetTexture("_MainTex", tex);
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.35f);
            mat.SetFloat("_Cull", 0f); // Render dua sisi (Double-Sided)
            mat.SetFloat("_Smoothness", 0.05f); // Matte alami tanpa pantulan plastik
            mat.SetFloat("_Metallic", 0.0f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", "TransparentCutout");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            EditorUtility.SetDirty(mat);
        }

        return mat;
    }

    static void CreatePbrMat(string folder, string matName, string texPrefix)
    {
        string matPath = $"{folder}/{matName}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, matPath);
        }

        string texDir = $"{MedievalArtFolder}/Textures";
        var baseTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texDir}/{texPrefix}_BaseColor.png");
        var normTex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{texDir}/{texPrefix}_Normal.png");

        if (baseTex)
        {
            mat.SetTexture("_BaseMap", baseTex);
            mat.SetTexture("_MainTex", baseTex);
        }
        if (normTex)
        {
            FixTextureAsNormalMap(AssetDatabase.GetAssetPath(normTex));
            mat.SetTexture("_BumpMap", normTex);
            mat.EnableKeyword("_NORMALMAP");
        }
        mat.SetFloat("_Smoothness", 0.2f);
        EditorUtility.SetDirty(mat);
    }

    static void FixTextureAsNormalMap(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }
}
#endif

