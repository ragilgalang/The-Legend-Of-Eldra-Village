#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

namespace Eldara.EditorTools
{
    public static class MainMenuSceneTool
    {
        public const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        public const string GameScenePath     = "Assets/Scenes/SampleScene.unity";

        public static void OpenMainMenuScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Peringatan", "Hentikan Play Mode terlebih dahulu sebelum berpindah scene.", "OK");
                return;
            }

            if (!File.Exists(MainMenuScenePath))
            {
                RebuildMainMenuScene();
            }
            else
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(MainMenuScenePath);
                    EnsureBuildSettingsConfigured();
                    Debug.Log("<color=green>[Eldara] Scene MainMenu berhasil dibuka. Tekan Play untuk mencoba!</color>");
                }
            }
        }

        public static void OpenGameScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Peringatan", "Hentikan Play Mode terlebih dahulu sebelum berpindah scene.", "OK");
                return;
            }

            if (File.Exists(GameScenePath))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(GameScenePath);
                    Debug.Log("<color=cyan>[Eldara] Kembali ke SampleScene (Game World).</color>");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Error", $"File scene game tidak ditemukan di: {GameScenePath}", "OK");
            }
        }

        public static void RebuildMainMenuScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Peringatan", "Hentikan Play Mode terlebih dahulu sebelum membuat scene.", "OK");
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            string dir = Path.GetDirectoryName(MainMenuScenePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var manager = GameObject.Find("MainMenuManager");
            if (!manager) manager = new GameObject("MainMenuManager");

            var menu = manager.GetComponent<EldaraMainMenu>();
            if (!menu) menu = manager.AddComponent<EldaraMainMenu>();
            menu.gameSceneName = "SampleScene";

            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            EnsureBuildSettingsConfigured();

            AssetDatabase.Refresh();
            Debug.Log("<color=green>[Eldara] Scene MainMenu siap dan berhasil dikonfigurasi! Tekan Play untuk mencoba.</color>");
        }

        public static void EnsureBuildSettingsConfigured()
        {
            var currentScenes = EditorBuildSettings.scenes;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>();

            list.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));

            if (File.Exists(GameScenePath))
            {
                list.Add(new EditorBuildSettingsScene(GameScenePath, true));
            }

            foreach (var s in currentScenes)
            {
                if (s.path != MainMenuScenePath && s.path != GameScenePath && !string.IsNullOrEmpty(s.path))
                {
                    list.Add(s);
                }
            }

            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
