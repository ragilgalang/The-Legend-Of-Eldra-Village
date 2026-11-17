#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static partial class SceneSetupTool
{
    // ─────────────────────────────────────────────────────────────────────
    //  6. NPC POPULATION (10 PENDUDUK TRADISIONAL RIMBA ELDARA)
    // ─────────────────────────────────────────────────────────────────────
    static void SetupNPCs()
    {
        GameObject npcRoot = GameObject.Find("--- NPCs ---");
        if (npcRoot) Object.DestroyImmediate(npcRoot);
        npcRoot = new GameObject("--- NPCs ---");

        var npcConfigs = new (
            string npcName,
            ProceduralNpcBuilder.NpcStyle style,
            Vector3 startPos,
            float yRot,
            string greeting,
            Vector3[] route
        )[]
        {
            // 1. Tetua / Kepala Desa
            ("NPC_KepalaDesa",
             ProceduralNpcBuilder.NpcStyle.Merchant,
             new Vector3(0f, 0.9f, 18.0f), 180f,
             "Selamat datang, petualang! Desa Eldara di tengah rimba ini membutuhkan bantuanmu. Saat malam tiba, makhluk kegelapan akan menyerang dari balik hutan.",
             new Vector3[] {
                 new Vector3(0f, 0.9f,  18.0f),
                 new Vector3(0f, 0.14f, 14.5f),
                 new Vector3(0f, 0.14f,  8.5f),
                 new Vector3(0f, 0.14f, 14.5f)
             }),

            // 2. Petani Ladang Rimba
            ("NPC_Petani",
             ProceduralNpcBuilder.NpcStyle.Farmer,
             new Vector3(-14.5f, 0.14f, 0f), 90f,
             "Kawanan serigala hutan sering merusak pagar ladang kami di sisi barat! Tolong bantu jaga desa dan kumpulkan kayu untuk memperkuat benteng.",
             new Vector3[] {
                 new Vector3(-14.5f, 0.14f,  0.0f),
                 new Vector3(-10.2f, 0.14f, 10.2f),
                 new Vector3(-14.5f, 0.14f,  0.0f),
                 new Vector3(-10.2f, 0.14f,-10.2f)
             }),

            // 3. Pandai Besi & Penjaga Rimba
            ("NPC_PandaiBesi",
             ProceduralNpcBuilder.NpcStyle.Guard,
             new Vector3(14.5f, 0.14f, 0f), 270f,
             "Salam, ksatria! Aku menempa mata panah baja untuk menara pengawas desa. Bawakan bijih besi dari tambang timur jika ingin memperkuat pertahanan kita!",
             new Vector3[] {
                 new Vector3(14.5f, 0.14f,  0.0f),
                 new Vector3(10.2f, 0.14f, 10.2f),
                 new Vector3(14.5f, 0.14f,  0.0f),
                 new Vector3(10.2f, 0.14f,-10.2f)
             }),

            // 4. Dukun / Tabib Hutan
            ("NPC_Dukun",
             ProceduralNpcBuilder.NpcStyle.Wizard,
             new Vector3(10.2f, 0.14f, -10.2f), 315f,
             "Roh hutan berbisik gelisah di sekitar Altar Segel Kuno... Jangan lengah saat kabut malam turun menyelimuti Eldara.",
             new Vector3[] {
                 new Vector3( 10.2f, 0.14f, -10.2f),
                 new Vector3(  0.0f, 0.14f, -14.5f),
                 new Vector3(-10.2f, 0.14f, -10.2f),
                 new Vector3(  0.0f, 0.14f, -14.5f)
             }),

            // 5. Gadis Penenun Desa
            ("NPC_GadisPenenun",
             ProceduralNpcBuilder.NpcStyle.Woman,
             new Vector3(0f, 0.05f, 4.5f), 180f,
             "Halo petualang! Kain tenun dan batik Eldara dibuat dari pewarna alami akar dan daun rimba. Hati-hati di perjalananmu ya!",
             new Vector3[] {
                 new Vector3( 0.0f, 0.05f,  4.5f),
                 new Vector3( 4.5f, 0.05f,  0.0f),
                 new Vector3( 0.0f, 0.05f, -4.5f),
                 new Vector3(-4.5f, 0.05f,  0.0f)
             }),

            // 6. Penebang Kayu Hutan
            ("NPC_PenebangKayu",
             ProceduralNpcBuilder.NpcStyle.Woodcutter,
             new Vector3(-22.0f, 0.05f, 22.0f), 135f,
             "Kayu besi dari rimba Eldara sangat kokoh untuk membangun pagar palisade dan menara panah. Kamu bisa mengambil tumpukan balok kayu di sisi barat!",
             new Vector3[] {
                 new Vector3(-22.0f, 0.05f,  22.0f),
                 new Vector3(-31.5f, 0.05f,   0.0f),
                 new Vector3(-22.0f, 0.05f, -22.0f),
                 new Vector3(-31.5f, 0.05f,   0.0f)
             }),

            // 7. Pemburu & Pengintai Rimba
            ("NPC_PemburuRimba",
             ProceduralNpcBuilder.NpcStyle.Hunter,
             new Vector3(-3.5f, 0.05f, -48f), 0f,
             "Aku baru kembali berpatroli dari batas hutan selatan. Jejak kaki goblin terlihat di balik pepohonan lebat—tetap waspada!",
             new Vector3[] {
                 new Vector3(-3.5f, 0.05f, -48f),
                 new Vector3( 3.5f, 0.05f, -48f),
                 new Vector3( 0.0f, 0.05f, -36f),
                 new Vector3( 0.0f, 0.05f, -52f)
             }),

            // 8. Nenek Peracik Jamu
            ("NPC_NenekJamu",
             ProceduralNpcBuilder.NpcStyle.Herbalist,
             new Vector3(6.5f, 0.05f, -18.5f), 320f,
             "Ramuan daun rimba dan kunyit hutan ini ampuh memulihkan tenaga setelah bertarung. Singgahlah kemari kapan saja, nak.",
             new Vector3[] {
                 new Vector3( 6.5f, 0.05f, -18.5f),
                 new Vector3( 9.5f, 0.05f, -15.0f),
                 new Vector3( 5.0f, 0.05f, -14.5f),
                 new Vector3( 6.5f, 0.05f, -18.5f)
             }),

            // 9. Nelayan Sungai Eldara (berpatroli di atas Dermaga Kayu & Jembatan Sungai)
            ("NPC_NelayanDesa",
             ProceduralNpcBuilder.NpcStyle.Fisherman,
             new Vector3(-50.5f, 0.34f, -14.0f), 270f,
             "Sungai Eldara sedang melimpah ikan mas dan patin hari ini! Ambil saja ikan segar di bakul dermaga kalau kamu butuh perbekalan.",
             new Vector3[] {
                 new Vector3(-53.5f, 0.34f, -14.0f), // Ujung dermaga kayu
                 new Vector3(-50.0f, 0.14f, -14.0f), // Pangkal dermaga
                 new Vector3(-50.0f, 0.14f,  -2.5f), // Tepi sungai dekat jembatan
                 new Vector3(-53.5f, 0.34f, -14.0f)  // Kembali ke dermaga
             }),

            // 10. Pemuda Pengantar Pesan
            ("NPC_PemudaDesa",
             ProceduralNpcBuilder.NpcStyle.VillageYouth,
             new Vector3(22.0f, 0.05f, 22.0f), 225f,
             "Aku bertugas mengantar kabar antar-pos menara pengawas desa! Semoga semua gerbang tetap aman sampai fajar tiba.",
             new Vector3[] {
                 new Vector3( 22.0f, 0.05f,  22.0f),
                 new Vector3( 31.5f, 0.05f,   0.0f),
                 new Vector3( 22.0f, 0.05f, -22.0f),
                 new Vector3(  0.0f, 0.05f, -31.5f)
             })
        };

        foreach (var (npcName, style, startPos, yRot, greeting, route) in npcConfigs)
        {
            var npcGo = ProceduralNpcBuilder.BuildNpc(style, startPos, Quaternion.Euler(0, yRot, 0), npcRoot.transform, npcName);
            var npcDlg = npcGo.GetComponent<NpcDialogue>() ?? npcGo.AddComponent<NpcDialogue>();
            CreateSimpleDialogue(npcName, greeting, npcDlg);

            var mover = npcGo.GetComponent<NpcMover>();
            if (mover)
            {
                mover.SetWaypoints(new List<Vector3>(route));
                mover.moveSpeed = 1.35f;
                mover.waitTimeAtWaypoint = Random.Range(3.5f, 5.5f);
            }
        }
    }
}
#endif
