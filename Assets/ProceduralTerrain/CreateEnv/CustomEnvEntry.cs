using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CreateEnv
{
    // Tiny, self-contained entry point. Dropped on a Button in FreeExploreConfig, it
    // loads the custom-environment builder scene when tapped — mirroring how the
    // marine app wires its other buttons in code (FreeExploreConfigController.Start),
    // so nothing existing has to change.
    [RequireComponent(typeof(Button))]
    public class CustomEnvEntry : MonoBehaviour
    {
        [SerializeField] private string sceneName = "CustomEnvBuilder";

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(() => SceneManager.LoadScene(sceneName));
        }
    }
}
