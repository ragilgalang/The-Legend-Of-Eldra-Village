using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text speakerText, bodyText;
    [SerializeField] Button[] choiceButtons;
    [SerializeField] PlayerController player;
    [SerializeField] float charDelay = 0.02f;

    DialogueData current;
    int index, startFrame;
    bool active, typing, choicesShown;
    Action onFinish;
    Coroutine typeRoutine;

    public bool IsActive => active;

    void Awake()
    {
        Instance = this;
        if (panel)
        {
            foreach (var text in panel.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!speakerText && text.name == "SpeakerName") speakerText = text;
                else if (!bodyText && text.name == "DialogueText") bodyText = text;
            }
        }
        if (panel) panel.SetActive(false);
        HideChoices();
    }

    public void Play(DialogueData data, Action finished = null)
    {
        if (active || data == null) return;
        if (!player) player = FindFirstObjectByType<PlayerController>();
        onFinish = finished;
        active = true;
        if (player) player.inputLocked = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (panel) panel.SetActive(true);
        Begin(data);
    }

    void Begin(DialogueData data)
    {
        current = data;
        index = 0;
        startFrame = Time.frameCount;   // cegah klik yang sama langsung melewati baris
        HideChoices();
        ShowLine();
    }

    void ShowLine()
    {
        var line = current.lines[index];
        speakerText.text = line.speaker;
        if (typeRoutine != null) StopCoroutine(typeRoutine);
        typeRoutine = StartCoroutine(Type(line.text));
    }

    IEnumerator Type(string text)
    {
        typing = true;
        bodyText.text = text;
        for (int i = 0; i <= text.Length; i++)
        {
            bodyText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(charDelay);
        }
        typing = false;
    }

    void Update()
    {
        if (!active || choicesShown || Time.frameCount == startFrame) return;

        var kb = Keyboard.current;
        var ms = Mouse.current;
        bool advance = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame))
                    || (ms != null && ms.leftButton.wasPressedThisFrame);
        if (!advance) return;

        if (typing)
        {
            StopCoroutine(typeRoutine);
            bodyText.maxVisibleCharacters = int.MaxValue;
            typing = false;
            return;
        }

        index++;
        if (index < current.lines.Length) { ShowLine(); return; }
        if (current.choices != null && current.choices.Length > 0) { ShowChoices(); return; }
        End();
    }

    void ShowChoices()
    {
        choicesShown = true;
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool has = i < current.choices.Length;
            choiceButtons[i].gameObject.SetActive(has);
            if (!has) continue;

            var c = current.choices[i];
            choiceButtons[i].GetComponentInChildren<TMP_Text>().text = c.text;
            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() =>
            {
                HideChoices();
                if (c.next) Begin(c.next); else End();
            });
        }
    }

    void HideChoices()
    {
        choicesShown = false;
        if (choiceButtons == null) return;
        foreach (var b in choiceButtons) if (b) b.gameObject.SetActive(false);
    }

    void End()
    {
        active = false;
        if (panel) panel.SetActive(false);
        if (player) player.inputLocked = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        var cb = onFinish;
        onFinish = null;
        cb?.Invoke();
    }
}

