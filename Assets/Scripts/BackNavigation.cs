using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class BackNavigation : MonoBehaviour
{
    public ARSession arSession;
    public string previousSceneName = "StartScene";

    public GameObject infoCanvas;
    public GameObject quizCanvas;

    // public CanvasToggleManager canvasToggleManager;
    // public CrossPlatformTTS ttsManager;

    private async void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        // Check if any UICanvasTag is active
        var allUICanvases = FindObjectsOfType<UICanvasTag>();

        foreach (var canvas in allUICanvases)
        {
            if (canvas.gameObject.activeSelf)
            {
                // if (ttsManager != null)
                // {
                //     ttsManager.Stop();
                // }

                canvas.gameObject.SetActive(false);

                if (canvas.name == "AICanvas")
                {
                    // canvasToggleManager.HideAICanvas();
                }

                return;
            }
        }

        // No UI canvas open -> exit AR session
        if (arSession != null)
        {
            arSession.Reset();
        }

        Debug.Log($"Navigating to '{previousSceneName}'.");

        switch (previousSceneName)
        {
            case "AISpawnerScene":
            case "FreeExploreConfig":
            case "FreeExplore":
            case "FreeExploreEndless":
            case "CustomEnvBuilder":
                await SceneLoaderBackend.LoadAddressableSceneAsync(previousSceneName);
                break;

            default:
                await SceneLoaderBackend.LoadLocalSceneAsync(previousSceneName);
                break;
        }
    }
}