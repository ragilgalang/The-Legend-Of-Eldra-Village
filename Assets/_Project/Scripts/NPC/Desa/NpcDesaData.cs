using System.Collections.Generic;

public enum BajuStyle { Pendek, Panjang }
public enum Bawah { Celana, CelanaPendek, Kain }
public enum GayaRambut { Pendek, Sanggul, Kepang, Botak }
public enum Penutup { Tidak, Caping, Udeng, Kerudung, Peci }

/// <summary>Data satu NPC desa. Warna ditulis sebagai hex (#RRGGBB). Teks kosong berarti tidak ada.</summary>
[System.Serializable]
public class NpcDef
{
    public string nama, peran, pakaian;
    public string kulit = "#A9764D", baju = "#8A6A45", wBawah = "#3A2E25", fgKain = "#8A7050", ikat = "#5A3A22", rambut = "#1D1612";
    public BajuStyle style = BajuStyle.Pendek;
    public int lengan = 2;                 // 0 tanpa lengan, 1 pendek, 2 panjang
    public Bawah bawah = Bawah.Celana;
    public GayaRambut gaya = GayaRambut.Pendek;
    public Penutup penutup = Penutup.Tidak;
    public string wPenutup = "#7A6A50";
    public string rompi = "", selendang = "", jenggot = "", kumis = "";
    public bool celemek = false, alas = true;
    public float tinggi = 1f, lebar = 1f, kepalaKali = 1f;
}

/// <summary>Daftar sepuluh warga desa dan posisi barisnya (6 depan, 4 belakang selang-seling).</summary>
public static class NpcDesaKatalog
{
    public struct Slot
    {
        public int indeks; public float x, z;
        public Slot(int i, float x, float z) { indeks = i; this.x = x; this.z = z; }
    }

    public static readonly Slot[] Barisan =
    {
        new Slot(0, -2.625f, 0.9f), new Slot(1, -1.575f, 0.9f), new Slot(2, -0.525f, 0.9f),
        new Slot(3, 0.525f, 0.9f),  new Slot(9, 1.575f, 1.2f),  new Slot(5, 2.625f, 0.9f),
        new Slot(6, -1.05f, -0.9f), new Slot(7, 0f, -0.9f),     new Slot(8, 1.05f, -0.9f), new Slot(4, -2.1f, -0.9f)
    };

    public static List<NpcDef> Semua()
    {
        return new List<NpcDef>
        {
            new NpcDef { nama = "Pak Darma", peran = "Petani",
                pakaian = "Baju kampret krem lusuh, celana hitam sebatas lutut, caping anyaman, tanpa alas kaki.",
                kulit = "#9A6A45", baju = "#BFB08A", style = BajuStyle.Pendek, lengan = 2, bawah = Bawah.CelanaPendek, wBawah = "#2E2A26",
                ikat = "#5A3A22", rambut = "#1D1612", gaya = GayaRambut.Pendek, penutup = Penutup.Caping, wPenutup = "#B99A5B",
                alas = false, tinggi = 1f, lebar = 1f },

            new NpcDef { nama = "Bu Sari", peran = "Pedagang pasar",
                pakaian = "Kebaya coklat tanah, kain jarik batik soga, sanggul sederhana, selendang krem.",
                kulit = "#B27D54", baju = "#8A5A3A", style = BajuStyle.Pendek, lengan = 2, bawah = Bawah.Kain, wBawah = "#4A3322", fgKain = "#A88A58",
                ikat = "#6B4A2D", rambut = "#1D1612", gaya = GayaRambut.Sanggul, selendang = "#CDBF9C",
                alas = true, tinggi = 0.95f, lebar = 0.92f },

            new NpcDef { nama = "Mbah Karto", peran = "Sesepuh desa",
                pakaian = "Baju lurik gelap panjang, sarung cokelat, ikat kepala, janggut putih.",
                kulit = "#8F6340", baju = "#3B3F4A", style = BajuStyle.Panjang, lengan = 2, bawah = Bawah.Kain, wBawah = "#4B3A2C", fgKain = "#7A6446",
                ikat = "#2A2A2A", rambut = "#D4D4D0", gaya = GayaRambut.Pendek, penutup = Penutup.Udeng, wPenutup = "#4A3A2A", jenggot = "#E6E6E2",
                alas = false, tinggi = 0.96f, lebar = 0.98f },

            new NpcDef { nama = "Mbok Ijah", peran = "Penjual sayur",
                pakaian = "Kebaya krem pudar, kain batik gelap, kerudung kain hijau lumut.",
                kulit = "#9C6B47", baju = "#D8D0BD", style = BajuStyle.Pendek, lengan = 2, bawah = Bawah.Kain, wBawah = "#3D2C22", fgKain = "#8A7050",
                ikat = "#7A6A50", rambut = "#D4D4D0", gaya = GayaRambut.Sanggul, penutup = Penutup.Kerudung, wPenutup = "#6B7A5E",
                alas = false, tinggi = 0.9f, lebar = 0.9f },

            new NpcDef { nama = "Kang Joko", peran = "Nelayan",
                pakaian = "Kaos dalam krem dengan rompi abu-biru, celana pendek, ikat kepala, tanpa alas kaki.",
                kulit = "#8A5C3B", baju = "#CFC3A3", style = BajuStyle.Pendek, lengan = 0, rompi = "#4A5A63", bawah = Bawah.CelanaPendek, wBawah = "#5A4A3A",
                ikat = "#6E3A2A", rambut = "#1D1612", gaya = GayaRambut.Pendek, penutup = Penutup.Udeng, wPenutup = "#6B6048",
                alas = false, tinggi = 1.04f, lebar = 1.05f },

            new NpcDef { nama = "Ayu", peran = "Gadis desa",
                pakaian = "Kebaya hijau sage, kain jarik cokelat, rambut dikepang dua.",
                kulit = "#B98458", baju = "#6F8A63", style = BajuStyle.Pendek, lengan = 2, bawah = Bawah.Kain, wBawah = "#5A3A22", fgKain = "#B89B6A",
                ikat = "#CDBF9C", rambut = "#1D1612", gaya = GayaRambut.Kepang,
                alas = true, tinggi = 0.92f, lebar = 0.88f },

            new NpcDef { nama = "Pak Lurah", peran = "Kepala desa",
                pakaian = "Baju indigo kusam, sarung cokelat, peci hitam, kumis tipis.",
                kulit = "#A97A52", baju = "#4A4F66", style = BajuStyle.Panjang, lengan = 2, bawah = Bawah.Kain, wBawah = "#5A4636", fgKain = "#8A7656",
                ikat = "#2A2A2A", rambut = "#2A2420", gaya = GayaRambut.Pendek, penutup = Penutup.Peci, wPenutup = "#1A1A1A", kumis = "#2A2420",
                alas = true, tinggi = 1.02f, lebar = 1f },

            new NpcDef { nama = "Bu Warti", peran = "Penjual jamu",
                pakaian = "Kebaya merah bata, kain batik gelap, selendang kuning kusam, sanggul.",
                kulit = "#A97A52", baju = "#8D4B3E", style = BajuStyle.Pendek, lengan = 2, bawah = Bawah.Kain, wBawah = "#3D2C22", fgKain = "#9A8052",
                ikat = "#6B4A2D", rambut = "#1D1612", gaya = GayaRambut.Sanggul, selendang = "#B89B5A",
                alas = true, tinggi = 0.94f, lebar = 1f },

            new NpcDef { nama = "Pak Rahmat", peran = "Tukang kayu",
                pakaian = "Kemeja linen lengan pendek, celemek kulit, celana cokelat tua, ikat kepala, kumis.",
                kulit = "#96673F", baju = "#8A7A5E", style = BajuStyle.Pendek, lengan = 1, celemek = true, bawah = Bawah.Celana, wBawah = "#3D3024",
                ikat = "#5A3A22", rambut = "#1D1612", gaya = GayaRambut.Pendek, penutup = Penutup.Udeng, wPenutup = "#7A6A50", kumis = "#1D1612",
                alas = true, tinggi = 1f, lebar = 1.08f },

            new NpcDef { nama = "Raka", peran = "Anak desa",
                pakaian = "Baju kuning tanah lengan pendek, celana pendek cokelat, tanpa alas kaki.",
                kulit = "#A9764D", baju = "#C49A45", style = BajuStyle.Pendek, lengan = 1, bawah = Bawah.CelanaPendek, wBawah = "#4A3322",
                ikat = "#5A3A22", rambut = "#1D1612", gaya = GayaRambut.Pendek,
                alas = false, tinggi = 0.72f, lebar = 0.82f, kepalaKali = 1.15f }
        };
    }
}
