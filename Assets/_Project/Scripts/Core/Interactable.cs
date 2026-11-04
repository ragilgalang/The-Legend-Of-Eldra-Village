using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
    public string prompt = "Tekan E";
    public abstract void Interact(PlayerController player);
}

