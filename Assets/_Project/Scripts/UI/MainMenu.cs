using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] string gameScene = "Village";
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
        SceneManager.LoadScene(gameScene);
    }

    public void Continue()
    {
        PlayerPrefs.SetInt("LoadSave", 1);   // dibaca SaveSystem saat scene mulai
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

