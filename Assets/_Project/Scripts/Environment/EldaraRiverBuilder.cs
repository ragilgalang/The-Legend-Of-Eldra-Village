using System.Collections.Generic;
using UnityEngine;

namespace Eldara
{
    /// <summary>
    /// Builder & Pengelola Sungai Desa Eldara (Elder River) lengkap dengan:
    /// - Aliran sungai luas & dalam (lebar 14m, panjang 112m) di sisi barat desa
    /// - Tanggul batu alam, vegetasi air (teratai & alang-alang), jembatan kayu lengkung, dermaga nelayan, & sampan kayu
    /// - Kawanan ikan 3D realistis (Ikan Mas, Ikan Sungai Perak, Ikan Patin) yang berenang hidup di dalam sungai
    /// - Kompatibel langsung dengan NPC Nelayan (NpcStyle.Fisherman) & sistem ResourcePickup
    /// </summary>
    public static class EldaraRiverBuilder
    {
        public static GameObject BuildEldaraRiver(Transform parent, Vector3 riverCenter, float width = 14f, float length = 112f, int fishCount = 24)
        {
            // Hapus sungai lama jika sudah ada untuk mencegah duplikasi
            var oldRiver = GameObject.Find("Eldara_Grand_River");
            if (oldRiver != null)
            {
                if (Application.isPlaying) Object.Destroy(oldRiver);
                else Object.DestroyImmediate(oldRiver);
            }

            var riverRoot = new GameObject("Eldara_Grand_River");
            if (parent != null) riverRoot.transform.SetParent(parent, false);
            riverRoot.transform.position = riverCenter;

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material matWater = GetOrCreateWaterMat();
            Material matRiverBed = MakeMat("RiverBed", shader, new Color(0.22f, 0.25f, 0.23f), 0.10f);
            Material matStone    = MakeMat("RiverStone", shader, new Color(0.46f, 0.45f, 0.42f), 0.15f);
            Material matTimber   = MakeMat("RiverTimber", shader, new Color(0.36f, 0.21f, 0.11f), 0.25f);
            Material matDeck     = MakeMat("RiverDeck", shader, new Color(0.46f, 0.28f, 0.14f), 0.25f);
            Material matReed     = MakeMat("RiverReed", shader, new Color(0.18f, 0.46f, 0.16f), 0.15f);
            Material matLotus    = MakeMat("LotusFlower", shader, new Color(0.95f, 0.55f, 0.68f), 0.25f);

            float halfW = width * 0.5f;
            float halfL = length * 0.5f;

            // 1. Dasar Sungai & Permukaan Air Jernih (Y dinaikkan sedikit di atas Ground Y=0 agar terlihat jelas dan tidak tertutup tanah)
            var bed = Part(PrimitiveType.Cube, "River_Bed", riverRoot.transform, matRiverBed,
                new Vector3(0f, 0.015f, 0f), new Vector3(width, 0.03f, length), false);

            var waterSurface = Part(PrimitiveType.Cube, "River_WaterSurface", riverRoot.transform, matWater,
                new Vector3(0f, 0.08f, 0f), new Vector3(width, 0.12f, length), false);

            // Batu-batu kerikil & karang sungai di dasar air
            var rng = new System.Random(2026);
            for (int r = 0; r < 35; r++)
            {
                float rx = (float)(rng.NextDouble() * 2.0 - 1.0) * (halfW - 1.2f);
                float rz = (float)(rng.NextDouble() * 2.0 - 1.0) * (halfL - 2.0f);
                float rs = 0.35f + (float)rng.NextDouble() * 0.65f;
                Part(PrimitiveType.Sphere, "RiverBoulder", riverRoot.transform, matStone,
                    new Vector3(rx, 0.04f, rz), new Vector3(rs * 1.2f, rs * 0.45f, rs * 1.0f), false);
            }

            // 2. Tanggul Batu Tepi Sungai (Kiri, Kanan, serta Ujung Utara & Selatan)
            foreach (float side in new[] { -1f, 1f })
            {
                float bankX = side * (halfW + 0.45f);
                // Segmen Utara (Z = 2.8 .. halfL)
                float lenN = halfL - 2.8f;
                Part(PrimitiveType.Cube, "RiverBank_N", riverRoot.transform, matStone,
                    new Vector3(bankX, 0.16f, 2.8f + lenN * 0.5f), new Vector3(0.95f, 0.32f, lenN), true);

                // Segmen Selatan (Z = -halfL .. -2.8)
                float lenS = halfL - 2.8f;
                Part(PrimitiveType.Cube, "RiverBank_S", riverRoot.transform, matStone,
                    new Vector3(bankX, 0.16f, -2.8f - lenS * 0.5f), new Vector3(0.95f, 0.32f, lenS), true);
            }

            // Tanggul Penutup Ujung Utara & Selatan
            Part(PrimitiveType.Cube, "RiverBank_Cap_N", riverRoot.transform, matStone,
                new Vector3(0f, 0.16f, halfL + 0.45f), new Vector3(width + 1.8f, 0.32f, 0.90f), true);
            Part(PrimitiveType.Cube, "RiverBank_Cap_S", riverRoot.transform, matStone,
                new Vector3(0f, 0.16f, -halfL - 0.45f), new Vector3(width + 1.8f, 0.32f, 0.90f), true);

            // 3. Jembatan Kayu Lengkung Utama di Z = 0
            BuildArchedRiverBridge(riverRoot.transform, width + 3.2f, 4.6f, matTimber, matDeck);

            // 4. Dermaga Kayu Nelayan (Fishing Pier) di Tepi Timur Sungai (Z = -14m, menghadap barat ke sungai)
            BuildFishingDock(riverRoot.transform, new Vector3(halfW - 0.5f, 0f, -14f), matTimber, matDeck);

            // 5. Perahu Sampan Kayu Tertambat di Samping Dermaga
            BuildWoodenRowboat(riverRoot.transform, new Vector3(halfW - 3.6f, 0.08f, -17.5f), 22f, matTimber, matDeck);
            BuildWoodenRowboat(riverRoot.transform, new Vector3(-halfW + 3.8f, 0.08f, 16.0f), -145f, matTimber, matDeck);

            // 6. Tanaman Air: Teratai & Alang-alang Tepi Sungai
            for (int p = 0; p < 18; p++)
            {
                float side = (p % 2 == 0) ? 1f : -1f;
                float px = side * (halfW - 1.2f - (float)rng.NextDouble() * 1.5f);
                float pz = (float)(rng.NextDouble() * 2.0 - 1.0) * (halfL - 6f);
                if (Mathf.Abs(pz) < 4.5f) continue; // hindari bawah jembatan

                // Daun teratai
                Part(PrimitiveType.Cylinder, "LotusPad", riverRoot.transform, matReed,
                    new Vector3(px, 0.142f, pz), new Vector3(0.65f, 0.015f, 0.65f), false);

                if (p % 3 == 0)
                {
                    Part(PrimitiveType.Sphere, "LotusBloom", riverRoot.transform, matLotus,
                        new Vector3(px, 0.185f, pz), new Vector3(0.22f, 0.16f, 0.22f), false);
                }
            }

            // 7. Kawanan Ikan 3D Hidup di Dalam Sungai (Ikan Mas, Ikan Perak, Ikan Patin)
            var fishGroup = new GameObject("River_Fish_School");
            fishGroup.transform.SetParent(riverRoot.transform, false);

            Material matGoldFish   = MakeMat("Fish_Gold", shader, new Color(0.95f, 0.42f, 0.08f), 0.65f);
            Material matSilverFish = MakeMat("Fish_Silver", shader, new Color(0.68f, 0.78f, 0.86f), 0.75f, 0.35f);
            Material matCatfish    = MakeMat("Fish_Catfish", shader, new Color(0.28f, 0.34f, 0.30f), 0.45f);
            Material matFin        = MakeMat("Fish_Fin", shader, new Color(0.92f, 0.65f, 0.25f), 0.40f);

            for (int f = 0; f < fishCount; f++)
            {
                int species = f % 3; // 0 = Ikan Mas, 1 = Ikan Nila Perak, 2 = Ikan Patin Sungai
                Material bodyMat = (species == 0) ? matGoldFish : (species == 1 ? matSilverFish : matCatfish);
                float fishScale = (species == 2) ? Random.Range(0.95f, 1.35f) : Random.Range(0.75f, 1.15f);

                float fx = Random.Range(-halfW + 1.6f, halfW - 1.6f);
                float fz = Random.Range(-halfL + 4.0f, halfL - 4.0f);
                float fy = Random.Range(0.05f, 0.09f);

                var fishGo = Build3DFish($"RiverFish_{f + 1}", fishGroup.transform, bodyMat, matFin, fishScale);
                fishGo.transform.localPosition = new Vector3(fx, fy, fz);

                var swimmer = fishGo.AddComponent<RiverFishSwimmer>();
                swimmer.Configure(
                    riverRoot.transform,
                    halfW - 1.4f,
                    halfL - 3.5f,
                    fy,
                    Random.Range(1.4f, 2.8f),
                    Random.Range(0f, 100f)
                );
            }

            return riverRoot;
        }

        private static GameObject Build3DFish(string name, Transform parent, Material bodyMat, Material finMat, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * scale;

            // Badan ikan aerodinamis
            Part(PrimitiveType.Sphere, "Body", root.transform, bodyMat, Vector3.zero, new Vector3(0.16f, 0.22f, 0.58f), false);
            // Kepala meruncing
            Part(PrimitiveType.Sphere, "Head", root.transform, bodyMat, new Vector3(0f, 0.01f, 0.24f), new Vector3(0.13f, 0.17f, 0.22f), false);
            // Sirip punggung (Dorsal Fin)
            Part(PrimitiveType.Cube, "DorsalFin", root.transform, finMat, new Vector3(0f, 0.13f, -0.02f), new Vector3(0.015f, 0.11f, 0.22f), false, new Vector3(-18f, 0f, 0f));
            // Sirip samping kiri & kanan (Pectoral Fins)
            Part(PrimitiveType.Cube, "PectoralL", root.transform, finMat, new Vector3(-0.09f, -0.04f, 0.10f), new Vector3(0.12f, 0.015f, 0.08f), false, new Vector3(0f, 25f, -20f));
            Part(PrimitiveType.Cube, "PectoralR", root.transform, finMat, new Vector3( 0.09f, -0.04f, 0.10f), new Vector3(0.12f, 0.015f, 0.08f), false, new Vector3(0f, -25f, 20f));

            // Pivot ekor agar bisa mengibas saat berenang
            var tailPivot = new GameObject("TailPivot");
            tailPivot.transform.SetParent(root.transform, false);
            tailPivot.transform.localPosition = new Vector3(0f, 0f, -0.26f);

            Part(PrimitiveType.Cube, "TailPeduncle", tailPivot.transform, bodyMat, new Vector3(0f, 0f, -0.08f), new Vector3(0.05f, 0.09f, 0.16f), false);
            Part(PrimitiveType.Cube, "CaudalFin", tailPivot.transform, finMat, new Vector3(0f, 0f, -0.20f), new Vector3(0.018f, 0.22f, 0.16f), false);

            return root;
        }

        private static void BuildArchedRiverBridge(Transform parent, float spanX, float widthZ, Material matTimber, Material matDeck)
        {
            var bridge = new GameObject("Eldara_River_Bridge");
            bridge.transform.SetParent(parent, false);

            // Perhitungan geometri presisi agar ramp bridge menyentuh tanah/jalan (Y = 0) dengan sempurna
            float midLen = 6.0f;
            float rampLenX = (spanX - midLen) * 0.5f + 0.08f; // ~5.68m
            float deckY = 0.40f; // Dek tengah datar
            float topY = deckY + 0.07f; // 0.47m
            float slopeAngle = 4.75f;
            float rampLenSlope = rampLenX / Mathf.Cos(slopeAngle * Mathf.Deg2Rad);
            float rampCenterX = midLen * 0.5f + rampLenX * 0.5f;
            float rampCenterY = topY * 0.5f - 0.07f; // 0.165m

            // 1. Dek tengah datar
            Part(PrimitiveType.Cube, "Deck_Center", bridge.transform, matDeck,
                new Vector3(0f, deckY, 0f), new Vector3(midLen + 0.1f, 0.14f, widthZ), true);

            // 2. Ramp Timur (sloping DOWN ke arah +X menuju jalan poros timur)
            Part(PrimitiveType.Cube, "Ramp_East", bridge.transform, matDeck,
                new Vector3(rampCenterX, rampCenterY, 0f), new Vector3(rampLenSlope, 0.14f, widthZ), true,
                new Vector3(0f, 0f, -slopeAngle));

            // 3. Ramp Barat (sloping DOWN ke arah -X menuju jalan poros barat)
            Part(PrimitiveType.Cube, "Ramp_West", bridge.transform, matDeck,
                new Vector3(-rampCenterX, rampCenterY, 0f), new Vector3(rampLenSlope, 0.14f, widthZ), true,
                new Vector3(0f, 0f, slopeAngle));

            // 4. Smooth Incline Transition Colliders di ujung jalan untuk memastikan CharacterController berjalan 100% mulus
            var transE = new GameObject("SmoothRamp_East");
            transE.transform.SetParent(bridge.transform, false);
            transE.transform.localPosition = new Vector3(rampCenterX + 2.0f, 0.04f, 0f);
            transE.transform.localRotation = Quaternion.Euler(0f, 0f, -slopeAngle);
            var boxE = transE.AddComponent<BoxCollider>();
            boxE.size = new Vector3(1.8f, 0.10f, widthZ);

            var transW = new GameObject("SmoothRamp_West");
            transW.transform.SetParent(bridge.transform, false);
            transW.transform.localPosition = new Vector3(-rampCenterX - 2.0f, 0.04f, 0f);
            transW.transform.localRotation = Quaternion.Euler(0f, 0f, slopeAngle);
            var boxW = transW.AddComponent<BoxCollider>();
            boxW.size = new Vector3(1.8f, 0.10f, widthZ);

            // 5. Tiang pancang jembatan ke dasar sungai
            foreach (float px in new[] { -midLen * 0.45f, midLen * 0.45f })
            {
                foreach (float pz in new[] { -widthZ * 0.44f, widthZ * 0.44f })
                {
                    Part(PrimitiveType.Cylinder, "BridgePiling", bridge.transform, matTimber,
                        new Vector3(px, -0.15f, pz), new Vector3(0.28f, 0.65f, 0.28f), false);
                }
            }

            // 6. Pagar pembatas & railing yang mengikuti kontur jembatan secara natural
            foreach (float sz in new[] { -1f, 1f })
            {
                float pz = sz * (widthZ * 0.5f - 0.12f);

                // Railing tengah datar
                Part(PrimitiveType.Cube, "Handrail_Mid", bridge.transform, matTimber,
                    new Vector3(0f, deckY + 0.88f, pz), new Vector3(midLen, 0.10f, 0.12f), false);

                // Railing ramp timur melandai
                Part(PrimitiveType.Cube, "Handrail_East", bridge.transform, matTimber,
                    new Vector3(rampCenterX, rampCenterY + 0.88f, pz), new Vector3(rampLenSlope, 0.10f, 0.12f), false,
                    new Vector3(0f, 0f, -slopeAngle));

                // Railing ramp barat melandai
                Part(PrimitiveType.Cube, "Handrail_West", bridge.transform, matTimber,
                    new Vector3(-rampCenterX, rampCenterY + 0.88f, pz), new Vector3(rampLenSlope, 0.10f, 0.12f), false,
                    new Vector3(0f, 0f, slopeAngle));

                // Tiang-tiang pagar penyangga
                float[] postX = { -spanX * 0.48f, -spanX * 0.32f, -midLen * 0.5f, 0f, midLen * 0.5f, spanX * 0.32f, spanX * 0.48f };
                foreach (float px in postX)
                {
                    float py = (Mathf.Abs(px) <= midLen * 0.5f) ? (deckY + 0.44f) : (deckY + 0.44f - Mathf.Abs(px - Mathf.Sign(px) * midLen * 0.5f) * Mathf.Tan(slopeAngle * Mathf.Deg2Rad));
                    Part(PrimitiveType.Cube, "RailPost", bridge.transform, matTimber,
                        new Vector3(px, py, pz), new Vector3(0.16f, 0.95f, 0.16f), false);
                }

                // Barrier collider di sisi samping jembatan agar pemain tidak terpeleset jatuh ke sungai
                var barrier = new GameObject("SideBarrier");
                barrier.transform.SetParent(bridge.transform, false);
                barrier.transform.localPosition = new Vector3(0f, 0.75f, sz * (widthZ * 0.5f));
                var barCol = barrier.AddComponent<BoxCollider>();
                barCol.size = new Vector3(spanX + 0.2f, 1.2f, 0.15f);

                // Lentera gantung di 4 sudut tiang masuk jembatan
                AddBridgeLantern(bridge.transform, new Vector3( spanX * 0.48f, 1.05f, pz), new Color(1f, 0.78f, 0.35f));
                AddBridgeLantern(bridge.transform, new Vector3(-spanX * 0.48f, 1.05f, pz), new Color(1f, 0.78f, 0.35f));
            }
        }

        private static void BuildFishingDock(Transform parent, Vector3 localPos, Material matTimber, Material matDeck)
        {
            var dock = new GameObject("Fisherman_Wooden_Dock");
            dock.transform.SetParent(parent, false);
            dock.transform.localPosition = localPos;

            // Papan dermaga menjorok 4.5 meter ke atas air sungai
            Part(PrimitiveType.Cube, "Dock_Platform", dock.transform, matDeck, new Vector3(-2.0f, 0.26f, 0f), new Vector3(4.6f, 0.16f, 2.8f), true);

            // Tiang pancang kayu ke dalam sungai
            foreach (float px in new[] { -0.4f, -2.2f, -4.0f })
            {
                foreach (float pz in new[] { -1.2f, 1.2f })
                {
                    Part(PrimitiveType.Cylinder, "DockPiling", dock.transform, matTimber,
                        new Vector3(px, -0.25f, pz), new Vector3(0.22f, 0.65f, 0.22f), false);
                }
            }

            // Tambatan tali & bakul ikan hasil tangkapan di ujung dermaga
            Part(PrimitiveType.Cylinder, "MooringPost", dock.transform, matTimber, new Vector3(-4.0f, 0.65f, -1.2f), new Vector3(0.24f, 0.45f, 0.24f), true);
            var catchBasket = Part(PrimitiveType.Cylinder, "FreshFishBasket", dock.transform, matDeck, new Vector3(-3.4f, 0.48f, 0.85f), new Vector3(0.65f, 0.30f, 0.65f), true);

            // Tambahkan komponen ResourcePickup agar pemain bisa mengambil ikan segar dari dermaga
            var pickup = catchBasket.AddComponent<ResourcePickup>();
            pickup.itemId = "fish";
            pickup.amount = 1;
            pickup.prompt = "Tekan E untuk mengambil Ikan Sungai Segar";
        }

        private static void BuildWoodenRowboat(Transform parent, Vector3 localPos, float yawDeg, Material matTimber, Material matDeck)
        {
            var boat = new GameObject("Wooden_Fishing_Boat");
            boat.transform.SetParent(parent, false);
            boat.transform.localPosition = localPos;
            boat.transform.localRotation = Quaternion.Euler(0f, yawDeg, 0f);

            // Lambung perahu kayu
            Part(PrimitiveType.Cube, "HullBase", boat.transform, matTimber, new Vector3(0f, 0.05f, 0f), new Vector3(1.15f, 0.14f, 3.2f), false);
            Part(PrimitiveType.Cube, "Side_L", boat.transform, matDeck, new Vector3(-0.56f, 0.22f, 0f), new Vector3(0.10f, 0.36f, 3.3f), false, new Vector3(0f, 0f, 14f));
            Part(PrimitiveType.Cube, "Side_R", boat.transform, matDeck, new Vector3( 0.56f, 0.22f, 0f), new Vector3(0.10f, 0.36f, 3.3f), false, new Vector3(0f, 0f, -14f));
            Part(PrimitiveType.Cube, "Bow", boat.transform, matTimber, new Vector3(0f, 0.24f,  1.65f), new Vector3(0.75f, 0.38f, 0.35f), false, new Vector3(-20f, 0f, 0f));
            Part(PrimitiveType.Cube, "Stern", boat.transform, matTimber, new Vector3(0f, 0.24f, -1.65f), new Vector3(0.85f, 0.38f, 0.25f), false, new Vector3(15f, 0f, 0f));
            // Bangku dayung
            Part(PrimitiveType.Cube, "Seat_F", boat.transform, matTimber, new Vector3(0f, 0.25f,  0.55f), new Vector3(1.05f, 0.06f, 0.28f), false);
            Part(PrimitiveType.Cube, "Seat_B", boat.transform, matTimber, new Vector3(0f, 0.25f, -0.55f), new Vector3(1.05f, 0.06f, 0.28f), false);
        }

        private static Material GetOrCreateWaterMat()
        {
            var shader = Shader.Find("Eldara/RealisticWater") ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "M_River_WaterSurface" };
            if (shader.name == "Eldara/RealisticWater")
            {
                mat.SetColor("_DeepColor", new Color(0.04f, 0.36f, 0.58f, 0.92f));
                mat.SetColor("_ShallowColor", new Color(0.18f, 0.72f, 0.88f, 0.75f));
                mat.SetColor("_FresnelColor", new Color(0.85f, 0.95f, 1.0f, 1.0f));
                mat.SetFloat("_Smoothness", 0.96f);
            }
            else
            {
                Color waterCol = new Color(0.15f, 0.62f, 0.88f, 0.82f);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", waterCol);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", waterCol);
                if (mat.HasProperty("_Surface"))
                {
                    mat.SetFloat("_Surface", 1f);
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.renderQueue = 3000;
                }
            }
            return mat;
        }

        private static void AddBridgeLantern(Transform parent, Vector3 localPos, Color color, float range = 8f)
        {
            var lantern = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lantern.name = "BridgeLantern";
            lantern.transform.SetParent(parent, false);
            lantern.transform.localPosition = localPos;
            lantern.transform.localScale = new Vector3(0.18f, 0.28f, 0.18f);
            var col = lantern.GetComponent<Collider>();
            if (col) Object.DestroyImmediate(col);

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = MakeMat("BridgeLanternGlow", shader, color, 0.9f, 0f, color * 2.5f);
            lantern.GetComponent<Renderer>().sharedMaterial = mat;

            var light = lantern.AddComponent<Light>();
            light.color = color;
            light.range = range;
            light.intensity = 1.6f;
            light.shadows = LightShadows.None;
        }

        private static Material MakeMat(string name, Shader shader, Color col, float smoothness, float metallic = 0f, Color? emission = null)
        {
            var m = new Material(shader) { name = $"M_River_{name}" };
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

    /// <summary>
    /// Komponen animasi renang ikan 3D di dalam Sungai Eldara:
    /// - Berenang berkelok halus mengikuti arus sungai & berputar balik secara natural sebelum menyentuh tepian
    /// - Mengibaskan ekor dan tubuh secara ritmis sesuai kecepatan renang
    /// </summary>
    public class RiverFishSwimmer : MonoBehaviour
    {
        private Transform riverOrigin;
        private Transform tailPivot;
        private float boundHalfX;
        private float boundHalfZ;
        private float baseDepthY;
        private float swimSpeed;
        private float phaseOffset;
        private Vector3 targetLocalPos;
        private float retargetTimer;

        public void Configure(Transform origin, float halfX, float halfZ, float depthY, float speed, float offset)
        {
            riverOrigin = origin;
            boundHalfX = Mathf.Max(1.5f, halfX);
            boundHalfZ = Mathf.Max(4.0f, halfZ);
            baseDepthY = depthY;
            swimSpeed = speed;
            phaseOffset = offset;
            tailPivot = transform.Find("TailPivot");
            PickNextTarget();
        }

        void Start()
        {
            if (tailPivot == null) tailPivot = transform.Find("TailPivot");
            if (boundHalfX <= 0f)
            {
                boundHalfX = 5.5f;
                boundHalfZ = 50f;
                baseDepthY = transform.localPosition.y;
                swimSpeed = 2.0f;
            }
            PickNextTarget();
        }

        void PickNextTarget()
        {
            Vector3 cur = transform.localPosition;
            float nx = Random.Range(-boundHalfX, boundHalfX);
            float nz = Mathf.Clamp(cur.z + Random.Range(-18f, 18f), -boundHalfZ, boundHalfZ);
            float ny = Mathf.Clamp(baseDepthY + Random.Range(-0.02f, 0.02f), 0.04f, 0.10f);
            targetLocalPos = new Vector3(nx, ny, nz);
            retargetTimer = Random.Range(3.5f, 7.5f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            retargetTimer -= dt;
            Vector3 toTarget = targetLocalPos - transform.localPosition;
            if (retargetTimer <= 0f || toTarget.sqrMagnitude < 1.2f)
            {
                PickNextTarget();
                toTarget = targetLocalPos - transform.localPosition;
            }

            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, dt * 2.4f);
            }

            // Gerak maju + kibasan ekor & liukan badan
            float wave = Mathf.Sin((Time.time + phaseOffset) * swimSpeed * 4.5f);
            transform.localPosition += transform.localRotation * Vector3.forward * (swimSpeed * dt);

            if (tailPivot != null)
            {
                tailPivot.localRotation = Quaternion.Euler(0f, wave * 26f, 0f);
            }
            // Sedikit goyangan kepala berlawanan dengan ekor
            Vector3 euler = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(euler.x, euler.y + wave * 0.45f, wave * 3.5f);
        }
    }
}
