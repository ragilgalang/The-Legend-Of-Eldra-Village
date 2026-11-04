using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public class BuildItem
{
    public string name = "Menara Jaga";
    public GameObject prefab;
    public string costId = "iron_ore";
    public int cost = 3;
}

public class BuildMode : MonoBehaviour
{
    public static bool Active { get; private set; }

    [SerializeField] BuildItem[] items;
    [SerializeField] LayerMask groundMask;
    [SerializeField] LayerMask blockMask;        // bangunan, pagar, rumah, pohon
    [SerializeField] float maxDistance = 12f;
    [SerializeField] Color okColor = new Color(0.2f, 1f, 0.2f, 0.5f);
    [SerializeField] Color badColor = new Color(1f, 0.2f, 0.2f, 0.5f);

    PlayerController player;
    GameObject ghost;
    Renderer[] ghostRenderers;
    int index;
    float yRotation;
    bool canPlace;

    void Awake() => player = GetComponent<PlayerController>();

    void OnDisable() { SetActive(false); }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || player.inputLocked) return;

        if (kb.bKey.wasPressedThisFrame) SetActive(!Active);
        if (!Active) return;

        if (kb.digit1Key.wasPressedThisFrame) Select(0);
        if (kb.digit2Key.wasPressedThisFrame) Select(1);
        if (kb.digit3Key.wasPressedThisFrame) Select(2);
        if (kb.rKey.wasPressedThisFrame) yRotation += 45f;

        UpdateGhost();

        if (canPlace && Mouse.current.leftButton.wasPressedThisFrame) Place();
    }

    void SetActive(bool on)
    {
        Active = on;
        if (on) Select(index);
        else if (ghost) { Destroy(ghost); ghost = null; }
    }

    void Select(int i)
    {
        if (items == null || i >= items.Length || !items[i].prefab) return;
        index = i;
        if (ghost) Destroy(ghost);

        ghost = Instantiate(items[i].prefab);
        // Bersihkan semua logika agar hanya jadi pratinjau
        foreach (var mb in ghost.GetComponentsInChildren<MonoBehaviour>()) Destroy(mb);
        foreach (var c in ghost.GetComponentsInChildren<Collider>()) Destroy(c);
        foreach (var a in ghost.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>()) Destroy(a);
        ghostRenderers = ghost.GetComponentsInChildren<Renderer>();
    }

    void UpdateGhost()
    {
        if (!ghost) return;

        var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, groundMask))
        {
            ghost.SetActive(false);
            canPlace = false;
            return;
        }

        ghost.SetActive(true);
        ghost.transform.SetPositionAndRotation(hit.point, Quaternion.Euler(0, yRotation, 0));

        bool free = !HitsSomething();
        bool afford = Inventory.Instance.Has(items[index].costId, items[index].cost);
        canPlace = free && afford;

        foreach (var r in ghostRenderers)
            foreach (var m in r.materials) m.color = canPlace ? okColor : badColor;
    }

    bool HitsSomething()
    {
        Bounds b = ghostRenderers[0].bounds;
        foreach (var r in ghostRenderers) b.Encapsulate(r.bounds);
        b.Expand(-0.1f);
        return Physics.CheckBox(b.center, b.extents, ghost.transform.rotation,
                                blockMask, QueryTriggerInteraction.Ignore);
    }

    void Place()
    {
        var it = items[index];
        if (!Inventory.Instance.Remove(it.costId, it.cost)) return;
        Instantiate(it.prefab, ghost.transform.position, ghost.transform.rotation);
    }
}

