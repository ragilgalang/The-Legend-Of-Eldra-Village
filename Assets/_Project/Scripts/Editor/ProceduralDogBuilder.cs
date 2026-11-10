#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Generator Anjing 3D Berdasarkan Spesifikasi Blender Script dog_walk.py.
/// Membangun hierarki rig tulang (DogRig) lengkap dengan bagian tubuh proporsional,
/// material bulu autentik, serta menghubungkannya ke ProceduralDogWalker & AnimalController.
/// </summary>
public static class ProceduralDogBuilder
{
    private const string FolderMats = "Assets/_Project/Art/Animals/Materials";

    private static Material GetOrCreateMaterial(string name, Color color, float roughness = 0.85f, float specular = 0.2f)
    {
        string path = $"{FolderMats}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", 1.0f - roughness);
        mat.SetFloat("_Metallic", 0.0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static GameObject BuildDog(Transform parent, Vector3 position, Quaternion rotation, string dogName = "Milo")
    {
        // ── 1. Muat Material dari Palette dog_walk.py ──
        var matFur      = GetOrCreateMaterial("M_BlenderDog_Fur",      new Color(0.34f, 0.19f, 0.08f), 0.85f);
        var matDarkFur  = GetOrCreateMaterial("M_BlenderDog_DarkFur",  new Color(0.12f, 0.06f, 0.03f), 0.85f);
        var matLightFur = GetOrCreateMaterial("M_BlenderDog_LightFur", new Color(0.62f, 0.45f, 0.27f), 0.85f);
        var matNose     = GetOrCreateMaterial("M_BlenderDog_Nose",     new Color(0.01f, 0.01f, 0.01f), 0.25f, 0.6f);
        var matEye      = GetOrCreateMaterial("M_BlenderDog_Eye",      new Color(0.02f, 0.01f, 0.005f), 0.10f, 0.8f);

        // ── 2. Root GameObject ──
        var dogRoot = new GameObject($"Animal_Dog_{dogName}");
        dogRoot.transform.SetParent(parent);
        position.y = 0f;
        dogRoot.transform.position = position;
        dogRoot.transform.rotation = rotation;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(dogRoot.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;

        // ── 3. Hirarki Tulang (DogRig Armature) ──
        var rig = new GameObject("DogRig");
        rig.transform.SetParent(visual.transform, false);

        var root = new GameObject("root");
        root.transform.SetParent(rig.transform, false);
        root.transform.localPosition = new Vector3(0f, 0.62f, 0f);

        var spine = new GameObject("spine");
        spine.transform.SetParent(root.transform, false);
        spine.transform.localPosition = new Vector3(0f, 0.02f, 0f); // Y = 0.64

        var neck = new GameObject("neck");
        neck.transform.SetParent(spine.transform, false);
        neck.transform.localPosition = new Vector3(0f, 0.02f, 0.32f); // Y = 0.66, Z = 0.32

        var head = new GameObject("head");
        head.transform.SetParent(neck.transform, false);
        head.transform.localPosition = new Vector3(0f, 0.20f, 0.28f); // Y = 0.86, Z = 0.60

        var tail1 = new GameObject("tail1");
        tail1.transform.SetParent(spine.transform, false);
        tail1.transform.localPosition = new Vector3(0f, 0.04f, -0.52f); // Y = 0.68, Z = -0.52

        var tail2 = new GameObject("tail2");
        tail2.transform.SetParent(tail1.transform, false);
        tail2.transform.localPosition = new Vector3(0f, 0.14f, -0.18f); // Y = 0.82, Z = -0.70

        // Kaki Depan (Front Legs: Y = 0.60, Z = 0.38)
        var flUp = new GameObject("FL_up");
        flUp.transform.SetParent(spine.transform, false);
        flUp.transform.localPosition = new Vector3(0.12f, -0.04f, 0.38f);
        var flLo = new GameObject("FL_lo");
        flLo.transform.SetParent(flUp.transform, false);
        flLo.transform.localPosition = new Vector3(0f, -0.28f, 0f);

        var frUp = new GameObject("FR_up");
        frUp.transform.SetParent(spine.transform, false);
        frUp.transform.localPosition = new Vector3(-0.12f, -0.04f, 0.38f);
        var frLo = new GameObject("FR_lo");
        frLo.transform.SetParent(frUp.transform, false);
        frLo.transform.localPosition = new Vector3(0f, -0.28f, 0f);

        // Kaki Belakang (Back Legs: Y = 0.60, Z = -0.40)
        var blUp = new GameObject("BL_up");
        blUp.transform.SetParent(spine.transform, false);
        blUp.transform.localPosition = new Vector3(0.12f, -0.04f, -0.40f);
        var blLo = new GameObject("BL_lo");
        blLo.transform.SetParent(blUp.transform, false);
        blLo.transform.localPosition = new Vector3(0f, -0.28f, 0f);

        var brUp = new GameObject("BR_up");
        brUp.transform.SetParent(spine.transform, false);
        brUp.transform.localPosition = new Vector3(-0.12f, -0.04f, -0.40f);
        var brLo = new GameObject("BR_lo");
        brLo.transform.SetParent(brUp.transform, false);
        brLo.transform.localPosition = new Vector3(0f, -0.28f, 0f);

        // ── 4. Mesh Bagian Tubuh (UV Spheres Terkalibrasi) ──
        // Torso (Dada, Perut, Pinggul)
        CreatePart("Chest", spine.transform, new Vector3(0f, -0.02f, 0.24f), new Vector3(0.42f, 0.46f, 0.60f), matFur);
        CreatePart("Belly", spine.transform, new Vector3(0f, -0.06f, -0.04f), new Vector3(0.36f, 0.36f, 0.60f), matFur);
        CreatePart("Rump",  spine.transform, new Vector3(0f, -0.02f, -0.32f), new Vector3(0.40f, 0.42f, 0.52f), matFur);

        // Leher
        CreatePart("Neck_Mesh", neck.transform, new Vector3(0f, 0.12f, 0.18f), new Vector3(0.22f, 0.32f, 0.34f), matLightFur, Quaternion.Euler(-40f, 0f, 0f));

        // Kepala, Moncong, Hidung, Mata, & Telinga
        CreatePart("Skull",  head.transform, new Vector3(0f, 0.02f, 0.12f),  new Vector3(0.24f, 0.24f, 0.28f), matFur);
        CreatePart("Muzzle", head.transform, new Vector3(0f, -0.03f, 0.28f), new Vector3(0.13f, 0.12f, 0.24f), matLightFur);
        CreatePart("Nose",   head.transform, new Vector3(0f, -0.025f, 0.395f), new Vector3(0.06f, 0.044f, 0.044f), matNose);

        CreatePart("Eye_L", head.transform, new Vector3( 0.075f, 0.055f, 0.22f), new Vector3(0.032f, 0.032f, 0.028f), matEye);
        CreatePart("Eye_R", head.transform, new Vector3(-0.075f, 0.055f, 0.22f), new Vector3(0.032f, 0.032f, 0.028f), matEye);

        CreatePart("Ear_L", head.transform, new Vector3( 0.105f, 0.11f, 0.08f), new Vector3(0.05f, 0.22f, 0.13f), matDarkFur, Quaternion.Euler(0f, 0f, -14f));
        CreatePart("Ear_R", head.transform, new Vector3(-0.105f, 0.11f, 0.08f), new Vector3(0.05f, 0.22f, 0.13f), matDarkFur, Quaternion.Euler(0f, 0f, 14f));

        // Ekor
        CreatePart("Tail1_Mesh", tail1.transform, new Vector3(0f, 0.07f, -0.08f), new Vector3(0.09f, 0.10f, 0.18f), matFur, Quaternion.Euler(-35f, 0f, 0f));
        CreatePart("Tail2_Mesh", tail2.transform, new Vector3(0f, -0.01f, -0.08f), new Vector3(0.08f, 0.08f, 0.18f), matDarkFur, Quaternion.Euler(10f, 0f, 0f));

        // Kaki Depan
        CreatePart("FUpL_Mesh", flUp.transform, new Vector3(0f, -0.15f, 0f), new Vector3(0.13f, 0.34f, 0.15f), matFur);
        CreatePart("FLoL_Mesh", flLo.transform, new Vector3(0f, -0.13f, 0f), new Vector3(0.08f, 0.30f, 0.09f), matLightFur);
        CreatePart("FPawL",     flLo.transform, new Vector3(0f, -0.285f, 0.025f), new Vector3(0.104f, 0.074f, 0.17f), matLightFur);

        CreatePart("FUpR_Mesh", frUp.transform, new Vector3(0f, -0.15f, 0f), new Vector3(0.13f, 0.34f, 0.15f), matFur);
        CreatePart("FLoR_Mesh", frLo.transform, new Vector3(0f, -0.13f, 0f), new Vector3(0.08f, 0.30f, 0.09f), matLightFur);
        CreatePart("FPawR",     frLo.transform, new Vector3(0f, -0.285f, 0.025f), new Vector3(0.104f, 0.074f, 0.17f), matLightFur);

        // Kaki Belakang
        CreatePart("BUpL_Mesh", blUp.transform, new Vector3(0f, -0.14f, 0f), new Vector3(0.16f, 0.38f, 0.22f), matFur);
        CreatePart("BLoL_Mesh", blLo.transform, new Vector3(0f, -0.13f, 0f), new Vector3(0.076f, 0.30f, 0.09f), matLightFur);
        CreatePart("BPawL",     blLo.transform, new Vector3(0f, -0.285f, 0.025f), new Vector3(0.10f, 0.074f, 0.17f), matLightFur);

        CreatePart("BUpR_Mesh", brUp.transform, new Vector3(0f, -0.14f, 0f), new Vector3(0.16f, 0.38f, 0.22f), matFur);
        CreatePart("BLoR_Mesh", brLo.transform, new Vector3(0f, -0.13f, 0f), new Vector3(0.076f, 0.30f, 0.09f), matLightFur);
        CreatePart("BPawR",     brLo.transform, new Vector3(0f, -0.285f, 0.025f), new Vector3(0.10f, 0.074f, 0.17f), matLightFur);

        // ── 5. Komponen Walk Cycle Prosedural (ProceduralDogWalker) ──
        var walker = dogRoot.AddComponent<ProceduralDogWalker>();
        walker.rootBone = root.transform;
        walker.spineBone = spine.transform;
        walker.neckBone = neck.transform;
        walker.headBone = head.transform;
        walker.tail1Bone = tail1.transform;
        walker.tail2Bone = tail2.transform;

        walker.flUp = flUp.transform; walker.flLo = flLo.transform;
        walker.frUp = frUp.transform; walker.frLo = frLo.transform;
        walker.blUp = blUp.transform; walker.blLo = blLo.transform;
        walker.brUp = brUp.transform; walker.brLo = brLo.transform;

        // ── 6. Collider & NavMeshAgent ──
        var col = dogRoot.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.42f, 0f);
        col.radius = 0.26f;
        col.height = 1.1f;
        col.direction = 2; // Sumbu Z (searah badan)

        var agent = dogRoot.AddComponent<NavMeshAgent>();
        agent.speed = 1.8f;
        agent.acceleration = 4.2f;
        agent.angularSpeed = 220f;
        agent.stoppingDistance = 1.5f;
        agent.radius = 0.35f;
        agent.height = 0.85f;

        // ── 7. AI AnimalController & Companion Interaksi ──
        var ctrl = dogRoot.AddComponent<AnimalController>();
        ctrl.animalType = AnimalController.AnimalType.Dog;
        ctrl.animalName = dogName;
        ctrl.wanderRadius = 8.0f;
        ctrl.moveSpeed = 1.8f;
        ctrl.turnSpeed = 220f;
        ctrl.turnSmoothness = 5.0f;
        ctrl.petMessage = $"Guk! {dogName} mengibaskan ekornya riang dan kini menemanimu menjelajahi desa Eldara!";
        ctrl.canFollowPlayer = true;
        ctrl.companionDuration = 60.0f;

        return dogRoot;
    }

    private static GameObject CreatePart(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, Quaternion? localRot = null)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;
        go.transform.localRotation = localRot ?? Quaternion.identity;

        var col = go.GetComponent<Collider>();
        if (col) Object.DestroyImmediate(col);

        var mr = go.GetComponent<MeshRenderer>();
        if (mr && mat) mr.sharedMaterial = mat;

        return go;
    }
}
#endif
