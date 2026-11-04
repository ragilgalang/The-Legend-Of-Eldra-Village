using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [SerializeField] float radius = 2.5f;
    [SerializeField] TMP_Text promptText;

    PlayerController player;
    Interactable focus;

    void Awake() => player = GetComponent<PlayerController>();

    void Update()
    {
        if (player != null && player.inputLocked) { SetFocus(null); return; }

        Interactable best = null;
        float bestDist = float.MaxValue;
        foreach (var c in Physics.OverlapSphere(transform.position, radius))
        {
            var it = c.GetComponentInParent<Interactable>();
            if (!it || !it.isActiveAndEnabled) continue;
            float d = (it.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { best = it; bestDist = d; }
        }
        SetFocus(best);

        if (focus && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            focus.Interact(player);
    }

    void SetFocus(Interactable it)
    {
        focus = it;
        if (promptText)
        {
            promptText.gameObject.SetActive(it != null);
            if (it) promptText.text = it.prompt;
        }
    }
}

