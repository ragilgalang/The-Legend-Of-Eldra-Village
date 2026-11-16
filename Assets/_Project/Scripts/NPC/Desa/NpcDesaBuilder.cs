using System.Collections.Generic;
using UnityEngine;
using U = KiWiraUtil;

/// <summary>
/// Merakit satu NPC desa dari data NpcDef. Struktur pivot memakai nama yang sama dengan
/// KiWiraBuilder (Torso, Kepala, Kaki_Kiri/Kanan, Lengan_Kiri/Kanan) sehingga KiWiraIdle bisa dipakai.
/// </summary>
public static class NpcDesaBuilder
{
    const float ZS = 0.72f;

    static readonly Vector2[] ProfPanjang =
    {
        new Vector2(0.29f, 0.70f), new Vector2(0.275f, 0.84f), new Vector2(0.23f, 1.0f),
        new Vector2(0.215f, 1.15f), new Vector2(0.235f, 1.33f), new Vector2(0.215f, 1.46f), new Vector2(0.09f, 1.53f)
    };
    static readonly Vector2[] ProfPendek =
    {
        new Vector2(0.255f, 0.82f), new Vector2(0.232f, 0.96f), new Vector2(0.215f, 1.15f),
        new Vector2(0.235f, 1.33f), new Vector2(0.215f, 1.46f), new Vector2(0.09f, 1.53f)
    };

    static float ProfR(Vector2[] p, float y)
    {
        for (int i = 0; i < p.Length - 1; i++)
        {
            if (y >= p[i].y && y <= p[i + 1].y)
                return p[i].x + (p[i + 1].x - p[i].x) * ((y - p[i].y) / (p[i + 1].y - p[i].y));
        }
        return p[p.Length - 1].x;
    }

    static Vector2[] Skala(Vector2[] p, float lb)
    {
        var r = new Vector2[p.Length];
        for (int i = 0; i < p.Length; i++) r[i] = new Vector2(p[i].x * lb, p[i].y);
        return r;
    }

    public static GameObject Bangun(NpcDef d, Transform induk)
    {
        var root = new GameObject(d.nama);
        root.transform.SetParent(induk, false);
        Transform model = root.transform;
        float lb = d.lebar;

        Vector2[] prof = d.style == BajuStyle.Panjang ? ProfPanjang : ProfPendek;
        System.Func<float, float> Rb = y => ProfR(prof, y) * lb;

        Material skin = U.Mat(U.Hex(d.kulit), 0.35f);
        Material hair = U.Mat(U.Hex(d.rambut), 0.4f);
        Material top = U.Mat(U.Hex(d.baju), 0.1f, U.Kain(U.Hex(d.baju), 1), 3, 3);
        Material leather = U.Mat(U.Hex("#5A3A22"), 0.4f);
        Material dark = U.Mat(U.Hex("#15100D"), 0.7f);
        Material legM = U.Mat(U.Hex(d.wBawah), 0.08f, U.Kain(U.Hex(d.wBawah), 2), 2, 2);
        Material shoe = d.alas ? leather : skin;

        // ------------------------------------------------ badan
        var torso = new GameObject("Torso").transform;
        torso.SetParent(model, false);

        U.MeshObj("Baju", torso, U.Lathe(U.Haluskan(Skala(prof, lb), 6), 44, ZS), top);
        float rk = Rb(1.5f) + 0.004f;
        U.Band("Kerah", torso, top, rk, rk, 0.03f, 1.5f, ZS);

        if (!string.IsNullOrEmpty(d.rompi))
        {
            var vp = new[]
            {
                new Vector2(Rb(1.0f) + 0.012f, 1.0f), new Vector2(Rb(1.15f) + 0.012f, 1.15f),
                new Vector2(Rb(1.33f) + 0.012f, 1.33f), new Vector2(Rb(1.46f) + 0.01f, 1.46f)
            };
            Material mr = U.Mat(U.Hex(d.rompi), 0.1f, U.Kain(U.Hex(d.rompi), 5), 2, 2);
            U.MeshObj("Rompi", torso, U.Lathe(U.Haluskan(vp, 4), 40, ZS), mr);
        }

        if (d.bawah == Bawah.Kain)
        {
            var kp = new[]
            {
                new Vector2(0.22f * lb, 0.2f), new Vector2(0.255f * lb, 0.5f),
                new Vector2(0.25f * lb, 0.82f), new Vector2(0.215f * lb, 1.02f)
            };
            Material mk = U.Mat(U.Hex(d.wBawah), 0.1f, U.Batik(U.Hex(d.wBawah), U.Hex(d.fgKain), 7), 4, 3);
            U.MeshObj("Kain", torso, U.Lathe(U.Haluskan(kp, 6), 44, 0.8f), mk);
        }

        Material mi = U.Mat(U.Hex(d.ikat), 0.1f, U.Kain(U.Hex(d.ikat), 6), 6, 1);
        U.Band("IkatPinggang", torso, mi, Rb(1.0f) + 0.008f, Rb(1.0f) + 0.008f, 0.08f, 1.0f, ZS);

        if (d.celemek)
        {
            U.Kotak("Celemek", torso, leather, new Vector3(0f, 0.9f, ZS * Rb(0.92f) + 0.01f), new Vector3(0.26f * lb, 0.5f, 0.012f));
            U.Kotak("TaliCelemek1", torso, leather, new Vector3(0.06f * lb, 1.2f, ZS * Rb(1.2f) + 0.008f), new Vector3(0.03f, 0.34f, 0.01f), 0f, 0f, 3f);
            U.Kotak("TaliCelemek2", torso, leather, new Vector3(-0.06f * lb, 1.2f, ZS * Rb(1.2f) + 0.008f), new Vector3(0.03f, 0.34f, 0.01f), 0f, 0f, -3f);
        }

        if (!string.IsNullOrEmpty(d.selendang))
        {
            Material ms = U.Mat(U.Hex(d.selendang), 0.1f, U.Kain(U.Hex(d.selendang), 8), 2, 2);
            var titik = new List<Vector3>();
            for (int i = 0; i <= 8; i++)
            {
                float t = i / 8f, px = (0.12f - 0.34f * t) * lb, py = 1.43f - 0.5f * t;
                float pz = ZS * Mathf.Sqrt(Mathf.Max(Rb(py) * Rb(py) - px * px, 0f)) + 0.016f;
                titik.Add(new Vector3(px, py, pz));
            }
            for (int i = 0; i < titik.Count - 1; i++)
                U.Seg("Selendang" + i, torso, ms, titik[i], titik[i + 1], 0.03f, 0.03f);
        }

        U.Seg("Leher", torso, skin, new Vector3(0f, 1.48f, 0f), new Vector3(0f, 1.6f, 0.005f), 0.05f, 0.048f);

        // ------------------------------------------------ kaki
        foreach (int s in new[] { -1, 1 })
        {
            string sisi = s < 0 ? "Kiri" : "Kanan";
            var pivot = U.Pivot(model, "Kaki_" + sisi, new Vector3(s * 0.085f, 0.8f, 0f));
            Vector3 pinggul = new Vector3(s * 0.085f, 0.8f, 0f);
            Vector3 lutut = new Vector3(s * 0.09f, 0.46f, 0.03f);
            Vector3 mata = new Vector3(s * 0.09f, 0.11f, 0f);
            var bagian = new List<GameObject>();

            if (d.bawah == Bawah.Celana)
            {
                bagian.Add(U.Seg("Paha", model, legM, pinggul, lutut, 0.088f, 0.066f));
                bagian.Add(U.Seg("Betis", model, legM, lutut, mata, 0.066f, 0.052f));
                bagian.Add(U.Ball("Lutut", model, legM, lutut, 0.067f));
            }
            else if (d.bawah == Bawah.CelanaPendek)
            {
                bagian.Add(U.Seg("Paha", model, legM, pinggul, lutut, 0.088f, 0.07f));
                bagian.Add(U.Ball("Lutut", model, legM, lutut, 0.07f));
                bagian.Add(U.Seg("Betis", model, skin, lutut, mata, 0.055f, 0.044f));
            }
            else
            {
                bagian.Add(U.Seg("Betis", model, skin, new Vector3(s * 0.09f, 0.3f, 0.02f), mata, 0.05f, 0.044f));
            }
            bagian.Add(U.Ball("Kaki", model, shoe, new Vector3(s * 0.09f, 0.05f, 0.045f), 0.06f, new Vector3(1f, d.alas ? 0.75f : 0.6f, 1.9f)));
            U.Para(pivot, bagian);
        }

        // ------------------------------------------------ lengan
        foreach (int s in new[] { -1, 1 })
        {
            string sisi = s < 0 ? "Kiri" : "Kanan";
            Vector3 bahu = new Vector3(s * 0.25f * lb, 1.4f, 0f);
            Vector3 siku = new Vector3(s * 0.32f * lb, 1.13f, 0f);
            Vector3 pergelangan = s > 0 ? new Vector3(0.355f * lb, 0.87f, 0.08f) : new Vector3(-0.365f * lb, 0.86f, 0.07f);
            Material atas = d.lengan >= 1 ? top : skin;
            Material bawahM = d.lengan >= 2 ? top : skin;
            var pivot = U.Pivot(model, "Lengan_" + sisi, bahu);

            var bagian = new List<GameObject>
            {
                U.Ball("Bahu", model, atas, bahu, 0.063f),
                U.Seg("LenganAtas", model, atas, bahu, siku, 0.062f, 0.052f),
                U.Ball("Siku", model, atas, siku, 0.052f),
                U.Seg("LenganBawah", model, bawahM, siku, pergelangan, 0.048f, d.lengan >= 2 ? 0.042f : 0.037f),
                U.Ball("Tangan", model, skin, pergelangan + new Vector3(0f, -0.02f, 0f), 0.043f, new Vector3(1f, 1.1f, 1f))
            };
            U.Para(pivot, bagian);
        }

        // ------------------------------------------------ kepala
        var kepala = U.Pivot(model, "Kepala", new Vector3(0f, 1.6f, 0.005f));
        System.Func<float, float, float, Vector3> P = (x, y, z) => new Vector3(x, 1.6f + y, 0.005f + z);
        const float cy = 0.09f;
        var k = new List<GameObject>();

        k.Add(U.Ball("Tengkorak", model, skin, P(0f, cy, 0f), 0.105f, new Vector3(0.95f, 1.13f, 1.0f)));
        Material alis = U.Mat(U.Hex(!string.IsNullOrEmpty(d.jenggot) ? d.jenggot : d.rambut), 0.4f);
        foreach (int s in new[] { -1, 1 })
        {
            k.Add(U.Ball("Telinga", model, skin, P(s * 0.1f, cy - 0.005f, 0f), 0.02f, new Vector3(0.6f, 1f, 0.9f)));
            k.Add(U.Ball("Mata", model, dark, P(s * 0.039f, cy + 0.006f, 0.097f), 0.0105f, new Vector3(1f, 1.1f, 0.6f)));
            k.Add(U.Kotak("Alis", model, alis, P(s * 0.041f, cy + 0.03f, 0.097f), new Vector3(0.04f, 0.008f, 0.01f), 0f, 0f, s * 7f));
        }
        k.Add(U.Ball("Hidung", model, skin, P(0f, cy - 0.022f, 0.108f), 0.018f, new Vector3(1f, 1.15f, 1f)));
        k.Add(U.Kotak("Mulut", model, dark, P(0f, cy - 0.058f, 0.098f), new Vector3(0.038f, 0.006f, 0.008f)));

        if (!string.IsNullOrEmpty(d.jenggot))
        {
            Material mj = U.Mat(U.Hex(d.jenggot), 0.2f);
            k.Add(U.Ball("Jenggot", model, mj, P(0f, cy - 0.085f, 0.06f), 0.055f, new Vector3(1f, 1.3f, 0.8f)));
            k.Add(U.Kotak("Kumis", model, mj, P(0f, cy - 0.04f, 0.106f), new Vector3(0.05f, 0.01f, 0.012f)));
        }
        else if (!string.IsNullOrEmpty(d.kumis))
        {
            k.Add(U.Kotak("Kumis", model, U.Mat(U.Hex(d.kumis), 0.2f), P(0f, cy - 0.04f, 0.106f), new Vector3(0.05f, 0.01f, 0.012f)));
        }

        // rambut
        if (d.gaya != GayaRambut.Botak)
        {
            var topi = U.MeshObj("Rambut", model, U.Lathe(U.Tudung(0.111f, Mathf.PI * 0.5f, 1.1f), 32, 1.02f), hair);
            topi.transform.position = P(0f, cy + 0.012f, -0.004f);
            k.Add(topi);
            k.Add(U.Ball("RambutBelakang", model, hair, P(0f, cy - 0.03f, -0.035f), 0.097f, new Vector3(1f, 0.95f, 0.9f)));
        }
        if (d.gaya == GayaRambut.Sanggul)
            k.Add(U.Ball("Sanggul", model, hair, P(0f, cy + 0.045f, -0.1f), 0.056f));
        if (d.gaya == GayaRambut.Kepang)
        {
            Material tali = U.Mat(U.Hex("#CDBF9C"), 0.1f);
            foreach (int s in new[] { -1, 1 })
            {
                k.Add(U.Seg("Kepang", model, hair, P(s * 0.07f, cy - 0.01f, -0.085f), P(s * 0.075f, cy - 0.24f, -0.1f), 0.024f, 0.02f));
                k.Add(U.Ball("TaliKepang", model, tali, P(s * 0.075f, cy - 0.25f, -0.1f), 0.024f));
            }
        }

        // penutup kepala
        if (d.penutup != Penutup.Tidak)
        {
            Color wp = U.Hex(d.wPenutup);
            Material mp = U.Mat(wp, 0.1f, U.Kain(wp, 9), 3, 3);
            switch (d.penutup)
            {
                case Penutup.Caping:
                    {
                        var cp = new List<Vector2> { new Vector2(0.27f, -0.05f), new Vector2(0f, 0.05f) };
                        var caping = U.MeshObj("Caping", model, U.Lathe(cp, 40, 1f), mp);
                        caping.transform.position = P(0f, cy + 0.125f, 0.005f);
                        caping.transform.rotation = Quaternion.Euler(4.6f, 0f, 0f);
                        k.Add(caping);
                        break;
                    }
                case Penutup.Peci:
                    {
                        var peci = U.Prim(PrimitiveType.Cylinder, "Peci", model, U.Mat(wp, 0.3f));
                        peci.transform.position = P(0f, cy + 0.095f, 0f);
                        peci.transform.localScale = new Vector3(0.193f, 0.0375f, 0.193f);
                        peci.transform.rotation = Quaternion.Euler(-4.6f, 0f, 0f);
                        k.Add(peci);
                        break;
                    }
                case Penutup.Udeng:
                    {
                        var band = U.Band("IkatKepala", model, mp, 0.113f, 0.117f, 0.05f, 0f, 1f);
                        band.transform.position = P(0f, cy + 0.043f, 0f);
                        k.Add(band);
                        k.Add(U.Ball("SimpulIkat", model, mp, P(0.108f, cy + 0.05f, 0.05f), 0.028f, new Vector3(1f, 1f, 0.8f)));
                        k.Add(U.Kotak("UjungIkat", model, mp, P(0.118f, cy, 0.048f), new Vector3(0.028f, 0.07f, 0.008f), 0f, 0f, 11f));
                        break;
                    }
                case Penutup.Kerudung:
                    {
                        var hood = U.MeshObj("Kerudung", model, U.Lathe(U.Tudung(0.123f, Mathf.PI * 0.56f, 1.12f), 32, 1.04f), mp);
                        hood.transform.position = P(0f, cy + 0.015f, -0.012f);
                        hood.transform.rotation = Quaternion.Euler(-17f, 0f, 0f);
                        k.Add(hood);
                        k.Add(U.Kotak("UjungKerudung", model, mp, P(-0.105f, cy - 0.06f, 0f), new Vector3(0.05f, 0.15f, 0.012f), 0f, 0f, 6f));
                        break;
                    }
            }
        }

        U.Para(kepala, k);
        kepala.localScale = Vector3.one * d.kepalaKali;

        model.localScale = Vector3.one * d.tinggi;
        return root;
    }
}
