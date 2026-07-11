using UnityEngine;
using UnityEngine.SceneManagement;

namespace CreateEnv.UI
{
    // The CustomEnvBuilder scene is JUST the environment settings editor — there is
    // no "Choose an Environment" list. It is reached from FreeExploreConfig in one of
    // two ways: "New Environment" opens a blank form, and "Edit" (available on
    // user-made environments) sets EnvironmentSession.EditRequestId first so the form
    // opens pre-filled. Save writes the profile to EnvironmentRepository; both Save
    // and Cancel return to FreeExploreConfig, where environments are played.
    public class StartScreenUI : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("Panel carrying the EnvironmentEditorUI (the configurable settings form).")]
        public GameObject editorPanel;

        [Header("Navigation")]
        [Tooltip("Scene returned to after Save/Cancel — where saved environments are played.")]
        public string configSceneName = "FreeExploreConfig";

        void Start()
        {
            var editor = editorPanel != null ? editorPanel.GetComponent<EnvironmentEditorUI>() : null;
            if (editor == null)
            {
                Debug.LogWarning("[StartScreenUI] No EnvironmentEditorUI on editorPanel; returning to config.");
                SceneManager.LoadScene(configSceneName);
                return;
            }

            // Consume a pending edit request (one-shot handoff from FreeExploreConfig).
            string editId = EnvironmentSession.EditRequestId;
            EnvironmentSession.EditRequestId = null;
            var existing = string.IsNullOrEmpty(editId) ? null : EnvironmentRepository.Load(editId);

            // Open the settings form straight away; both Save and Cancel come back here.
            editor.Open(existing, () => SceneManager.LoadScene(configSceneName));
        }
    }
}
