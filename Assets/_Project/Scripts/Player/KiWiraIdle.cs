using UnityEngine;

/// <summary>
/// Animasi prosedural untuk Ki Wira: napas halus saat idle, putar otomatis,
/// dan ayunan langkah kaki/tangan dinamis yang otomatis merespon pergerakan pemain.
/// Pasang di GameObject yang sama dengan KiWiraBuilder.
/// </summary>
public class KiWiraIdle : MonoBehaviour
{
    [Header("Napas")]
    public bool napas = true;

    [Header("Putar otomatis")]
    public bool putarOtomatis = false;
    public float kecepatanPutar = 25f;

    [Header("Jalan")]
    public bool jalanDiTempat = false;
    public float kecepatanJalan = 7f;
    public float sudutAyun = 26f;

    [Header("Lainnya")]
    public float waktuAwal = 0f;

    Transform torso, kepala, kakiKiri, kakiKanan, lenganKiri, lenganKanan;
    Quaternion rKakiKiri, rKakiKanan, rLenganKiri, rLenganKanan, rKepala;
    float waktu;
    CharacterController cc;

    void Start()
    {
        waktu = waktuAwal;
        Cari();
        cc = GetComponent<CharacterController>();
    }

    public void Cari()
    {
        torso = Temukan("Torso");
        kepala = Temukan("Kepala");
        kakiKiri = Temukan("Kaki_Kiri");
        kakiKanan = Temukan("Kaki_Kanan");
        lenganKiri = Temukan("Lengan_Kiri");
        lenganKanan = Temukan("Lengan_Kanan");

        if (kakiKiri) rKakiKiri = kakiKiri.localRotation;
        if (kakiKanan) rKakiKanan = kakiKanan.localRotation;
        if (lenganKiri) rLenganKiri = lenganKiri.localRotation;
        if (lenganKanan) rLenganKanan = lenganKanan.localRotation;
        if (kepala) rKepala = kepala.localRotation;
    }

    Transform Temukan(string nama)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t.name == nama) return t;
        return null;
    }

    void Update()
    {
        // Re-find parts jika model baru saja dibangun ulang
        if (!torso || !kakiKiri) Cari();

        if (putarOtomatis)
            transform.Rotate(0f, kecepatanPutar * Time.deltaTime, 0f, Space.World);

        // Deteksi pergerakan karakter secara dinamis
        bool bergerak = jalanDiTempat;
        float speedMultiplier = 1f;

        if (cc != null)
        {
            Vector3 horizVel = new Vector3(cc.velocity.x, 0f, cc.velocity.z);
            float spd = horizVel.magnitude;
            if (spd > 0.15f)
            {
                bergerak = true;
                speedMultiplier = Mathf.Clamp(spd / 3.8f, 0.7f, 1.8f);
            }
        }

        waktu += Time.deltaTime * speedMultiplier;

        // Animasi napas halus di torso saat berdiri diam
        if (napas && torso)
        {
            float napasScale = 1f + Mathf.Sin(Time.time * 1.8f) * 0.005f;
            torso.localScale = new Vector3(1f, napasScale, 1f);
        }

        if (bergerak)
        {
            float s = Mathf.Sin(waktu * kecepatanJalan) * sudutAyun;
            if (kakiKiri)   kakiKiri.localRotation   = rKakiKiri   * Quaternion.Euler( s, 0f, 0f);
            if (kakiKanan)  kakiKanan.localRotation  = rKakiKanan  * Quaternion.Euler(-s, 0f, 0f);
            if (lenganKiri)  lenganKiri.localRotation  = rLenganKiri  * Quaternion.Euler(-s * 0.75f, 0f, 0f);
            if (lenganKanan) lenganKanan.localRotation = rLenganKanan * Quaternion.Euler( s * 0.75f, 0f, 0f);
            if (kepala)     kepala.localRotation     = rKepala     * Quaternion.Euler(0f, s * 0.10f, 0f);
        }
        else
        {
            // Kembali ke pose istirahat tegak secara halus
            float t = Time.deltaTime * 10f;
            if (kakiKiri)   kakiKiri.localRotation   = Quaternion.Slerp(kakiKiri.localRotation, rKakiKiri, t);
            if (kakiKanan)  kakiKanan.localRotation  = Quaternion.Slerp(kakiKanan.localRotation, rKakiKanan, t);
            if (lenganKiri)  lenganKiri.localRotation  = Quaternion.Slerp(lenganKiri.localRotation, rLenganKiri, t);
            if (lenganKanan) lenganKanan.localRotation = Quaternion.Slerp(lenganKanan.localRotation, rLenganKanan, t);
            if (kepala)     kepala.localRotation     = Quaternion.Slerp(kepala.localRotation, rKepala, t);
        }
    }
}
