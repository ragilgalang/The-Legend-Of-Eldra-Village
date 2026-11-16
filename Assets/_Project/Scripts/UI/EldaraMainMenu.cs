using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Main Menu "Legenda Desa Eldara".
/// Pasang script ini pada 1 GameObject kosong di scene MainMenu.
/// UI + background pemandangan desa dibangun otomatis saat Play (tidak butuh asset tambahan).
/// </summary>
public class EldaraMainMenu : MonoBehaviour
{
    [Header("Scene")]
    public string gameSceneName = "SampleScene";

    [Header("Teks")]
    public string title    = "LEGENDA DESA ELDARA";
    public string subtitle = "Pertahankan desa hingga fajar menyingsing";

    [Header("Warna Tema")]
    public Color goldColor   = new Color(0.96f, 0.80f, 0.40f, 1f);
    public Color buttonColor = new Color(0.10f, 0.12f, 0.20f, 0.95f);

    // ── Referensi internal ──
    Font         font;
    GameObject   mainPanel, settingsPanel;
    Text         volumeLabel;
    Text         sensLabel;
    Text         chunkLabel;
    Texture2D    bgTex;

    // ───────────────────────────────────────────────────────────────────────
    //  INISIALISASI
    // ───────────────────────────────────────────────────────────────────────
    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        AudioListener.volume = PlayerPrefs.GetFloat("eldara_volume", 1f);

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (!font) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        if (!EventSystem.current)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        BuildUI();
    }

    // ───────────────────────────────────────────────────────────────────────
    //  BANGUN SELURUH UI
    // ───────────────────────────────────────────────────────────────────────
    void BuildUI()
    {
        var canvasGO = new GameObject("MainMenuCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        var root = canvasGO.transform;

        // ── 1. BACKGROUND PEMANDANGAN DESA ──
        bgTex = GenerateVillageBackground(1920, 1080);
        var bgRaw = MakeRawImage("Background", root, bgTex);
        Stretch(bgRaw.rectTransform);
        bgRaw.raycastTarget = false;

        // Overlay gelap tipis di bawah (agar teks terbaca)
        var vignette = MakeImage("Vignette", root, new Color(0f, 0f, 0f, 0f));
        Stretch(vignette.rectTransform);
        vignette.raycastTarget = false;

        // Pendaran keemasan di bawah judul (efek cahaya obor desa)
        var warmGlow = MakeImage("WarmGlow", root, new Color(0.80f, 0.50f, 0.10f, 0.13f));
        Center(warmGlow.rectTransform, new Vector2(1300, 600), new Vector2(0, 90));
        warmGlow.raycastTarget = false;

        // Garis dekorasi horizontal emas di bawah judul
        var line = MakeImage("TitleLine", root, goldColor);
        Center(line.rectTransform, new Vector2(640, 3), new Vector2(0, 175));
        line.raycastTarget = false;

        // ── 2. PANEL UTAMA ──
        mainPanel = MakePanel("MainPanel", root);

        var t = MakeText("Title", mainPanel.transform, title, 92, goldColor, FontStyle.Bold);
        Center(t.rectTransform, new Vector2(1600, 140), new Vector2(0, 275));
        var sh = t.gameObject.AddComponent<Shadow>();
        sh.effectColor    = new Color(0.1f, 0.0f, 0.0f, 0.9f);
        sh.effectDistance = new Vector2(5, -5);
        // Outline emas tipis supaya kontras di langit cerah
        var ol = t.gameObject.AddComponent<Outline>();
        ol.effectColor    = new Color(0.60f, 0.40f, 0.05f, 0.8f);
        ol.effectDistance = new Vector2(2, -2);

        var s = MakeText("Subtitle", mainPanel.transform, subtitle, 32,
                         new Color(0.95f, 0.90f, 0.75f, 0.95f), FontStyle.Italic);
        Center(s.rectTransform, new Vector2(1200, 50), new Vector2(0, 128));
        s.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);

        MakeButton("PlayButton",     mainPanel.transform, "▶  MULAI PETUALANGAN",  new Vector2(0, -30 )).onClick.AddListener(OnPlay);
        MakeButton("SettingsButton", mainPanel.transform, "⚙  PENGATURAN",         new Vector2(0, -155)).onClick.AddListener(() => ShowSettings(true));
        MakeButton("QuitButton",     mainPanel.transform, "✕  KELUAR",             new Vector2(0, -280)).onClick.AddListener(() => Application.Quit());

        var footer = MakeText("Footer", mainPanel.transform,
            "Kelompok 4  •  Multimedia Permainan  •  Politeknik Negeri Jember  •  2026",
            20, new Color(1, 1, 1, 0.45f), FontStyle.Normal);
        Center(footer.rectTransform, new Vector2(1400, 36), new Vector2(0, -470));

        // ── 3. PANEL PENGATURAN ──
        BuildSettingsPanel(root);
        ShowSettings(false);
    }

    // ───────────────────────────────────────────────────────────────────────
    //  PANEL PENGATURAN
    // ───────────────────────────────────────────────────────────────────────
    void BuildSettingsPanel(Transform parent)
    {
        settingsPanel = MakePanel("SettingsPanel", parent);

        var dim = MakeImage("Dim", settingsPanel.transform, new Color(0, 0, 0, 0.75f));
        Stretch(dim.rectTransform);

        // Kotak dialog pengaturan dengan proporsi pas dan lega
        var box = MakeImage("Box", settingsPanel.transform, new Color(0.07f, 0.08f, 0.14f, 0.98f));
        Center(box.rectTransform, new Vector2(880, 800), Vector2.zero);
        var outl = box.gameObject.AddComponent<Outline>();
        outl.effectColor    = goldColor;
        outl.effectDistance = new Vector2(3, -3);

        var head = MakeText("Header", box.transform, "PENGATURAN", 52, goldColor, FontStyle.Bold);
        Center(head.rectTransform, new Vector2(700, 60), new Vector2(0, 340));

        // ── 1. VOLUME SUARA ──
        float vol = PlayerPrefs.GetFloat("eldara_volume", 1f);
        volumeLabel = MakeText("VolumeLabel", box.transform,
            "Volume Suara: " + Mathf.RoundToInt(vol * 100) + "%", 28, Color.white, FontStyle.Normal);
        Center(volumeLabel.rectTransform, new Vector2(640, 36), new Vector2(0, 275));

        var volSlider = MakeSlider(box.transform, new Vector2(0, 235), vol);
        volSlider.onValueChanged.AddListener(v =>
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat("eldara_volume", v);
            volumeLabel.text = "Volume Suara: " + Mathf.RoundToInt(v * 100) + "%";
        });

        // ── 2. PENGATURAN KAMERA (SENSITIVITAS MOUSE) ──
        float sens = PlayerPrefs.GetFloat("eldara_cam_sensitivity", 0.12f);
        sensLabel = MakeText("SensLabel", box.transform,
            $"Sensitivitas Kamera: {Mathf.RoundToInt((sens / 0.12f) * 100)}%", 28, Color.white, FontStyle.Normal);
        Center(sensLabel.rectTransform, new Vector2(640, 36), new Vector2(0, 175));

        float normSens = Mathf.InverseLerp(0.03f, 0.30f, sens);
        var sensSlider = MakeSlider(box.transform, new Vector2(0, 135), normSens);
        sensSlider.onValueChanged.AddListener(v =>
        {
            float actualSens = Mathf.Lerp(0.03f, 0.30f, v);
            PlayerPrefs.SetFloat("eldara_cam_sensitivity", actualSens);
            sensLabel.text = $"Sensitivitas Kamera: {Mathf.RoundToInt((actualSens / 0.12f) * 100)}%";
            if (CameraRig.Instance != null)
                CameraRig.Instance.Sensitivity = actualSens;
        });

        // ── 3. JARAK RENDER CHUNK (DUNIA / POHON & RUMPUT) ──
        int chunkDist = PlayerPrefs.GetInt("eldara_chunk_distance", 4);
        chunkLabel = MakeText("ChunkLabel", box.transform,
            FormatChunkLabel(chunkDist), 28, Color.white, FontStyle.Normal);
        Center(chunkLabel.rectTransform, new Vector2(700, 36), new Vector2(0, 75));

        float normChunk = Mathf.InverseLerp(2f, 10f, chunkDist);
        var chunkSlider = MakeSlider(box.transform, new Vector2(0, 35), normChunk);
        chunkSlider.onValueChanged.AddListener(v =>
        {
            int actualChunks = Mathf.RoundToInt(Mathf.Lerp(2f, 10f, v));
            if (actualChunks % 2 != 0) actualChunks += 1;
            actualChunks = Mathf.Clamp(actualChunks, 2, 10);
            PlayerPrefs.SetInt("eldara_chunk_distance", actualChunks);
            chunkLabel.text = FormatChunkLabel(actualChunks);

            if (ChunkRenderDistance.Instance != null)
                ChunkRenderDistance.Instance.SetRenderDistance(actualChunks);
        });

        // ── 4. BARIS PILIHAN: MODE KAMERA & KUALITAS GRAFIS ──
        // Mode Kamera
        int camMode = PlayerPrefs.GetInt("eldara_cam_mode", 1);
        var camTitle = MakeText("CamTitle", box.transform, "Mode Kamera", 26, Color.white, FontStyle.Normal);
        Center(camTitle.rectTransform, new Vector2(280, 32), new Vector2(-170, -35));

        var camBtn = MakeButton("CamModeButton", box.transform, FormatCameraModeName(camMode),
                                new Vector2(-170, -80), new Vector2(280, 52), 22);
        camBtn.onClick.AddListener(() =>
        {
            int cur = PlayerPrefs.GetInt("eldara_cam_mode", 1);
            cur = (cur + 1) % 3;
            PlayerPrefs.SetInt("eldara_cam_mode", cur);
            camBtn.GetComponentInChildren<Text>().text = FormatCameraModeName(cur);
            if (CameraRig.Instance != null)
                CameraRig.Instance.SetMode((CameraMode)cur);
        });

        // Kualitas Grafis
        var qLabel = MakeText("QualityLabel", box.transform, "Kualitas Grafis", 26, Color.white, FontStyle.Normal);
        Center(qLabel.rectTransform, new Vector2(280, 32), new Vector2(170, -35));

        var qBtn = MakeButton("QualityButton", box.transform, QualityName(),
                              new Vector2(170, -80), new Vector2(280, 52), 24);
        qBtn.onClick.AddListener(() =>
        {
            int next = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
            QualitySettings.SetQualityLevel(next, true);
            qBtn.GetComponentInChildren<Text>().text = QualityName();
        });

        // ── 5. TOGGLE LAYAR PENUH ──
        var toggle = MakeToggle(box.transform, "Layar Penuh", new Vector2(0, -155), Screen.fullScreen);
        toggle.onValueChanged.AddListener(on => Screen.fullScreen = on);

        // ── 6. TOMBOL KEMBALI ──
        var back = MakeButton("BackButton", box.transform, "← KEMBALI",
                              new Vector2(0, -280), new Vector2(340, 68), 32);
        back.onClick.AddListener(() =>
        {
            PlayerPrefs.Save();
            ShowSettings(false);
        });
    }

    static string FormatChunkLabel(int chunks)
    {
        int meter = chunks * 25;
        string desc = chunks switch
        {
            <= 2 => "Sangat Ringan",
            <= 4 => "Standar (Seimbang)",
            <= 6 => "Jauh",
            <= 8 => "Sangat Jauh",
            _    => "Ultra"
        };
        return $"Jarak Render Chunk: {chunks} Chunk ({meter} m) • {desc}";
    }

    static string FormatCameraModeName(int mode)
    {
        return mode switch
        {
            0 => "Orang Pertama",
            1 => "Orang Ketiga (Belakang)",
            2 => "Orang Ketiga (Depan)",
            _ => "Orang Ketiga"
        };
    }

    string QualityName() => QualitySettings.names[QualitySettings.GetQualityLevel()];

    // ───────────────────────────────────────────────────────────────────────
    //  GENERATE BACKGROUND: PEMANDANGAN DESA MALAM (prosedural, tanpa asset)
    //
    //  Lapisan dari belakang ke depan:
    //   1. Gradien langit malam (biru tua → ungu → oranye fajar di bawah)
    //   2. Bulan purnama + cincin cahaya
    //   3. Bintang bertebaran
    //   4. Kabut / aurora tipis
    //   5. Pegunungan jauh (siluet hitam pekat berlapis)
    //   6. Hutan tengah (siluet pohon tradisional)
    //   7. Desa: rumah gadang + menara + pagar
    //   8. Cahaya obor di jendela (titik-titik hangat)
    //   9. Kabut bawah (ground fog tipis)
    // ───────────────────────────────────────────────────────────────────────
    Texture2D GenerateVillageBackground(int W, int H)
    {
        var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[W * H];

        // ── Helper: set pixel (clamp) ──
        void Set(int x, int y, Color c)
        {
            if (x < 0 || x >= W || y < 0 || y >= H) return;
            px[y * W + x] = c;
        }
        Color Get(int x, int y)
        {
            x = Mathf.Clamp(x, 0, W - 1);
            y = Mathf.Clamp(y, 0, H - 1);
            return px[y * W + x];
        }
        void Blend(int x, int y, Color c, float alpha)
        {
            if (x < 0 || x >= W || y < 0 || y >= H) return;
            var old = px[y * W + x];
            px[y * W + x] = Color.Lerp(old, c, alpha);
        }

        var rng = new System.Random(42);

        // ─────────────────────────────────────
        //  1. GRADIEN LANGIT MALAM
        // ─────────────────────────────────────
        var skyTop    = new Color(0.03f, 0.04f, 0.14f); // biru tengah malam
        var skyMid    = new Color(0.06f, 0.05f, 0.20f); // ungu medium
        var skyHoriz  = new Color(0.22f, 0.10f, 0.08f); // merah tembaga (cahaya desa)
        var skyGlow   = new Color(0.60f, 0.28f, 0.06f); // oranye fajar / obor di horizon

        int horizonY = (int)(H * 0.30f); // garis horizon (30% dari bawah)

        for (int y = 0; y < H; y++)
        {
            float nt = (float)y / H; // 0 = bawah, 1 = atas
            Color sky;
            if (nt < 0.30f)
            {
                // Bawah: oranye obor → merah tembaga
                float t2 = nt / 0.30f;
                sky = Color.Lerp(skyGlow, skyHoriz, t2);
            }
            else if (nt < 0.55f)
            {
                // Tengah: merah tembaga → ungu
                float t2 = (nt - 0.30f) / 0.25f;
                sky = Color.Lerp(skyHoriz, skyMid, t2);
            }
            else
            {
                // Atas: ungu → biru tengah malam
                float t2 = (nt - 0.55f) / 0.45f;
                sky = Color.Lerp(skyMid, skyTop, t2);
            }

            for (int x = 0; x < W; x++)
                Set(x, y, sky);
        }

        // ─────────────────────────────────────
        //  2. BULAN PURNAMA
        // ─────────────────────────────────────
        int moonX = (int)(W * 0.78f);
        int moonY = (int)(H * 0.80f);
        int moonR = (int)(H * 0.085f);

        for (int dy = -moonR - 40; dy <= moonR + 40; dy++)
        for (int dx = -moonR - 40; dx <= moonR + 40; dx++)
        {
            float dist = Mathf.Sqrt(dx * dx + dy * dy);
            if (dist <= moonR)
            {
                // Badan bulan: putih kekuningan dengan highlight sedikit
                float shade = 1f - Mathf.Clamp01((dist / moonR) * 0.25f);
                float nx2 = (float)dx / moonR, ny2 = (float)dy / moonR;
                float rim = Mathf.Clamp01(1f - Mathf.Sqrt((nx2 + 0.3f) * (nx2 + 0.3f) + (ny2 + 0.2f) * (ny2 + 0.2f)) * 0.9f);
                Color moon = Color.Lerp(
                    new Color(0.92f, 0.88f, 0.72f),
                    new Color(1.00f, 0.98f, 0.90f),
                    rim * shade);
                Set(moonX + dx, moonY + dy, moon);
            }
            else if (dist <= moonR + 40)
            {
                // Halo bulan
                float a = 1f - (dist - moonR) / 40f;
                a = a * a * 0.35f;
                Blend(moonX + dx, moonY + dy, new Color(0.80f, 0.76f, 0.55f), a);
            }
        }

        // ─────────────────────────────────────
        //  3. BINTANG
        // ─────────────────────────────────────
        int starCount = 420;
        for (int i = 0; i < starCount; i++)
        {
            int sx = rng.Next(0, W);
            int sy = (int)(H * 0.35f) + rng.Next(0, (int)(H * 0.65f));
            float brightness = 0.5f + (float)rng.NextDouble() * 0.5f;
            float size = (float)rng.NextDouble();
            Color starCol = Color.Lerp(new Color(0.75f, 0.80f, 1.00f), Color.white, (float)rng.NextDouble());
            starCol.a = brightness;
            Set(sx, sy, starCol);
            if (size > 0.7f)
            {
                // Bintang sedikit lebih besar + sinar silang
                Blend(sx + 1, sy, starCol, brightness * 0.4f);
                Blend(sx - 1, sy, starCol, brightness * 0.4f);
                Blend(sx, sy + 1, starCol, brightness * 0.4f);
                Blend(sx, sy - 1, starCol, brightness * 0.4f);
            }
        }

        // ─────────────────────────────────────
        //  4. KABUT / AURORA TIPIS
        // ─────────────────────────────────────
        // Aurora horizontal tipis di langit atas
        for (int y = (int)(H * 0.65f); y < H; y++)
        {
            float nt = (float)(y - H * 0.65f) / (H * 0.35f);
            for (int x = 0; x < W; x++)
            {
                float wave = Mathf.Sin(x * 0.003f + nt * 2f) * 0.5f + 0.5f;
                float a    = wave * nt * 0.07f;
                Blend(x, y, new Color(0.30f, 0.55f, 0.90f), a);
            }
        }

        // ─────────────────────────────────────
        //  5. PEGUNUNGAN JAUH (siluet 2 lapis)
        // ─────────────────────────────────────
        // Layer A: gunung jauh — abu biru tua, berlekuk halus
        int mountainBaseA = horizonY + (int)(H * 0.08f);
        for (int x = 0; x < W; x++)
        {
            float nx = x / (float)W;
            float h1 = Mathf.Sin(nx * 2.1f) * 0.055f + Mathf.Sin(nx * 5.7f) * 0.030f
                     + Mathf.Sin(nx * 11.3f) * 0.015f + Mathf.Cos(nx * 3.4f + 1.2f) * 0.040f;
            int peakA = mountainBaseA + (int)(h1 * H * 1.1f);
            var colA = new Color(0.09f, 0.09f, 0.18f);
            for (int y = 0; y <= peakA && y < H; y++)
                Set(x, y, colA);
        }

        // Layer B: gunung dekat — lebih gelap, lebih besar
        int mountainBaseB = horizonY + (int)(H * 0.04f);
        for (int x = 0; x < W; x++)
        {
            float nx = x / (float)W;
            float h2 = Mathf.Sin(nx * 1.6f + 0.5f) * 0.07f + Mathf.Sin(nx * 4.2f + 1f) * 0.04f
                     + Mathf.Sin(nx * 8.8f + 0.3f) * 0.02f + Mathf.Cos(nx * 2.5f + 2.1f) * 0.055f;
            int peakB = mountainBaseB + (int)(h2 * H * 0.9f);
            var colB = new Color(0.06f, 0.06f, 0.11f);
            for (int y = 0; y <= peakB && y < H; y++)
                Set(x, y, colB);
        }

        // ─────────────────────────────────────
        //  6. HUTAN TENGAH (siluet pohon)
        // ─────────────────────────────────────
        // Pohon: segitiga susun (pinus tradisional) + pohon tropis (oval)
        var treeCol = new Color(0.04f, 0.07f, 0.05f);

        // Lajur hutan kiri
        for (int i = 0; i < 22; i++)
        {
            int tx  = (int)(W * (0.01f + i * 0.028f));
            int ty  = (int)(H * 0.26f);
            int th  = (int)(H * (0.12f + (rng.NextDouble() * 0.07f)));
            int tw  = (int)(th * (0.45f + rng.NextDouble() * 0.25f));
            DrawTree(px, W, H, tx, ty, tw, th, treeCol);
        }

        // Lajur hutan kanan
        for (int i = 0; i < 20; i++)
        {
            int tx  = (int)(W * (0.60f + i * 0.025f));
            int ty  = (int)(H * 0.26f);
            int th  = (int)(H * (0.11f + rng.NextDouble() * 0.08f));
            int tw  = (int)(th * (0.40f + rng.NextDouble() * 0.25f));
            DrawTree(px, W, H, tx, ty, tw, th, treeCol);
        }

        // ─────────────────────────────────────
        //  7. BANGUNAN DESA
        //  Urutan: rumah warga kecil → rumah gadang → balai desa → menara
        // ─────────────────────────────────────
        var buildCol  = new Color(0.08f, 0.055f, 0.04f);  // kayu gelap
        var roofCol   = new Color(0.14f, 0.07f, 0.04f);   // genteng kayu tua
        var wallCol   = new Color(0.10f, 0.070f, 0.05f);  // dinding kayu agak terang
        var darkCol   = new Color(0.04f, 0.030f, 0.02f);  // shadow sangat gelap

        int groundY = (int)(H * 0.26f); // garis tanah desa

        // Rumah warga kecil — kiri bawah
        DrawHouseSimple(px, W, H, (int)(W * 0.17f), groundY, 100, 68, roofCol, wallCol);
        DrawHouseSimple(px, W, H, (int)(W * 0.24f), groundY, 88,  58, roofCol, buildCol);
        DrawHouseSimple(px, W, H, (int)(W * 0.30f), groundY, 95,  62, darkCol, wallCol);

        // Rumah warga kecil — kanan bawah
        DrawHouseSimple(px, W, H, (int)(W * 0.70f), groundY, 92,  60, roofCol, wallCol);
        DrawHouseSimple(px, W, H, (int)(W * 0.76f), groundY, 100, 65, darkCol, buildCol);
        DrawHouseSimple(px, W, H, (int)(W * 0.83f), groundY, 86,  56, roofCol, wallCol);

        // Rumah Gadang (kiri-tengah, atap lengkung tanduk kerbau)
        DrawRumahGadang(px, W, H, (int)(W * 0.32f), groundY, 230, 110, darkCol, roofCol, wallCol);

        // Balai Desa / Rumah Utama (tengah)
        DrawBalaiDesa(px, W, H, (int)(W * 0.50f), groundY, 280, 130, buildCol, roofCol, wallCol);

        // Menara Pengawas (kanan)
        DrawTower(px, W, H, (int)(W * 0.68f), groundY, 65, 180, darkCol, roofCol);

        // Menara kecil kiri
        DrawTower(px, W, H, (int)(W * 0.19f), groundY, 50, 150, darkCol, roofCol);

        // Pagar kayu di bawah bangunan
        DrawFence(px, W, H, (int)(W * 0.14f), groundY, (int)(W * 0.88f), buildCol);

        // ─────────────────────────────────────
        //  8. CAHAYA OBOR DI JENDELA
        // ─────────────────────────────────────
        var torchPoints = new (int x, int y, float r, Color c)[]
        {
            ((int)(W*0.42f), (int)(H*0.26f+30), 55, new Color(1f, 0.65f, 0.15f)),
            ((int)(W*0.50f), (int)(H*0.26f+50), 65, new Color(1f, 0.72f, 0.20f)),
            ((int)(W*0.58f), (int)(H*0.26f+30), 55, new Color(1f, 0.60f, 0.10f)),
            ((int)(W*0.34f), (int)(H*0.26f+20), 40, new Color(1f, 0.68f, 0.15f)),
            ((int)(W*0.65f), (int)(H*0.26f+20), 40, new Color(1f, 0.65f, 0.12f)),
            ((int)(W*0.22f), (int)(H*0.26f+10), 30, new Color(1f, 0.60f, 0.10f)),
            ((int)(W*0.78f), (int)(H*0.26f+10), 30, new Color(1f, 0.62f, 0.10f)),
        };
        foreach (var (lx, ly, lr, lc) in torchPoints)
        {
            for (int dy = -(int)lr; dy <= (int)lr; dy++)
            for (int dx = -(int)lr; dx <= (int)lr; dx++)
            {
                float d = Mathf.Sqrt(dx*dx + dy*dy);
                if (d >= lr) continue;
                float a = (1f - d / lr);
                a = a * a * 0.55f;
                Blend(lx + dx, ly + dy, lc, a);
            }
        }

        // Cahaya jendela kecil (kotak terang di dinding rumah)
        DrawWindowLight(px, W, H, (int)(W*0.465f), groundY + 50, 22, 18, new Color(1f, 0.80f, 0.40f));
        DrawWindowLight(px, W, H, (int)(W*0.520f), groundY + 50, 22, 18, new Color(1f, 0.80f, 0.40f));
        DrawWindowLight(px, W, H, (int)(W*0.350f), groundY + 30, 18, 15, new Color(1f, 0.72f, 0.30f));
        DrawWindowLight(px, W, H, (int)(W*0.640f), groundY + 30, 18, 15, new Color(1f, 0.72f, 0.30f));

        // ─────────────────────────────────────
        //  9. KABUT BAWAH (ground fog)
        // ─────────────────────────────────────
        var fogCol = new Color(0.50f, 0.42f, 0.32f);
        for (int y = 0; y < (int)(H * 0.20f); y++)
        {
            float nt  = (float)y / (H * 0.20f);
            float alf = (1f - nt) * (1f - nt) * 0.55f;
            for (int x = 0; x < W; x++)
            {
                float wave = Mathf.Sin(x * 0.005f + y * 0.02f) * 0.5f + 0.5f;
                Blend(x, y, fogCol, alf * (0.6f + wave * 0.4f));
            }
        }

        // ─────────────────────────────────────
        //  10. VIGNETTE HITAM DI TEPI LAYAR
        // ─────────────────────────────────────
        for (int y = 0; y < H; y++)
        for (int x = 0; x < W; x++)
        {
            float nx2 = (x / (float)W) * 2f - 1f;
            float ny2 = (y / (float)H) * 2f - 1f;
            float v = Mathf.Clamp01((nx2*nx2 + ny2*ny2) * 0.6f);
            Blend(x, y, Color.black, v * 0.65f);
        }

        tex.SetPixels(px);
        tex.Apply(false);
        return tex;
    }

    // ───────────────────────────────────────────────────────────────────────
    //  HELPER DRAWING (menggambar langsung ke array pixel)
    // ───────────────────────────────────────────────────────────────────────

    static void FillRect(Color[] px, int W, int H, int x, int y, int w, int h, Color c)
    {
        for (int dy = 0; dy < h; dy++)
        for (int dx = 0; dx < w; dx++)
        {
            int px2 = x + dx, py2 = y + dy;
            if (px2 >= 0 && px2 < W && py2 >= 0 && py2 < H)
                px[py2 * W + px2] = c;
        }
    }

    static void BlendRect(Color[] px, int W, int H, int x, int y, int w, int h, Color c, float a)
    {
        for (int dy = 0; dy < h; dy++)
        for (int dx = 0; dx < w; dx++)
        {
            int px2 = x + dx, py2 = y + dy;
            if (px2 < 0 || px2 >= W || py2 < 0 || py2 >= H) continue;
            px[py2 * W + px2] = Color.Lerp(px[py2 * W + px2], c, a);
        }
    }

    // Pohon: 3 segitiga bertumpuk (pinus/cemara) + batang
    static void DrawTree(Color[] px, int W, int H, int cx, int baseY, int tw, int th, Color c)
    {
        // 3 lapisan segitiga makin kecil ke atas
        int layers = 3;
        for (int l = 0; l < layers; l++)
        {
            int ly     = baseY + (int)(th * l / (float)layers * 0.55f);
            int lw     = tw - (int)(tw * l / (float)layers * 0.45f);
            int lh     = th / layers + 4;
            int lhEff  = (int)(lh * 1.2f);
            for (int dy = 0; dy < lhEff; dy++)
            for (int dx = -(lw / 2); dx <= lw / 2; dx++)
            {
                float edge = (float)dy / lhEff;
                int halfW  = (int)(lw / 2f * (1f - edge));
                if (Mathf.Abs(dx) <= halfW)
                {
                    int px2 = cx + dx, py2 = ly + dy;
                    if (px2 >= 0 && px2 < W && py2 >= 0 && py2 < H)
                        px[py2 * W + px2] = c;
                }
            }
        }
        // Batang
        FillRect(px, W, H, cx - 3, baseY, 6, (int)(th * 0.2f), new Color(c.r * 0.7f, c.g * 0.5f, c.b * 0.3f));
    }

    // Rumah sederhana: persegi dinding + atap segitiga
    static void DrawHouseSimple(Color[] px, int W, int H, int cx, int groundY, int bw, int bh, Color roof, Color wall)
    {
        int x0 = cx - bw / 2;
        // Dinding
        FillRect(px, W, H, x0, groundY, bw, bh, wall);
        // Atap segitiga
        int roofH = (int)(bw * 0.55f);
        for (int dy = 0; dy < roofH; dy++)
        {
            int halfW = (int)((bw / 2f) * (1f - (float)dy / roofH));
            FillRect(px, W, H, cx - halfW, groundY + bh + dy, halfW * 2, 1, roof);
        }
    }

    // Rumah Gadang: atap lengkung tanduk kerbau (karakteristik Minangkabau)
    static void DrawRumahGadang(Color[] px, int W, int H, int cx, int groundY,
                                 int bw, int bh, Color dark, Color roof, Color wall)
    {
        int x0 = cx - bw / 2;
        // Fondasi/panggung
        FillRect(px, W, H, x0 - 10, groundY, bw + 20, 15, dark);
        // Dinding
        FillRect(px, W, H, x0, groundY + 15, bw, bh, wall);
        // Tiang panggung
        for (int ti = 0; ti < 5; ti++)
        {
            int tx = x0 + 20 + ti * (bw / 5);
            FillRect(px, W, H, tx, groundY - 20, 8, 20, dark);
        }
        // Atap Gadang: dua lengkungan runcing ke atas
        int roofBase = groundY + 15 + bh;
        for (int dx = -bw / 2; dx <= bw / 2; dx++)
        {
            float nx = (float)dx / (bw / 2f); // -1..1
            // Profil atap: parabola terbalik dengan ujung melengkung naik di tepi
            float curve = -nx * nx * 0.5f + Mathf.Abs(Mathf.Abs(nx) - 0.3f) * 0.6f;
            int roofH2  = (int)((bh * 0.75f) + curve * bh * 0.6f);
            for (int dy = 0; dy <= roofH2; dy++)
                if (cx + dx >= 0 && cx + dx < W && roofBase + dy >= 0 && roofBase + dy < H)
                    px[(roofBase + dy) * W + (cx + dx)] = Color.Lerp(roof, dark, (float)dy / roofH2 * 0.4f);
        }
        // Dekorasi jendela
        DrawWindowLight(px, W, H, cx - bw / 4, groundY + 15 + bh / 2, 20, 16, new Color(1f, 0.72f, 0.3f, 0.7f));
        DrawWindowLight(px, W, H, cx + bw / 4, groundY + 15 + bh / 2, 20, 16, new Color(1f, 0.72f, 0.3f, 0.7f));
    }

    // Balai Desa: bangunan utama bertingkat dengan atap pelana bertumpuk
    static void DrawBalaiDesa(Color[] px, int W, int H, int cx, int groundY,
                               int bw, int bh, Color dark, Color roof, Color wall)
    {
        int x0 = cx - bw / 2;
        // Lantai 1
        FillRect(px, W, H, x0, groundY, bw, (int)(bh * 0.55f), wall);
        // Lantai 2 (lebih sempit)
        int bw2 = (int)(bw * 0.65f);
        int x02 = cx - bw2 / 2;
        int y2  = groundY + (int)(bh * 0.55f);
        FillRect(px, W, H, x02, y2, bw2, (int)(bh * 0.35f), dark);

        // Atap tingkat 1 (pelana lebar)
        int r1Base = groundY + (int)(bh * 0.55f);
        int r1H    = (int)(bw * 0.30f);
        for (int dx = -bw / 2 - 10; dx <= bw / 2 + 10; dx++)
        {
            float nx = Mathf.Abs((float)dx / (bw / 2f + 10));
            int rh = (int)(r1H * (1f - nx));
            for (int dy = 0; dy <= rh; dy++)
                if (cx + dx >= 0 && cx + dx < W && r1Base + dy < H)
                    px[(r1Base + dy) * W + (cx + dx)] = Color.Lerp(roof, dark, 0.3f + nx * 0.3f);
        }

        // Atap tingkat 2 (lebih kecil, lebih tinggi)
        int r2Base = y2 + (int)(bh * 0.35f);
        int r2H    = (int)(bw2 * 0.40f);
        for (int dx = -bw2 / 2 - 5; dx <= bw2 / 2 + 5; dx++)
        {
            float nx = Mathf.Abs((float)dx / (bw2 / 2f + 5));
            int rh = (int)(r2H * (1f - nx));
            for (int dy = 0; dy <= rh; dy++)
                if (cx + dx >= 0 && cx + dx < W && r2Base + dy < H)
                    px[(r2Base + dy) * W + (cx + dx)] = Color.Lerp(roof, dark, 0.2f + nx * 0.4f);
        }

        // Pintu
        FillRect(px, W, H, cx - 18, groundY, 36, (int)(bh * 0.42f), dark);
        // Jendela lantai 1
        DrawWindowLight(px, W, H, cx - 65, groundY + 25, 24, 20, new Color(1f, 0.80f, 0.35f, 0.85f));
        DrawWindowLight(px, W, H, cx + 65, groundY + 25, 24, 20, new Color(1f, 0.80f, 0.35f, 0.85f));
    }

    // Menara pengawas kayu (segi empat, atap limas)
    static void DrawTower(Color[] px, int W, int H, int cx, int groundY,
                           int tw, int th, Color dark, Color roof)
    {
        int x0 = cx - tw / 2;
        // Badan menara
        FillRect(px, W, H, x0, groundY, tw, th, dark);
        // Lantai atas lebih lebar sedikit (balkon)
        FillRect(px, W, H, x0 - 6, groundY + (int)(th * 0.65f), tw + 12, 8, new Color(dark.r * 1.3f, dark.g * 1.2f, dark.b * 1.1f));
        // Atap limas
        int roofH = (int)(tw * 0.90f);
        int roofBase = groundY + th;
        for (int dy = 0; dy <= roofH; dy++)
        {
            float edge = (float)dy / roofH;
            int halfW  = (int)(tw / 2f * (1f - edge)) + 5;
            FillRect(px, W, H, cx - halfW, roofBase + dy, halfW * 2, 1, Color.Lerp(roof, dark, edge * 0.5f));
        }
        // Cahaya obor di balkon
        int torchY = groundY + (int)(th * 0.72f);
        BlendRect(px, W, H, cx - 6, torchY, 12, 6, new Color(1f, 0.65f, 0.1f), 0.9f);
    }

    // Pagar kayu sederhana (tiang-tiang vertikal)
    static void DrawFence(Color[] px, int W, int H, int x0, int groundY, int x1, Color col)
    {
        // Palang horizontal
        FillRect(px, W, H, x0, groundY + 10, x1 - x0, 5, col);
        FillRect(px, W, H, x0, groundY + 24, x1 - x0, 4, col);
        // Tiang-tiang vertikal
        int spacing = 22;
        for (int x = x0; x < x1; x += spacing)
            FillRect(px, W, H, x, groundY, 7, 38, col);
    }

    // Cahaya jendela: kotak oranye + halo kecil di luar
    static void DrawWindowLight(Color[] px, int W, int H, int cx, int y, int ww, int wh, Color c)
    {
        int x0 = cx - ww / 2;
        // Cahaya utama
        for (int dy = 0; dy < wh; dy++)
        for (int dx = 0; dx < ww; dx++)
        {
            int px2 = x0 + dx, py2 = y + dy;
            if (px2 < 0 || px2 >= W || py2 < 0 || py2 >= H) continue;
            px[py2 * W + px2] = Color.Lerp(px[py2 * W + px2], c, 0.85f);
        }
        // Halo tipis di sekitar jendela
        int halo = 10;
        for (int dy = -halo; dy < wh + halo; dy++)
        for (int dx = -halo; dx < ww + halo; dx++)
        {
            if (dx >= 0 && dx < ww && dy >= 0 && dy < wh) continue;
            int px2 = x0 + dx, py2 = y + dy;
            if (px2 < 0 || px2 >= W || py2 < 0 || py2 >= H) continue;
            float nx = Mathf.Clamp01(1f - Mathf.Min(Mathf.Abs(dx < 0 ? dx : dx - ww),
                                                     Mathf.Abs(dy < 0 ? dy : dy - wh)) / (float)halo);
            px[py2 * W + px2] = Color.Lerp(px[py2 * W + px2], c, nx * 0.3f);
        }
    }

    // ───────────────────────────────────────────────────────────────────────
    //  HELPER UI
    // ───────────────────────────────────────────────────────────────────────

    GameObject MakePanel(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());
        return go;
    }

    Image MakeImage(string name, Transform parent, Color c)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = c;
        return img;
    }

    RawImage MakeRawImage(string name, Transform parent, Texture tex)
    {
        var go  = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var ri  = go.GetComponent<RawImage>();
        ri.texture = tex;
        return ri;
    }

    Text MakeText(string name, Transform parent, string content, int size, Color c, FontStyle style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t  = go.GetComponent<Text>();
        t.font       = font;
        t.text       = content;
        t.fontSize   = size;
        t.fontStyle  = style;
        t.color      = c;
        t.alignment  = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    Button MakeButton(string name, Transform parent, string label,
                      Vector2 pos, Vector2? size = null, int fontSize = 38)
    {
        var img = MakeImage(name, parent, buttonColor);
        Center(img.rectTransform, size ?? new Vector2(480, 90), pos);

        var outl = img.gameObject.AddComponent<Outline>();
        outl.effectColor    = goldColor;
        outl.effectDistance = new Vector2(2, -2);

        var btn = img.gameObject.AddComponent<Button>();
        btn.targetGraphic = img;

        var cb = btn.colors;
        cb.normalColor      = new Color(1f, 1f, 1f, 1f);
        cb.highlightedColor = new Color(1f, 0.80f, 0.30f, 1f); // hover → emas
        cb.pressedColor     = new Color(0.70f, 0.50f, 0.10f, 1f);
        cb.selectedColor    = Color.white;
        cb.fadeDuration     = 0.10f;
        btn.colors = cb;

        var t = MakeText("Label", img.transform, label, fontSize, goldColor, FontStyle.Bold);
        Stretch(t.rectTransform);
        return btn;
    }

    Slider MakeSlider(Transform parent, Vector2 pos, float value)
    {
        var root = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        root.transform.SetParent(parent, false);
        Center(root.GetComponent<RectTransform>(), new Vector2(580, 36), pos);

        var bg = MakeImage("Bg", root.transform, new Color(0.18f, 0.20f, 0.30f, 1f));
        Stretch(bg.rectTransform);

        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(root.transform, false);
        var far = fillArea.GetComponent<RectTransform>();
        Stretch(far); far.offsetMin = new Vector2(10, 0); far.offsetMax = new Vector2(-10, 0);

        var fill = MakeImage("Fill", fillArea.transform, goldColor);
        fill.rectTransform.anchorMin  = Vector2.zero;
        fill.rectTransform.anchorMax  = new Vector2(0, 1);
        fill.rectTransform.sizeDelta  = new Vector2(10, 0);

        var handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(root.transform, false);
        var har = handleArea.GetComponent<RectTransform>();
        Stretch(har); har.offsetMin = new Vector2(14, 0); har.offsetMax = new Vector2(-14, 0);

        var handle = MakeImage("Handle", handleArea.transform, Color.white);
        handle.rectTransform.sizeDelta = new Vector2(28, 52);

        var slider = root.GetComponent<Slider>();
        slider.fillRect      = fill.rectTransform;
        slider.handleRect    = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue      = 0f;
        slider.maxValue      = 1f;
        slider.value         = value;
        return slider;
    }

    Toggle MakeToggle(Transform parent, string label, Vector2 pos, bool isOn)
    {
        var root = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
        root.transform.SetParent(parent, false);
        Center(root.GetComponent<RectTransform>(), new Vector2(580, 50), pos);

        var box = MakeImage("Box", root.transform, new Color(0.18f, 0.20f, 0.30f, 1f));
        box.rectTransform.anchorMin = box.rectTransform.anchorMax = new Vector2(0, 0.5f);
        box.rectTransform.pivot     = new Vector2(0, 0.5f);
        box.rectTransform.sizeDelta = new Vector2(44, 44);
        box.rectTransform.anchoredPosition = Vector2.zero;

        var check = MakeImage("Check", box.transform, goldColor);
        Stretch(check.rectTransform);
        check.rectTransform.offsetMin = new Vector2(8, 8);
        check.rectTransform.offsetMax = new Vector2(-8, -8);

        var t = MakeText("Label", root.transform, label, 32, Color.white, FontStyle.Normal);
        t.alignment = TextAnchor.MiddleLeft;
        t.rectTransform.anchorMin = Vector2.zero;
        t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = new Vector2(60, 0);
        t.rectTransform.offsetMax = Vector2.zero;

        var toggle = root.GetComponent<Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic       = check;
        toggle.isOn          = isOn;
        return toggle;
    }

    // ───────────────────────────────────────────────────────────────────────
    //  HELPER RECT
    // ───────────────────────────────────────────────────────────────────────

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin     = Vector2.zero;
        rt.anchorMax     = Vector2.one;
        rt.offsetMin     = Vector2.zero;
        rt.offsetMax     = Vector2.zero;
    }

    static void Center(RectTransform rt, Vector2 size, Vector2 pos)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta         = size;
        rt.anchoredPosition  = pos;
    }

    // ───────────────────────────────────────────────────────────────────────
    //  AKSI MENU
    // ───────────────────────────────────────────────────────────────────────

    void OnPlay() => SceneManager.LoadScene(gameSceneName);

    void ShowSettings(bool show)
    {
        settingsPanel.SetActive(show);
        mainPanel.SetActive(!show);
    }

    void OnDestroy()
    {
        // Bersihkan Texture2D yang dibuat secara prosedural
        if (bgTex) Destroy(bgTex);
    }
}
