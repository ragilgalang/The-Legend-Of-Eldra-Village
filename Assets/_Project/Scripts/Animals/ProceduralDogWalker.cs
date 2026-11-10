using UnityEngine;

/// <summary>
/// Kontroler Siklus Langkah Prosedural Anjing (Porting algoritma Blender dog_walk.py).
/// Menggerakkan tulang-tulang DogRig secara matematis:
/// - Urutan fase langkah kuadruped: BL=0.0, FL=0.25, BR=0.5, FR=0.75
/// - Ayunan sudut paha (SWING) & tekukan lutut (FLEX)
/// - Pantulan vertikal badan (root bounce), goyangan pinggang (spine wag), anggukan kepala, & kibasan ekor.
/// </summary>
public class ProceduralDogWalker : MonoBehaviour
{
    [Header("Armature Bones")]
    public Transform rootBone;
    public Transform spineBone;
    public Transform neckBone;
    public Transform headBone;
    public Transform tail1Bone;
    public Transform tail2Bone;

    [Header("Leg Bones (Depan / Belakang)")]
    public Transform flUp;
    public Transform flLo;
    public Transform frUp;
    public Transform frLo;
    public Transform blUp;
    public Transform blLo;
    public Transform brUp;
    public Transform brLo;

    [Header("Parameter Siklus")]
    public float cycleDuration = 1.8f; // Durasi satu siklus langkah (detik)
    public float walkSpeedThreshold = 0.12f;

    private AnimalController animalCtrl;
    private float walkTime;

    private Vector3 initialRootPos;
    private Quaternion initSpineRot;
    private Quaternion initNeckRot;
    private Quaternion initHeadRot;
    private Quaternion initTail1Rot;
    private Quaternion initTail2Rot;

    private Quaternion initFlUp, initFlLo;
    private Quaternion initFrUp, initFrLo;
    private Quaternion initBlUp, initBlLo;
    private Quaternion initBrUp, initBrLo;

    private Vector3 lastPos;

    void Awake()
    {
        animalCtrl = GetComponent<AnimalController>();
        if (!animalCtrl) animalCtrl = GetComponentInParent<AnimalController>();

        if (rootBone) initialRootPos = rootBone.localPosition;
        if (spineBone) initSpineRot = spineBone.localRotation;
        if (neckBone) initNeckRot = neckBone.localRotation;
        if (headBone) initHeadRot = headBone.localRotation;
        if (tail1Bone) initTail1Rot = tail1Bone.localRotation;
        if (tail2Bone) initTail2Rot = tail2Bone.localRotation;

        if (flUp) initFlUp = flUp.localRotation;
        if (flLo) initFlLo = flLo.localRotation;
        if (frUp) initFrUp = frUp.localRotation;
        if (frLo) initFrLo = frLo.localRotation;

        if (blUp) initBlUp = blUp.localRotation;
        if (blLo) initBlLo = blLo.localRotation;
        if (brUp) initBrUp = brUp.localRotation;
        if (brLo) initBrLo = brLo.localRotation;

        lastPos = transform.position;
    }

    void Update()
    {
        float speed = 0f;
        if (animalCtrl != null)
        {
            speed = animalCtrl.CurrentSpeed;
        }
        else
        {
            speed = (transform.position - lastPos).magnitude / Mathf.Max(Time.deltaTime, 0.001f);
            lastPos = transform.position;
        }

        bool isWalking = speed > walkSpeedThreshold;
        float TAU = Mathf.PI * 2f;

        if (isWalking)
        {
            // Sinkronkan frekuensi langkah dengan kecepatan jalan nyata
            float speedFactor = Mathf.Clamp(speed / 1.75f, 0.6f, 1.8f);
            walkTime += Time.deltaTime * speedFactor;

            float t = (walkTime % cycleDuration) / cycleDuration;

            // Algoritma fase langkah dari dog_walk.py:
            // PHASE: BL = 0.0, FL = 0.25, BR = 0.5, FR = 0.75
            // SWING: F = 26 deg, B = 22 deg
            // FLEX: F = 55 deg, B = 45 deg
            AnimateLeg(blUp, blLo, initBlUp, initBlLo, t, 0.0f,  22f, 45f, TAU);
            AnimateLeg(flUp, flLo, initFlUp, initFlLo, t, 0.25f, 26f, 55f, TAU);
            AnimateLeg(brUp, brLo, initBrUp, initBrLo, t, 0.5f,  22f, 45f, TAU);
            AnimateLeg(frUp, frLo, initFrUp, initFrLo, t, 0.75f, 26f, 55f, TAU);

            // Pantulan vertikal badan (Root Bounce: 0.012 * cos(2 * TAU * t))
            if (rootBone)
            {
                rootBone.localPosition = initialRootPos + new Vector3(0, 0.015f * Mathf.Cos(TAU * 2f * t), 0);
            }

            // Goyangan pinggang (Spine Yaw: 2.5 deg * sin(TAU * t))
            if (spineBone)
            {
                spineBone.localRotation = initSpineRot * Quaternion.Euler(0, 3.2f * Mathf.Sin(TAU * t), 0);
            }

            // Anggukan leher & kepala berirama (Neck & Head Bob)
            if (neckBone)
            {
                neckBone.localRotation = initNeckRot * Quaternion.Euler(-3.0f * Mathf.Sin(TAU * 2f * t + 0.6f), 0, 0);
            }
            if (headBone)
            {
                headBone.localRotation = initHeadRot * Quaternion.Euler(3.0f * Mathf.Sin(TAU * 2f * t + 0.6f), 0, 0);
            }

            // Kibasan ekor riang saat berjalan (Tail Wag: 22 deg)
            if (tail1Bone)
            {
                tail1Bone.localRotation = initTail1Rot * Quaternion.Euler(0, 22f * Mathf.Sin(TAU * 2f * t), 0);
            }
            if (tail2Bone)
            {
                tail2Bone.localRotation = initTail2Rot * Quaternion.Euler(0, 18f * Mathf.Sin(TAU * 2f * t - 0.8f), 0);
            }
        }
        else
        {
            // Transisi halus kembali ke posisi berdiri alami saat diam (Idle Stance)
            float lerpSpeed = Time.deltaTime * 6f;

            if (flUp) flUp.localRotation = Quaternion.Slerp(flUp.localRotation, initFlUp, lerpSpeed);
            if (flLo) flLo.localRotation = Quaternion.Slerp(flLo.localRotation, initFlLo, lerpSpeed);
            if (frUp) frUp.localRotation = Quaternion.Slerp(frUp.localRotation, initFrUp, lerpSpeed);
            if (frLo) frLo.localRotation = Quaternion.Slerp(frLo.localRotation, initFrLo, lerpSpeed);
            if (blUp) blUp.localRotation = Quaternion.Slerp(blUp.localRotation, initBlUp, lerpSpeed);
            if (blLo) blLo.localRotation = Quaternion.Slerp(blLo.localRotation, initBlLo, lerpSpeed);
            if (brUp) brUp.localRotation = Quaternion.Slerp(brUp.localRotation, initBrUp, lerpSpeed);
            if (brLo) brLo.localRotation = Quaternion.Slerp(brLo.localRotation, initBrLo, lerpSpeed);

            if (rootBone) rootBone.localPosition = Vector3.Lerp(rootBone.localPosition, initialRootPos, lerpSpeed);
            if (spineBone) spineBone.localRotation = Quaternion.Slerp(spineBone.localRotation, initSpineRot, lerpSpeed);
            if (neckBone) neckBone.localRotation = Quaternion.Slerp(neckBone.localRotation, initNeckRot, lerpSpeed);
            if (headBone) headBone.localRotation = Quaternion.Slerp(headBone.localRotation, initHeadRot, lerpSpeed);

            // Kibasan ekor ramah saat diam
            if (tail1Bone)
            {
                tail1Bone.localRotation = initTail1Rot * Quaternion.Euler(0, 14f * Mathf.Sin(Time.time * 3.2f), 0);
            }
            if (tail2Bone)
            {
                tail2Bone.localRotation = initTail2Rot * Quaternion.Euler(0, 10f * Mathf.Sin(Time.time * 3.2f - 0.6f), 0);
            }
        }
    }

    private void AnimateLeg(Transform up, Transform lo, Quaternion initUp, Quaternion initLo,
                            float t, float phase, float swingDeg, float flexDeg, float TAU)
    {
        if (!up || !lo) return;

        float p = TAU * (t - phase);
        // Di Unity (sumbu +Z adalah arah maju, kaki menjuntai ke bawah pada sumbu -Y):
        // Rotasi pitch negatif (-X) mengayunkan kaki maju ke depan (+Z).
        // Pada fase ayun maju (sin(p) > 0), kaki melangkah maju ke depan.
        float swing = -swingDeg * Mathf.Sin(p);

        // Pada fase layang di udara (cos(p) > 0), lutut / sendi tarsus menekuk ke belakang (-Z)
        // dan mengangkat telapak kaki agar tidak terseret tanah.
        float lift = Mathf.Max(0f, Mathf.Cos(p));
        float flex = flexDeg * lift * lift;

        up.localRotation = initUp * Quaternion.Euler(swing, 0f, 0f);
        lo.localRotation = initLo * Quaternion.Euler(flex, 0f, 0f);
    }
}

