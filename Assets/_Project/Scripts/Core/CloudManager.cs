using UnityEngine;

/// <summary>
/// Sistem Awan Dinamis 3D yang melayang dan bergerak ditiup angin di langit Desa Eldara.
/// Warna awan berubah secara dinamis mengikuti waktu siang, senja, dan malam.
/// </summary>
public class CloudManager : MonoBehaviour
{
    public static CloudManager Instance { get; private set; }

    [Header("Jumlah & Ketinggian Awan")]
    [SerializeField] int cloudCount = 22;
    [SerializeField] float minHeight = 45f;
    [SerializeField] float maxHeight = 70f;
    [SerializeField] float mapBounds = 130f;

    [Header("Pergerakan Angin")]
    [SerializeField] Vector3 windDirection = new Vector3(1f, 0f, 0.25f);
    [SerializeField] float windSpeed = 3.5f;

    [Header("Warna Awan Sesuai Waktu")]
    [SerializeField] Gradient cloudColorGradient;

    private GameObject[] clouds;
    private Material cloudMaterial;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateCloudMaterial();
        SpawnClouds();
    }

    void CreateCloudMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        cloudMaterial = new Material(shader);
        cloudMaterial.name = "M_Dynamic_Cloud";
        cloudMaterial.SetFloat("_Smoothness", 0.0f);
        cloudMaterial.SetFloat("_Metallic", 0.0f);

        // Gradient warna awan default jika belum diset
        if (cloudColorGradient == null || cloudColorGradient.colorKeys.Length == 0)
        {
            cloudColorGradient = new Gradient();
            cloudColorGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.25f, 0.32f, 0.45f), 0.0f),  // Tengah Malam (Biru gelap keperakan)
                    new GradientColorKey(new Color(1.0f, 0.75f, 0.60f), 0.25f),  // Fajar (Oranye lembut)
                    new GradientColorKey(new Color(1.0f, 1.0f, 1.0f), 0.50f),    // Siang Terik (Putih bersih)
                    new GradientColorKey(new Color(1.0f, 0.55f, 0.40f), 0.75f),  // Senja (Merah tembaga / lembayung)
                    new GradientColorKey(new Color(0.25f, 0.32f, 0.45f), 1.0f)   // Tengah Malam
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(0.9f, 0f),
                    new GradientAlphaKey(0.9f, 1f)
                }
            );
        }
    }

    void SpawnClouds()
    {
        clouds = new GameObject[cloudCount];
        Vector3 windNorm = windDirection.normalized;

        for (int i = 0; i < cloudCount; i++)
        {
            var cloudObj = new GameObject($"Cloud_{i + 1}");
            cloudObj.transform.SetParent(transform);

            // Posisi acak di langit
            float x = Random.Range(-mapBounds, mapBounds);
            float y = Random.Range(minHeight, maxHeight);
            float z = Random.Range(-mapBounds, mapBounds);
            cloudObj.transform.position = new Vector3(x, y, z);

            // Bangun gumpalan awan 3D organik (5 sampai 8 bola tumpang tindih)
            BuildCloudCluster(cloudObj.transform);

            clouds[i] = cloudObj;
        }
    }

    void BuildCloudCluster(Transform parent)
    {
        int puffCount = Random.Range(5, 9);
        float baseScale = Random.Range(1.8f, 3.2f);

        for (int p = 0; p < puffCount; p++)
        {
            var puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            puff.name = $"Puff_{p}";
            puff.transform.SetParent(parent, false);

            // Hapus collider agar tidak membebani physics
            var col = puff.GetComponent<Collider>();
            if (col) DestroyImmediate(col);

            // Offset gumpalan awan dari pusat
            Vector3 offset = new Vector3(
                Random.Range(-3.5f, 3.5f),
                Random.Range(-0.8f, 0.8f),
                Random.Range(-2.5f, 2.5f)
            );
            puff.transform.localPosition = offset;

            // Skala elips/gepeng pipih khas awan
            Vector3 scale = new Vector3(
                Random.Range(3.5f, 5.5f) * baseScale,
                Random.Range(1.6f, 2.6f) * baseScale,
                Random.Range(3.0f, 4.8f) * baseScale
            );
            puff.transform.localScale = scale;

            // Beri material awan bersama
            var renderer = puff.GetComponent<MeshRenderer>();
            if (renderer)
            {
                renderer.sharedMaterial = cloudMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // Opsional: matikan shadow awan agar ringan
                renderer.receiveShadows = false;
            }
        }
    }

    void Update()
    {
        if (clouds == null) return;

        Vector3 moveStep = windDirection.normalized * (windSpeed * Time.deltaTime);

        // Gerakkan setiap awan ditiup angin
        for (int i = 0; i < clouds.Length; i++)
        {
            if (!clouds[i]) continue;

            clouds[i].transform.position += moveStep;

            Vector3 pos = clouds[i].transform.position;

            // Jika awan keluar dari batas peta (boundary), putar balik ke sisi seberang
            if (pos.x > mapBounds)
            {
                pos.x = -mapBounds;
                pos.z = Random.Range(-mapBounds, mapBounds);
                pos.y = Random.Range(minHeight, maxHeight);
                clouds[i].transform.position = pos;
            }
            else if (pos.x < -mapBounds)
            {
                pos.x = mapBounds;
                pos.z = Random.Range(-mapBounds, mapBounds);
                pos.y = Random.Range(minHeight, maxHeight);
                clouds[i].transform.position = pos;
            }

            if (pos.z > mapBounds) pos.z = -mapBounds;
            else if (pos.z < -mapBounds) pos.z = mapBounds;
        }

        // Sinkronisasi warna awan dengan siklus waktu (DayNightCycle)
        if (DayNightCycle.Instance && cloudMaterial && cloudColorGradient != null)
        {
            float t = DayNightCycle.Instance.Hour / 24f;
            Color currentColor = cloudColorGradient.Evaluate(t);
            cloudMaterial.SetColor("_BaseColor", currentColor);
            cloudMaterial.color = currentColor;
        }
    }
}

