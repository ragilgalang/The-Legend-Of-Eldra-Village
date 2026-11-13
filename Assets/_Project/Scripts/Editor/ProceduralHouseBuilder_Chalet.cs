#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Chalet Salju Pegunungan — versi tembok kayu yang BISA DIMASUKI.
/// - Tembok kayu dibangun per-segmen sehingga pintu & jendela benar-benar berlubang (collider tidak menutup).
/// - Teras depan setinggi lantai (tangga 1 anak tangga), pintu terbuka, lantai dalam, balok atap,
///   perapian batu + cerobong, tempat tidur, meja, kursi, dan lentera.
/// - Atap curam bermantel salju tetap dipertahankan.
/// </summary>
public static partial class ProceduralHouseBuilder
{
    // ── Ukuran bangunan (satu tempat agar mudah disesuaikan) ──
    private const float ChaletHalfX   = 4.0f;   // setengah lebar (sumbu X) — as tembok samping
    private const float ChaletHalfZ   = 3.0f;   // setengah kedalaman (sumbu Z) — as tembok depan/belakang
    private const float ChaletWallT   = 0.25f;  // tebal tembok
    private const float ChaletFloorY  = 0.30f;  // permukaan fondasi / dasar tembok
    private const float ChaletWallTop = 3.60f;  // puncak tembok

    private struct WallOpening
    {
        public float center, width, sill, head;
        public WallOpening(float center, float width, float sill, float head)
        { this.center = center; this.width = width; this.sill = sill; this.head = head; }
    }

    // Potong satu potongan tembok (kotak berkollider)
    private static void WallPiece(GameObject root, bool alongX, float fixedPos,
        float a, float b, float y0, float y1, Material mat)
    {
        float len = b - a, h = y1 - y0;
        if (len < 0.01f || h < 0.01f) return;
        float mid = (a + b) * 0.5f;
        Box(root,
            alongX ? len : ChaletWallT, h, alongX ? ChaletWallT : len,
            mat,
            new Vector3(alongX ? mid : fixedPos, (y0 + y1) * 0.5f, alongX ? fixedPos : mid));
    }

    // Tembok kayu dengan lubang (pintu/jendela). alongX=true: tembok memanjang di sumbu X pada z=fixedPos.
    private static void WoodWall(GameObject root, bool alongX, float fixedPos,
        float min, float max, WallOpening[] ops, Material mat)
    {
        System.Array.Sort(ops, (p, q) => p.center.CompareTo(q.center));
        float cur = min;
        foreach (var op in ops)
        {
            float left = op.center - op.width * 0.5f;
            float right = op.center + op.width * 0.5f;
            WallPiece(root, alongX, fixedPos, cur, left, ChaletFloorY, ChaletWallTop, mat);      // tembok penuh sebelum lubang
            WallPiece(root, alongX, fixedPos, left, right, ChaletFloorY, op.sill, mat);           // bawah jendela
            WallPiece(root, alongX, fixedPos, left, right, op.head, ChaletWallTop, mat);          // atas lubang
            cur = right;
        }
        WallPiece(root, alongX, fixedPos, cur, max, ChaletFloorY, ChaletWallTop, mat);
    }

    private static void BuildChaletSalju(GameObject root)
    {
        float xo = ChaletHalfX + ChaletWallT * 0.5f;   // batas luar tembok depan/belakang
        float zo = ChaletHalfZ + ChaletWallT * 0.5f;   // batas luar tembok samping

        // ── Fondasi batu rendah + lantai papan di dalam ──
        Box(root, 8.5f, 0.30f, 6.5f, matConcrete, new Vector3(0, 0.15f, 0));
        Box(root, 7.6f, 0.06f, 5.6f, matDeck,     new Vector3(0, 0.33f, 0));

        // ── Tembok kayu (semua berlubang pintu/jendela) ──
        // Depan (z=+3): pintu di tengah + dua jendela
        WoodWall(root, true, ChaletHalfZ, -xo, xo, new[]
        {
            new WallOpening( 0.0f, 1.3f, ChaletFloorY, 2.5f),   // pintu
            new WallOpening(-2.3f, 1.3f, 1.2f, 2.4f),
            new WallOpening( 2.3f, 1.3f, 1.2f, 2.4f),
        }, matSlat);

        // Belakang (z=-3): satu jendela (sisi kanan dipakai perapian)
        WoodWall(root, true, -ChaletHalfZ, -xo, xo, new[]
        {
            new WallOpening(-1.5f, 1.3f, 1.2f, 2.4f),
        }, matSlat);

        // Samping kiri & kanan: dua jendela masing-masing
        foreach (float sx in new[] { -ChaletHalfX, ChaletHalfX })
        {
            WoodWall(root, false, sx, -zo, zo, new[]
            {
                new WallOpening(-1.4f, 1.2f, 1.2f, 2.4f),
                new WallOpening( 1.4f, 1.2f, 1.2f, 2.4f),
            }, matSlat);
        }

        // Tiang sudut kayu
        foreach (float cx in new[] { -ChaletHalfX, ChaletHalfX })
            foreach (float cz in new[] { -ChaletHalfZ, ChaletHalfZ })
                Box(root, 0.32f, ChaletWallTop - ChaletFloorY + 0.05f, 0.32f, matWood,
                    new Vector3(cx, (ChaletWallTop + ChaletFloorY) * 0.5f, cz), collider: false);

        // ── Kaca & bingkai jendela (di bidang tembok, di dalam lubang) ──
        Window(root, 1.3f, 1.2f, new Vector3(-2.3f, 1.8f,  ChaletHalfZ),  0f);
        Window(root, 1.3f, 1.2f, new Vector3( 2.3f, 1.8f,  ChaletHalfZ),  0f);
        Window(root, 1.3f, 1.2f, new Vector3(-1.5f, 1.8f, -ChaletHalfZ), 180f);
        Window(root, 1.2f, 1.2f, new Vector3(-ChaletHalfX, 1.8f, -1.4f), -90f);
        Window(root, 1.2f, 1.2f, new Vector3(-ChaletHalfX, 1.8f,  1.4f), -90f);
        Window(root, 1.2f, 1.2f, new Vector3( ChaletHalfX, 1.8f, -1.4f),  90f);
        Window(root, 1.2f, 1.2f, new Vector3( ChaletHalfX, 1.8f,  1.4f),  90f);

        // ── Bingkai pintu + daun pintu terbuka (tanpa collider supaya tidak menghalangi) ──
        Box(root, 0.10f, 2.2f, 0.34f, matWood, new Vector3(-0.70f, 1.40f, ChaletHalfZ), collider: false);
        Box(root, 0.10f, 2.2f, 0.34f, matWood, new Vector3( 0.70f, 1.40f, ChaletHalfZ), collider: false);
        Box(root, 1.50f, 0.12f, 0.34f, matDark, new Vector3(0, 2.56f, ChaletHalfZ), collider: false);
        // Daun pintu terbuka ke dalam (engsel di kiri)
        Box(root, 1.2f, 2.15f, 0.07f, matWood,
            new Vector3(-0.62f, 1.38f, ChaletHalfZ - 0.65f), new Vector3(0, 90f, 0), collider: false);

        // ── Teras depan setinggi lantai + tangga 1 anak tangga ──
        Box(root, 8.0f, 0.14f, 1.9f, matDeck, new Vector3(0, 0.23f, 3.95f));
        Box(root, 1.8f, 0.15f, 0.5f, matDeck, new Vector3(0, 0.075f, 5.15f));

        // Pagar teras (ada celah di tengah untuk jalan masuk)
        foreach (float px in new[] { -4.0f, -2.5f, -0.9f, 0.9f, 2.5f, 4.0f })
            Box(root, 0.1f, 0.95f, 0.1f, matWood, new Vector3(px, 0.78f, 4.75f), collider: false);
        Box(root, 3.1f, 0.08f, 0.12f, matWood, new Vector3(-2.45f, 1.25f, 4.75f), collider: false);
        Box(root, 3.1f, 0.08f, 0.12f, matWood, new Vector3( 2.45f, 1.25f, 4.75f), collider: false);
        Box(root, 3.1f, 0.05f, 0.06f, matWood, new Vector3(-2.45f, 0.80f, 4.75f), collider: false);
        Box(root, 3.1f, 0.05f, 0.06f, matWood, new Vector3( 2.45f, 0.80f, 4.75f), collider: false);
        // Tiang penyangga atap teras
        Cylinder(root, 0.11f, 2.3f, matWood, new Vector3(-3.9f, 1.45f, 4.55f));
        Cylinder(root, 0.11f, 2.3f, matWood, new Vector3( 3.9f, 1.45f, 4.55f));

        // ── Atap curam menjorok ke teras + mantel salju ──
        // baseY dipilih agar sisi bawah atap tepat bertemu puncak tembok depan/belakang (z = ±3).
        float rise = 3.0f, half = 4.6f, baseY = 2.6f;
        float pitch = Mathf.Atan(rise / half) * Mathf.Rad2Deg;
        float slope = Mathf.Sqrt(half * half + rise * rise);
        float midY = baseY + rise * 0.5f + 0.09f;
        Box(root, 9.2f, 0.18f, slope, matRoof, new Vector3(0, midY,  half * 0.5f), new Vector3( pitch, 0, 0));
        Box(root, 9.2f, 0.18f, slope, matRoof, new Vector3(0, midY, -half * 0.5f), new Vector3(-pitch, 0, 0));

        float cos = Mathf.Cos(pitch * Mathf.Deg2Rad), sin = Mathf.Sin(pitch * Mathf.Deg2Rad);
        float off = 0.15f; // tebal salju bertumpu di normal atap
        Box(root, 9.3f, 0.14f, slope - 0.3f, matSnow,
            new Vector3(0, midY + off * cos,  half * 0.5f + off * sin), new Vector3( pitch, 0, 0), collider: false);
        Box(root, 9.3f, 0.14f, slope - 0.3f, matSnow,
            new Vector3(0, midY + off * cos, -half * 0.5f - off * sin), new Vector3(-pitch, 0, 0), collider: false);

        Box(root, 9.3f, 0.16f, 0.30f, matDark, new Vector3(0, baseY + rise + 0.02f, 0), collider: false);
        Box(root, 9.4f, 0.20f, 0.55f, matSnow, new Vector3(0, baseY + rise + 0.18f, 0), collider: false);

        // Gable samping (menutup segitiga di atas tembok samping)
        GableTriangle(root, half * 2f, rise, 0.15f, matSlat, new Vector3(-ChaletHalfX, baseY, 0), -90f);
        GableTriangle(root, half * 2f, rise, 0.15f, matSlat, new Vector3( ChaletHalfX, baseY, 0),  90f);

        // ── Interior: balok melintang atap ──
        foreach (float bx in new[] { -2.8f, 0f, 2.8f })
            Box(root, 0.22f, 0.22f, 6.0f, matWood, new Vector3(bx, 3.45f, 0), collider: false);

        // ── Interior: perapian batu + cerobong menembus atap (di sudut belakang kanan) ──
        Box(root, 1.0f, 6.2f, 1.0f, matConcrete, new Vector3(2.2f, 3.4f, -2.3f));          // badan perapian + cerobong
        Box(root, 1.8f, 0.12f, 1.3f, matConcrete, new Vector3(2.2f, 0.36f, -1.45f), collider: false); // lantai tungku
        Box(root, 0.55f, 0.55f, 0.06f, matDark, new Vector3(2.2f, 0.75f, -1.78f), collider: false);   // mulut tungku
        Box(root, 1.2f, 0.12f, 1.2f, matDark, new Vector3(2.2f, 6.55f, -2.3f), collider: false);
        Box(root, 1.1f, 0.08f, 1.1f, matSnow, new Vector3(2.2f, 6.65f, -2.3f), collider: false);

        // ── Interior: tempat tidur (kiri belakang) ──
        Box(root, 2.0f, 0.35f, 1.2f, matWood, new Vector3(-2.85f, 0.535f, -2.2f));
        Box(root, 1.9f, 0.14f, 1.1f, matSnow, new Vector3(-2.85f, 0.78f, -2.2f), collider: false);
        Box(root, 0.5f, 0.12f, 0.8f, matSnow, new Vector3(-3.55f, 0.90f, -2.2f), collider: false);   // bantal
        Box(root, 0.12f, 0.9f, 1.2f, matWood, new Vector3(-3.88f, 0.80f, -2.2f), collider: false);   // sandaran kepala

        // ── Interior: meja + dua kursi ──
        Box(root, 1.2f, 0.08f, 0.8f, matWood, new Vector3(-1.8f, 1.05f, 0.9f));
        foreach (float lx in new[] { -2.3f, -1.3f })
            foreach (float lz in new[] { 0.6f, 1.2f })
                Box(root, 0.08f, 0.69f, 0.08f, matWood, new Vector3(lx, 0.705f, lz), collider: false);
        Box(root, 0.45f, 0.06f, 0.45f, matWood, new Vector3(-1.8f, 0.62f, 1.55f));
        Box(root, 0.45f, 0.06f, 0.45f, matWood, new Vector3(-1.8f, 0.62f, 0.25f));

        // ── Interior: rak dinding di tembok belakang ──
        Box(root, 1.6f, 0.06f, 0.3f, matWood, new Vector3(-1.5f, 2.6f, -2.8f), collider: false);

        // ── Lentera: dalam (gantung dari balok), dua di teras ──
        Box(root, 0.03f, 0.35f, 0.03f, matDark, new Vector3(0f, 3.25f, 0.5f), collider: false);
        Lantern(root, new Vector3(0f, 2.95f, 0.5f));
        Lantern(root, new Vector3(-3.9f, 2.2f, 4.3f));
        Lantern(root, new Vector3( 3.9f, 2.2f, 4.3f));

        // ── Timbunan salju di tanah ──
        Sphere(root, 1.0f, matSnow, new Vector3(-5.0f, 0.05f,  2.5f), new Vector3(1.6f, 0.25f, 1.2f));
        Sphere(root, 1.0f, matSnow, new Vector3( 5.2f, 0.05f, -1.5f), new Vector3(1.4f, 0.22f, 1.5f));
    }
}
#endif
