using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] string gameScene = "SampleScene";
    [SerializeField] GameObject settingsPanel;

    void Start()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void NewGame()
    {
        PlayerPrefs.SetInt("LoadSave", 0);
        LoadGameScene();
    }

    public void Continue()
    {
        PlayerPrefs.SetInt("LoadSave", 1);   // dibaca SaveSystem saat scene mulai
        LoadGameScene();
    }

    void LoadGameScene()
    {
        if (string.IsNullOrEmpty(gameScene) || !Application.CanStreamedLevelBeLoaded(gameScene))
        {
            Debug.LogWarning("Scene '" + gameScene + "' tidak ditemukan. Memuat scene aktif/preset default.");
            if (SceneManager.sceneCountInBuildSettings > 0)
            {
                SceneManager.LoadScene(0);
                return;
            }
        }

        SceneManager.LoadScene(gameScene);
    }

    public void OpenSettings(bool open) => settingsPanel.SetActive(open);

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

