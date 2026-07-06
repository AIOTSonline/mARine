using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CreateEnv.UI
{
    // Marine adaptation of the terrain project's start screen. Unlike the source app,
    // the marine app has no per-biome scenes — every environment (built-in preset OR a
    // user-made one) is EnvironmentProfile DATA that plays through the shared
    // ConfigurableTerrainEnvironment prefab in the existing FreeExploreEndless scene.
    //
    // Flow: pick/create a profile here -> EnvironmentSession carries it ->
    // FreeExploreEndless AR-taps to place the configurable prefab, whose
    // EnvironmentLoader reads the session and configures the terrain once.
    public class StartScreenUI : MonoBehaviour
    {
        [Header("Wiring (set by the Editor generator / scene)")]
        public RectTransform listContainer;   // ScrollView content
        public GameObject    envTilePrefab;   // children: Title, Subtitle, PlayButton, EditButton, DeleteButton
        public Button        addButton;       // "+ Add own env"
        public GameObject    editorPanel;     // panel carrying EnvironmentEditorUI (hidden until used)

        [Header("Marine Free Explore")]
        [Tooltip("The ConfigurableTerrainEnvironment prefab that FreeExploreEndless AR-places. " +
                 "Its EnvironmentLoader applies the selected profile.")]
        public GameObject configurablePrefab;
        [Tooltip("Scene that AR-places the configurable prefab (the marine endless explore scene).")]
        public string endlessSceneName = "FreeExploreEndless";

        EnvironmentEditorUI _editor;

        void Start()
        {
            if (editorPanel != null)
            {
                _editor = editorPanel.GetComponent<EnvironmentEditorUI>();
                if (_editor != null) _editor.PlayRouter = PlayProfile; // route "Save & Play" the marine way
                editorPanel.SetActive(false);
            }
            if (addButton != null)
                addButton.onClick.AddListener(() => { if (_editor != null) _editor.Open(null, Refresh); });

            Refresh();
        }

        public void Refresh()
        {
            if (listContainer == null || envTilePrefab == null) return;

            for (int i = listContainer.childCount - 1; i >= 0; i--)
                Destroy(listContainer.GetChild(i).gameObject);

            // Built-in presets: play directly, or Edit to start a customised copy.
            foreach (var b in BuiltInProfiles.All())
            {
                var prof = b;
                MakeTile(prof.displayName, Tagline(prof),
                         onPlay:   () => PlayProfile(prof),
                         onEdit:   () => { if (_editor != null) _editor.Open(prof, Refresh); },
                         onDelete: null); // built-ins can't be deleted
            }

            // User-saved environments: play / edit / delete.
            foreach (var p in EnvironmentRepository.ListUser())
            {
                var prof = p;
                MakeTile(prof.displayName, Tagline(prof),
                         onPlay:   () => PlayProfile(prof),
                         onEdit:   () => { if (_editor != null) _editor.Open(prof, Refresh); },
                         onDelete: () => { EnvironmentRepository.Delete(prof.id); Refresh(); });
            }
        }

        // The single marine routing: hand the profile to the session and load the
        // endless scene, telling its placement manager to spawn the configurable prefab.
        public void PlayProfile(EnvironmentProfile p)
        {
            if (p == null) return;
            EnvironmentSession.Selected = p.Clone();
            ExploreSession.SelectedBoundlessEnvironment = new EnvironmentInfo
            {
                EnvironmentName   = p.displayName,
                Description       = p.displayName,
                EnvironmentPrefab = configurablePrefab
            };
            SceneManager.LoadScene(endlessSceneName);
        }

        void MakeTile(string title, string subtitle,
                      System.Action onPlay, System.Action onEdit, System.Action onDelete)
        {
            var tile = Instantiate(envTilePrefab, listContainer);
            SetText(tile, "Title", title);
            SetText(tile, "Subtitle", subtitle);
            WireButton(tile, "PlayButton", onPlay);
            WireButton(tile, "EditButton", onEdit);
            WireButton(tile, "DeleteButton", onDelete);
        }

        static void SetText(GameObject root, string child, string value)
        {
            var t = root.transform.Find(child);
            if (t != null && t.TryGetComponent(out TMP_Text tmp)) tmp.text = value;
        }

        static void WireButton(GameObject root, string child, System.Action action)
        {
            var t = root.transform.Find(child);
            if (t == null) return;
            if (action == null) { t.gameObject.SetActive(false); return; }
            if (t.TryGetComponent(out Button b))
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => action());
            }
        }

        static string Tagline(EnvironmentProfile p)
        {
            string style = p.styleIndex == 1 ? "Canyons" : "Dunes";
            string view = p.viewDistanceIndex == 0 ? "Near" : p.viewDistanceIndex == 2 ? "Far" : "Medium";
            return $"{style} · view {view} · seed {p.seed}";
        }
    }
}
