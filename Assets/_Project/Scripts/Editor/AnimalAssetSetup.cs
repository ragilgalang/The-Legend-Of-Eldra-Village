#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

/// <summary>
/// Tool penyiapan dan penempatan aset fauna/hewan untuk Desa Eldara.
/// Mengonfigurasi model 3D, material URP, Animator Controller otomatis, collider, dan AI Controller untuk:
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
    private const string AnimsFolder = "Assets/_Project/Art/Animals/Animations";

    [MenuItem("Eldara/Setup Animal Assets & Controllers")]
    public static void SetupAllAnimalsMenu()
    {
        PreflightSetup();
        EditorUtility.DisplayDialog("Sukses!", "Aset hewan (Material URP, Animasi Looping, Animator Controller) berhasil disiapkan!", "OK");
    }

    [MenuItem("Eldara/Hapus Ikan di Darat")]
    public static void RemoveLandFishMenu()
    {
        int count = 0;
        var transforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var toDestroy = new System.Collections.Generic.HashSet<GameObject>();

        foreach (var t in transforms)
        {
            if (!t) continue;
            string n = t.name.ToLower();
            if (n.Contains("fish") || n.Contains("piranha") || t.GetComponent<FishController>())
            {
                // Ambil root object ikan
                Transform curr = t;
                while (curr.parent != null && curr.parent.name != "--- FAUNA & ANIMALS ---" && 
                      (curr.parent.name.ToLower().Contains("fish") || curr.parent.name.ToLower().Contains("piranha")))
                {
                    curr = curr.parent;
                }
                toDestroy.Add(curr.gameObject);
            }
        }

        foreach (var go in toDestroy)
        {
            if (go)
            {
                Object.DestroyImmediate(go);
                count++;
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        EditorUtility.DisplayDialog("Ikan Berhasil Dihapus!", $"Berhasil membersihkan {count} objek ikan di darat dari scene.", "OK");
    }

    public static void PreflightSetup()
    {
        EnsureFolders();
        EnsureMaterials();
        ConfigureModelImporters();
        EnsureAnimatorControllers();
        AssetDatabase.SaveAssets();
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(AnimalsFolder))
            AssetDatabase.CreateFolder("Assets/_Project/Art", "Animals");
        if (!AssetDatabase.IsValidFolder(MatsFolder))
            AssetDatabase.CreateFolder(AnimalsFolder, "Materials");
        if (!AssetDatabase.IsValidFolder(AnimsFolder))
            AssetDatabase.CreateFolder(AnimalsFolder, "Animations");
    }

    public static void EnsureMaterials()
    {
        EnsureFolders();

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

    /// <summary>
    /// Mengonfigurasi ModelImporter untuk FBX hewan agar animasi di-set LoopTime = true
    /// </summary>
    public static void ConfigureModelImporters()
    {
        string[] animals = { "Dog", "Cat", "Wolf", "Eagle" };
        foreach (var a in animals)
        {
            string path = $"{AnimalsFolder}/FBX/{a}.fbx";
            var importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                bool reimport = false;
                if (importer.animationType != ModelImporterAnimationType.Generic)
                {
                    importer.animationType = ModelImporterAnimationType.Generic;
                    reimport = true;
                }

                var defaultClips = importer.defaultClipAnimations;
                if (defaultClips != null && defaultClips.Length > 0)
                {
                    var customClips = new List<ModelImporterClipAnimation>();
                    foreach (var dc in defaultClips)
                    {
                        var clip = new ModelImporterClipAnimation();
                        clip.name = dc.name;
                        clip.takeName = dc.takeName;
                        clip.firstFrame = dc.firstFrame;
                        clip.lastFrame = dc.lastFrame;
                        clip.loopTime = true;
                        clip.wrapMode = WrapMode.Loop;
                        customClips.Add(clip);
                    }
                    importer.clipAnimations = customClips.ToArray();
                    reimport = true;
                }

                if (reimport)
                {
                    importer.SaveAndReimport();
                }
            }
        }
    }

    /// <summary>
    /// Membuat Runtime Animator Controller otomatis untuk setiap hewan
    /// </summary>
    public static void EnsureAnimatorControllers()
    {
        EnsureAnimalController("Dog");
        EnsureAnimalController("Cat");
        EnsureAnimalController("Wolf");
        EnsureAnimalController("Eagle");
    }

    private static RuntimeAnimatorController EnsureAnimalController(string animalName)
    {
        string controllerPath = $"{AnimsFolder}/AC_{animalName}.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (ctrl != null) return ctrl;

        string fbxPath = $"{AnimalsFolder}/FBX/{animalName}.fbx";
        AnimationClip idleClip = null;
        AnimationClip walkClip = null;

        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (obj is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                string lower = clip.name.ToLowerInvariant();
                if (lower.Contains("walk") || lower.Contains("run"))
                    walkClip = clip;
                else if (lower.Contains("idle"))
                    idleClip = clip;
            }
        }

        if (idleClip == null) return null;

        ctrl = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        ctrl.AddParameter("isMoving", AnimatorControllerParameterType.Bool);

        var rootSm = ctrl.layers[0].stateMachine;

        var idleState = rootSm.AddState("Idle");
        idleState.motion = idleClip;
        rootSm.defaultState = idleState;

        if (walkClip != null)
        {
            var walkState = rootSm.AddState("Walk");
            walkState.motion = walkClip;

            // Idle -> Walk
            var toWalk = idleState.AddTransition(walkState);
            toWalk.AddCondition(AnimatorConditionMode.If, 0, "isMoving");
            toWalk.duration = 0.2f;
            toWalk.hasExitTime = false;

            // Walk -> Idle
            var toIdle = walkState.AddTransition(idleState);
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "isMoving");
            toIdle.duration = 0.25f;
            toIdle.hasExitTime = false;
        }

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    public static RuntimeAnimatorController GetAnimatorController(string animalName)
    {
        return AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"{AnimsFolder}/AC_{animalName}.controller");
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
    //  SPAWNERS HEWAN DESA ELDARA (SKALA REALISTIS)
    // ─────────────────────────────────────────────────────────────────────

    public static GameObject SpawnDog(Vector3 position, Quaternion rotation, Transform parent)
    {
        PreflightSetup();

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
        // Skala realistis: panjang ~0.85m, tinggi punggung ~0.60m
        visual.transform.localScale = Vector3.one * 0.15f;

        ApplyAnimalMaterials(visual, "Dog");

        // Hubungkan Animator Controller
        var anim = visual.GetComponent<Animator>();
        if (!anim) anim = visual.AddComponent<Animator>();
        var ac = GetAnimatorController("Dog");
        if (ac != null) anim.runtimeAnimatorController = ac;

        // Collider proporsional (horizontal sepanjang tubuh Z)
        var col = dogRoot.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.28f, 0);
        col.radius = 0.22f;
        col.height = 0.75f;
        col.direction = 2;

        // NavMeshAgent proporsional
        var agent = dogRoot.AddComponent<NavMeshAgent>();
        agent.baseOffset = 0f;
        agent.radius = 0.25f;
        agent.height = 0.60f;
        agent.speed = 2.0f;
        agent.acceleration = 7.0f;
        agent.angularSpeed = 240.0f;
        agent.stoppingDistance = 0.35f;

        // AI Controller
        var ctrl = dogRoot.AddComponent<AnimalController>();
        ctrl.animalType = AnimalController.AnimalType.Dog;
        ctrl.animalName = "Milo";
        ctrl.wanderRadius = 7.0f;
        ctrl.moveSpeed = 1.9f;
        ctrl.acceleration = 3.2f;
        ctrl.turnSmoothness = 5.0f;
        ctrl.petMessage = "Guk! Milo si anjing desa mengibaskan ekornya riang dan kini menemanimu berjalan-jalan!";
        ctrl.canFollowPlayer = true;

        return dogRoot;
    }

    public static GameObject SpawnCat(Vector3 position, Quaternion rotation, Transform parent, string catName, string petMsg, bool altColor = false)
    {
        PreflightSetup();

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
        // Skala realistis: panjang ~0.45m, tinggi punggung ~0.30m
        visual.transform.localScale = Vector3.one * 0.075f;

        ApplyAnimalMaterials(visual, "Cat", altColor);

        // Hubungkan Animator Controller
        var anim = visual.GetComponent<Animator>();
        if (!anim) anim = visual.AddComponent<Animator>();
        var ac = GetAnimatorController("Cat");
        if (ac != null) anim.runtimeAnimatorController = ac;

        // Collider proporsional (horizontal Z)
        var col = catRoot.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.15f, 0);
        col.radius = 0.14f;
        col.height = 0.40f;
        col.direction = 2;

        // NavMeshAgent proporsional
        var agent = catRoot.AddComponent<NavMeshAgent>();
        agent.baseOffset = 0f;
        agent.radius = 0.16f;
        agent.height = 0.30f;
        agent.speed = 1.35f;
        agent.acceleration = 6.0f;
        agent.angularSpeed = 280.0f;
        agent.stoppingDistance = 0.25f;

        // AI Controller
        var ctrl = catRoot.AddComponent<AnimalController>();
        ctrl.animalType = AnimalController.AnimalType.Cat;
        ctrl.animalName = catName;
        ctrl.wanderRadius = 4.5f;
        ctrl.moveSpeed = 1.3f;
        ctrl.acceleration = 3.0f;
        ctrl.turnSmoothness = 4.8f;
        ctrl.petMessage = petMsg;
        ctrl.canFollowPlayer = false;

        return catRoot;
    }

    public static GameObject SpawnWolf(Vector3 position, Quaternion rotation, Transform parent, string wolfName = "Serigala Rimba")
    {
        PreflightSetup();

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
        // Skala realistis serigala: panjang ~1.2m, tinggi ~0.80m
        visual.transform.localScale = Vector3.one * 0.165f;

        ApplyAnimalMaterials(visual, "Wolf");

        // Hubungkan Animator Controller
        var anim = visual.GetComponent<Animator>();
        if (!anim) anim = visual.AddComponent<Animator>();
        var ac = GetAnimatorController("Wolf");
        if (ac != null) anim.runtimeAnimatorController = ac;

        // Collider proporsional (horizontal Z sepanjang tubuh 4 kaki)
        var col = wolfRoot.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0, 0.38f, 0);
        col.radius = 0.28f;
        col.height = 0.95f;
        col.direction = 2;

        // NavMeshAgent proporsional
        var agent = wolfRoot.AddComponent<NavMeshAgent>();
        agent.baseOffset = 0f;
        agent.radius = 0.30f;
        agent.height = 0.75f;
        agent.speed = 2.6f;
        agent.acceleration = 8.0f;
        agent.angularSpeed = 260.0f;
        agent.stoppingDistance = 0.45f;

        // AI Controller
        var ctrl = wolfRoot.AddComponent<AnimalController>();
        ctrl.animalType = AnimalController.AnimalType.Wolf;
        ctrl.animalName = wolfName;
        ctrl.wanderRadius = 14.0f;
        ctrl.moveSpeed = 2.4f;
        ctrl.acceleration = 3.6f;
        ctrl.turnSmoothness = 4.5f;
        ctrl.petMessage = "Auuu! Serigala rimba menatapmu tajam dari sela rimbunnya pepohonan.";
        ctrl.canFollowPlayer = false;

        return wolfRoot;
    }

    public static GameObject SpawnEagle(Vector3 flightCenter, Transform parent)
    {
        PreflightSetup();

        var modelPrefab = LoadAnimalModel("Eagle");
        if (!modelPrefab) return null;

        var eagleRoot = new GameObject("Animal_Eagle_Aquila");
        eagleRoot.transform.SetParent(parent);
        eagleRoot.transform.position = flightCenter;

        var visual = Object.Instantiate(modelPrefab, eagleRoot.transform);
        visual.name = "Visual";
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        // Skala rentang sayap elang perkasa ~2.0m
        visual.transform.localScale = Vector3.one * 0.45f;

        ApplyAnimalMaterials(visual, "Eagle");

        var anim = visual.GetComponent<Animator>();
        if (!anim) anim = visual.AddComponent<Animator>();
        var ac = GetAnimatorController("Eagle");
        if (ac != null) anim.runtimeAnimatorController = ac;

        var ctrl = eagleRoot.AddComponent<EagleController>();
        ctrl.flightCenter = flightCenter;
        ctrl.orbitRadius = 42.0f;
        ctrl.flightSpeed = 12.5f;
        ctrl.bankAngle = 22.0f;

        return eagleRoot;
    }

    public static GameObject SpawnFish(Vector3 position, Quaternion rotation, Transform parent)
    {
        PreflightSetup();

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
        // Skala ikan air tawar ~0.45m
        visual.transform.localScale = Vector3.one * 0.05f;

        ApplyAnimalMaterials(visual, "Piranha");

        var ctrl = fishRoot.AddComponent<FishController>();
        ctrl.swimRadius = 3.0f;
        ctrl.swimSpeed = 1.3f;

        return fishRoot;
    }
}
#endif
