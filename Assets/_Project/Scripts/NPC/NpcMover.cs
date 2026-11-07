using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pengendali Pergerakan & Animasi Prosedural NPC Stylized.
/// Mengatur:
/// 1. Navigasi mandiri menyusuri waypoints patroli di desa (autonomous wander/patrol).
/// 2. Animasi langkah kaki dan ayunan tangan prosedural (walking cycle swing & bounce).
/// 3. Animasi bernapas saat diam (idle breathing bobbing & head glances).
/// 4. Reaksi ramah saat pemain mendekat (berhenti dan menatap pemain).
/// 5. Penguncian pijakan tanah (anti-melayang mutlak via ground raycasting).
/// </summary>
public class NpcMover : MonoBehaviour
{
    [Header("Titik Rute Patroli (Waypoints)")]
    public List<Vector3> waypoints = new List<Vector3>();
    public float moveSpeed = 1.5f;
    public float turnSpeed = 6f;
    public float stopDistance = 0.6f;
    public float waitTimeAtWaypoint = 4f;

    [Header("Deteksi & Interaksi Pemain")]
    public float playerDetectRadius = 3.5f;
    public bool pauseWhenPlayerNear = true;

    [Header("Hierarki Sendi Animasi Prosedural")]
    public Transform visualRoot;
    public Transform torsoTransform;
    public Transform headTransform;
    public Transform leftLeg;
    public Transform rightLeg;
    public Transform leftArm;
    public Transform rightArm;

    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;
    private bool isPlayerNear = false;
    private Transform playerTransform;

    private Vector3 baseTorsoLocalPos;
    private float walkCycle = 0f;
    private float idleCycle = 0f;

    void Start()
    {
        var playerObj = GameObject.FindWithTag("Player");
        if (playerObj) playerTransform = playerObj.transform;

        if (torsoTransform)
            baseTorsoLocalPos = torsoTransform.localPosition;

        // Jika tidak ada waypoints, buat waypoint di sekitar posisi awal
        if (waypoints == null || waypoints.Count == 0)
        {
            waypoints = new List<Vector3>
            {
                transform.position,
                transform.position + transform.forward * 4f,
                transform.position + transform.right * 3f,
                transform.position - transform.forward * 3f
            };
        }

        ClampToGround();
    }

    void Update()
    {
        CheckPlayerProximity();

        if (isPlayerNear && pauseWhenPlayerNear)
        {
            FacePlayer();
            AnimateIdle(true);
            return;
        }

        if (waypoints.Count == 0)
        {
            AnimateIdle(false);
            return;
        }

        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            AnimateIdle(false);

            if (waitTimer <= 0f)
            {
                isWaiting = false;
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            }
            return;
        }

        // Bergerak menuju waypoint aktif
        Vector3 targetPos = waypoints[currentWaypointIndex];
        Vector3 moveOffset = targetPos - transform.position;
        moveOffset.y = 0; // Abaikan perbedaan ketinggian untuk kalkulasi horizontal

        float dist = moveOffset.magnitude;
        if (dist <= stopDistance)
        {
            isWaiting = true;
            waitTimer = waitTimeAtWaypoint + Random.Range(-1f, 1.5f);
            AnimateIdle(false);
            return;
        }

        // Putar menghadap arah gerak
        Vector3 moveDir = moveOffset.normalized;
        Quaternion targetRot = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);

        // Langkah maju
        transform.position += moveDir * (moveSpeed * Time.deltaTime);

        ClampToGround();
        AnimateWalk();
    }

    void CheckPlayerProximity()
    {
        if (!playerTransform) return;
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        isPlayerNear = (dist <= playerDetectRadius);
    }

    void FacePlayer()
    {
        if (!playerTransform) return;
        Vector3 lookDir = playerTransform.position - transform.position;
        lookDir.y = 0;
        if (lookDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        if (headTransform)
        {
            // Tatap pemain dengan sedikit anggukan hangat
            headTransform.localRotation = Quaternion.Slerp(
                headTransform.localRotation,
                Quaternion.Euler(4f, 0, 0),
                Time.deltaTime * 4f
            );
        }
    }

    void AnimateWalk()
    {
        walkCycle += Time.deltaTime * (moveSpeed * 4.2f);

        float legAngle = Mathf.Sin(walkCycle) * 28f;
        float armAngle = -legAngle * 0.75f;

        if (leftLeg)  leftLeg.localRotation  = Quaternion.Euler(legAngle, 0, 0);
        if (rightLeg) rightLeg.localRotation = Quaternion.Euler(-legAngle, 0, 0);

        if (leftArm)  leftArm.localRotation  = Quaternion.Euler(armAngle, 0, 0);
        if (rightArm) rightArm.localRotation = Quaternion.Euler(-armAngle, 0, 0);

        // Langkah naik-turun tubuh realistis (hip bobbing)
        if (torsoTransform)
        {
            float bob = Mathf.Abs(Mathf.Sin(walkCycle)) * 0.04f;
            torsoTransform.localPosition = baseTorsoLocalPos + Vector3.up * bob;
            torsoTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(walkCycle) * 2.2f);
        }

        if (headTransform)
        {
            headTransform.localRotation = Quaternion.Euler(Mathf.Sin(walkCycle * 2f) * 2f, 0, 0);
        }
    }

    void AnimateIdle(bool attentiveToPlayer)
    {
        idleCycle += Time.deltaTime * 2.0f;

        // Bernapas lembut
        float breathe = Mathf.Sin(idleCycle) * 0.015f;
        if (torsoTransform)
        {
            torsoTransform.localPosition = Vector3.Lerp(
                torsoTransform.localPosition,
                baseTorsoLocalPos + Vector3.up * breathe,
                Time.deltaTime * 5f
            );
            torsoTransform.localRotation = Quaternion.Slerp(torsoTransform.localRotation, Quaternion.identity, Time.deltaTime * 4f);
        }

        // Kaki kembali tegak rileks
        if (leftLeg)  leftLeg.localRotation  = Quaternion.Slerp(leftLeg.localRotation, Quaternion.identity, Time.deltaTime * 6f);
        if (rightLeg) rightLeg.localRotation = Quaternion.Slerp(rightLeg.localRotation, Quaternion.identity, Time.deltaTime * 6f);

        // Tangan beristirahat rileks di samping tubuh
        if (leftArm)  leftArm.localRotation  = Quaternion.Slerp(leftArm.localRotation, Quaternion.identity, Time.deltaTime * 6f);
        if (rightArm) rightArm.localRotation = Quaternion.Slerp(rightArm.localRotation, Quaternion.identity, Time.deltaTime * 6f);

        // Menoleh santai jika tidak sedang menatap pemain
        if (headTransform && !attentiveToPlayer)
        {
            float glance = Mathf.Sin(idleCycle * 0.45f) * 14f;
            headTransform.localRotation = Quaternion.Euler(0, glance, 0);
        }
    }

    void ClampToGround()
    {
        // Pastikan NPC tidak pernah melayang dan berdiri kokoh di atas tanah.
        // Abaikan collider milik NPC itu sendiri agar tidak terangkat/melayang.
        RaycastHit[] hits = Physics.RaycastAll(transform.position + Vector3.up * 2f, Vector3.down, 6f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(transform) || hit.transform == transform)
                continue;

            Vector3 p = transform.position;
            p.y = hit.point.y;
            transform.position = p;
            return;
        }
    }

    public void SetWaypoints(List<Vector3> newWaypoints)
    {
        waypoints = new List<Vector3>(newWaypoints);
        currentWaypointIndex = 0;
        isWaiting = false;
    }
}

