using System.Collections.Generic;
using UnityEngine;

public class LevelOneBossController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 2.8f;
    [SerializeField] float attackRange = 2.2f;
    [SerializeField] float attackDamage = 12f;
    [SerializeField] float attackInterval = 1.7f;

    readonly List<Material> runtimeMaterials = new();
    Health health;
    Transform target;
    float nextAttackTime;
    bool defeated;

    public static LevelOneBossController Spawn(Vector3 position)
    {
        var bossObject = new GameObject("LevelOne_RajaGoblin");
        bossObject.transform.position = position;

        var collider = bossObject.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.1f, 0f);
        collider.height = 2.4f;
        collider.radius = 0.65f;

        bossObject.AddComponent<Health>();
        var boss = bossObject.AddComponent<LevelOneBossController>();
        boss.BuildVisual();
        return boss;
    }

    void Awake()
    {
        health = GetComponent<Health>();
        if (health) health.OnDied += HandleDefeated;
    }

    void Start()
    {
        var player = FindAnyObjectByType<PlayerController>();
        if (player)
        {
            target = player.transform;
            if (!player.GetComponent<Health>())
                Debug.LogError("Pemain tidak memiliki komponen Health; Raja Goblin tidak dapat menyerang.");
        }
        else
        {
            Debug.LogError("Pemain tidak ditemukan; Raja Goblin tidak dapat memulai pertarungan.");
        }
    }

    void OnDestroy()
    {
        if (health) health.OnDied -= HandleDefeated;
        foreach (var material in runtimeMaterials)
            if (material) Destroy(material);
    }

    void Update()
    {
        if (defeated || !target) return;

        var direction = target.position - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;
        if (distance > attackRange)
        {
            direction.Normalize();
            transform.position += direction * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(direction);
            return;
        }

        if (distance > 0.01f)
            transform.rotation = Quaternion.LookRotation(direction.normalized);

        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackInterval;

        var playerHealth = target.GetComponent<Health>();
        if (playerHealth) playerHealth.Damage(attackDamage);
    }

    void HandleDefeated(Health _)
    {
        if (defeated) return;
        defeated = true;

        if (GameManager.Instance) GameManager.Instance.NotifyBossDefeated();
        if (Inventory.Instance)
        {
            Inventory.Instance.Add("shard_mine");
            Inventory.Instance.Add("shard_lake");
            Inventory.Instance.Add("shard_ruins");
        }
        else
        {
            Debug.LogError("Inventory tidak ditemukan; pecahan segel Raja Goblin tidak dapat diberikan.");
        }

        if (QuestManager.Instance)
            QuestManager.Instance.Report(QuestObjective.Type.Kill, "raja_goblin");
        else
            Debug.LogError("QuestManager tidak ditemukan; objective mengalahkan Raja Goblin tidak dapat diperbarui.");

        if (HintManager.Instance)
            HintManager.Instance.Show("level1_shards", "Raja Goblin menjatuhkan tiga pecahan segel. Kembali ke altar!");

        Destroy(gameObject, 3f);
    }

    void BuildVisual()
    {
        var visual = new GameObject("Visual");
        visual.transform.SetParent(transform, false);

        var body = CreateMaterial(new Color(0.25f, 0.38f, 0.19f));
        var armor = CreateMaterial(new Color(0.19f, 0.16f, 0.14f));
        var horn = CreateMaterial(new Color(0.72f, 0.59f, 0.37f));
        var eye = CreateMaterial(new Color(0.9f, 0.16f, 0.04f));

        CreatePart(visual.transform, PrimitiveType.Capsule, "Body", new Vector3(0f, 1.1f, 0f),
            new Vector3(0.95f, 0.9f, 0.78f), body);
        CreatePart(visual.transform, PrimitiveType.Sphere, "Head", new Vector3(0f, 2.05f, 0f),
            new Vector3(0.82f, 0.72f, 0.75f), body);
        CreatePart(visual.transform, PrimitiveType.Cube, "ChestArmor", new Vector3(0f, 1.35f, 0.38f),
            new Vector3(0.82f, 0.62f, 0.16f), armor);
        CreatePart(visual.transform, PrimitiveType.Cylinder, "LeftHorn", new Vector3(-0.3f, 2.48f, 0f),
            new Vector3(0.16f, 0.32f, 0.16f), horn).localRotation = Quaternion.Euler(0f, 0f, -24f);
        CreatePart(visual.transform, PrimitiveType.Cylinder, "RightHorn", new Vector3(0.3f, 2.48f, 0f),
            new Vector3(0.16f, 0.32f, 0.16f), horn).localRotation = Quaternion.Euler(0f, 0f, 24f);
        CreatePart(visual.transform, PrimitiveType.Sphere, "LeftEye", new Vector3(-0.18f, 2.1f, 0.34f),
            new Vector3(0.1f, 0.1f, 0.06f), eye);
        CreatePart(visual.transform, PrimitiveType.Sphere, "RightEye", new Vector3(0.18f, 2.1f, 0.34f),
            new Vector3(0.1f, 0.1f, 0.06f), eye);
        CreatePart(visual.transform, PrimitiveType.Cube, "Club", new Vector3(0.74f, 1.08f, 0.08f),
            new Vector3(0.22f, 0.9f, 0.22f), armor).localRotation = Quaternion.Euler(0f, 0f, -18f);
    }

    Material CreateMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        if (!shader)
        {
            Debug.LogError("Shader untuk visual Raja Goblin tidak ditemukan.");
            return null;
        }

        var material = new Material(shader) { color = color };
        runtimeMaterials.Add(material);
        return material;
    }

    static Transform CreatePart(Transform parent, PrimitiveType shape, string partName,
        Vector3 position, Vector3 scale, Material material)
    {
        var part = GameObject.CreatePrimitive(shape);
        part.name = partName;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        var collider = part.GetComponent<Collider>();
        if (collider) Destroy(collider);
        var renderer = part.GetComponent<Renderer>();
        if (renderer && material) renderer.sharedMaterial = material;
        return part.transform;
    }
}
