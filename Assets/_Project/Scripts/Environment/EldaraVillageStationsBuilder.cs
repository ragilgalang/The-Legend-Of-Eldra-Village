using UnityEngine;

namespace Eldara
{
    /// <summary>
    /// Pembangun Workstation (Pos Kerja & Peralatan Profesi) untuk 10 Penduduk Desa Eldara:
    /// - Pandai Besi: Tungku tempa api membara, paron baja (anvil), bak sepuh air, & rak palu
    /// - Petani: Tumpukan jerami gandum, keranjang panen, cangkul, & wadah benang
    /// - Dukun / Alkemis: Meja ritual kayu, kuali ramuan mistis berkabut, & dupa
    /// - Penenun: Alat tenun kayu (loom) tradisional & tumpukan kain batik warna-warni
    /// - Penebang Kayu: Tunggul belah kayu, kapak tertancap, & balok kayu gelondongan
    /// - Pemburu: Papan sasaran tembak panah jerami (archery targets) & rak busur
    /// - Nenek Jamu: Meja racik obat herbal, lumpang batu (mortar & pestle), toples rempah
    /// - Tetua Desa: Meja kayu jati musyawarah & kursi kehormatan kepala desa
    /// - Pemuda Desa: Papan pengumuman kayu beratap jerami di persimpangan jalan
    /// </summary>
    public static class EldaraVillageStationsBuilder
    {
        public static GameObject BuildAllProfessionsStations(Transform parent)
        {
            var oldStations = GameObject.Find("--- VILLAGE_WORKSTATIONS ---");
            if (oldStations != null)
            {
                if (Application.isPlaying) Object.Destroy(oldStations);
                else Object.DestroyImmediate(oldStations);
            }

            var root = new GameObject("--- VILLAGE_WORKSTATIONS ---");
            if (parent != null) root.transform.SetParent(parent, false);

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var matWood   = MakeMat("Work_Wood",   shader, new Color(0.34f, 0.20f, 0.11f), 0.20f);
            var matStone  = MakeMat("Work_Stone",  shader, new Color(0.48f, 0.46f, 0.44f), 0.15f);
            var matIron   = MakeMat("Work_Iron",   shader, new Color(0.22f, 0.24f, 0.26f), 0.75f, 0.85f);
            var matHay    = MakeMat("Work_Hay",    shader, new Color(0.82f, 0.68f, 0.28f), 0.10f);
            var matCloth  = MakeMat("Work_Cloth",  shader, new Color(0.72f, 0.25f, 0.22f), 0.25f);
            var matBatik  = MakeMat("Work_Batik",  shader, new Color(0.18f, 0.45f, 0.65f), 0.25f);
            var matPotion = MakeMat("Work_Potion", shader, new Color(0.45f, 0.85f, 0.35f), 0.85f);
            var matEmber  = MakeMat("Work_Ember",  shader, new Color(1.0f, 0.35f, 0.08f), 0.90f);

            // 1. Bengkel Pandai Besi (X = 33, Z = 11) - Depan bengkel timur
            BuildBlacksmithStation(root.transform, new Vector3(33f, 0f, 11f), matWood, matIron, matStone, matEmber);

            // 2. Pos Petani & Lumbung (X = -24, Z = 8) - Sisi barat laut alun-alun
            BuildFarmerStation(root.transform, new Vector3(-24f, 0f, 8f), matWood, matHay);

            // 3. Altar Ramuan Dukun Mistis (X = 13.5, Z = -31) - Sisi luar pondok dukun
            BuildDukunStation(root.transform, new Vector3(13.5f, 0f, -31f), matWood, matStone, matPotion);

            // 4. Alat Tenun Gadis Penenun (X = 6.5, Z = 6.5) - Sisi timur laut alun-alun
            BuildWeaverStation(root.transform, new Vector3(6.5f, 0f, 6.5f), matWood, matCloth, matBatik);

            // 5. Area Penebang Kayu (X = -25f, Z = 19f) - Dekat lumbung & rimbun pohon barat
            BuildWoodcutterStation(root.transform, new Vector3(-25f, 0f, 19f), matWood, matIron);

            // 6. Sasaran Panah Pemburu Rimba (X = -8f, Z = -44f) - Dekat gerbang selatan benteng
            BuildHunterStation(root.transform, new Vector3(-8f, 0f, -44f), matWood, matHay);

            // 7. Meja Racik Nenek Jamu (X = 8.5f, Z = -16.5f) - Sisi selatan alun-alun
            BuildHerbalistStation(root.transform, new Vector3(8.5f, 0f, -16.5f), matWood, matStone, matPotion);

            // 8. Meja Musyawarah Kepala Desa (X = 0f, Z = 16.0f) - Teras balai desa alun-alun utara
            BuildElderCouncilStation(root.transform, new Vector3(0f, 0.14f, 16.0f), matWood, matCloth);

            // 9. Papan Pengumuman Pemuda Pengantar Pesan (X = 18f, Z = 18f) - Persimpangan timur laut
            BuildNoticeBoardStation(root.transform, new Vector3(18f, 0f, 18f), matWood, matHay);

            return root;
        }

        private static void BuildBlacksmithStation(Transform parent, Vector3 pos, Material matWood, Material matIron, Material matStone, Material matEmber)
        {
            var station = new GameObject("Station_PandaiBesi");
            station.transform.SetParent(parent, false);
            station.transform.position = pos;

            // Tungku tempa batu dengan bara menyala
            Part(PrimitiveType.Cube, "ForgeBase", station.transform, matStone, new Vector3(0f, 0.65f, 0f), new Vector3(1.8f, 1.3f, 1.4f), true);
            Part(PrimitiveType.Cube, "ForgeChimney", station.transform, matStone, new Vector3(0f, 2.1f, 0.3f), new Vector3(0.9f, 1.6f, 0.8f), true);
            var ember = Part(PrimitiveType.Cube, "ForgeEmber", station.transform, matEmber, new Vector3(0f, 0.85f, -0.2f), new Vector3(0.95f, 0.25f, 0.65f), false);
            var fireLight = ember.AddComponent<Light>();
            fireLight.color = new Color(1.0f, 0.45f, 0.15f);
            fireLight.range = 7f;
            fireLight.intensity = 1.8f;

            // Paron tempa baja (Anvil) di atas balok kayu jati
            Part(PrimitiveType.Cylinder, "AnvilStump", station.transform, matWood, new Vector3(1.6f, 0.35f, -0.5f), new Vector3(0.7f, 0.35f, 0.7f), true);
            Part(PrimitiveType.Cube, "AnvilBase", station.transform, matIron, new Vector3(1.6f, 0.82f, -0.5f), new Vector3(0.45f, 0.22f, 0.85f), true);
            Part(PrimitiveType.Cube, "AnvilHorn", station.transform, matIron, new Vector3(1.6f, 0.85f, -0.98f), new Vector3(0.24f, 0.16f, 0.35f), false);

            // Bak sepuh air pendingin
            Part(PrimitiveType.Cube, "WaterTrough", station.transform, matWood, new Vector3(-1.4f, 0.40f, -0.4f), new Vector3(1.1f, 0.80f, 0.75f), true);
        }

        private static void BuildFarmerStation(Transform parent, Vector3 pos, Material matWood, Material matHay)
        {
            var station = new GameObject("Station_Petani");
            station.transform.SetParent(parent, false);
            station.transform.position = pos;

            // Tumpukan bal jerami gandum panen
            Part(PrimitiveType.Cube, "HayBale_1", station.transform, matHay, new Vector3(0f, 0.42f, 0f), new Vector3(1.4f, 0.85f, 0.95f), true);
            Part(PrimitiveType.Cube, "HayBale_2", station.transform, matHay, new Vector3(0.3f, 1.15f, 0f), new Vector3(1.3f, 0.75f, 0.9f), true, new Vector3(0f, 15f, 0f));
            Part(PrimitiveType.Cube, "HayBale_3", station.transform, matHay, new Vector3(1.2f, 0.42f, -0.2f), new Vector3(1.1f, 0.85f, 0.9f), true);

            // Keranjang hasil panen & peti kayu
            Part(PrimitiveType.Cylinder, "HarvestBasket", station.transform, matWood, new Vector3(-1.1f, 0.35f, -0.3f), new Vector3(0.75f, 0.35f, 0.75f), true);
            Part(PrimitiveType.Cube, "ProduceCrate", station.transform, matWood, new Vector3(-1.1f, 0.28f, 0.7f), new Vector3(0.85f, 0.55f, 0.75f), true);
        }

        private static void BuildDukunStation(Transform parent, Vector3 pos, Material matWood, Material matStone, Material matPotion)
        {
            var station = new GameObject("Station_Dukun");
            station.transform.SetParent(parent, false);
            station.transform.position = pos;

            // Meja ritual kayu gelap & kuali batu mendidih
            Part(PrimitiveType.Cube, "RitualTable", station.transform, matWood, new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.9f, 0.95f), true);
            var cauldron = Part(PrimitiveType.Sphere, "PotionCauldron", station.transform, matStone, new Vector3(0f, 1.05f, 0f), new Vector3(0.65f, 0.55f, 0.65f), false);
            var potionBrew = Part(PrimitiveType.Cylinder, "PotionLiquid", station.transform, matPotion, new Vector3(0f, 1.16f, 0f), new Vector3(0.48f, 0.05f, 0.48f), false);

            var aura = potionBrew.AddComponent<Light>();
            aura.color = new Color(0.4f, 0.95f, 0.65f);
            aura.range = 4.5f;
            aura.intensity = 1.4f;

            // Botol-botol ramuan mistis di meja
            Part(PrimitiveType.Cylinder, "PotionFlask_1", station.transform, matPotion, new Vector3(0.65f, 0.98f, 0.2f), new Vector3(0.12f, 0.20f, 0.12f), false);
            Part(PrimitiveType.Cylinder, "PotionFlask_2", station.transform, matPotion, new Vector3(-0.65f, 0.98f, -0.2f), new Vector3(0.14f, 0.22f, 0.14f), false);
        }

        private static void BuildWeaverStation(Transform parent, Vector3 pos, Material matWood, Material matCloth, Material matBatik)
        {
            var station = new GameObject("Station_GadisPenenun");
            station.transform.SetParent(parent, false);
            station.transform.position = pos;

            // Rangka alat tenun kayu tradisional (Loom)
            Part(PrimitiveType.Cube, "LoomFrame_L", station.transform, matWood, new Vector3(-0.75f, 0.65f, 0f), new Vector3(0.10f, 1.3f, 1.2f), false);
            Part(PrimitiveType.Cube, "LoomFrame_R", station.transform, matWood, new Vector3( 0.75f, 0.65f, 0f), new Vector3(0.10f, 1.3f, 1.2f), false);
            Part(PrimitiveType.Cube, "LoomBeam_Top", station.transform, matWood, new Vector3(0f, 1.25f, 0f), new Vector3(1.6f, 0.10f, 0.12f), false);
            Part(PrimitiveType.Cube, "LoomFabric", station.transform, matBatik, new Vector3(0f, 0.65f, 0.15f), new Vector3(1.35f, 0.95f, 0.04f), false, new Vector3(18f, 0f, 0f));

            // Gulungan kain tenun/batik di samping alat tenun
            Part(PrimitiveType.Cylinder, "FabricRoll_1", station.transform, matCloth, new Vector3(1.15f, 0.22f, -0.2f), new Vector3(0.24f, 0.70f, 0.24f), false, new Vector3(0f, 0f, 85f));
            Part(PrimitiveType.Cylinder, "FabricRoll_2", station.transform, matBatik, new Vector3(1.15f, 0.42f, -0.2f), new Vector3(0.24f, 0.65f, 0.24f), false, new Vector3(0f, 0f, 85f));
        }

        private static void BuildWoodcutterStation(Transform parent, Vector3 pos, Material matWood, Material matIron)
        {
            var station = new GameObject("Station_PenebangKayu");
            station.transform.SetParent(parent, false);
            station.transform.position = pos;

            // Tunggul besar untuk membelah kayu dengan kapak tertancap
            Part(PrimitiveType.Cylinder, "ChoppingStump", station.transform, matWood, new Vector3(0f, 0.45f, 0f), new Vector3(1.1f, 0.45f, 1.1f), true);
            // Gagang kapak & mata kapak besi
            Part(PrimitiveType.Cube, "AxeHandle", station.transform, matWood, new Vector3(0.08f, 1.15f, 0f), new Vector3(0.06f, 0.85f, 0.06f), false, new Vector3(25f, 0f, -15f));
            Part(PrimitiveType.Cube, "AxeHead", station.transform, matIron, new Vector3(0.22f, 1.35f, 0f), new Vector3(0.18f, 0.22f, 0.08f), false);

            // Tumpukan kayu gelondongan rimba siap pakai
            for (int k = 0; k < 5; k++)
            {
                float ly = 0.20f + (k >= 3 ? 0.35f : 0f);
                float lz = -0.7f + (k % 3) * 0.45f;
                Part(PrimitiveType.Cylinder, $"FirewoodLog_{k}", station.transform, matWood, new Vector3(1.4f, ly, lz), new Vector3(0.35f, 1.6f, 0.35f), true, new Vector3(90f, 0f, 0f));
            }
        }

        private static void BuildHunterStation(Transform parent, Vector3 pos, Material matWood, Material matHay)
        {
            var station = new GameObject("Station_PemburuRimba");
            station.transform.SetParent(parent, false);
            station.transform.position = pos;

            // 2 Papan sasaran panah jerami (Archery Targets)
            foreach (float side in new[] { -1.1f, 1.1f })
            {
                var targetRoot = new GameObject($"ArcheryTarget_{side}");
                targetRoot.transform.SetParent(station.transform, false);
                targetRoot.transform.localPosition = new Vector3(side, 0f, 0f);

                // Kaki tripod kayu
                Part(PrimitiveType.Cube, "TargetLeg_L", targetRoot.transform, matWood, new Vector3(-0.35f, 0.85f, 0f), new Vector3(0.08f, 1.7f, 0.08f), false, new Vector3(0f, 0f, 14f));
                Part(PrimitiveType.Cube, "TargetLeg_R", targetRoot.transform, matWood, new Vector3( 0.35f, 0.85f, 0f), new Vector3(0.08f, 1.7f, 0.08f), false, new Vector3(0f, 0f, -14f));
                Part(PrimitiveType.Cube, "TargetLeg_B", targetRoot.transform, matWood, new Vector3( 0.0f,  0.85f, 0.35f), new Vector3(0.08f, 1.7f, 0.08f), false, new Vector3(-18f, 0f, 0f));

                // Lingkaran sasaran jerami
                Part(PrimitiveType.Cylinder, "TargetFace", targetRoot.transform, matHay, new Vector3(0f, 1.25f, -0.08f), new Vector3(1.1f, 0.08f, 1.1f), true, new Vector3(78f, 0f, 0f));
            }

            // Rak busur & senjata kayu
            Part(PrimitiveType.Cube, "WeaponRack", station.transform, matWood, new Vector3(0f, 0.75f, 1.6f), new Vector3(1.8f, 1.5f, 0.35f), true);
        }

        private static void BuildHerbalistStation(Transform parent, Vector3 pos, Material matWood, Material matStone, Material matPotion)
        {
            var station = new GameObject("Station_NenekJamu");
            station.transform.SetParent(parent, false);
            station.transform.position = pos;

            // Meja racik jamu kayu & lumpang batu (Mortar and Pestle)
            Part(PrimitiveType.Cube, "HerbTable", station.transform, matWood, new Vector3(0f, 0.42f, 0f), new Vector3(1.7f, 0.85f, 0.9f), true);
            Part(PrimitiveType.Cylinder, "MortarStone", station.transform, matStone, new Vector3(-0.35f, 0.95f, 0f), new Vector3(0.42f, 0.16f, 0.42f), false);
            Part(PrimitiveType.Cylinder, "PestleStick", station.transform, matWood, new Vector3(-0.32f, 1.08f, 0f), new Vector3(0.06f, 0.32f, 0.06f), false, new Vector3(20f, 0f, 0f));

            // Botol-botol jamu & toples herbal
            Part(PrimitiveType.Cylinder, "HerbalJar_1", station.transform, matPotion, new Vector3(0.45f, 0.96f, 0.2f), new Vector3(0.18f, 0.22f, 0.18f), false);
            Part(PrimitiveType.Cylinder, "HerbalJar_2", station.transform, matStone, new Vector3(0.45f, 0.94f, -0.2f), new Vector3(0.20f, 0.18f, 0.20f), false);
        }

        private static void BuildElderCouncilStation(Transform parent, Vector3 pos, Material matWood, Material matCloth)
        {
            var station = new GameObject("Station_KepalaDesa");
            station.transform.SetParent(parent, false);
            station.transform.position = pos;

            // Meja musyawarah balai desa dengan taplak kehormatan
            Part(PrimitiveType.Cube, "CouncilTable", station.transform, matWood, new Vector3(0f, 0.44f, 0f), new Vector3(2.4f, 0.88f, 1.1f), true);
            Part(PrimitiveType.Cube, "TableCloth", station.transform, matCloth, new Vector3(0f, 0.89f, 0f), new Vector3(2.0f, 0.03f, 0.85f), false);

            // Kursi tetua desa berukir kayu
            Part(PrimitiveType.Cube, "ElderChair_Seat", station.transform, matWood, new Vector3(0f, 0.32f, 0.95f), new Vector3(0.85f, 0.12f, 0.75f), false);
            Part(PrimitiveType.Cube, "ElderChair_Back", station.transform, matWood, new Vector3(0f, 0.85f, 1.28f), new Vector3(0.85f, 1.05f, 0.12f), false);
        }

        private static void BuildNoticeBoardStation(Transform parent, Vector3 pos, Material matWood, Material matHay)
        {
            var station = new GameObject("Station_PemudaDesa");
            station.transform.SetParent(parent, false);
            station.transform.position = pos;

            // Tiang papan buletin pengumuman desa
            Part(PrimitiveType.Cube, "NoticePost_L", station.transform, matWood, new Vector3(-0.95f, 1.1f, 0f), new Vector3(0.14f, 2.2f, 0.14f), false);
            Part(PrimitiveType.Cube, "NoticePost_R", station.transform, matWood, new Vector3( 0.95f, 1.1f, 0f), new Vector3(0.14f, 2.2f, 0.14f), false);
            Part(PrimitiveType.Cube, "NoticeBoard",  station.transform, matWood, new Vector3( 0.00f, 1.45f, 0f), new Vector3(1.85f, 1.1f, 0.08f), true);
            Part(PrimitiveType.Cube, "NoticeRoof",   station.transform, matHay,  new Vector3( 0.00f, 2.15f, 0f), new Vector3(2.3f, 0.22f, 0.65f), false);
        }

        private static Material MakeMat(string name, Shader shader, Color col, float smoothness, float metallic = 0f)
        {
            var m = new Material(shader) { name = $"M_{name}" };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_Color")) m.SetColor("_Color", col);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            return m;
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
    }
}
