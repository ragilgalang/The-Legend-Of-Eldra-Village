#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Tool penyiapan dan penempatan aset fauna/hewan untuk Desa Eldara.
/// Mengonfigurasi model 3D, material URP, collider, dan AI Controller untuk:
/// - Dog (Milo - Anjing Penjaga Desa)
/// - Cat (Luna & Oliver - Kucing Desa)
/// - Wolf (Kawanan Serigala Rimba)
/// - Eagle (Elang Perkasa Penjaga Langit)
/// - Fish (Ikan Kolam Desa)
/// </summary>
public static class AnimalAssetSetup
{
    private const string AnimalsFolder = "Assets/_Project/Art/Animals";
    private const string MatsFolder = "Assets/_Project/Art/Animals/Materials";

    public static void EnsureMaterials()
    {
        if (!AssetDatabase.IsValidFolder(MatsFolder))
        {
            if (!AssetDatabase.IsValidFolder(AnimalsFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Animals");
            AssetDatabase.CreateFolder(AnimalsFolder, "Materials");
        }

        // 1. Dog (Anjing Desa) - Bulu keemasan hangat
        GetOrCreateMat("M_Animal_Dog", new Color(0.76f, 0.48f, 0.22f), 0.25f);

        // 2. Cat (Kucing Desa) - Kucing manis dengan telinga pink & dada putih
        GetOrCreateMat("M_Animal_Cat_Grey", new Color(0.48f, 0.52f, 0.56f), 0.25f);
        GetOrCreateMat("M_Animal_Cat_Pink", new Color(1.0f, 0.72f, 0.72f), 0.35f);
        GetOrCreateMat("M_Animal_Cat_White", new Color(0.96f, 0.96f, 0.96f), 0.25f);
        GetOrCreateMat("M_Animal_Cat_Orange", new Color(0.88f, 0.48f, 0.18f), 0.25f);

        // 3. Wolf (Serigala Rimba) - Bulu kelabu gelap pekat
        GetOrCreateMat("M_Animal_Wolf", new Color(0.24f, 0.26f, 0.28f), 0.15f);

        // 4. Eagle (Elang Perkasa) - Sayap cokelat tua, kepala putih, paruh/cakar emas
        GetOrCreateMat("M_Animal_Eagle_Wings", new Color(0.22f, 0.14f, 0.10f), 0.3f);
        GetOrCreateMat("M_Animal_Eagle_Head", new Color(0.96f, 0.96f, 0.96f), 0.3f);
        GetOrCreateMat("M_Animal_Eagle_Beak", new Color(1.0f, 0.74f, 0.05f), 0.55f);

        // 5. Piranha / River Fish - Sisik hijau-zamrud, perut kuning, mata hitam, gigi putih
        GetOrCreateMat("M_Animal_Fish_Body", new Color(0.18f, 0.45f, 0.32f), 0.65f);
        GetOrCreateMat("M_Animal_Fish_Belly", new Color(0.95f, 0.88f, 0.55f), 0.55f);
        GetOrCreateMat("M_Animal_Fish_Black", new Color(0.1f, 0.1f, 0.1f), 0.85f);
        GetOrCreateMat("M_Animal_Fish_White", new Color(0.95f, 0.95f, 0.95f), 0.4f);

        AssetDatabase.SaveAssets();
    }

    private static Material GetOrCreateMat(string matName, Color color, float smoothness)
    {
        string path = $"{MatsFolder}/{matName}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", 0.0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static Material GetMaterial(string matName)
    {
        return AssetDatabase.LoadAssetAtPath<Material>($"{MatsFolder}/{matName}.mat");
    }

    public static GameObject LoadAnimalModel(string animalName)
    {
        string fbxPath = $"{AnimalsFolder}/FBX/{animalName}.fbx";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        if (!prefab)
        {
            string objPath = $"{AnimalsFolder}/OBJ/{animalName}.obj";
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(objPath);
        }
        return prefab;
    }

    public static void ApplyAnimalMaterials(GameObject instance, string animalName, bool altColor = false)
    {
        EnsureMaterials();
        var renderers = instance.GetComponentsInChildren<Renderer>(true);

        foreach (var rend in renderers)
        {
            var sharedMats = rend.sharedMaterials;
            var newMats = new Material[sharedMats.Length];

            for (int i = 0; i < sharedMats.Length; i++)
            {
                string slotName = sharedMats[i] != null ? sharedMats[i].name.ToLowerInvariant() : "";

                if (animalName == "Dog")
                {
                    newMats[i] = GetMaterial("M_Animal_Dog");
                }
                else if (animalName == "Cat")
                {
                    if (slotName.Contains("pink")) newMats[i] = GetMaterial("M_Animal_Cat_Pink");
                    else if (slotName.Contains("white")) newMats[i] = GetMaterial("M_Animal_Cat_White");
                    else newMats[i] = altColor ? GetMaterial("M_Animal_Cat_Orange") : GetMaterial("M_Animal_Cat_Grey");
                }
                else if (animalName == "Wolf")
                {
                    newMats[i] = GetMaterial("M_Animal_Wolf");
                }
                else if (animalName == "Eagle")
                {
                    if (slotName.Contains("beak") || slotName.Contains("claw")) newMats[i] = GetMaterial("M_Animal_Eagle_Beak");
                    else if (slotName.Contains("head")) newMats[i] = GetMaterial("M_Animal_Eagle_Head");
                    else newMats[i] = GetMaterial("M_Animal_Eagle_Wings");
                }
                else if (animalName == "Piranha")
                {
                    if (slotName.Contains("black")) newMats[i] = GetMaterial("M_Animal_Fish_Black");
                    else if (slotName.Contains("yellow")) newMats[i] = GetMaterial("M_Animal_Fish_Belly");
                    else if (slotName.Contains("white")) newMats[i] = GetMaterial("M_Animal_Fish_White");
                    else newMats[i] = GetMaterial("M_Animal_Fish_Body");
                }
                else
                {
                    newMats[i] = sharedMats[i];
                }
            }
            rend.sharedMaterials = newMats;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  SPAWNERS HEWAN DESA ELDARA
    // ─────────────────────────────────────────────────────────────────────

    public static GameObject SpawnDog(Vector3 position, Quaternion rotation, Transform parent)
    {
        var modelPrefab = LoadAnimalModel("Dog");
        if (!modelPrefab) return null;

        var dogRoot = new GameObject("Animal_Dog_Milo");
        dogRoot.transform.SetParent(parent);
        dogRoot.transform.position = position;
        dogRoot.transform.rotation = rotation;

        var visual = Object.Instantiate(modelPrefab, dogRoot.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 0.9f;

        ApplyAnimalMaterials(visual, "Dog");

        // Collider
        var col = dogRoot.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.45f, 0);
        col.radius = 0.38f;
        col.height = 1.0f;

        // NavMeshAgent
        var agent = dogRoot.AddComponent<NavMeshAgent>();
        agent.radius = 0.35f;
        agent.height = 0.8f;
        agent.speed = 1.9f;
        agent.acceleration = 6.0f;
        agent.stoppingDistance = 0.3f;

        // AI Controller
        var ctrl = dogRoot.AddComponent<AnimalController>();
        ctrl.animalType = AnimalController.AnimalType.Dog;
        ctrl.animalName = "Milo";
        ctrl.wanderRadius = 8.0f;
        ctrl.moveSpeed = 1.9f;
        ctrl.petMessage = "Guk! Milo si anjing desa mengibaskan ekornya riang dan kini menemanimu berjalan-jalan!";
        ctrl.canFollowPlayer = true;

        return dogRoot;
    }

    public static GameObject SpawnCat(Vector3 position, Quaternion rotation, Transform parent, string catName, string petMsg, bool altColor = false)
    {
        var modelPrefab = LoadAnimalModel("Cat");
        if (!modelPrefab) return null;

        var catRoot = new GameObject($"Animal_Cat_{catName}");
        catRoot.transform.SetParent(parent);
        catRoot.transform.position = position;
        catRoot.transform.rotation = rotation;

        var visual = Object.Instantiate(modelPrefab, catRoot.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 0.72f;

        ApplyAnimalMaterials(visual, "Cat", altColor);

        // Collider
        var col = catRoot.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.3f, 0);
        col.radius = 0.28f;
        col.height = 0.7f;

        // NavMeshAgent
        var agent = catRoot.AddComponent<NavMeshAgent>();
        agent.radius = 0.25f;
        agent.height = 0.6f;
        agent.speed = 1.4f;
        agent.acceleration = 5.0f;
        agent.stoppingDistance = 0.3f;

        // AI Controller
        var ctrl = catRoot.AddComponent<AnimalController>();
        ctrl.animalType = AnimalController.AnimalType.Cat;
        ctrl.animalName = catName;
        ctrl.wanderRadius = 5.0f;
        ctrl.moveSpeed = 1.4f;
        ctrl.petMessage = petMsg;
        ctrl.canFollowPlayer = false;

        return catRoot;
    }

    public static GameObject SpawnWolf(Vector3 position, Quaternion rotation, Transform parent, string wolfName = "Serigala Rimba")
    {
        var modelPrefab = LoadAnimalModel("Wolf");
        if (!modelPrefab) return null;

        var wolfRoot = new GameObject($"Animal_{wolfName.Replace(" ", "_")}");
        wolfRoot.transform.SetParent(parent);
        wolfRoot.transform.position = position;
        wolfRoot.transform.rotation = rotation;

        var visual = Object.Instantiate(modelPrefab, wolfRoot.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 1.05f;

        ApplyAnimalMaterials(visual, "Wolf");

        // Collider
        var col = wolfRoot.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.5f, 0);
        col.radius = 0.42f;
        col.height = 1.25f;

        // NavMeshAgent
        var agent = wolfRoot.AddComponent<NavMeshAgent>();
        agent.radius = 0.4f;
        agent.height = 1.0f;
        agent.speed = 2.4f;
        agent.acceleration = 7.0f;
        agent.stoppingDistance = 0.5f;

        // AI Controller
        var ctrl = wolfRoot.AddComponent<AnimalController>();
        ctrl.animalType = AnimalController.AnimalType.Wolf;
        ctrl.animalName = wolfName;
        ctrl.wanderRadius = 15.0f;
        ctrl.moveSpeed = 2.4f;
        ctrl.petMessage = "Auuu! Serigala rimba menatapmu tajam dari sela rimbunnya pepohonan.";
        ctrl.canFollowPlayer = false;

        return wolfRoot;
    }

    public static GameObject SpawnEagle(Vector3 flightCenter, Transform parent)
    {
        var modelPrefab = LoadAnimalModel("Eagle");
        if (!modelPrefab) return null;

        var eagleRoot = new GameObject("Animal_Eagle_Aquila");
        eagleRoot.transform.SetParent(parent);
        eagleRoot.transform.position = flightCenter;

        var visual = Object.Instantiate(modelPrefab, eagleRoot.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 1.35f;

        ApplyAnimalMaterials(visual, "Eagle");

        var ctrl = eagleRoot.AddComponent<EagleController>();
        ctrl.flightCenter = flightCenter;
        ctrl.orbitRadius = 42.0f;
        ctrl.flightSpeed = 12.5f;
        ctrl.bankAngle = 22.0f;

        return eagleRoot;
    }

    public static GameObject SpawnFish(Vector3 position, Quaternion rotation, Transform parent)
    {
        var modelPrefab = LoadAnimalModel("Piranha");
        if (!modelPrefab) return null;

        var fishRoot = new GameObject("Animal_RiverFish");
        fishRoot.transform.SetParent(parent);
        fishRoot.transform.position = position;
        fishRoot.transform.rotation = rotation;

        var visual = Object.Instantiate(modelPrefab, fishRoot.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one * 0.85f;

        ApplyAnimalMaterials(visual, "Piranha");

        var ctrl = fishRoot.AddComponent<FishController>();
        ctrl.swimRadius = 3.0f;
        ctrl.swimSpeed = 1.3f;

        return fishRoot;
    }
}
#endif
