using System.Collections.Generic;
using UnityEngine;
using Eldara;

namespace Eldara
{
    /// <summary>
    /// Builder Prosedural 3D untuk 8 Pos Kerja & Fasilitas Profesi Penduduk Desa Eldara:
    /// 1. Merchant (Kepala Desa / Tetua) -> Pendopo Balai Desa & Papan Pengumuman Desa
    /// 2. Farmer (Petani) -> Ladang Gandum & Kebun Sayur Berpagar (bisa dipanen)
    /// 3. Guard (Pandai Besi & Penjaga) -> Bengkel Tempa Pandai Besi (Tungku Api, Paron/Anvil, Rak Senjata)
    /// 4. Wizard (Dukun Hutan) -> Pondok & Altar Ramuan Mistis (Kuali Mendidih, Totem Kayu, Kristal Bercahaya)
    /// 5. Woodcutter (Penebang Kayu) -> Area Penebangan Kayu & Tunggul Gergaji (Tumpukan Log, Kapak Tertancap)
    /// 6. Hunter (Pemburu Rimba) -> Pos Kemah Pemburu (Target Panahan Jerami, Rak Busur, Api Unggun Kecil)
    /// 7. Herbalist (Nenek Jamu) -> Kedai & Kebun Tanaman Obat & Meja Racik Jamu (Lesung Batu, Gentong, Rak Pengering Daun)
    /// 8. VillageYouth (Pemuda Kurir) -> Pos Kurir & Kandang Burung Merpati Pos + Rambu Jarak
    /// </summary>
    public static class EldaraVillageStationsBuilder
    {
        public static GameObject BuildAllProfessionsStations(Transform parent)
        {
            var oldStations = GameObject.Find("Village_Profession_Stations");
            if (oldStations != null)
            {
                if (Application.isPlaying) Object.Destroy(oldStations);
                else Object.DestroyImmediate(oldStations);
            }

            var root = new GameObject("Village_Profession_Stations");
            if (parent != null) root.transform.SetParent(parent, false);

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material matTimber = MakeMat("St_Timber", shader, new Color(0.36f, 0.21f, 0.11f), 0.25f);
            Material matDark   = MakeMat("St_Dark", shader, new Color(0.14f, 0.11f, 0.09f), 0.30f);
            Material matDeck   = MakeMat("St_Deck", shader, new Color(0.46f, 0.28f, 0.14f), 0.25f);
            Material matStone  = MakeMat("St_Stone", shader, new Color(0.45f, 0.44f, 0.41f), 0.15f);
            Material matIron   = MakeMat("St_Iron", shader, new Color(0.25f, 0.27f, 0.30f), 0.70f, 0.85f);
            Material matSteel  = MakeMat("St_Steel", shader, new Color(0.75f, 0.78f, 0.82f), 0.85f, 0.90f);
            Material matHay    = MakeMat("St_Hay", shader, new Color(0.78f, 0.63f, 0.24f), 0.10f);
            Material matSoil   = MakeMat("St_Soil", shader, new Color(0.24f, 0.16f, 0.09f), 0.05f);
            Material matCrop   = MakeMat("St_Crop", shader, new Color(0.22f, 0.56f, 0.16f), 0.12f);
            Material matWheat  = MakeMat("St_Wheat", shader, new Color(0.85f, 0.68f, 0.20f), 0.12f);
            Material matCloth  = MakeMat("St_Cloth", shader, new Color(0.68f, 0.18f, 0.14f), 0.15f);
            Material matClay   = MakeMat("St_Clay", shader, new Color(0.60f, 0.32f, 0.18f), 0.20f);
            Material matGold   = MakeMat("St_Gold", shader, new Color(0.88f, 0.68f, 0.18f), 0.75f, 0.85f);
            Material matFire   = MakeMat("St_Fire", shader, new Color(1.0f, 0.45f, 0.08f), 0.50f, 0f, new Color(1.0f, 0.42f, 0.05f) * 3.0f);
            Material matMagic  = MakeMat("St_Magic", shader, new Color(0.25f, 0.95f, 0.55f), 0.90f, 0.1f, new Color(0.20f, 0.90f, 0.45f) * 2.5f);

            // 1. MERCHANT (Kepala Desa / Tetua): Papan Pengumuman & Meja Upeti di Depan Balai Desa (-3.8, 0, 19.5)
            BuildMerchantNoticeBoard(root.transform, new Vector3(-3.8f, 0f, 19.5f), 155f, matTimber, matDeck, matGold, matCloth);

            // 2. FARMER (Petani): Ladang Gandum & Kebun Sayur Berpagar di Sisi Barat (-43.5, 0, 8.5)
            BuildFarmerCropField(root.transform, new Vector3(-43.5f, 0f, 8.5f), matTimber, matSoil, matWheat, matCrop, matHay);

            // 3. GUARD (Pandai Besi & Penjaga): Bengkel Tempa Terbuka di Sisi Timur (20.5, 0, -5.5)
            BuildBlacksmithForge(root.transform, new Vector3(20.5f, 0f, -5.5f), -90f, matStone, matTimber, matIron, matSteel, matFire);

            // 4. WIZARD (Dukun Hutan): Lingkaran Totem & Kuali Ramuan di Tenggara (12.5, 0, -12.5)
            BuildWizardShrine(root.transform, new Vector3(12.5f, 0f, -12.5f), matTimber, matStone, matIron, matMagic);

            // 5. WOODCUTTER (Penebang Kayu): Area Penebangan & Tunggul Gergaji di Barat Laut (-36.5, 0, 23.5)
            BuildWoodcutterYard(root.transform, new Vector3(-36.5f, 0f, 23.5f), 135f, matTimber, matDeck, matSteel);

            // 6. HUNTER (Pemburu Rimba): Pos Latihan Panahan & Kemah Pemburu di Selatan (-8.5, 0, -42.0)
            BuildHunterCamp(root.transform, new Vector3(-8.5f, 0f, -42.0f), 45f, matTimber, matHay, matCloth, matFire);

            // 7. HERBALIST (Nenek Jamu): Kedai & Kebun Tanaman Obat di Selatan-Tenggara (7.5, 0, -19.5)
            BuildHerbalistBooth(root.transform, new Vector3(7.5f, 0f, -19.5f), -35f, matTimber, matDeck, matClay, matCrop, matCloth);

            // 8. VILLAGE YOUTH (Pemuda Kurir): Pos Merpati Pos & Rak Surat di Timur Laut (22.5, 0, 21.5)
            BuildCourierPost(root.transform, new Vector3(22.5f, 0f, 21.5f), -135f, matTimber, matDeck, matGold, matCloth);

            return root;
        }

        private static void BuildMerchantNoticeBoard(Transform parent, Vector3 pos, float yaw, Material wood, Material deck, Material gold, Material cloth)
        {
            var g = Group(parent, "Station_Merchant_NoticeBoard", pos, yaw);
            foreach (float sx in new[] { -0.85f, 0.85f })
                Part(PrimitiveType.Cylinder, "Post", g, wood, new Vector3(sx, 1.25f, 0f), new Vector3(0.14f, 1.25f, 0.14f), true);

            Part(PrimitiveType.Cube, "Board", g, deck, new Vector3(0f, 1.65f, 0f), new Vector3(1.65f, 1.05f, 0.10f), true);
            Part(PrimitiveType.Cube, "Roof", g, wood, new Vector3(0f, 2.32f, 0f), new Vector3(1.95f, 0.12f, 0.55f), false, new Vector3(15f, 0f, 0f));
            Part(PrimitiveType.Cube, "Crest", g, gold, new Vector3(0f, 2.22f, 0.08f), new Vector3(0.28f, 0.22f, 0.06f), false);

            Part(PrimitiveType.Cube, "Scroll_1", g, cloth, new Vector3(-0.42f, 1.72f, 0.06f), new Vector3(0.36f, 0.44f, 0.02f), false, new Vector3(0f, 0f, 4f));
            Part(PrimitiveType.Cube, "Scroll_2", g, cloth, new Vector3( 0.35f, 1.60f, 0.06f), new Vector3(0.40f, 0.38f, 0.02f), false, new Vector3(0f, 0f, -5f));
        }

        private static void BuildFarmerCropField(Transform parent, Vector3 pos, Material wood, Material soil, Material wheat, Material crop, Material hay)
        {
            var g = Group(parent, "Station_Farmer_CropField", pos, 0f);
            float w = 7.2f, d = 5.4f;

            Part(PrimitiveType.Cube, "SoilBed", g, soil, new Vector3(0f, 0.04f, 0f), new Vector3(w, 0.08f, d), false);

            for (int row = 0; row < 4; row++)
            {
                float rz = -1.9f + row * 1.25f;
                Material mPlant = (row % 2 == 0) ? wheat : crop;
                for (int col = 0; col < 7; col++)
                {
                    float cx = -2.8f + col * 0.92f;
                    var stalk = Part(PrimitiveType.Cylinder, "CropStalk", g, mPlant,
                        new Vector3(cx, 0.32f, rz), new Vector3(0.36f, 0.28f, 0.36f), false);

                    if (row == 0 && (col == 2 || col == 4))
                    {
                        var bc = stalk.AddComponent<BoxCollider>();
                        bc.size = new Vector3(1.5f, 1.5f, 1.5f);
                        var pickup = stalk.AddComponent<ResourcePickup>();
                        pickup.itemId = "wheat";
                        pickup.amount = 1;
                        pickup.prompt = "Tekan E untuk memanen Gandum Eldara";
                    }
                }
            }

            Part(PrimitiveType.Cube, "Fence_W", g, wood, new Vector3(-w * 0.5f, 0.45f, 0f), new Vector3(0.10f, 0.10f, d), true);
            Part(PrimitiveType.Cube, "Fence_N", g, wood, new Vector3(0f, 0.45f,  d * 0.5f), new Vector3(w, 0.10f, 0.10f), true);
            Part(PrimitiveType.Cube, "Fence_S", g, wood, new Vector3(0f, 0.45f, -d * 0.5f), new Vector3(w, 0.10f, 0.10f), true);
            Part(PrimitiveType.Cylinder, "HayBale", g, hay, new Vector3(-2.6f, 0.45f, -2.0f), new Vector3(0.95f, 0.42f, 0.95f), true, new Vector3(0f, 0f, 90f));
        }

        private static void BuildBlacksmithForge(Transform parent, Vector3 pos, float yaw, Material stone, Material wood, Material iron, Material steel, Material fire)
        {
            var g = Group(parent, "Station_Guard_BlacksmithForge", pos, yaw);

            Part(PrimitiveType.Cube, "ForgeBase", g, stone, new Vector3(-1.1f, 0.45f, 0f), new Vector3(1.4f, 0.90f, 1.3f), true);
            Part(PrimitiveType.Cube, "ForgeChimney", g, stone, new Vector3(-1.35f, 1.55f, 0f), new Vector3(0.85f, 1.4f, 0.95f), true);
            Part(PrimitiveType.Sphere, "ForgeEmbers", g, fire, new Vector3(-0.85f, 0.92f, 0f), new Vector3(0.55f, 0.22f, 0.65f), false);

            var lightGo = new GameObject("ForgeFireLight");
            lightGo.transform.SetParent(g, false);
            lightGo.transform.localPosition = new Vector3(-0.7f, 1.25f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.45f, 0.10f); l.intensity = 2.2f; l.range = 9f; l.shadows = LightShadows.None;

            Part(PrimitiveType.Cylinder, "AnvilStump", g, wood, new Vector3(0.45f, 0.32f, 0.35f), new Vector3(0.52f, 0.32f, 0.52f), true);
            Part(PrimitiveType.Cube, "AnvilBody", g, iron, new Vector3(0.45f, 0.74f, 0.35f), new Vector3(0.28f, 0.22f, 0.65f), true);
            Part(PrimitiveType.Cube, "AnvilHorn", g, iron, new Vector3(0.45f, 0.78f, 0.75f), new Vector3(0.16f, 0.14f, 0.28f), false);

            Part(PrimitiveType.Cylinder, "QuenchBarrel", g, wood, new Vector3(0.55f, 0.38f, -0.75f), new Vector3(0.55f, 0.38f, 0.55f), true);
            Part(PrimitiveType.Cube, "WeaponRackFrame", g, wood, new Vector3(1.45f, 0.85f, 0f), new Vector3(0.14f, 1.65f, 1.6f), true);
            for (int w = 0; w < 3; w++)
            {
                float wz = -0.45f + w * 0.45f;
                Part(PrimitiveType.Cube, "ForgedSpear", g, steel, new Vector3(1.36f, 0.95f, wz), new Vector3(0.05f, 1.45f, 0.08f), false, new Vector3(0f, 0f, -8f));
            }
        }

        private static void BuildWizardShrine(Transform parent, Vector3 pos, Material wood, Material stone, Material iron, Material magic)
        {
            var g = Group(parent, "Station_Wizard_MysticAltar", pos, 0f);

            Part(PrimitiveType.Sphere, "CauldronBody", g, iron, new Vector3(0f, 0.48f, 0f), new Vector3(0.95f, 0.78f, 0.95f), true);
            Part(PrimitiveType.Cylinder, "CauldronRim", g, iron, new Vector3(0f, 0.80f, 0f), new Vector3(0.82f, 0.08f, 0.82f), false);
            Part(PrimitiveType.Cylinder, "PotionBrew", g, magic, new Vector3(0f, 0.82f, 0f), new Vector3(0.74f, 0.02f, 0.74f), false);

            var lightGo = new GameObject("CauldronGlow");
            lightGo.transform.SetParent(g, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(0.25f, 0.95f, 0.55f); l.intensity = 1.8f; l.range = 8f; l.shadows = LightShadows.None;

            for (int i = 0; i < 3; i++)
            {
                float ang = (i * 120f + 30f) * Mathf.Deg2Rad;
                Vector3 tp = new Vector3(Mathf.Cos(ang) * 1.65f, 0f, Mathf.Sin(ang) * 1.65f);
                Part(PrimitiveType.Cylinder, "TotemLog", g, wood, tp + Vector3.up * 1.0f, new Vector3(0.26f, 1.0f, 0.26f), true);
                Part(PrimitiveType.Sphere, "TotemCrystal", g, magic, tp + Vector3.up * 2.12f, new Vector3(0.18f, 0.26f, 0.18f), false);
            }
        }

        private static void BuildWoodcutterYard(Transform parent, Vector3 pos, float yaw, Material wood, Material deck, Material steel)
        {
            var g = Group(parent, "Station_Woodcutter_LogYard", pos, yaw);

            Part(PrimitiveType.Cylinder, "ChoppingBlock", g, wood, new Vector3(0f, 0.35f, 0f), new Vector3(0.75f, 0.35f, 0.75f), true);
            Part(PrimitiveType.Cube, "EmbeddedAxeBlade", g, steel, new Vector3(0f, 0.76f, 0f), new Vector3(0.04f, 0.22f, 0.24f), false, new Vector3(15f, 0f, 0f));
            Part(PrimitiveType.Cylinder, "EmbeddedAxeHandle", g, deck, new Vector3(0f, 0.98f, -0.22f), new Vector3(0.04f, 0.35f, 0.04f), false, new Vector3(-42f, 0f, 0f));

            var logPile = new GameObject("StackedFirewood");
            logPile.transform.SetParent(g, false);
            logPile.transform.localPosition = new Vector3(-1.35f, 0f, 0.45f);

            for (int row = 0; row < 3; row++)
            {
                int count = 3 - row;
                for (int c = 0; c < count; c++)
                {
                    float lx = (c - (count - 1) * 0.5f) * 0.28f;
                    float ly = 0.14f + row * 0.23f;
                    Part(PrimitiveType.Cylinder, "TimberLog", logPile.transform, (row + c) % 2 == 0 ? wood : deck,
                        new Vector3(lx, ly, 0f), new Vector3(0.26f, 0.85f, 0.26f), false, new Vector3(90f, 0f, 0f));
                }
            }
            var pileCol = logPile.AddComponent<BoxCollider>();
            pileCol.center = new Vector3(0f, 0.38f, 0f);
            pileCol.size = new Vector3(1.0f, 0.75f, 1.7f);
            var pickup = logPile.AddComponent<ResourcePickup>();
            pickup.itemId = "wood";
            pickup.amount = 1;
            pickup.prompt = "Tekan E untuk mengambil Balok Kayu Rimba";
        }

        private static void BuildHunterCamp(Transform parent, Vector3 pos, float yaw, Material wood, Material hay, Material cloth, Material fire)
        {
            var g = Group(parent, "Station_Hunter_Camp", pos, yaw);

            Part(PrimitiveType.Cylinder, "TargetStandL", g, wood, new Vector3(-0.45f, 0.65f, 1.3f), new Vector3(0.08f, 0.65f, 0.08f), false, new Vector3(12f, 0f, 0f));
            Part(PrimitiveType.Cylinder, "TargetStandR", g, wood, new Vector3( 0.45f, 0.65f, 1.3f), new Vector3(0.08f, 0.65f, 0.08f), false, new Vector3(12f, 0f, 0f));
            Part(PrimitiveType.Cylinder, "ArcheryTarget", g, hay, new Vector3(0f, 1.05f, 1.25f), new Vector3(1.05f, 0.14f, 1.05f), true, new Vector3(78f, 0f, 0f));
            Part(PrimitiveType.Cylinder, "TargetBullseye", g, cloth, new Vector3(0f, 1.06f, 1.17f), new Vector3(0.42f, 0.15f, 0.42f), false, new Vector3(78f, 0f, 0f));

            Part(PrimitiveType.Cube, "TentSlopeL", g, cloth, new Vector3(-1.35f, 0.75f, -0.8f), new Vector3(0.08f, 1.65f, 2.2f), true, new Vector3(0f, 0f, -32f));
            Part(PrimitiveType.Cube, "TentSlopeR", g, cloth, new Vector3(-0.35f, 0.75f, -0.8f), new Vector3(0.08f, 1.65f, 2.2f), true, new Vector3(0f, 0f,  32f));
            Part(PrimitiveType.Sphere, "HunterCampfire", g, fire, new Vector3(0.95f, 0.15f, -0.6f), new Vector3(0.45f, 0.35f, 0.45f), false);
        }

        private static void BuildHerbalistBooth(Transform parent, Vector3 pos, float yaw, Material wood, Material deck, Material clay, Material crop, Material cloth)
        {
            var g = Group(parent, "Station_Herbalist_Booth", pos, yaw);

            Part(PrimitiveType.Cube, "HerbalTable", g, deck, new Vector3(0f, 0.75f, 0f), new Vector3(1.8f, 0.08f, 0.85f), true);
            foreach (float sx in new[] { -0.8f, 0.8f })
            {
                Part(PrimitiveType.Cylinder, "BoothPole", g, wood, new Vector3(sx, 1.15f, -0.35f), new Vector3(0.08f, 1.15f, 0.08f), false);
                Part(PrimitiveType.Cylinder, "TableLeg", g, wood, new Vector3(sx, 0.37f,  0.35f), new Vector3(0.08f, 0.37f, 0.08f), false);
            }
            Part(PrimitiveType.Cube, "Canopy", g, cloth, new Vector3(0f, 2.15f, 0f), new Vector3(2.0f, 0.06f, 1.25f), false, new Vector3(14f, 0f, 0f));

            Part(PrimitiveType.Sphere, "JamuPot_1", g, clay, new Vector3(-0.55f, 0.96f, 0f), new Vector3(0.32f, 0.36f, 0.32f), false);
            Part(PrimitiveType.Sphere, "JamuPot_2", g, clay, new Vector3(-0.15f, 0.94f, 0.1f), new Vector3(0.26f, 0.30f, 0.26f), false);
            var herbBundle = Part(PrimitiveType.Sphere, "MedicinalHerbs", g, crop, new Vector3(0.45f, 0.88f, 0f), new Vector3(0.45f, 0.22f, 0.38f), true);

            var pickup = herbBundle.AddComponent<ResourcePickup>();
            pickup.itemId = "rare_herb";
            pickup.amount = 1;
            pickup.prompt = "Tekan E untuk mengambil Ramuan Daun Obat";
        }

        private static void BuildCourierPost(Transform parent, Vector3 pos, float yaw, Material wood, Material deck, Material gold, Material cloth)
        {
            var g = Group(parent, "Station_VillageYouth_CourierPost", pos, yaw);

            Part(PrimitiveType.Cylinder, "DovecotePole", g, wood, new Vector3(0f, 1.35f, 0f), new Vector3(0.14f, 1.35f, 0.14f), true);
            Part(PrimitiveType.Cube, "PigeonHouse", g, deck, new Vector3(0f, 2.75f, 0f), new Vector3(0.75f, 0.65f, 0.65f), false);
            Part(PrimitiveType.Cube, "PigeonRoof", g, cloth, new Vector3(0f, 3.22f, 0f), new Vector3(0.95f, 0.14f, 0.85f), false);
            Part(PrimitiveType.Cube, "GoldBell", g, gold, new Vector3(0f, 2.30f, 0.16f), new Vector3(0.14f, 0.16f, 0.14f), false);

            Part(PrimitiveType.Cube, "MailDesk", g, deck, new Vector3(0.85f, 0.72f, 0f), new Vector3(0.85f, 0.08f, 0.65f), true);
            Part(PrimitiveType.Cylinder, "DeskLeg1", g, wood, new Vector3(0.52f, 0.36f, 0f), new Vector3(0.08f, 0.36f, 0.08f), false);
            Part(PrimitiveType.Cylinder, "DeskLeg2", g, wood, new Vector3(1.18f, 0.36f, 0f), new Vector3(0.08f, 0.36f, 0.08f), false);
        }

        private static Transform Group(Transform parent, string name, Vector3 pos, float yaw)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go.transform;
        }

        private static GameObject Part(PrimitiveType type, string name, Transform parent, Material mat,
            Vector3 pos, Vector3 scale, bool collider, Vector3 euler = default)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            if (euler != default) go.transform.localRotation = Quaternion.Euler(euler);
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null)
                {
                    if (Application.isPlaying) Object.Destroy(c);
                    else Object.DestroyImmediate(c);
                }
            }
            return go;
        }

        private static Material MakeMat(string name, Shader shader, Color col, float smoothness, float metallic = 0f, Color? emission = null)
        {
            var m = new Material(shader) { name = $"M_Station_{name}" };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Color")) m.SetColor("_Color", col);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (emission.HasValue && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            return m;
        }
    }
}
