/*using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AddScriptsSceneManager : MonoBehaviour
{
    public Transform scriptListContainer;
    public GameObject scriptblockbuttonPrefab;

    private List<string> availableScripts = new List<string>
    {
        "Food Consumption"
    };

    void Start()
    {
        string selectedEnvKey = PlayerPrefs.GetString("SelectedEnvironmentKey", "");
        if (string.IsNullOrEmpty(selectedEnvKey))
        {
            Debug.LogError("Missing environment key!");
            return;
        }

        string json = PlayerPrefs.GetString(selectedEnvKey, "");
        EnvironmentData environmentData = JsonUtility.FromJson<EnvironmentData>(json);

        if (environmentData == null)
        {
            Debug.LogError("Invalid environment data!");
            return;
        }

        // Migrate legacy data just in case
        environmentData.MigrateFromLegacy();

        // Get selected actor by unique ID
        string currentActorUniqueID = PlayerPrefs.GetString("SelectedActorUniqueID", "");
        if (string.IsNullOrEmpty(currentActorUniqueID))
        {
            Debug.LogError("Missing selected actor unique ID!");
            return;
        }

        // Find current actor across all layers
        PlacedActorData currentActor = null;
        for (int i = 0; i < 3; i++)
        {
            currentActor = environmentData.GetLayerActors(i).Find(a => a.uniqueID == currentActorUniqueID);
            if (currentActor != null) break;
        }

        if (currentActor == null)
        {
            Debug.LogError($"Actor with ID '{currentActorUniqueID}' not found!");
            return;
        }

        Debug.Log($"AddScripts for: {currentActor.prefabName} on Layer {currentActor.layerIndex}");

        // Get ALL actors across all layers for food target dropdown
        List<PlacedActorData> allActors = new List<PlacedActorData>();
        for (int i = 0; i < 3; i++)
            allActors.AddRange(environmentData.GetLayerActors(i));

        foreach (string scriptName in availableScripts)
        {
            GameObject buttonObj = Instantiate(scriptblockbuttonPrefab, scriptListContainer);

            Text label = buttonObj.GetComponentInChildren<Text>();
            if (label != null) label.text = scriptName;

            Dropdown dropdown = buttonObj.GetComponentInChildren<Dropdown>(true);

            if (scriptName == "Food Consumption" && dropdown != null)
            {
                dropdown.gameObject.SetActive(true);
                dropdown.ClearOptions();

                List<string> actorDisplayNames = new List<string>();
                Dictionary<string, string> displayNameToIdMap = new Dictionary<string, string>();
                Dictionary<string, int> prefabCounts = new Dictionary<string, int>();

                foreach (var actor in allActors)
                {
                    // Skip current actor
                    if (actor.uniqueID == currentActorUniqueID) continue;

                    if (!prefabCounts.ContainsKey(actor.prefabName))
                        prefabCounts[actor.prefabName] = 1;
                    else
                        prefabCounts[actor.prefabName]++;

                    // Show layer info in dropdown so user knows which layer the target is on
                    string displayName = $"{actor.prefabName} ({prefabCounts[actor.prefabName]}) [L{actor.layerIndex}]";
                    actorDisplayNames.Add(displayName);
                    displayNameToIdMap[displayName] = actor.uniqueID;
                }

                dropdown.AddOptions(actorDisplayNames);

                // Save default selection
                if (actorDisplayNames.Count > 0)
                {
                    string defaultName = actorDisplayNames[0];
                    string defaultID = displayNameToIdMap[defaultName];
                    PlayerPrefs.SetString("PendingFoodTargetID", defaultID);
                    PlayerPrefs.Save();
                    Debug.Log($"Default food target: {defaultName} => {defaultID}");
                }

                dropdown.onValueChanged.AddListener((int index) =>
                {
                    string selectedName = actorDisplayNames[index];
                    string selectedID = displayNameToIdMap[selectedName];
                    PlayerPrefs.SetString("PendingFoodTargetID", selectedID);
                    PlayerPrefs.Save();
                    Debug.Log($"Food target changed: {selectedName} => {selectedID}");
                });
            }

            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    PlayerPrefs.SetString("PendingScriptToAdd", scriptName);
                    PlayerPrefs.Save();
                    SceneManager.LoadScene("BehaviorScene");
                });
            }
            else
            {
                Debug.LogError("No Button component found in scriptblockbuttonPrefab!");
            }
        }
    }
}*/
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AddScriptsSceneManager : MonoBehaviour
{
    public Transform scriptListContainer;
    public GameObject scriptblockbuttonPrefab;

    private List<string> availableScripts = new List<string>
    {
        "Food Consumption"
    };

    void Start()
    {
        string selectedEnvKey = PlayerPrefs.GetString("SelectedEnvironmentKey", "");
        if (string.IsNullOrEmpty(selectedEnvKey))
        {
            Debug.LogError("Missing environment key!");
            return;
        }

        string json = PlayerPrefs.GetString(selectedEnvKey, "");
        EnvironmentData environmentData = JsonUtility.FromJson<EnvironmentData>(json);

        if (environmentData == null)
        {
            Debug.LogError("Invalid environment data!");
            return;
        }

        environmentData.MigrateFromLegacy();

        string currentActorUniqueID = PlayerPrefs.GetString("SelectedActorUniqueID", "");
        if (string.IsNullOrEmpty(currentActorUniqueID))
        {
            Debug.LogError("Missing selected actor unique ID!");
            return;
        }

        // Find current actor across all layers
        PlacedActorData currentActor = null;
        int total = environmentData.layerCount > 0 ? environmentData.layerCount : 5;

        for (int i = 0; i < total; i++)
        {
            currentActor = environmentData.GetLayerActors(i)
                                          .Find(a => a.uniqueID == currentActorUniqueID);
            if (currentActor != null) break;
        }

        if (currentActor == null)
        {
            Debug.LogError($"Actor '{currentActorUniqueID}' not found!");
            return;
        }

        Debug.Log($"AddScripts for: {currentActor.prefabName} " +
                  $"on Layer {currentActor.layerIndex}");

        // Get all actors across all layers for dropdown
        List<PlacedActorData> allActors = new List<PlacedActorData>();
        for (int i = 0; i < total; i++)
            allActors.AddRange(environmentData.GetLayerActors(i));

        foreach (string scriptName in availableScripts)
        {
            GameObject buttonObj = Instantiate(scriptblockbuttonPrefab, scriptListContainer);

            Text label = buttonObj.GetComponentInChildren<Text>();
            if (label != null) label.text = scriptName;

            Dropdown dropdown = buttonObj.GetComponentInChildren<Dropdown>(true);

            if (scriptName == "Food Consumption" && dropdown != null)
            {
                dropdown.gameObject.SetActive(true);
                dropdown.ClearOptions();

                List<string> actorDisplayNames = new List<string>();
                Dictionary<string, string> displayToIdMap = new Dictionary<string, string>();
                Dictionary<string, int> prefabCounts = new Dictionary<string, int>();

                foreach (var actor in allActors)
                {
                    if (actor.uniqueID == currentActorUniqueID) continue;

                    if (!prefabCounts.ContainsKey(actor.prefabName))
                        prefabCounts[actor.prefabName] = 1;
                    else
                        prefabCounts[actor.prefabName]++;

                    // Show layer info in dropdown
                    string displayName =
                        $"{actor.prefabName} ({prefabCounts[actor.prefabName]}) [L{actor.layerIndex}]";
                    actorDisplayNames.Add(displayName);
                    displayToIdMap[displayName] = actor.uniqueID;
                }

                dropdown.AddOptions(actorDisplayNames);

                if (actorDisplayNames.Count > 0)
                {
                    string defaultName = actorDisplayNames[0];
                    string defaultID = displayToIdMap[defaultName];
                    PlayerPrefs.SetString("PendingFoodTargetID", defaultID);
                    PlayerPrefs.Save();
                    Debug.Log($"Default food target: {defaultName} => {defaultID}");
                }

                dropdown.onValueChanged.AddListener((int index) =>
                {
                    string selectedName = actorDisplayNames[index];
                    string selectedID = displayToIdMap[selectedName];
                    PlayerPrefs.SetString("PendingFoodTargetID", selectedID);
                    PlayerPrefs.Save();
                    Debug.Log($"Food target: {selectedName} => {selectedID}");
                });
            }

            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    PlayerPrefs.SetString("PendingScriptToAdd", scriptName);
                    PlayerPrefs.Save();
                    SceneManager.LoadScene("BehaviorScene");
                });
            }
            else
            {
                Debug.LogError("No Button on scriptblockbuttonPrefab!");
            }
        }
    }
}