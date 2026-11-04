using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    public static HintManager Instance { get; private set; }

    [SerializeField] TMP_Text hintText;
    [SerializeField] float showTime = 4f;

    readonly HashSet<string> shown = new();
    Coroutine routine;

    void Awake() { Instance = this; hintText.gameObject.SetActive(false); }

    void Start()
    {
        Show("move", "WASD untuk bergerak, Shift untuk berlari, V untuk mengganti kamera.");
        DayNightCycle.Instance.OnPhaseChanged += p =>
        {
            if (p == TimePhase.Dusk)  Show("dusk",  "Senja tiba. Kembali ke desa dan siapkan pertahanan!");
            if (p == TimePhase.Night) Show("night", "Malam hari: goblin dan serigala turun. Klik kiri untuk menyerang.");
            if (p == TimePhase.Dawn)  Show("dawn",  "Fajar. Gunakan siang untuk membangun (B) dan menyelesaikan quest.");
        };
    }

    public void Show(string id, string message)
    {
        if (!shown.Add(id)) return;      // tiap petunjuk hanya sekali
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(Do(message));
    }

    IEnumerator Do(string msg)
    {
        hintText.text = msg;
        hintText.gameObject.SetActive(true);
        yield return new WaitForSeconds(showTime);
        hintText.gameObject.SetActive(false);
    }
}

