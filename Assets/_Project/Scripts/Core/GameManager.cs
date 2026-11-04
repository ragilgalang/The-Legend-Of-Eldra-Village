using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public bool BossDefeated { get; set; }

    [Header("Referensi")]
    [SerializeField] PlayerController player;
    [SerializeField] Health playerHealth;
    [SerializeField] GameObject gameOverPanel, pausePanel, endingPanel;

    [Header("Malam Terakhir")]
    [SerializeField] string finalQuestId = "pecahan_batu";
    [SerializeField] GameObject bossPrefab;
    [SerializeField] Transform bossSpawn;

    bool paused, over;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void Start()
    {
        gameOverPanel.SetActive(false);
        pausePanel.SetActive(false);
        endingPanel.SetActive(false);

        playerHealth.OnDied += _ => GameOver();
        QuestManager.Instance.OnQuestCompleted += q =>
        {
            if (q.questId == finalQuestId) StartFinalNight();
        };
    }

    void Update()
    {
        if (over) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SetPaused(!paused);
    }

    public void SetPaused(bool value)
    {
        paused = value;
        Time.timeScale = paused ? 0f : 1f;
        pausePanel.SetActive(paused);
        UnlockCursor(paused);
    }

    void StartFinalNight()
    {
        var c = DayNightCycle.Instance;
        c.SetTime(19.5f, c.DayCount);           // paksa malam
        if (bossPrefab) Instantiate(bossPrefab, bossSpawn.position, bossSpawn.rotation);
        if (MusicManager.Instance != null) MusicManager.Instance.SetBoss(true);
    }

    public void TriggerEnding()
    {
        if (MusicManager.Instance != null) MusicManager.Instance.SetBoss(false);
        player.inputLocked = true;
        endingPanel.SetActive(true);
        UnlockCursor(true);
    }

    void GameOver()
    {
        over = true;
        player.inputLocked = true;
        gameOverPanel.SetActive(true);
        UnlockCursor(true);
        Time.timeScale = 0f;
    }

    void UnlockCursor(bool unlock)
    {
        Cursor.lockState = unlock ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = unlock;
    }

    // Dipanggil dari OnClick tombol UI
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

