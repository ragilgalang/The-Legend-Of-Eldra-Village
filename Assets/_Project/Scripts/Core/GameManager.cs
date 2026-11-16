using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool BossDefeated { get; set; }
    public bool BossEncounterActive { get; private set; }

    [Header("Referensi")]
    [SerializeField] PlayerController player;
    [SerializeField] Health playerHealth;
    [SerializeField] GameObject gameOverPanel, pausePanel, endingPanel;

    bool paused, over, levelComplete;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
        BossDefeated = false;
        BossEncounterActive = false;
    }

    void Start()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        if (pausePanel) pausePanel.SetActive(false);
        if (endingPanel) endingPanel.SetActive(false);

        if (!player) player = FindAnyObjectByType<PlayerController>();
        if (!playerHealth && player) playerHealth = player.GetComponent<Health>();
        if (playerHealth) playerHealth.OnDied += HandlePlayerDied;

        StartCoroutine(PlayOpening());
    }

    void OnDestroy()
    {
        if (playerHealth) playerHealth.OnDied -= HandlePlayerDied;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (over || levelComplete) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SetPaused(!paused);
    }

    IEnumerator PlayOpening()
    {
        float timeout = 5f;
        while (timeout > 0f && (!QuestManager.Instance || QuestManager.Instance.Current == null))
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!QuestManager.Instance || QuestManager.Instance.Current == null)
            yield break;

        const string openingText =
            "Fajar menyingsing di Desa Eldara. Jejak goblin terlihat dari arah Rawa Selatan. " +
            "Temui Kepala Desa di balai untuk mengetahui ancaman yang mendekat.";

        if (DialogueManager.Instance)
        {
            var opening = ScriptableObject.CreateInstance<DialogueData>();
            opening.name = "LevelOneOpening";
            opening.lines = new[]
            {
                new DialogueData.Line { speaker = "Narator", text = openingText }
            };
            DialogueManager.Instance.Play(opening, () => Destroy(opening));
        }
        else if (HintManager.Instance)
        {
            HintManager.Instance.Show("level1_opening", openingText);
        }
    }

    public bool StartBossEncounter()
    {
        if (BossDefeated) return false;
        if (BossEncounterActive) return true;

        var existingBoss = FindAnyObjectByType<LevelOneBossController>();
        if (existingBoss)
        {
            BossEncounterActive = true;
            return true;
        }

        var marker = GameObject.Find("BossSpawnPoint");
        var spawnPosition = marker ? marker.transform.position : new Vector3(0f, 0f, 80f);

        var cycle = DayNightCycle.Instance;
        if (cycle) cycle.SetTime(19.5f, cycle.DayCount);
        if (MusicManager.Instance) MusicManager.Instance.SetBoss(true);

        LevelOneBossController.Spawn(spawnPosition);
        BossEncounterActive = true;
        return true;
    }

    public void NotifyBossDefeated()
    {
        BossDefeated = true;
        BossEncounterActive = false;
        if (MusicManager.Instance) MusicManager.Instance.SetBoss(false);
    }

    public void SetPaused(bool value)
    {
        if (over || levelComplete) return;
        paused = value;
        Time.timeScale = paused ? 0f : 1f;
        if (pausePanel) pausePanel.SetActive(paused);
        UnlockCursor(paused);
    }

    public void TriggerEnding()
    {
        if (levelComplete) return;

        levelComplete = true;
        if (MusicManager.Instance) MusicManager.Instance.SetBoss(false);
        if (player) player.inputLocked = true;
        EnsureEndingPanel();
        if (endingPanel) endingPanel.SetActive(true);
        Time.timeScale = 0f;
        UnlockCursor(true);
    }

    void EnsureEndingPanel()
    {
        if (endingPanel) return;

        Canvas canvas = null;
        foreach (var candidate in FindObjectsByType<Canvas>())
        {
            if (candidate.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = candidate;
                break;
            }
        }

        if (!canvas)
        {
            var canvasObject = new GameObject("LevelOneEndingCanvas");
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        var panelObject = new GameObject("LevelOneEndingPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(canvas.transform, false);
        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelObject.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);

        var messageObject = new GameObject("EndingMessage", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        messageObject.transform.SetParent(panelObject.transform, false);
        var messageRect = messageObject.GetComponent<RectTransform>();
        messageRect.anchorMin = new Vector2(0.1f, 0.48f);
        messageRect.anchorMax = new Vector2(0.9f, 0.88f);
        messageRect.offsetMin = Vector2.zero;
        messageRect.offsetMax = Vector2.zero;
        var message = messageObject.GetComponent<TextMeshProUGUI>();
        message.text = "LEVEL 1 SELESAI\nSegel Eldara telah tertutup. Desa aman untuk sementara.";
        message.fontSize = 34f;
        message.color = Color.white;
        message.alignment = TextAlignmentOptions.Center;

        var buttonObject = new GameObject("RetryButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panelObject.transform, false);
        var buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 0.34f);
        buttonRect.sizeDelta = new Vector2(260f, 68f);
        var button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(Retry);
        buttonObject.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.28f, 1f);

        var buttonTextObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        buttonTextObject.transform.SetParent(buttonObject.transform, false);
        var buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        var buttonText = buttonTextObject.GetComponent<TextMeshProUGUI>();
        buttonText.text = "Main Lagi";
        buttonText.fontSize = 24f;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        endingPanel = panelObject;
    }

    void HandlePlayerDied(Health _) => GameOver();

    void GameOver()
    {
        over = true;
        if (player) player.inputLocked = true;
        if (gameOverPanel) gameOverPanel.SetActive(true);
        UnlockCursor(true);
        Time.timeScale = 0f;
    }

    void UnlockCursor(bool unlock)
    {
        Cursor.lockState = unlock ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = unlock;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Application.Quit();
    }
}
