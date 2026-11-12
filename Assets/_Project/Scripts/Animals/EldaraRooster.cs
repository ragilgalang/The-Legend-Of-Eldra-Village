// =====================================================================
//  Legenda Desa Eldara - Ayam Jago (Eldara Rooster) untuk Unity
//  Port dari skrip Blender v3: model dibangun dari primitif saat Play,
//  11 klip animasi dihitung prosedural (tanpa Animator / file .anim).
//
//  CARA PAKAI
//   1. Taruh file ini di Assets/ (mis. Assets/Scripts/EldaraRooster.cs).
//   2. Buat GameObject kosong > Add Component > Eldara > Eldara Rooster.
//   3. Tekan Play. Ayam menghadap +Z, origin di tanah di antara kedua kaki.
//   4. Dari skrip lain:   rooster.Play(RoosterClip.Run);
//                         rooster.PlayThenIdle(RoosterClip.Attack);
//
//  CATATAN
//   - Material: kalau kosong, skrip memakai shader URP/Lit, HDRP/Lit, atau Standard
//     (yang ditemukan). Untuk build, isi "Base Material" dengan material Lit
//     dari pipeline proyekmu supaya shader pasti ikut ter-build.
//   - Ayam ini ~300 mesh kecil. Untuk banyak ayam sekaligus (satu desa penuh),
//     sebaiknya export FBX dari Blender (skrip v3 sudah membuat keyframe) dan
//     pakai SkinnedMesh / Animator biasa.
// =====================================================================
using System;
using UnityEngine;

namespace Eldara
{
    public enum RoosterClip { Idle, Walk, Peck, Crow, Attack, Run, Hit, Die, Scratch, Flap, Sleep }

    [DisallowMultipleComponent]
    [AddComponentMenu("Eldara/Eldara Rooster")]
    public class EldaraRooster : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // PENGATURAN (Inspector)
        // ------------------------------------------------------------------
        [Header("Model")]
        [Tooltip("Skala keseluruhan (1 = tinggi sekitar 0.7 m).")]
        [SerializeField] float size = 1f;
        [Tooltip("Opsional: material Lit dari pipeline proyek. Kalau kosong dicari otomatis.")]
        [SerializeField] Material baseMaterial;

        [Header("Animasi")]
        [SerializeField] RoosterClip startClip = RoosterClip.Idle;
        [SerializeField, Range(0.1f, 3f)] float playSpeed = 1f;
        [Tooltip("Lama peralihan halus antar klip (detik).")]
        [SerializeField, Range(0f, 1f)] float crossFade = 0.2f;
        [Tooltip("Putar semua klip bergantian (untuk mencoba).")]
        [SerializeField] bool autoDemo;

        /// <summary>Dipanggil saat klip sekali-jalan (Crow, Attack, Hit, Die, Scratch, Flap) selesai.</summary>
        public event Action<RoosterClip> ClipFinished;

        public RoosterClip Current { get { return (RoosterClip)cur; } }

        // ------------------------------------------------------------------
        // DATA KLIP (24 fps, frame sama dengan skrip Blender)
        // ------------------------------------------------------------------
        struct ClipInfo
        {
            public int start, end;
            public bool loop;
            public ClipInfo(int s, int e, bool l) { start = s; end = e; loop = l; }
        }

        static readonly ClipInfo[] Clips =
        {
            new ClipInfo(1, 48, true),      // Idle
            new ClipInfo(49, 96, true),     // Walk
            new ClipInfo(97, 144, true),    // Peck
            new ClipInfo(145, 216, false),  // Crow
            new ClipInfo(217, 252, false),  // Attack
            new ClipInfo(253, 288, true),   // Run
            new ClipInfo(289, 312, false),  // Hit
            new ClipInfo(313, 372, false),  // Die
            new ClipInfo(373, 432, false),  // Scratch
            new ClipInfo(433, 480, false),  // Flap
            new ClipInfo(481, 552, true),   // Sleep
        };
        const float Fps = 24f;

        // Gerak susulan (overlapping action): ujung tubuh menyusul badan beberapa frame.
        static readonly J[] LagJoints = { J.Chest, J.Neck, J.Head, J.BeakLow, J.Tail, J.WingL, J.WingR };
        static readonly float[] LagFrames = { 1.5f, 2f, 3f, 4f, 4f, 3f, 3f };

        // ------------------------------------------------------------------
        // SENDI + POSE
        // ------------------------------------------------------------------
        enum J { Body, Chest, Neck, Head, BeakLow, Tail, WingL, WingR, HipL, HipR, HockL, HockR, FootL, FootR }
        const int N = 14;

        /// <summary>Selisih terhadap pose istirahat. Satuan: koordinat Blender (Z atas, depan -Y), rotasi derajat.</summary>
        sealed class RPose
        {
            public readonly Vector3[] rot = new Vector3[N];
            public readonly Vector3[] loc = new Vector3[N];
            public readonly float[] scl = new float[N];
            public RPose() { Clear(); }
            public void Clear()
            {
                Array.Clear(rot, 0, N);
                Array.Clear(loc, 0, N);
                for (int i = 0; i < N; i++) scl[i] = 1f;
            }
            public void Rot(J j, float x, float y, float z) { rot[(int)j] = new Vector3(x, y, z); }
            public void Loc(J j, float x, float y, float z) { loc[(int)j] = new Vector3(x, y, z); }
            public void Scl(J j, float s) { scl[(int)j] = s; }
            public void CopyJoint(int i, RPose o) { rot[i] = o.rot[i]; loc[i] = o.loc[i]; scl[i] = o.scl[i]; }
            public void CopyFrom(RPose o)
            {
                Array.Copy(o.rot, rot, N);
                Array.Copy(o.loc, loc, N);
                Array.Copy(o.scl, scl, N);
            }
            public static void Blend(RPose a, RPose b, float w, RPose r)
            {
                for (int i = 0; i < N; i++)
                {
                    r.rot[i] = Vector3.Lerp(a.rot[i], b.rot[i], w);
                    r.loc[i] = Vector3.Lerp(a.loc[i], b.loc[i], w);
                    r.scl[i] = Mathf.Lerp(a.scl[i], b.scl[i], w);
                }
            }
        }

        readonly Transform[] joints = new Transform[N];
        readonly Vector3[] restLoc = new Vector3[N];   // lokasi istirahat (koordinat Blender)

        readonly RPose shown = new RPose();    // pose yang ditampilkan
        readonly RPose fromPose = new RPose();     // pose saat peralihan dimulai
        readonly RPose target = new RPose();   // pose klip aktif
        readonly RPose tmp = new RPose();

        int cur;
        float time;           // 0..1 di dalam klip
        float blendT = 1f;
        bool finished;
        bool returnToIdle;
        int demoIndex;
        int demoLoops;

        // ------------------------------------------------------------------
        // SIKLUS HIDUP
        // ------------------------------------------------------------------
        void Awake()
        {
            Build();
            cur = (int)startClip;
            demoIndex = cur;
            time = 0f;
            EvalClip(cur, 0f, target);
            shown.CopyFrom(target);
            Apply(shown);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            Advance(dt * playSpeed);
            EvalClip(cur, Mathf.Clamp01(time), target);

            if (blendT < 1f)
            {
                blendT += crossFade > 0.0001f ? dt / crossFade : 1f;
                float w = Sstep(0f, 1f, Mathf.Min(1f, blendT));
                RPose.Blend(fromPose, target, w, shown);
            }
            else
            {
                shown.CopyFrom(target);
            }
            Apply(shown);
        }

        // ------------------------------------------------------------------
        // API
        // ------------------------------------------------------------------
        /// <summary>Putar klip. Klip yang sama tidak diulang dari awal kecuali restart = true.</summary>
        public void Play(RoosterClip clip, bool restart = false)
        {
            int i = (int)clip;
            if (i == cur && !restart && !finished) return;
            fromPose.CopyFrom(shown);
            blendT = crossFade > 0.0001f ? 0f : 1f;
            cur = i;
            time = 0f;
            finished = false;
            returnToIdle = false;
        }

        /// <summary>Putar klip sekali, lalu kembali ke Idle (kecuali Die yang berhenti di pose akhir).</summary>
        public void PlayThenIdle(RoosterClip clip)
        {
            Play(clip, true);
            returnToIdle = clip != RoosterClip.Die;
        }

        // ------------------------------------------------------------------
        // WAKTU
        // ------------------------------------------------------------------
        void Advance(float dt)
        {
            ClipInfo info = Clips[cur];
            float dur = (info.end - info.start) / Fps;
            time += dt / dur;

            if (info.loop)
            {
                if (time >= 1f)
                {
                    time -= Mathf.Floor(time);
                    if (autoDemo) { demoLoops++; if (demoLoops >= 2) NextDemo(); }
                }
            }
            else if (time >= 1f)
            {
                time = 1f;
                if (!finished)
                {
                    finished = true;
                    Action<RoosterClip> h = ClipFinished;
                    if (h != null) h((RoosterClip)cur);
                    if (autoDemo) NextDemo();
                    else if (returnToIdle) Play(RoosterClip.Idle);
                }
            }
        }

        void NextDemo()
        {
            demoLoops = 0;
            demoIndex = (demoIndex + 1) % Clips.Length;
            Play((RoosterClip)demoIndex, true);
        }

        // ------------------------------------------------------------------
        // EVALUASI KLIP (+ gerak susulan)
        // ------------------------------------------------------------------
        void EvalClip(int ci, float t, RPose output)
        {
            ClipInfo info = Clips[ci];
            output.Clear();
            Fn(ci, t, output);
            float span = info.end - info.start;
            for (int k = 0; k < LagJoints.Length; k++)
            {
                float lag = LagFrames[k] / span;
                float tl;
                if (info.loop) tl = Mathf.Repeat(t - lag, 1f);
                else tl = Mathf.Max(0f, t - lag * (1f - Sstep(0.85f, 1f, t)));  // jeda mengecil di akhir klip
                tmp.Clear();
                Fn(ci, tl, tmp);
                output.CopyJoint((int)LagJoints[k], tmp);
            }
        }

        static void Fn(int ci, float t, RPose P)
        {
            switch ((RoosterClip)ci)
            {
                case RoosterClip.Idle: PoseIdle(t, P); break;
                case RoosterClip.Walk: PoseWalk(t, P); break;
                case RoosterClip.Peck: PosePeck(t, P); break;
                case RoosterClip.Crow: PoseCrow(t, P); break;
                case RoosterClip.Attack: PoseAttack(t, P); break;
                case RoosterClip.Run: PoseRun(t, P); break;
                case RoosterClip.Hit: PoseHit(t, P); break;
                case RoosterClip.Die: PoseDie(t, P); break;
                case RoosterClip.Scratch: PoseScratch(t, P); break;
                case RoosterClip.Flap: PoseFlap(t, P); break;
                case RoosterClip.Sleep: PoseSleep(t, P); break;
            }
        }

        // ------------------------------------------------------------------
        // MATEMATIKA BANTU
        // ------------------------------------------------------------------
        const float PI = Mathf.PI;
        static float Sin(float x) { return Mathf.Sin(x); }
        static float Cos(float x) { return Mathf.Cos(x); }
        static float Max(float a, float b) { return a > b ? a : b; }
        static float Lerp(float a, float b, float x) { return a + (b - a) * x; }

        static float Sstep(float a, float b, float x)
        {
            float t = Mathf.Clamp01((x - a) / (b - a));
            return t * t * (3f - 2f * t);
        }

        /// <summary>Interpolasi halus antar titik (waktu, nilai): menoleh cepat lalu menahan.</summary>
        static float Keys(float t, float[] ts, float[] vs)
        {
            if (t <= ts[0]) return vs[0];
            for (int i = 0; i < ts.Length - 1; i++)
                if (t <= ts[i + 1]) return Lerp(vs[i], vs[i + 1], Sstep(ts[i], ts[i + 1], t));
            return vs[vs.Length - 1];
        }

        /// <summary>Kepala ayam: menyentak ke depan lalu "tertahan" sementara badan maju. -0.5..0.5 (negatif = depan).</summary>
        static float HeadHold(float u)
        {
            if (u < 0.25f) return Lerp(0.5f, -0.5f, Sstep(0f, 0.25f, u));
            return Lerp(-0.5f, 0.5f, (u - 0.25f) / 0.75f);
        }

        static readonly float[] GazeT = { 0f, 0.10f, 0.18f, 0.38f, 0.46f, 0.54f, 0.78f, 0.88f, 1f };
        static readonly float[] GazeV = { 0f, 0f, 24f, 24f, -2f, -20f, -20f, 0f, 0f };

        // ------------------------------------------------------------------
        // POSE (port dari skrip Blender v3)
        // ------------------------------------------------------------------
        static void PoseIdle(float t, RPose P)
        {
            float w = 2f * PI * t;
            float g = Keys(t, GazeT, GazeV);
            P.Loc(J.Body, 0, 0, 0.004f * Sin(w));
            P.Rot(J.Body, 1.0f * Sin(w), 1.2f * Sin(w + 0.7f), 0.10f * g);
            P.Scl(J.Chest, 1f + 0.02f * Sin(w));
            P.Rot(J.Neck, 2f * Sin(w + 0.5f), 0, 0.30f * g);
            P.Rot(J.Head, 3f * Sin(2f * w) + 2f * Mathf.Abs(g) / 24f, 0.12f * g, 0.60f * g + 2f * Sin(3f * w));
            P.Rot(J.Tail, 2f * Sin(w + 1f), 0, 3f * Sin(w) - 0.08f * g);
            P.Rot(J.WingL, 0, -1.5f * Sin(w), 0);
            P.Rot(J.WingR, 0, 1.5f * Sin(w), 0);
        }

        static void PoseWalk(float t, RPose P)
        {
            float phi = 2f * PI * 2f * t;
            float hh = HeadHold((2f * t) % 1f);
            for (int s = 0; s < 2; s++)
            {
                bool left = s == 0;
                float ph = phi + (left ? 0f : PI);
                float hip = -22f * Sin(ph) + 4f;
                float hock = 38f * Max(0f, Cos(ph)) + 5f * Max(0f, -Cos(ph));
                P.Rot(left ? J.HipL : J.HipR, hip, 0, 0);
                P.Rot(left ? J.HockL : J.HockR, hock, 0, 0);
                P.Rot(left ? J.FootL : J.FootR, -(hip + hock), 0, 0);
            }
            P.Loc(J.Body, 0, 0, 0.010f * Cos(2f * phi));
            P.Rot(J.Body, 2f * Cos(2f * phi), 2.5f * Sin(phi), 3f * Sin(phi));
            P.Rot(J.Neck, -10f * hh, 0, 0);
            P.Loc(J.Head, 0, 0.05f * hh, 0);
            P.Rot(J.Tail, 4f * Sin(2f * phi + 0.5f), 0, 4f * Sin(phi));
        }

        static void PosePeck(float t, RPose P)
        {
            float u = (2f * t) % 1f;
            float lift = Sstep(0f, 0.12f, u) * (1f - Sstep(0.12f, 0.22f, u));
            float p = Sstep(0.12f, 0.26f, u) * (1f - Sstep(0.50f, 0.80f, u));
            float s = Max(0f, u - 0.26f);
            if (u < 0.5f) p += 0.04f * Mathf.Exp(-14f * s) * Sin(2f * PI * 5f * s);
            float beak = Sstep(0.12f, 0.24f, u) * (1f - Sstep(0.24f, 0.34f, u));
            P.Rot(J.Body, 44f * p - 4f * lift, 0, 0);
            P.Loc(J.Body, 0, 0, -0.075f * p);
            P.Rot(J.Neck, 64f * p - 9f * lift, 0, 0);
            P.Rot(J.Head, 48f * p - 12f * lift, 0, 0);
            P.Rot(J.Tail, -10f * p + 6f * lift, 0, 0);
            P.Rot(J.BeakLow, 14f * beak, 0, 0);
            P.Rot(J.HipL, 16f * p, 0, 0); P.Rot(J.HipR, 16f * p, 0, 0);
            P.Rot(J.HockL, 20f * p, 0, 0); P.Rot(J.HockR, 20f * p, 0, 0);
            P.Rot(J.FootL, -36f * p, 0, 0); P.Rot(J.FootR, -36f * p, 0, 0);
        }

        static void PoseCrow(float t, RPose P)
        {
            float dip = Sstep(0f, 0.10f, t) * (1f - Sstep(0.10f, 0.24f, t));
            float e = Sstep(0.10f, 0.32f, t) * (1f - Sstep(0.82f, 1f, t));
            float open = e * (0.45f + 0.55f * (0.5f + 0.5f * Sin(2f * PI * 4f * t)));
            float flap = e * (22f + 10f * Sin(2f * PI * 3f * t));
            float shake = e * Sin(2f * PI * 7f * t);
            P.Rot(J.Body, -14f * e + 6f * dip, 0, 0);
            P.Loc(J.Body, 0, 0, 0.02f * e - 0.015f * dip);
            P.Scl(J.Chest, 1f + 0.16f * e - 0.04f * dip);
            P.Rot(J.Neck, -28f * e + 10f * dip, 0, 1.5f * shake);
            P.Rot(J.Head, -34f * e + 12f * dip, 0, 2.0f * shake);
            P.Rot(J.BeakLow, 34f * open, 0, 0);
            P.Rot(J.WingL, 0, -(flap + 4f * dip), 0);
            P.Rot(J.WingR, 0, flap + 4f * dip, 0);
            P.Rot(J.Tail, 10f * e - 5f * dip, 0, 0);
            P.Rot(J.HipL, -4f * e, 0, 0); P.Rot(J.HipR, -4f * e, 0, 0);
        }

        static void PoseAttack(float t, RPose P)
        {
            float wind = Sstep(0f, 0.3f, t) - Sstep(0.3f, 0.38f, t);
            float strike = Sstep(0.28f, 0.42f, t) - Sstep(0.55f, 0.9f, t);
            float spread = 50f * (wind + strike);
            P.Rot(J.Body, -10f * wind + 24f * strike, 0, 0);
            P.Loc(J.Body, 0, -0.07f * strike, -0.02f * strike);
            P.Rot(J.Neck, -18f * wind + 38f * strike, 0, 0);
            P.Rot(J.Head, -10f * wind + 24f * strike, 0, 0);
            P.Rot(J.BeakLow, 25f * wind + 12f * strike, 0, 0);
            P.Rot(J.WingL, 0, -spread, 0);
            P.Rot(J.WingR, 0, spread, 0);
            P.Rot(J.Tail, -6f * strike + 8f * wind, 0, 0);
            P.Scl(J.Chest, 1f + 0.08f * wind);
            for (int s = 0; s < 2; s++)
            {
                bool left = s == 0;
                P.Rot(left ? J.HipL : J.HipR, -16f * strike + 4f * wind, 0, 0);
                P.Rot(left ? J.HockL : J.HockR, 10f * strike, 0, 0);
                P.Rot(left ? J.FootL : J.FootR, 6f * strike - 4f * wind, 0, 0);
            }
        }

        static void PoseRun(float t, RPose P)
        {
            float phi = 2f * PI * 3f * t;
            float hh = HeadHold((3f * t) % 1f);
            for (int s = 0; s < 2; s++)
            {
                bool left = s == 0;
                float ph = phi + (left ? 0f : PI);
                float hip = -35f * Sin(ph) + 6f;
                float hock = 55f * Max(0f, Cos(ph));
                P.Rot(left ? J.HipL : J.HipR, hip, 0, 0);
                P.Rot(left ? J.HockL : J.HockR, hock, 0, 0);
                P.Rot(left ? J.FootL : J.FootR, -(hip + hock), 0, 0);
            }
            float sp = Sin(phi);
            P.Loc(J.Body, 0, 0, 0.02f * sp * sp);
            P.Rot(J.Body, 10f, 2.5f * sp, 4f * sp);
            P.Rot(J.Neck, 14f - 8f * hh, 0, 0);
            P.Loc(J.Head, 0, 0.04f * hh, 0);
            P.Rot(J.Head, 8f, 0, 0);
            P.Rot(J.WingL, 0, -(25f + 10f * sp), 0);
            P.Rot(J.WingR, 0, 25f + 10f * sp, 0);
            P.Rot(J.Tail, 6f * sp, 0, 5f * sp);
        }

        static void PoseHit(float t, RPose P)
        {
            float s = Max(0f, t - 0.1f);
            float p = Sstep(0f, 0.1f, t) * Mathf.Exp(-4f * s) * Cos(2f * PI * 1.3f * s) * (1f - Sstep(0.85f, 1f, t));
            P.Rot(J.Body, -18f * p, 0, 0);
            P.Loc(J.Body, 0, 0.05f * p, 0);
            P.Rot(J.Neck, -25f * p, 0, 0);
            P.Rot(J.Head, -20f * p, 0, 12f * Sin(2f * PI * 3f * t) * p);
            P.Rot(J.BeakLow, 30f * Max(0f, p), 0, 0);
            P.Rot(J.WingL, 0, -35f * p, 0);
            P.Rot(J.WingR, 0, 35f * p, 0);
            P.Rot(J.Tail, 15f * p, 0, 0);
            P.Scl(J.Chest, 1f + 0.1f * p);
        }

        static void PoseDie(float t, RPose P)
        {
            float stag = Sstep(0f, 0.15f, t) * (1f - Sstep(0.15f, 0.3f, t));
            float f = Mathf.Pow(Sstep(0.2f, 0.6f, t), 1.6f);
            float tw = Sstep(0.6f, 0.68f, t) * (1f - Sstep(0.8f, 0.95f, t));
            float kick = Sin(2f * PI * 5f * t) * tw;
            float land = Max(0f, t - 0.6f);
            float bounce = 0.025f * Mathf.Exp(-18f * land) * Mathf.Abs(Sin(2f * PI * 3.5f * land));
            P.Rot(J.Body, -14f * stag, 90f * f, 0);
            P.Loc(J.Body, 0, 0.04f * stag, -0.21f * f + bounce);
            P.Rot(J.Neck, -20f * stag + 18f * f, 0, 22f * f);
            P.Rot(J.Head, -10f * stag + 20f * f, 0, 28f * f);
            P.Rot(J.BeakLow, 30f * stag + 22f * f, 0, 0);
            P.Rot(J.WingL, 0, -(40f * stag + 35f * f + 8f * kick), 0);
            P.Rot(J.WingR, 0, 40f * stag + 8f * f, 0);
            P.Rot(J.Tail, 12f * stag - 6f * f, 0, 14f * f);
            P.Scl(J.Chest, 1f + 0.08f * stag);
            for (int s = 0; s < 2; s++)
            {
                bool left = s == 0;
                P.Rot(left ? J.HipL : J.HipR, 55f * f + 14f * kick, 0, 0);
                P.Loc(left ? J.HipL : J.HipR, 0, 0, -0.21f * f);
                P.Rot(left ? J.HockL : J.HockR, -70f * f, 0, 0);
                P.Rot(left ? J.FootL : J.FootR, 15f * f - 20f * kick, 0, 0);
            }
        }

        static void PoseScratch(float t, RPose P)
        {
            float e = Sstep(0f, 0.1f, t) * (1f - Sstep(0.9f, 1f, t));
            float c = 0.5f - 0.5f * Cos(2f * PI * 3f * t);
            float s3 = Sin(2f * PI * 3f * t);
            P.Rot(J.Body, 10f * e, 0, -4f * e);
            P.Loc(J.Body, 0, 0, -0.01f * e);
            P.Rot(J.Neck, 14f * e, 0, 0);
            P.Rot(J.Head, 18f * e, 0, 8f * e * s3);
            P.Rot(J.Tail, -4f * e, 0, 3f * e * s3);
            P.Rot(J.HipL, 2f * e, 0, 0);
            P.Rot(J.FootL, -2f * e, 0, 0);
            P.Rot(J.HipR, e * (-48f + 62f * c), 0, 0);
            P.Rot(J.HockR, e * (34f - 10f * c), 0, 0);
            P.Rot(J.FootR, e * (14f - 18f * c), 0, 0);
        }

        static void PoseFlap(float t, RPose P)
        {
            float crouch = Sstep(0f, 0.10f, t) * (1f - Sstep(0.10f, 0.17f, t));
            float e = Sstep(0.10f, 0.20f, t) * (1f - Sstep(0.85f, 1f, t));
            float air = Sstep(0.14f, 0.34f, t) * (1f - Sstep(0.6f, 0.9f, t));
            float fl = 5f * 2f * PI * t;
            float amp = e * (45f + 40f * Sin(fl)) + 15f * crouch;
            P.Rot(J.Body, -12f * e + 8f * crouch, 0, 0);
            P.Loc(J.Body, 0, 0, 0.14f * air - 0.03f * crouch);
            P.Rot(J.Neck, -14f * e + 8f * crouch, 0, 0);
            P.Rot(J.Head, -8f * e + 6f * crouch, 0, 0);
            P.Rot(J.BeakLow, 22f * e, 0, 0);
            P.Rot(J.WingL, 0, -amp, 0);
            P.Rot(J.WingR, 0, amp, 0);
            P.Rot(J.Tail, 14f * e + 6f * Sin(fl) * e, 0, 0);
            P.Scl(J.Chest, 1f + 0.06f * e);
            for (int s = 0; s < 2; s++)
            {
                bool left = s == 0;
                P.Rot(left ? J.HipL : J.HipR, 22f * air + 12f * crouch, 0, 0);
                P.Loc(left ? J.HipL : J.HipR, 0, 0, 0.14f * air - 0.033f * crouch);
                P.Rot(left ? J.HockL : J.HockR, 34f * air + 25f * crouch, 0, 0);
                P.Rot(left ? J.FootL : J.FootR, -56f * air - 37f * crouch, 0, 0);
            }
        }

        static void PoseSleep(float t, RPose P)
        {
            float w = 2f * PI * t;
            P.Rot(J.Body, -4f, 0, 0);
            P.Loc(J.Body, 0, 0, -0.13f + 0.004f * Sin(w));
            P.Scl(J.Chest, 1f + 0.03f * Sin(w));
            P.Rot(J.Neck, 30f, 0, 0);
            P.Rot(J.Head, 10f, 0, 165f);
            P.Rot(J.WingL, 0, 3f + 0.8f * Sin(w), 0);
            P.Rot(J.WingR, 0, -3f - 0.8f * Sin(w), 0);
            P.Rot(J.Tail, -6f, 0, 0);
            for (int s = 0; s < 2; s++)
            {
                bool left = s == 0;
                P.Rot(left ? J.HipL : J.HipR, 70f, 0, 0);
                P.Loc(left ? J.HipL : J.HipR, 0, 0, -0.13f);
                P.Rot(left ? J.HockL : J.HockR, -120f, 0, 0);
                P.Rot(left ? J.FootL : J.FootR, 50f, 0, 0);
            }
        }

        // ------------------------------------------------------------------
        // KONVERSI KOORDINAT: Blender (Z atas, depan -Y, kanan-tangan)  ->  Unity (Y atas, depan +Z)
        //   posisi : (x, y, z) -> (x, z, -y)
        //   rotasi : Euler XYZ Blender = Rz * Ry * Rx, dengan sumbu Y Blender = -Z Unity
        // ------------------------------------------------------------------
        static Vector3 ToU(Vector3 b) { return new Vector3(b.x, b.z, -b.y); }

        static Quaternion ToQ(Vector3 bDeg)
        {
            Quaternion qx = Quaternion.AngleAxis(bDeg.x, Vector3.right);
            Quaternion qy = Quaternion.AngleAxis(bDeg.y, Vector3.back);
            Quaternion qz = Quaternion.AngleAxis(bDeg.z, Vector3.up);
            return qz * qy * qx;
        }

        void Apply(RPose P)
        {
            for (int i = 0; i < N; i++)
            {
                Transform t = joints[i];
                if (t == null) continue;
                t.localPosition = ToU(restLoc[i] + P.loc[i]);
                t.localRotation = ToQ(P.rot[i]);
                t.localScale = Vector3.one * P.scl[i];
            }
        }

        // ------------------------------------------------------------------
        // MODEL
        // ------------------------------------------------------------------
        enum Kind { Sphere, Cyl, Cone }

        sealed class Mats
        {
            public Material orange, orangeL, brown, green, black, comb, wattle, beak, leg, eye, pupil;
        }
        Mats M;

        static Mesh coneMesh;

        static Mesh ConeMesh()
        {
            if (coneMesh != null) return coneMesh;
            const int n = 16;
            // alas radius 1 di y = -1, ujung di y = +1 (sama dengan kerucut Blender: alas -Z, ujung +Z)
            var v = new Vector3[(n + 1) * 2 + 1 + n];
            var nr = new Vector3[v.Length];
            var tri = new int[n * 3 + n * 3];
            int vi = 0, ti = 0;
            int ringStart = 0;
            for (int i = 0; i <= n; i++)
            {
                float a = 2f * PI * i / n;
                v[vi] = new Vector3(Cos(a), -1f, Sin(a));
                nr[vi] = new Vector3(2f * Cos(a), 1f, 2f * Sin(a)).normalized;
                vi++;
            }
            int apexStart = vi;
            for (int i = 0; i <= n; i++)
            {
                float a = 2f * PI * (i + 0.5f) / n;
                v[vi] = new Vector3(0f, 1f, 0f);
                nr[vi] = new Vector3(2f * Cos(a), 1f, 2f * Sin(a)).normalized;
                vi++;
            }
            for (int i = 0; i < n; i++)
            {
                tri[ti++] = ringStart + i;
                tri[ti++] = apexStart + i;
                tri[ti++] = ringStart + i + 1;
            }
            int centerIdx = vi;
            v[vi] = new Vector3(0f, -1f, 0f); nr[vi] = Vector3.down; vi++;
            int capStart = vi;
            for (int i = 0; i < n; i++)
            {
                float a = 2f * PI * i / n;
                v[vi] = new Vector3(Cos(a), -1f, Sin(a)); nr[vi] = Vector3.down; vi++;
            }
            for (int i = 0; i < n; i++)
            {
                tri[ti++] = centerIdx;
                tri[ti++] = capStart + i;
                tri[ti++] = capStart + (i + 1) % n;
            }
            var m = new Mesh();
            m.name = "EldaraCone";
            m.vertices = v;
            m.normals = nr;
            m.triangles = tri;
            m.RecalculateBounds();
            coneMesh = m;
            return m;
        }

        Material MakeMat(string name, float r, float g, float b, float metallic = 0f, float rough = 0.7f)
        {
            Material mat;
            if (baseMaterial != null)
            {
                mat = new Material(baseMaterial);
            }
            else
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("HDRP/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                mat = new Material(sh);
            }
            mat.name = name;
            // nilai di skrip Blender bersifat linear; material Unity menerima warna ruang-gamma
            Color c = new Color(r, g, b, 1f).gamma;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f - rough);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 1f - rough);
            return mat;
        }

        void CreateMaterials()
        {
            M = new Mats();
            M.orange = MakeMat("Bulu_Oranye", 0.62f, 0.20f, 0.035f);
            M.orangeL = MakeMat("Bulu_OranyeTerang", 0.80f, 0.33f, 0.06f);
            M.brown = MakeMat("Bulu_Cokelat", 0.28f, 0.08f, 0.02f);
            M.green = MakeMat("Bulu_Hijau", 0.04f, 0.13f, 0.06f, 0.1f, 0.5f);
            M.black = MakeMat("Bulu_HitamHijau", 0.008f, 0.016f, 0.012f, 0.1f, 0.5f);
            M.comb = MakeMat("Jengger_Merah", 0.48f, 0.06f, 0.03f, 0f, 0.5f);
            M.wattle = MakeMat("Pial_Merah", 0.30f, 0.03f, 0.03f, 0f, 0.5f);
            M.beak = MakeMat("Paruh_Kuning", 0.75f, 0.45f, 0.08f, 0f, 0.4f);
            M.leg = MakeMat("Kaki_Kuning", 0.55f, 0.33f, 0.09f, 0f, 0.5f);
            M.eye = MakeMat("Mata_Amber", 0.80f, 0.50f, 0.03f, 0f, 0.3f);
            M.pupil = MakeMat("Pupil", 0.005f, 0.005f, 0.005f, 0f, 0.3f);
        }

        /// <summary>Bentuk dasar. loc/scale/rot dalam konvensi Blender (lihat skrip Blender).</summary>
        void Prim(Kind kind, string name, Vector3 loc, Vector3 scale, Material mat, Vector3 rot, Transform parent)
        {
            GameObject go;
            Vector3 s;   // skala Unity: sumbu Blender (x, y, z) -> Unity (x, z, y)
            if (kind == Kind.Cone)
            {
                go = new GameObject(name);
                go.AddComponent<MeshFilter>().sharedMesh = ConeMesh();
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                s = new Vector3(scale.x, scale.z, scale.y);
            }
            else
            {
                go = GameObject.CreatePrimitive(kind == Kind.Sphere ? PrimitiveType.Sphere : PrimitiveType.Cylinder);
                go.name = name;
                Collider col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                // sphere: diameter 1, silinder: radius 0.5 dan tinggi 2 (sumbu Y)
                s = kind == Kind.Sphere
                    ? new Vector3(2f * scale.x, 2f * scale.z, 2f * scale.y)
                    : new Vector3(2f * scale.x, scale.z, 2f * scale.y);
            }
            Transform t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = ToU(loc);
            t.localRotation = ToQ(rot);
            t.localScale = s;
        }

        static Vector3 V(float x, float y, float z) { return new Vector3(x, y, z); }

        Transform Piv(J j, Transform parent, Vector3 blenderLoc)
        {
            var go = new GameObject(j.ToString());
            go.transform.SetParent(parent, false);
            joints[(int)j] = go.transform;
            restLoc[(int)j] = blenderLoc;
            return go.transform;
        }

        void Build()
        {
            Transform old = transform.Find("Ayam_Root");
            if (old != null) Destroy(old.gameObject);

            CreateMaterials();
            var root = new GameObject("Ayam_Root").transform;
            root.SetParent(transform, false);
            root.localScale = Vector3.one * size;

            BuildLeg(root, 1f, true);
            BuildLeg(root, -1f, false);

            Transform body = Piv(J.Body, root, V(0f, 0f, 0.34f));
            Prim(Kind.Sphere, "BodyMain", V(0, 0.02f, 0), V(0.125f, 0.21f, 0.14f), M.black, V(-18, 0, 0), body);
            Prim(Kind.Sphere, "BodySaddle", V(0, 0.05f, 0.05f), V(0.128f, 0.17f, 0.09f), M.orange, V(-12, 0, 0), body);
            Prim(Kind.Sphere, "RumpFeathers", V(0, 0.13f, 0.075f), V(0.10f, 0.09f, 0.06f), M.orangeL, V(-20, 0, 0), body);

            Transform chest = Piv(J.Chest, body, V(0f, -0.14f, 0.04f));
            Prim(Kind.Sphere, "ChestUpper", V(0, 0, 0), V(0.115f, 0.115f, 0.13f), M.orange, V(0, 0, 0), chest);
            Prim(Kind.Sphere, "ChestLower", V(0, -0.01f, -0.05f), V(0.11f, 0.10f, 0.09f), M.black, V(0, 0, 0), chest);
            Prim(Kind.Sphere, "ChestGreen", V(0, -0.075f, -0.02f), V(0.085f, 0.04f, 0.06f), M.green, V(0, 0, 0), chest);

            BuildWing(body, 1f, true);
            BuildWing(body, -1f, false);
            BuildTail(body);
            BuildNeckHead(body);

            Apply(shown);
        }

        void BuildLeg(Transform root, float sx, bool left)
        {
            string s = left ? "L" : "R";
            Transform hip = Piv(left ? J.HipL : J.HipR, root, V(0.07f * sx, 0f, 0.30f));
            Prim(Kind.Sphere, "ThighFeathers_" + s, V(0, 0.01f, -0.035f), V(0.055f, 0.08f, 0.085f), M.black, V(0, 0, 0), hip);
            Prim(Kind.Sphere, "ThighTop_" + s, V(0.01f * sx, 0, -0.005f), V(0.05f, 0.075f, 0.06f), M.orange, V(0, 0, 0), hip);
            Prim(Kind.Cyl, "Tibia_" + s, V(0, 0, -0.085f), V(0.026f, 0.026f, 0.075f), M.leg, V(0, 0, 0), hip);

            Transform hock = Piv(left ? J.HockL : J.HockR, hip, V(0f, 0f, -0.15f));
            Prim(Kind.Cyl, "Tarsus_" + s, V(0, 0, -0.075f), V(0.017f, 0.017f, 0.078f), M.leg, V(0, 0, 0), hock);
            Prim(Kind.Cone, "Spur_" + s, V(0, 0.022f, -0.06f), V(0.008f, 0.008f, 0.022f), M.beak, V(-90, 0, 0), hock);

            Transform foot = Piv(left ? J.FootL : J.FootR, hock, V(0f, 0f, -0.15f));
            float[] toes = { -24f, 0f, 24f };
            for (int i = 0; i < 3; i++)
            {
                float a = toes[i] * Mathf.Deg2Rad;
                Prim(Kind.Cone, "Toe_" + s + i, V(0.04f * Sin(a), -0.04f * Cos(a), 0.009f), V(0.009f, 0.009f, 0.042f), M.leg, V(90, 0, toes[i]), foot);
            }
            Prim(Kind.Cone, "ToeBack_" + s, V(0, 0.022f, 0.009f), V(0.008f, 0.008f, 0.026f), M.leg, V(-90, 0, 0), foot);
            Prim(Kind.Sphere, "Ankle_" + s, V(0, 0, 0.008f), V(0.022f, 0.022f, 0.016f), M.leg, V(0, 0, 0), foot);
        }

        void BuildWing(Transform body, float sx, bool left)
        {
            string s = left ? "L" : "R";
            Transform w = Piv(left ? J.WingL : J.WingR, body, V(0.118f * sx, 0f, 0.075f));
            Prim(Kind.Sphere, "WingMain_" + s, V(0.015f * sx, 0.02f, -0.055f), V(0.03f, 0.13f, 0.08f), M.orange, V(-8, 0, 0), w);
            Prim(Kind.Sphere, "WingPatch_" + s, V(0.026f * sx, 0.045f, -0.078f), V(0.028f, 0.075f, 0.042f), M.green, V(-8, 0, 0), w);
            Prim(Kind.Sphere, "WingEdge_" + s, V(0.014f * sx, 0.05f, -0.105f), V(0.026f, 0.1f, 0.022f), M.brown, V(-8, 0, 0), w);
            Prim(Kind.Cone, "WingTip_" + s, V(0.012f * sx, 0.15f, -0.062f), V(0.018f, 0.012f, 0.08f), M.black, V(-98, 0, 0), w);
        }

        void BuildTail(Transform body)
        {
            Transform t = Piv(J.Tail, body, V(0f, 0.19f, 0.06f));
            Prim(Kind.Sphere, "TailBase", V(0, 0.02f, 0), V(0.07f, 0.07f, 0.07f), M.orange, V(0, 0, 0), t);
            float[] segLen = { 0.11f, 0.10f, 0.08f };
            float[] widths = { 0.034f, 0.038f, 0.026f };
            for (int i = 0; i < 7; i++)   // bulu ekor panjang melengkung (kipas)
            {
                float a1 = 14f + i * 9f;
                float[] angles = { a1, a1 + 34f, a1 + 66f };
                float lm = 1.08f - 0.07f * i;
                float yaw = (i - 3) * 2.5f;
                float xoff = (i - 3) * 0.006f;
                Material mat = i % 2 == 0 ? M.black : M.green;
                float py = 0f, pz = 0f;
                for (int j = 0; j < 3; j++)
                {
                    float ang = angles[j];
                    float h = segLen[j] * lm;
                    float dy = Sin(ang * Mathf.Deg2Rad), dz = Cos(ang * Mathf.Deg2Rad);
                    float cy = py + dy * h, cz = pz + dz * h;
                    py += dy * 2f * h; pz += dz * 2f * h;
                    float yy = cy * Cos(yaw * Mathf.Deg2Rad);
                    float xx = -cy * Sin(yaw * Mathf.Deg2Rad) + xoff;
                    Prim(Kind.Sphere, "TailFeather_" + i + "_" + j, V(xx, yy, cz), V(0.007f, widths[j], h), mat, V(-ang, 0, yaw), t);
                }
            }
            float[] shortPhi = { -18f, 0f, 18f };   // bulu oranye pendek di pangkal ekor
            for (int i = 0; i < 3; i++)
            {
                float a = shortPhi[i] * Mathf.Deg2Rad;
                Prim(Kind.Sphere, "TailShort_" + i, V(0.05f * Sin(a), 0.07f, 0.06f), V(0.012f, 0.03f, 0.07f), M.orangeL, V(-40, 0, shortPhi[i]), t);
            }
        }

        void BuildNeckHead(Transform body)
        {
            Transform neck = Piv(J.Neck, body, V(0f, -0.17f, 0.10f));
            Prim(Kind.Sphere, "NeckMain", V(0, -0.005f, 0.075f), V(0.072f, 0.078f, 0.115f), M.orange, V(8, 0, 0), neck);
            for (int a = 0; a < 8; a++)   // bulu leher (hackle)
            {
                float ang = a * 45f * Mathf.Deg2Rad;
                Prim(Kind.Sphere, "Hackle_" + a, V(0.075f * Sin(ang), -0.005f + 0.07f * Cos(ang), 0.025f), V(0.024f, 0.024f, 0.065f), M.orangeL, V(0, 0, 0), neck);
            }

            Transform head = Piv(J.Head, neck, V(0f, -0.04f, 0.18f));
            Prim(Kind.Sphere, "HeadMain", V(0, 0, 0.03f), V(0.047f, 0.054f, 0.05f), M.orange, V(0, 0, 0), head);
            for (int k = 0; k < 2; k++)
            {
                float sx = k == 0 ? 1f : -1f;
                string s = k == 0 ? "L" : "R";
                Prim(Kind.Sphere, "Cheek_" + s, V(0.038f * sx, -0.018f, 0.026f), V(0.014f, 0.034f, 0.032f), M.comb, V(0, 0, 0), head);
                Prim(Kind.Sphere, "Eye_" + s, V(0.043f * sx, -0.03f, 0.05f), V(0.011f, 0.013f, 0.013f), M.eye, V(0, 0, 0), head);
                Prim(Kind.Sphere, "Pupil_" + s, V(0.0505f * sx, -0.031f, 0.05f), V(0.005f, 0.007f, 0.007f), M.pupil, V(0, 0, 0), head);
                Prim(Kind.Sphere, "Wattle_" + s, V(0.009f * sx, -0.052f, -0.012f), V(0.008f, 0.011f, 0.028f), M.wattle, V(0, 0, 0), head);
            }
            // jengger bergerigi
            Prim(Kind.Sphere, "CombBase", V(0, -0.002f, 0.077f), V(0.007f, 0.04f, 0.014f), M.comb, V(0, 0, 0), head);
            float[] cy = { -0.03f, -0.01f, 0.011f, 0.032f };
            float[] ch = { 0.02f, 0.03f, 0.028f, 0.019f };
            for (int i = 0; i < 4; i++)
                Prim(Kind.Cone, "CombPoint_" + i, V(0, cy[i], 0.09f + ch[i] * 0.4f), V(0.007f, 0.012f, ch[i]), M.comb, V(0, 0, 0), head);
            // paruh atas + paruh bawah (bisa membuka)
            Prim(Kind.Cone, "BeakUpper", V(0, -0.078f, 0.03f), V(0.016f, 0.011f, 0.032f), M.beak, V(90, 0, 0), head);
            Transform beak = Piv(J.BeakLow, head, V(0f, -0.058f, 0.021f));
            Prim(Kind.Cone, "BeakLower", V(0, -0.02f, 0), V(0.012f, 0.007f, 0.024f), M.beak, V(90, 0, 0), beak);
        }
    }
}

