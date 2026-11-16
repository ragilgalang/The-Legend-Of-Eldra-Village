using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Generator 3D Karakter NPC Stylized Game-Art (Porting Python Blender).
/// Menghasilkan 5 Karakter Ikonik:
/// 1. Guard (Penjaga Benteng): Helm baja, jubah merah, pelindung dada, & pedang ksatria.
/// 2. Farmer (Petani): Topi caping jerami, rompi kulit, kumis, & garpu rumput 3 taring.
/// 3. Merchant (Pedagang / Kepala Desa): Baret beludru bulu, jubah merah emas, kumis melengkung, & tas ransel ekspedisi.
/// 4. Wizard (Dukun Mistis): Topi kerucut sihir melengkung, janggut putih panjang, & tongkat kristal bercahaya cyan.
/// 5. Woman (Gadis Desa): Rambut kepang panjang jatuh ke dada, gaun zamrud, celemek putih, & tas selempang.
///
/// Seluruh model dibangun dengan hierarki sendi (Visual -> Torso -> Head, Arms, Legs)
/// sehingga langsung kompatibel dengan animasi melangkah NpcMover secara prosedural.
/// </summary>
public static class ProceduralNpcBuilder
{
    public enum NpcStyle
    {
        Guard,
        Farmer,
        Merchant,
        Wizard,
        Woman,
        Woodcutter,
        Hunter,
        Herbalist,
        Fisherman,
        VillageYouth
    }

    private static readonly Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();
    private static readonly Dictionary<string, Material> matCache = new Dictionary<string, Material>();

    // ─────────────────────────────────────────────────────────────────────
    //  ENTRY POINT UTAMA
    // ─────────────────────────────────────────────────────────────────────
    public static GameObject BuildNpc(NpcStyle style, Vector3 worldPos, Quaternion worldRot, Transform parent, string npcName = null)
    {
        string defaultName = $"NPC_{style}";
        var root = new GameObject(npcName ?? defaultName);
        if (parent) root.transform.SetParent(parent);
        root.transform.position = worldPos;
        root.transform.rotation = worldRot;
        root.tag = "NPC";

        // Collider Fisik & Interaksi
        var col = root.AddComponent<CapsuleCollider>();
        col.radius = 0.4f;
        col.height = 1.8f;
        col.center = new Vector3(0, 0.9f, 0);

        // Sub-root Visual
        var visualRoot = new GameObject("Visual");
        visualRoot.transform.SetParent(root.transform, false);

        // Skala Karakter sesuai spesifikasi Blender
        float scaleH = 1.0f;
        float bodyFat = 1.0f;
        int gender = 0; // 0 = male/unisex, 1 = female

        Color skinCol, hairCol, eyeCol, clothCol, pantsCol, bootCol;

        switch (style)
        {
            case NpcStyle.Guard:
                scaleH = 1.0f; bodyFat = 1.05f; gender = 0;
                skinCol = HexColor(0xf0b896); hairCol = HexColor(0x7a4420); eyeCol = HexColor(0x3b82c4);
                clothCol = HexColor(0x2a4f9e); pantsCol = HexColor(0x30343c); bootCol = HexColor(0x3a2a1e);
                break;
            case NpcStyle.Farmer:
                scaleH = 0.95f; bodyFat = 1.0f; gender = 0;
                skinCol = HexColor(0xd69a6b); hairCol = HexColor(0x3a2414); eyeCol = HexColor(0x5a3a1a);
                clothCol = HexColor(0xeee0b8); pantsCol = HexColor(0x7a5530); bootCol = HexColor(0x4a3220);
                break;
            case NpcStyle.Merchant:
                scaleH = 0.93f; bodyFat = 1.22f; gender = 0;
                skinCol = HexColor(0xe0a87e); hairCol = HexColor(0x1e1510); eyeCol = HexColor(0x4a3018);
                clothCol = HexColor(0xb0262e); pantsCol = HexColor(0x4a3524); bootCol = HexColor(0x2e2018);
                break;
            case NpcStyle.Wizard:
                scaleH = 1.0f; bodyFat = 0.95f; gender = 0;
                skinCol = HexColor(0xf4d2bc); hairCol = HexColor(0xe8e8f0); eyeCol = HexColor(0x68a8e0);
                clothCol = HexColor(0x2c3a9c); pantsCol = HexColor(0x2c3a9c); bootCol = HexColor(0x3a2a1e);
                break;
            case NpcStyle.Woman:
                scaleH = 0.92f; bodyFat = 0.9f; gender = 1;
                skinCol = HexColor(0xd9a07a); hairCol = HexColor(0x2a1810); eyeCol = HexColor(0x5a3a1a);
                clothCol = HexColor(0x2f8a5a); pantsCol = HexColor(0x2f8a5a); bootCol = HexColor(0x5a3a24);
                break;
            case NpcStyle.Woodcutter:
                scaleH = 1.02f; bodyFat = 1.15f; gender = 0;
                skinCol = HexColor(0xd69a6b); hairCol = HexColor(0x2a1a10); eyeCol = HexColor(0x4a3018);
                clothCol = HexColor(0x8a3a20); pantsCol = HexColor(0x3a2c20); bootCol = HexColor(0x201810);
                break;
            case NpcStyle.Hunter:
                scaleH = 1.0f; bodyFat = 0.98f; gender = 0;
                skinCol = HexColor(0xdfa075); hairCol = HexColor(0x4a321e); eyeCol = HexColor(0x2a5030);
                clothCol = HexColor(0x2d5a27); pantsCol = HexColor(0x4a3e2a); bootCol = HexColor(0x302214);
                break;
            case NpcStyle.Herbalist:
                scaleH = 0.88f; bodyFat = 0.95f; gender = 1;
                skinCol = HexColor(0xd9a07a); hairCol = HexColor(0xd0d0d8); eyeCol = HexColor(0x4a3a28);
                clothCol = HexColor(0x4f7a52); pantsCol = HexColor(0x4f7a52); bootCol = HexColor(0x3a2418);
                break;
            case NpcStyle.Fisherman:
                scaleH = 0.96f; bodyFat = 1.05f; gender = 0;
                skinCol = HexColor(0xd89565); hairCol = HexColor(0x3a2414); eyeCol = HexColor(0x2b4f6e);
                clothCol = HexColor(0x286088); pantsCol = HexColor(0x605040); bootCol = HexColor(0x202a35);
                break;
            case NpcStyle.VillageYouth:
            default:
                scaleH = 0.94f; bodyFat = 0.95f; gender = 0;
                skinCol = HexColor(0xf0b896); hairCol = HexColor(0x6a3818); eyeCol = HexColor(0x386040);
                clothCol = HexColor(0xd07028); pantsCol = HexColor(0x384050); bootCol = HexColor(0x3a2a1e);
                break;
        }

        visualRoot.transform.localScale = Vector3.one * scaleH;

        // Material Umum Stylized
        Material matSkin   = GetToonMat($"Npc_{style}_Skin", skinCol);
        Material matHair   = GetToonMat($"Npc_{style}_Hair", hairCol);
        Material matEye    = GetToonMat($"Npc_{style}_Eye", eyeCol);
        Material matCloth  = GetToonMat($"Npc_{style}_Cloth", clothCol);
        Material matPants  = GetToonMat($"Npc_{style}_Pants", pantsCol);
        Material matBoot   = GetToonMat($"Npc_{style}_Boot", bootCol);

        Material matSteel  = GetToonMat("Npc_Steel", HexColor(0xb6bcc8), 0.5f, 0.4f);
        Material matGold   = GetToonMat("Npc_Gold", HexColor(0xe8b830), 0.6f, 0.5f);
        Material matWood   = GetToonMat("Npc_Wood", HexColor(0x7a5230));
        Material matLea    = GetToonMat("Npc_Leather", HexColor(0x6a3e1e));
        Material matWhite  = GetToonMat("Npc_White", Color.white);
        Material matDark   = GetToonMat("Npc_Dark", HexColor(0x111111));
        Material matBlush  = GetToonMat("Npc_Blush", HexColor(0xf08a8a));
        Material matRed    = GetToonMat("Npc_Red", HexColor(0xc0262e));

        // ── 1. SENDI BADAN (TORSO / PELVIS) ──
        var torsoGo = new GameObject("Torso");
        torsoGo.transform.SetParent(visualRoot.transform, false);
        torsoGo.transform.localPosition = new Vector3(0, 0.75f, 0);

        // Profil Baju Badan (Lathe Revolve)
        var bodyProf = new (float r, float y)[]
        {
            (0.001f, 0f),
            (0.17f * bodyFat + 0.02f * gender, 0.05f),
            (0.145f * bodyFat - 0.01f * gender, 0.18f),
            (0.185f * bodyFat - 0.02f * gender, 0.38f),
            (0.2f * bodyFat - 0.02f * gender, 0.50f),
            (0.1f, 0.57f),
            (0.001f, 0.59f)
        };
        CreatePart("BodyMesh", torsoGo.transform, CreateRevolveMesh("Revolve_Torso", bodyProf, 20), matCloth, Vector3.zero, outline: true);

        // Leher
        CreatePart("Neck", torsoGo.transform, CreateCylinderMesh("Cyl_Neck", 0.065f, 0.06f, 0.14f), matSkin, new Vector3(0, 0.59f, 0));

        // ── 2. KEPALA & WAJAH (HEAD CHIBI/STYLIZED) ──
        var headGo = new GameObject("Head");
        headGo.transform.SetParent(torsoGo.transform, false);
        float headLocalY = 0.77f; // Setara HY = 1.52 di world lokal visual
        headGo.transform.localPosition = new Vector3(0, headLocalY, 0);

        // Tengkorak & Pipi Wajah
        CreatePart("Skull", headGo.transform, CreateSphereMesh("Sph_HeadBase"), matSkin, Vector3.zero, new Vector3(0.2f, 0.21f, 0.195f), outline: true);
        CreatePart("Cheeks", headGo.transform, CreateSphereMesh("Sph_Cheeks"), matSkin, new Vector3(0, -0.09f, 0.03f), new Vector3(0.145f, 0.13f, 0.15f));

        // Telinga, Mata, Alis, dan Pipi Merona (Kiri & Kanan)
        for (int s = -1; s <= 1; s += 2)
        {
            float side = (float)s;
            // Telinga
            CreatePart($"Ear_{side}", headGo.transform, CreateSphereMesh("Sph_Ear"), matSkin, new Vector3(side * 0.2f, 0, 0), new Vector3(0.03f, 0.05f, 0.035f));

            // Mata Besar Ekspresif (Sclera Putih)
            float eyeScale = 1f + gender * 0.08f;
            CreatePart($"EyeWhite_{side}", headGo.transform, CreateSphereMesh("Sph_EyeWhite"), matWhite,
                new Vector3(side * 0.077f, 0.01f, 0.165f), new Vector3(0.058f * eyeScale, 0.072f * eyeScale, 0.03f));

            // Iris Berwarna
            CreatePart($"EyeIris_{side}", headGo.transform, CreateSphereMesh("Sph_EyeIris"), matEye,
                new Vector3(side * 0.077f, 0.005f, 0.19f), new Vector3(0.038f * eyeScale, 0.052f * eyeScale, 0.012f));

            // Pupil Hitam
            CreatePart($"EyePupil_{side}", headGo.transform, CreateSphereMesh("Sph_EyePupil"), matDark,
                new Vector3(side * 0.077f, 0.005f, 0.197f), new Vector3(0.02f * eyeScale, 0.03f * eyeScale, 0.012f));

            // Kilauan Cahaya Mata (Anime Highlights)
            CreatePart($"EyeGlintLarge_{side}", headGo.transform, CreateSphereMesh("Sph_EyeGlint1"), matWhite,
                new Vector3(side * 0.077f - 0.013f, 0.028f, 0.205f), new Vector3(0.011f, 0.011f, 0.01f));
            CreatePart($"EyeGlintSmall_{side}", headGo.transform, CreateSphereMesh("Sph_EyeGlint2"), matWhite,
                new Vector3(side * 0.077f + 0.014f, -0.015f, 0.203f), new Vector3(0.006f, 0.006f, 0.008f));

            // Alis
            Material matBrow = (style == NpcStyle.Wizard) ? matWhite : matHair;
            CreatePart($"Eyebrow_{side}", headGo.transform, CreateSphereMesh("Sph_Eyebrow"), matBrow,
                new Vector3(side * 0.078f, 0.09f, 0.172f), new Vector3(0.044f, 0.009f, 0.012f),
                Quaternion.Euler(0, 0, -side * (style == NpcStyle.Wizard ? 8f : 12f)));

            // Pipi Merona Lembut
            CreatePart($"Blush_{side}", headGo.transform, CreateSphereMesh("Sph_Blush"), matBlush,
                new Vector3(side * 0.125f, -0.045f, 0.15f), new Vector3(0.034f, 0.02f, 0.01f));
        }

        // Hidung Imut
        CreatePart("Nose", headGo.transform, CreateSphereMesh("Sph_Nose"), matSkin, new Vector3(0, -0.03f, 0.2f), new Vector3(0.022f, 0.02f, 0.02f));

        // Senyum Ramah (Torus Arc)
        CreatePart("MouthSmile", headGo.transform, CreateTorusMesh("Torus_Mouth", 0.03f, 0.005f, Mathf.PI),
            (gender == 1) ? matRed : matDark, new Vector3(0, -0.083f, 0.185f), new Vector3(1f, 0.8f, 1f), Quaternion.Euler(0, 0, 180f));

        // ── 3. RAMBUT, TOPI, & AKSESORI KHAS TIAP KARAKTER ──
        BuildCharacterHeadwearAndHair(style, headGo.transform, matHair, matSteel, matGold, matWood, matWhite, matDark, matRed, matCloth);

        // ── 4. KAKI KIRI & KANAN (LEGS HIERARCHY) ──
        var leftLegGo  = BuildLeg(-1f, visualRoot.transform, style, matPants, matBoot, matSkin);
        var rightLegGo = BuildLeg( 1f, visualRoot.transform, style, matPants, matBoot, matSkin);

        // ── 5. TANGAN KIRI & KANAN (ARMS HIERARCHY) ──
        var leftArmGo  = BuildArm(-1f, torsoGo.transform, style, matCloth, matSkin, matSteel, matWood, matGold);
        var rightArmGo = BuildArm( 1f, torsoGo.transform, style, matCloth, matSkin, matSteel, matWood, matGold);

        // ── 6. AKSESORI KHAS BADAN (JUBAH, SABUK, RANSEL, DLL) ──
        BuildCharacterBodyAccessories(style, torsoGo.transform, bodyFat, matSteel, matGold, matCloth, matRed, matLea, matWhite);

        // ── 7. SAMBUNGKAN KE NPC MOVER (AUTONOMOUS PATROL & ANIMATION) ──
        var mover = root.AddComponent<NpcMover>();
        mover.visualRoot     = visualRoot.transform;
        mover.torsoTransform = torsoGo.transform;
        mover.headTransform  = headGo.transform;
        mover.leftLeg        = leftLegGo.transform;
        mover.rightLeg       = rightLegGo.transform;
        mover.leftArm        = leftArmGo.transform;
        mover.rightArm       = rightArmGo.transform;

        return root;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PERAKITAN KAKI (LEGS)
    // ─────────────────────────────────────────────────────────────────────
    static GameObject BuildLeg(float side, Transform parent, NpcStyle style, Material matPants, Material matBoot, Material matSkin)
    {
        var hipGo = new GameObject(side < 0 ? "LeftLeg" : "RightLeg");
        hipGo.transform.SetParent(parent, false);
        hipGo.transform.localPosition = new Vector3(side * 0.085f, 0.8f, 0);

        Material legMat = (style == NpcStyle.Wizard || style == NpcStyle.Woman) ? matSkin : matPants;

        // Paha (Upper Leg)
        Vector3 hipPos = Vector3.zero;
        Vector3 kneePos = new Vector3(0, -0.37f, 0.01f);
        CreateLimb("UpperLeg", hipGo.transform, hipPos, kneePos, 0.078f, 0.062f, legMat);

        // Betis (Lower Leg)
        Vector3 anklePos = new Vector3(0, -0.7f, 0);
        CreateLimb("LowerLeg", hipGo.transform, kneePos, anklePos, 0.062f, 0.052f, legMat);

        // Sepatu Boot & Telapak Kaki
        CreatePart("BootShaft", hipGo.transform, CreateCylinderMesh("Cyl_Boot", 0.062f, 0.066f, 0.22f), matBoot, new Vector3(0, -0.61f, 0));
        CreatePart("ShoeFoot", hipGo.transform, CreateSphereMesh("Sph_Foot"), matBoot, new Vector3(0, -0.745f, 0.05f), new Vector3(0.075f, 0.062f, 0.13f));

        return hipGo;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  PERAKITAN TANGAN (ARMS)
    // ─────────────────────────────────────────────────────────────────────
    static GameObject BuildArm(float side, Transform parent, NpcStyle style, Material matCloth, Material matSkin, Material matSteel, Material matWood, Material matGold)
    {
        var shoulderGo = new GameObject(side < 0 ? "LeftArm" : "RightArm");
        shoulderGo.transform.SetParent(parent, false);
        // Pivot di bahu (Y dihitung relatif terhadap Torso)
        shoulderGo.transform.localPosition = new Vector3(side * 0.235f, 0.47f, 0);

        Material sleeveMat = (style == NpcStyle.Woman) ? matSkin : matCloth;

        // Bola Sendi Bahu
        CreatePart("ShoulderJoint", shoulderGo.transform, CreateSphereMesh("Sph_Shoulder"), matCloth, Vector3.zero, Vector3.one * 0.062f);

        // Lengan Atas (Upper Arm)
        Vector3 elbowPos = new Vector3(side * 0.05f, -0.27f, 0.02f);
        CreateLimb("UpperArm", shoulderGo.transform, Vector3.zero, elbowPos, 0.06f, 0.052f, matCloth);

        // Lengan Bawah (Forearm)
        Vector3 wristPos = new Vector3(side * 0.04f, -0.52f, 0.07f);
        CreateLimb("Forearm", shoulderGo.transform, elbowPos, wristPos, 0.05f, 0.04f, sleeveMat);

        // Telapak Tangan & Ibu Jari
        Vector3 handPos = wristPos + new Vector3(0, -0.06f, 0.015f);
        CreatePart("HandPalm", shoulderGo.transform, CreateSphereMesh("Sph_Hand"), matSkin, handPos, new Vector3(0.05f, 0.06f, 0.045f));
        CreatePart("Thumb", shoulderGo.transform, CreateSphereMesh("Sph_Thumb"), matSkin, handPos + new Vector3(-side * 0.035f, 0.01f, 0.035f), new Vector3(0.02f, 0.035f, 0.02f));

        // ── SENJATA / PROP TANGAN SPESIAL ──
        if (style == NpcStyle.Farmer && side < 0)
        {
            // Garpu Rumput Petani (Pitchfork) di tangan kiri
            var forkGo = new GameObject("Pitchfork");
            forkGo.transform.SetParent(shoulderGo.transform, false);
            forkGo.transform.localPosition = handPos;

            // Gagang Kayu
            CreateLimb("Shaft", forkGo.transform, new Vector3(0, -0.4f, 0), new Vector3(0, 1.05f, 0), 0.016f, 0.016f, matWood);
            // Palang Besi
            CreateLimb("Crossbar", forkGo.transform, new Vector3(-0.06f, 1.05f, 0), new Vector3(0.06f, 1.05f, 0), 0.012f, 0.012f, matSteel);
            // 3 Taring Besi
            for (float tx = -0.055f; tx <= 0.056f; tx += 0.055f)
                CreateLimb($"Tine_{tx:F2}", forkGo.transform, new Vector3(tx, 1.05f, 0), new Vector3(tx, 1.25f, 0), 0.01f, 0.005f, matSteel);
        }
        else if (style == NpcStyle.Wizard && side > 0)
        {
            // Tongkat Sihir Kristal (Wizard Staff) di tangan kanan
            var staffGo = new GameObject("MagicStaff");
            staffGo.transform.SetParent(shoulderGo.transform, false);
            staffGo.transform.localPosition = handPos;

            // Tongkat Kayu Tua
            CreateLimb("StaffWood", staffGo.transform, new Vector3(0, -0.6f, 0), new Vector3(0, 1.25f, 0), 0.022f, 0.02f, matWood);
            CreatePart("StaffCap", staffGo.transform, CreateSphereMesh("Sph_StaffCap"), matWood, new Vector3(0, 1.28f, 0), new Vector3(0.07f, 0.05f, 0.07f));

            // Kristal Sihir Bercahaya Cyan
            Material matCrystal = GetToonMat("Npc_Crystal", HexColor(0x5ad2ff), 0.2f, 0.9f);
            matCrystal.EnableKeyword("_EMISSION");
            matCrystal.SetColor("_EmissionColor", HexColor(0x5ad2ff) * 2.5f);

            var crystal = CreatePart("MagicCrystal", staffGo.transform, CreateSphereMesh("Sph_Crystal"), matCrystal, new Vector3(0, 1.4f, 0), new Vector3(0.08f, 0.12f, 0.08f));

            // Lampu Pendar Mistis
            var lightGo = new GameObject("CrystalLight");
            lightGo.transform.SetParent(crystal.transform, false);
            var ptLight = lightGo.AddComponent<Light>();
            ptLight.type = LightType.Point;
            ptLight.color = HexColor(0x5ad2ff);
            ptLight.range = 5.5f;
            ptLight.intensity = 2.0f;
        }
        else if (style == NpcStyle.Guard && side > 0)
        {
            // Pedang Ksatria Terhunus / Siaga di tangan kanan
            var swordGo = new GameObject("KnightSword");
            swordGo.transform.SetParent(shoulderGo.transform, false);
            swordGo.transform.localPosition = handPos;
            swordGo.transform.localRotation = Quaternion.Euler(30f, 0, 0);

            // Bilah Baja
            CreatePart("Blade", swordGo.transform, CreateBoxMesh("Box_Blade", 0.045f, 0.65f, 0.015f), matSteel, new Vector3(0, 0.35f, 0));
            // Gagang Silang Emas
            CreatePart("Crossguard", swordGo.transform, CreateBoxMesh("Box_Guard", 0.18f, 0.028f, 0.028f), matGold, Vector3.zero);
            // Pegangan Kulit & Pommel Emas
            CreatePart("Grip", swordGo.transform, CreateCylinderMesh("Cyl_Grip", 0.018f, 0.018f, 0.14f), matWood, new Vector3(0, -0.08f, 0));
            CreatePart("Pommel", swordGo.transform, CreateSphereMesh("Sph_Pommel"), matGold, new Vector3(0, -0.16f, 0), Vector3.one * 0.035f);
        }

        return shoulderGo;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  TOPI, RAMBUT, & AKSESORI KEPALA KHAS
    // ─────────────────────────────────────────────────────────────────────
    static void BuildCharacterHeadwearAndHair(NpcStyle style, Transform headParent, Material matHair,
        Material matSteel, Material matGold, Material matWood, Material matWhite, Material matDark, Material matRed, Material matCloth)
    {
        Vector3 hairScale = new Vector3(0.215f, 0.228f, 0.212f);

        switch (style)
        {
            case NpcStyle.Guard:
                // Rambut jambang samping
                CreatePart("HairLeft",  headParent, CreateConeMesh("Cone_SideHair", 0.05f, 0.1f), matHair, new Vector3(-0.2f, -0.02f, -0.02f), Vector3.one, Quaternion.Euler(180f, 0, 0));
                CreatePart("HairRight", headParent, CreateConeMesh("Cone_SideHair", 0.05f, 0.1f), matHair, new Vector3( 0.2f, -0.02f, -0.02f), Vector3.one, Quaternion.Euler(180f, 0, 0));

                // Helm Baja Pengawal (Steel Helmet)
                CreatePart("HelmetDome", headParent, CreateSphereMesh("Sph_Helmet"), matSteel, new Vector3(0, 0.02f, -0.01f), new Vector3(0.235f, 0.24f, 0.232f));
                // Pelindung Hidung & Dahi
                CreatePart("HelmetNoseGuard", headParent, CreateBoxMesh("Box_NoseGuard", 0.03f, 0.14f, 0.025f), matSteel, new Vector3(0, 0, 0.222f));
                // Lingkar Alis Emas
                CreatePart("HelmetBrowGold", headParent, CreateTorusMesh("Torus_Helmbrow", 0.225f, 0.016f), matGold, new Vector3(0, 0.1f, 0), new Vector3(1f, 1.04f, 1f), Quaternion.Euler(85f, 0, 0));
                // Bulu Merah Jambul Ksatria (Red Feather Plume)
                CreatePart("HelmetPlume", headParent, CreateConeMesh("Cone_Plume", 0.05f, 0.22f), matRed, new Vector3(0, 0.34f, -0.02f), Vector3.one, Quaternion.Euler(-20f, 0, 0));
                CreatePart("PlumeBaseGold", headParent, CreateSphereMesh("Sph_PlumeBase"), matGold, new Vector3(0, 0.24f, 0), Vector3.one * 0.014f);
                break;

            case NpcStyle.Farmer:
                // Rambut Belakang & Poni Petani
                CreatePart("HairCap", headParent, CreateSphereMesh("Sph_HairCap"), matHair, new Vector3(0, 0.02f, -0.012f), hairScale);
                CreateFringeBangs(headParent, 5, 0.14f, 0.17f, 0.1f, matHair);

                // Topi Caping Jerami Lebar (Straw Hat)
                Material matStraw = GetToonMat("Npc_Straw", HexColor(0xe2bc62));
                CreatePart("HatBrim", headParent, CreateCylinderMesh("Cyl_HatBrim", 0.34f, 0.34f, 0.02f), matStraw, new Vector3(0, 0.17f, 0));
                CreatePart("HatCrown", headParent, CreateSphereMesh("Sph_HatCrown"), matStraw, new Vector3(0, 0.17f, 0), new Vector3(0.21f, 0.19f, 0.21f));
                // Pita Merah Topi
                CreatePart("HatRibbon", headParent, CreateTorusMesh("Torus_HatRibbon", 0.205f, 0.015f), matRed, new Vector3(0, 0.19f, 0), Vector3.one, Quaternion.Euler(90f, 0, 0));

                // Kumis Petani Ramah
                CreatePart("MustacheL", headParent, CreateSphereMesh("Sph_Mustache"), matHair, new Vector3(-0.03f, -0.06f, 0.2f), new Vector3(0.04f, 0.014f, 0.015f), Quaternion.Euler(0, 0, 12f));
                CreatePart("MustacheR", headParent, CreateSphereMesh("Sph_Mustache"), matHair, new Vector3( 0.03f, -0.06f, 0.2f), new Vector3(0.04f, 0.014f, 0.015f), Quaternion.Euler(0, 0, -12f));
                break;

            case NpcStyle.Merchant:
                // Rambut Belakang & Poni
                CreatePart("HairCap", headParent, CreateSphereMesh("Sph_HairCap"), matHair, new Vector3(0, 0.015f, -0.012f), hairScale);
                CreateFringeBangs(headParent, 5, 0.13f, 0.17f, 0.09f, matHair);

                // Baret Beludru Pedagang (Velvet Beret)
                Material matBeret = GetToonMat("Npc_Beret", HexColor(0x5a3a22));
                CreatePart("BeretBrim", headParent, CreateCylinderMesh("Cyl_BeretBrim", 0.25f, 0.25f, 0.025f), matBeret, new Vector3(0, 0.12f, 0));
                CreatePart("BeretDome", headParent, CreateSphereMesh("Sph_BeretDome"), matBeret, new Vector3(0, 0.17f, 0), new Vector3(0.2f, 0.12f, 0.2f));
                CreatePart("BeretGoldRim", headParent, CreateTorusMesh("Torus_BeretGold", 0.19f, 0.014f), matGold, new Vector3(0, 0.14f, 0), Vector3.one, Quaternion.Euler(90f, 0, 0));
                // Bulu Unggas Putih di Baret
                CreatePart("BeretFeather", headParent, CreateConeMesh("Cone_Feather", 0.015f, 0.32f), matWhite, new Vector3(0.16f, 0.2f, 0), Vector3.one, Quaternion.Euler(0, 0, -60f));

                // Janggut & Kumis Tebal Saudagar
                CreatePart("MerchantBeard", headParent, CreateSphereMesh("Sph_MerchantBeard"), matHair, new Vector3(0, -0.15f, 0.1f), new Vector3(0.13f, 0.09f, 0.08f));
                CreatePart("MustacheCenter", headParent, CreateSphereMesh("Sph_MustacheCenter"), matHair, new Vector3(0, -0.065f, 0.19f), new Vector3(0.06f, 0.025f, 0.02f));
                break;

            case NpcStyle.Wizard:
                // Rambut Putih Panjang Dukun
                CreatePart("HairCap", headParent, CreateSphereMesh("Sph_HairCap"), matWhite, new Vector3(0, 0.02f, -0.012f), hairScale);
                CreatePart("HairBackFlow", headParent, CreateSphereMesh("Sph_HairBackFlow"), matWhite, new Vector3(0, -0.2f, -0.12f), new Vector3(0.22f, 0.38f, 0.12f));

                // Janggut Putih Panjang Menjuntai
                CreatePart("LongBeard", headParent, CreateConeMesh("Cone_LongBeard", 0.17f, 0.7f), matWhite, new Vector3(0, -0.38f, 0.12f), Vector3.one, Quaternion.Euler(180f, 0, 0));
                CreatePart("BeardTop", headParent, CreateSphereMesh("Sph_BeardTop"), matWhite, new Vector3(0, -0.07f, 0.19f), new Vector3(0.1f, 0.07f, 0.05f));

                // Topi Kerucut Sihir Melengkung (Wizard Hat)
                Material matHatBlue = GetToonMat("Npc_HatBlue", HexColor(0x232d78));
                CreatePart("HatBrim", headParent, CreateCylinderMesh("Cyl_HatBrim", 0.34f, 0.34f, 0.02f), matHatBlue, new Vector3(0, 0.12f, 0));
                CreatePart("HatCone1", headParent, CreateConeMesh("Cone_Hat1", 0.21f, 0.5f), matHatBlue, new Vector3(0, 0.4f, -0.02f), Vector3.one, Quaternion.Euler(-7f, 0, 0));
                CreatePart("HatCrookedTip", headParent, CreateConeMesh("Cone_HatTip", 0.1f, 0.35f), matHatBlue, new Vector3(0, 0.78f, -0.12f), Vector3.one, Quaternion.Euler(-32f, 0, 0));
                CreatePart("HatGoldBand", headParent, CreateTorusMesh("Torus_HatGold", 0.205f, 0.016f), matGold, new Vector3(0, 0.17f, 0), Vector3.one, Quaternion.Euler(83f, 0, 0));

                // Bintang-Bintang Emas di Topi
                CreatePart("Star1", headParent, CreateSphereMesh("Sph_Star"), matGold, new Vector3( 0.1f,  0.35f, 0.2f),  Vector3.one * 0.016f);
                CreatePart("Star2", headParent, CreateSphereMesh("Sph_Star"), matGold, new Vector3(-0.08f, 0.5f,  0.17f), Vector3.one * 0.016f);
                CreatePart("Star3", headParent, CreateSphereMesh("Sph_Star"), matGold, new Vector3( 0.06f, 0.65f, 0.12f), Vector3.one * 0.016f);
                break;

            case NpcStyle.Woman:
            default:
                // Rambut Belakang & Poni Anggun
                CreatePart("HairCap", headParent, CreateSphereMesh("Sph_HairCap"), matHair, new Vector3(0, 0.02f, -0.012f), hairScale);
                CreateFringeBangs(headParent, 7, 0.15f, 0.165f, 0.14f, matHair);
                CreatePart("HairBack", headParent, CreateSphereMesh("Sph_HairBack"), matHair, new Vector3(0, -0.15f, -0.12f), new Vector3(0.22f, 0.34f, 0.11f));

                // Rambut Kepang Panjang Jatuh ke Depan Dada
                for (int i = 0; i < 7; i++)
                {
                    float rad = 0.05f - 0.003f * i;
                    Vector3 braidPos = new Vector3(0.2f, -0.2f - i * 0.085f, 0.02f + i * 0.003f);
                    CreatePart($"Braid_{i}", headParent, CreateSphereMesh("Sph_Braid"), matHair, braidPos, new Vector3(rad, 0.06f, rad));
                }

                // Mahkota Bandana Emas di Dahi
                CreatePart("HeadbandGold", headParent, CreateTorusMesh("Torus_Headband", 0.205f, 0.012f), matGold, new Vector3(0, 0.1f, 0), new Vector3(1f, 1.05f, 1f), Quaternion.Euler(70f, 0, 0));
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  AKSESORI BADAN KHAS (JUBAH, PERISAI, RANSEL, CELEMEK)
    // ─────────────────────────────────────────────────────────────────────
    static void BuildCharacterBodyAccessories(NpcStyle style, Transform torsoParent, float bodyFat,
        Material matSteel, Material matGold, Material matCloth, Material matRed, Material matLea, Material matWhite)
    {
        switch (style)
        {
            case NpcStyle.Guard:
                // Zirah Dada Baja (Steel Breastplate)
                CreatePart("Breastplate", torsoParent, CreateBoxMesh("Box_Armor", 0.36f, 0.38f, 0.24f), matSteel, new Vector3(0, 0.28f, 0.02f));
                // Sabuk Kulit & Gesper Emas
                CreatePart("BeltLeather", torsoParent, CreateTorusMesh("Torus_Belt", 0.18f, 0.022f), matLea, new Vector3(0, 0.1f, 0), new Vector3(1f, 0.8f, 1f), Quaternion.Euler(90f, 0, 0));
                CreatePart("BeltBuckleGold", torsoParent, CreateBoxMesh("Box_Buckle", 0.05f, 0.05f, 0.02f), matGold, new Vector3(0, 0.1f, 0.15f));
                // Pelindung Bahu Baja (Pauldrons)
                CreatePart("PauldronL", torsoParent, CreateSphereMesh("Sph_Pauldron"), matSteel, new Vector3(-0.255f, 0.52f, 0), new Vector3(0.115f, 0.08f, 0.115f));
                CreatePart("PauldronR", torsoParent, CreateSphereMesh("Sph_Pauldron"), matSteel, new Vector3( 0.255f, 0.52f, 0), new Vector3(0.115f, 0.08f, 0.115f));
                // Jubah Merah Ksatria Berkibar di Punggung
                CreatePart("CapeRed", torsoParent, CreateBoxMesh("Box_Cape", 0.48f, 0.85f, 0.025f), matRed, new Vector3(0, 0.22f, -0.18f), Vector3.one, Quaternion.Euler(6f, 0, 0));
                break;

            case NpcStyle.Farmer:
                // Celemek Kerja Kulit Cokelat (Farmer Apron)
                Material matApron = GetToonMat("Npc_Apron", HexColor(0x8a5a30));
                CreatePart("ApronFront", torsoParent, CreateBoxMesh("Box_Apron", 0.32f, 0.44f, 0.02f), matApron, new Vector3(0, 0.22f, 0.14f));
                // Sabuk Tali & Tas Perkakas
                CreatePart("ToolBelt", torsoParent, CreateTorusMesh("Torus_ToolBelt", 0.17f, 0.018f), matLea, new Vector3(0, 0.15f, 0), new Vector3(1f, 0.8f, 1f), Quaternion.Euler(90f, 0, 0));
                CreatePart("SeedPouch", torsoParent, CreateBoxMesh("Box_Pouch", 0.16f, 0.12f, 0.1f), matApron, new Vector3(0.18f, 0.06f, 0.06f));
                break;

            case NpcStyle.Merchant:
                // Mantel Beludru Mewah & Garis Emas Bawah
                CreatePart("VelvetCoatTrim", torsoParent, CreateTorusMesh("Torus_CoatTrim", 0.27f * bodyFat, 0.025f), matGold, new Vector3(0, -0.38f, 0), new Vector3(1f, 0.82f, 1f), Quaternion.Euler(90f, 0, 0));
                // Sabuk Pedagang & Kantong Emas Tebal
                CreatePart("MerchantBelt", torsoParent, CreateTorusMesh("Torus_MBelt", 0.22f, 0.02f), matLea, new Vector3(0, 0.18f, 0), new Vector3(1f, 0.8f, 1f), Quaternion.Euler(90f, 0, 0));
                CreatePart("GoldBuckle", torsoParent, CreateBoxMesh("Box_MBuckle", 0.06f, 0.05f, 0.02f), matGold, new Vector3(0, 0.18f, 0.2f));
                CreatePart("MoneyBagGold", torsoParent, CreateSphereMesh("Sph_MoneyBag"), matGold, new Vector3(0.27f, 0.06f, 0.1f), new Vector3(0.07f, 0.08f, 0.06f));
                // Ransel Besar Pedagang (Expedition Merchant Backpack)
                Material matPack = GetToonMat("Npc_Backpack", HexColor(0xc9a56a));
                CreatePart("Backpack", torsoParent, CreateSphereMesh("Sph_Backpack"), matPack, new Vector3(0, 0.25f, -0.28f), new Vector3(0.24f, 0.32f, 0.16f));
                CreatePart("Bedroll", torsoParent, CreateTorusMesh("Torus_Bedroll", 0.1f, 0.025f), matLea, new Vector3(0, 0.46f, -0.16f), Vector3.one, Quaternion.Euler(20f, 0, 0));
                break;

            case NpcStyle.Wizard:
                // Jubah Penyihir Panjang & Sabuk Emas
                CreatePart("RobeTrimGold", torsoParent, CreateTorusMesh("Torus_RobeGold", 0.37f * bodyFat, 0.02f), matGold, new Vector3(0, -0.68f, 0), new Vector3(1f, 0.82f, 1f), Quaternion.Euler(90f, 0, 0));
                CreatePart("WaistGold", torsoParent, CreateTorusMesh("Torus_WaistGold", 0.19f * bodyFat, 0.02f), matGold, new Vector3(0, 0.16f, 0), new Vector3(1f, 0.8f, 1f), Quaternion.Euler(90f, 0, 0));
                break;

            case NpcStyle.Woman:
            default:
                // Gaun Anggun & Sabuk Emas Bawah
                CreatePart("DressHemGold", torsoParent, CreateTorusMesh("Torus_DressHem", 0.3f, 0.016f), matGold, new Vector3(0, -0.42f, 0), new Vector3(1f, 0.82f, 1f), Quaternion.Euler(90f, 0, 0));
                // Celemek Putih (White Apron)
                Material matApronWhite = GetToonMat("Npc_ApronWhite", HexColor(0xf0e6cc));
                CreatePart("ApronWhite", torsoParent, CreateBoxMesh("Box_ApronWhite", 0.3f, 0.5f, 0.02f), matApronWhite, new Vector3(0, -0.12f, 0.2f), Vector3.one, Quaternion.Euler(3f, 0, 0));
                // Sabuk Pinggang & Tas Selempang Kulit
                CreatePart("WaistBelt", torsoParent, CreateTorusMesh("Torus_WBelt", 0.15f, 0.018f), matLea, new Vector3(0, 0.16f, 0), new Vector3(1f, 0.75f, 1f), Quaternion.Euler(90f, 0, 0));
                CreatePart("SatchelBag", torsoParent, CreateBoxMesh("Box_Satchel", 0.18f, 0.15f, 0.08f), matLea, new Vector3(0.24f, 0.03f, 0.05f), Vector3.one, Quaternion.Euler(0, 0, 6f));
                CreatePart("SatchelClaspGold", torsoParent, CreateSphereMesh("Sph_SatchelClasp"), matGold, new Vector3(0.24f, 0.03f, 0.1f), Vector3.one * 0.035f);
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HELPER: RAMBUT PONI (FRINGE BANGS)
    // ─────────────────────────────────────────────────────────────────────
    static void CreateFringeBangs(Transform headParent, int n, float y, float z, float len, Material matHair)
    {
        for (int i = 0; i < n; i++)
        {
            float x = ((i / (float)(n - 1)) - 0.5f) * 0.3f;
            Vector3 pos = new Vector3(x, y - Mathf.Abs(x) * 0.25f, z - Mathf.Abs(x) * 0.3f);
            Quaternion rot = Quaternion.Euler(160f, 0, -x * 120f);
            CreatePart($"Fringe_{i}", headParent, CreateConeMesh("Cone_Fringe", 0.045f, len, 12), matHair, pos, Vector3.one, rot);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HELPER: PEMBUAT ANGGOTA TUBUH (LIMB CYLINDER + JOINT SPHERES)
    // ─────────────────────────────────────────────────────────────────────
    static void CreateLimb(string name, Transform parent, Vector3 a, Vector3 b, float r1, float r2, Material mat)
    {
        Vector3 delta = b - a;
        float len = delta.magnitude;
        if (len < 0.001f) return;

        var limbGo = new GameObject(name);
        limbGo.transform.SetParent(parent, false);
        limbGo.transform.localPosition = (a + b) * 0.5f;
        limbGo.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);

        // Silinder Batang
        var mesh = CreateCylinderMesh($"Cyl_{r1:F2}_{r2:F2}_{len:F2}", r1, r2, len, 16);
        var mf = limbGo.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = limbGo.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        // Bola Penutup Sendi Ujung
        CreatePart("JointA", parent, CreateSphereMesh("Sph_Joint"), mat, a, Vector3.one * r1);
        CreatePart("JointB", parent, CreateSphereMesh("Sph_Joint"), mat, b, Vector3.one * r2);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HELPER: PEMBUAT BAGIAN MESH
    // ─────────────────────────────────────────────────────────────────────
    static GameObject CreatePart(string name, Transform parent, Mesh mesh, Material mat, Vector3 localPos, Vector3? scale = null, Quaternion? rot = null, bool outline = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = rot ?? Quaternion.identity;
        go.transform.localScale    = scale ?? Vector3.one;

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;

        if (outline && mesh != null)
        {
            AddOutline(go, mesh);
        }

        return go;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  GENERATOR GEOMETRI PRIMITIF
    // ─────────────────────────────────────────────────────────────────────
    public static Mesh CreateRevolveMesh(string key, (float r, float y)[] prof, int seg = 20)
    {
        if (meshCache.TryGetValue(key, out var cached) && cached) return cached;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();
        var rows = new List<(bool isPoint, int startIdx)>();

        for (int p = 0; p < prof.Length; p++)
        {
            float r = prof[p].r;
            float y = prof[p].y;

            if (r < 0.003f)
            {
                vertices.Add(new Vector3(0, y, 0));
                rows.Add((true, vertices.Count - 1));
            }
            else
            {
                int start = vertices.Count;
                for (int i = 0; i < seg; i++)
                {
                    float angle = (i / (float)seg) * Mathf.PI * 2f;
                    vertices.Add(new Vector3(r * Mathf.Cos(angle), y, r * Mathf.Sin(angle)));
                }
                rows.Add((false, start));
            }
        }

        for (int p = 0; p < rows.Count - 1; p++)
        {
            var (isP0, s0) = rows[p];
            var (isP1, s1) = rows[p + 1];

            for (int k = 0; k < seg; k++)
            {
                int k2 = (k + 1) % seg;
                if (!isP0 && !isP1)
                {
                    triangles.Add(s0 + k);
                    triangles.Add(s1 + k);
                    triangles.Add(s1 + k2);

                    triangles.Add(s0 + k);
                    triangles.Add(s1 + k2);
                    triangles.Add(s0 + k2);
                }
                else if (isP0 && !isP1)
                {
                    triangles.Add(s0);
                    triangles.Add(s1 + k);
                    triangles.Add(s1 + k2);
                }
                else if (!isP0 && isP1)
                {
                    triangles.Add(s0 + k2);
                    triangles.Add(s0 + k);
                    triangles.Add(s1);
                }
            }
        }

        var mesh = new Mesh { name = key };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        SaveMeshAsset(mesh, key);
        meshCache[key] = mesh;
        return mesh;
    }

    public static Mesh CreateSphereMesh(string key, float tmax = Mathf.PI, int rings = 14, int seg = 20)
    {
        if (meshCache.TryGetValue(key, out var cached) && cached) return cached;

        var prof = new (float r, float y)[rings + 1];
        for (int i = 0; i <= rings; i++)
        {
            float t = tmax * (1f - (i / (float)rings));
            prof[i] = (Mathf.Sin(t), Mathf.Cos(t));
        }

        return CreateRevolveMesh(key, prof, seg);
    }

    public static Mesh CreateCylinderMesh(string key, float r1, float r2, float h, int seg = 16)
    {
        if (meshCache.TryGetValue(key, out var cached) && cached) return cached;
        var prof = new (float r, float y)[]
        {
            (0.001f, -h * 0.5f),
            (r1,     -h * 0.5f),
            (r2,      h * 0.5f),
            (0.001f,  h * 0.5f)
        };
        return CreateRevolveMesh(key, prof, seg);
    }

    public static Mesh CreateConeMesh(string key, float r, float h, int seg = 16)
    {
        if (meshCache.TryGetValue(key, out var cached) && cached) return cached;
        var prof = new (float r, float y)[]
        {
            (0.001f, -h * 0.5f),
            (r,      -h * 0.5f),
            (0.001f,  h * 0.5f)
        };
        return CreateRevolveMesh(key, prof, seg);
    }

    public static Mesh CreateTorusMesh(string key, float R, float r, float arc = Mathf.PI * 2f, int M = 24, int N = 8)
    {
        if (meshCache.TryGetValue(key, out var cached) && cached) return cached;

        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        int seg = M + (arc < Mathf.PI * 2f - 1e-4f ? 1 : 0);
        for (int i = 0; i < seg; i++)
        {
            float a = arc * i / M;
            for (int j = 0; j < N; j++)
            {
                float b = Mathf.PI * 2f * j / N;
                float px = (R + r * Mathf.Cos(b)) * Mathf.Cos(a);
                float pz = (R + r * Mathf.Cos(b)) * Mathf.Sin(a);
                float py = r * Mathf.Sin(b);
                vertices.Add(new Vector3(px, py, pz));
            }
        }

        for (int i = 0; i < M; i++)
        {
            int i2 = (arc >= Mathf.PI * 2f - 1e-4f) ? (i + 1) % seg : i + 1;
            for (int j = 0; j < N; j++)
            {
                int j2 = (j + 1) % N;
                int v0 = i * N + j;
                int v1 = i2 * N + j;
                int v2 = i2 * N + j2;
                int v3 = i * N + j2;

                triangles.Add(v0); triangles.Add(v1); triangles.Add(v2);
                triangles.Add(v0); triangles.Add(v2); triangles.Add(v3);
            }
        }

        var mesh = new Mesh { name = key };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        SaveMeshAsset(mesh, key);
        meshCache[key] = mesh;
        return mesh;
    }

    public static Mesh CreateBoxMesh(string key, float w, float h, float d)
    {
        if (meshCache.TryGetValue(key, out var cached) && cached) return cached;

        float hw = w * 0.5f, hh = h * 0.5f, hd = d * 0.5f;

        Vector3[] v = {
            // Front
            new Vector3(-hw, -hh,  hd), new Vector3( hw, -hh,  hd), new Vector3( hw,  hh,  hd), new Vector3(-hw,  hh,  hd),
            // Back
            new Vector3( hw, -hh, -hd), new Vector3(-hw, -hh, -hd), new Vector3(-hw,  hh, -hd), new Vector3( hw,  hh, -hd),
            // Top
            new Vector3(-hw,  hh,  hd), new Vector3( hw,  hh,  hd), new Vector3( hw,  hh, -hd), new Vector3(-hw,  hh, -hd),
            // Bottom
            new Vector3(-hw, -hh, -hd), new Vector3( hw, -hh, -hd), new Vector3( hw, -hh,  hd), new Vector3(-hw, -hh,  hd),
            // Left
            new Vector3(-hw, -hh, -hd), new Vector3(-hw, -hh,  hd), new Vector3(-hw,  hh,  hd), new Vector3(-hw,  hh, -hd),
            // Right
            new Vector3( hw, -hh,  hd), new Vector3( hw, -hh, -hd), new Vector3( hw,  hh, -hd), new Vector3( hw,  hh,  hd)
        };

        int[] t = {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23
        };

        var mesh = new Mesh { name = key };
        mesh.vertices = v;
        mesh.triangles = t;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        SaveMeshAsset(mesh, key);
        meshCache[key] = mesh;
        return mesh;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  MATERIAL & ASSET HELPERS
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Material Toon Stylized URP — mendekati efek Shader-to-RGB Blender EEVEE.
    /// Menggunakan URP/Lit dengan emisi untuk tampilan flat + highlight.
    /// </summary>
    public static Material GetToonMat(string matName, Color col, float metal = 0f, float rough = 0.8f)
    {
        if (matCache.TryGetValue(matName, out var cached) && cached) return cached;

#if UNITY_EDITOR
        string path = $"Assets/_Project/Art/NpcMaterials/{matName}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) { matCache[matName] = existing; return existing; }
        EnsureDir("Assets/_Project/Art/NpcMaterials");
#endif

        // Coba pakai shader toon khusus dulu, fallback ke URP Lit
        Shader sh = Shader.Find("Universal Render Pipeline/Lit")
                 ?? Shader.Find("Standard");

        var mat = new Material(sh) { name = matName };

        // Warna dasar
        if (mat.HasProperty("_BaseColor"))   mat.SetColor("_BaseColor", col);
        else if (mat.HasProperty("_Color"))  mat.SetColor("_Color", col);

        // Metallic & Roughness
        if (mat.HasProperty("_Metallic"))    mat.SetFloat("_Metallic", metal);
        if (mat.HasProperty("_Smoothness"))  mat.SetFloat("_Smoothness", 1f - rough);

        // Emisi ringan agar warna tidak terlalu gelap (meniru Emission Strength 1 Blender)
        Color emitCol = col * 0.35f;
        emitCol.a = 1f;
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emitCol);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

#if UNITY_EDITOR
        AssetDatabase.CreateAsset(mat, $"Assets/_Project/Art/NpcMaterials/{matName}.mat");
#endif
        matCache[matName] = mat;
        return mat;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  OUTLINE — Inverted Hull (Hardware Backface Render)
    // ─────────────────────────────────────────────────────────────────────
    private const float OUTLINE_THICKNESS = 0.008f; // 8mm — sama dengan Blender OUTLINE=0.008

    /// <summary>
    /// Tambahkan outline hitam menggunakan teknik inverted hull:
    /// Mesh yang sama di-render dengan Cull Front (hanya permukaan belakang yang terlihat)
    /// dan diperbesar sedikit untuk menghasilkan garis tepi siluet hitam kartun.
    /// </summary>
    public static void AddOutline(GameObject target, Mesh sourceMesh)
    {
        Material outlineMat = GetOutlineMat();
        if (outlineMat == null || sourceMesh == null) return;

        var outlineGo = new GameObject("Outline");
        outlineGo.transform.SetParent(target.transform, false);

        var mf = outlineGo.AddComponent<MeshFilter>();
        mf.sharedMesh = sourceMesh;

        var mr = outlineGo.AddComponent<MeshRenderer>();
        mr.sharedMaterial = outlineMat;

        // Skala sedikit diperbesar agar tampak sebagai tepi siluet
        float scale = 1f + OUTLINE_THICKNESS * 4.5f;
        outlineGo.transform.localScale = Vector3.one * scale;
    }

    static Material GetOutlineMat()
    {
        const string key = "Npc_Outline";
        if (matCache.TryGetValue(key, out var cached) && cached) return cached;

#if UNITY_EDITOR
        string path = $"Assets/_Project/Art/NpcMaterials/{key}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing) { matCache[key] = existing; return existing; }
        EnsureDir("Assets/_Project/Art/NpcMaterials");
#endif
        // Shader yang bisa render backface (inverted hull) — gunakan Unlit
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit")
                 ?? Shader.Find("Unlit/Color")
                 ?? Shader.Find("Standard");
        if (sh == null) return null;

        var mat = new Material(sh) { name = key };
        // Warna outline: hitam cokelat gelap persis seperti Blender (.012, .008, .005)
        Color outlineCol = new Color(0.012f, 0.008f, 0.005f, 1f);
        if (mat.HasProperty("_BaseColor"))   mat.SetColor("_BaseColor", outlineCol);
        else if (mat.HasProperty("_Color"))  mat.SetColor("_Color", outlineCol);

        // Render hanya backface (cull front) agar outline tidak menutupi mesh asli
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Front);

#if UNITY_EDITOR
        AssetDatabase.CreateAsset(mat, $"Assets/_Project/Art/NpcMaterials/{key}.mat");
#endif
        matCache[key] = mat;
        return mat;
    }

    static Color HexColor(int hex)
    {
        float r = ((hex >> 16) & 255) / 255f;
        float g = ((hex >> 8) & 255) / 255f;
        float b = (hex & 255) / 255f;
        return new Color(r, g, b, 1f);
    }

    static void SaveMeshAsset(Mesh mesh, string key)
    {
#if UNITY_EDITOR
        EnsureDir("Assets/_Project/Art/NpcMeshes");
        string safeKey = key.Replace(',', '_').Replace(' ', '_');
        string path = $"Assets/_Project/Art/NpcMeshes/Mesh_{safeKey}.asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) == null)
        {
            AssetDatabase.CreateAsset(Object.Instantiate(mesh), path);
        }
#endif
    }

    static void EnsureDir(string path)
    {
#if UNITY_EDITOR
        if (!AssetDatabase.IsValidFolder(path))
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
#endif
    }
}

