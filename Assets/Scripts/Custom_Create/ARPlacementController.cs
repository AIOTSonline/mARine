/*
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementController : MonoBehaviour
{
    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    public ARPlaneManager planeManager;

    [Header("Environment Prefabs")]
    public ActorDatabase actorDatabase;

    [Header("UI")]
    public FloatingJoystick joystick;
    public GameObject joystickUIRoot;

    [Header("Layer Settings")]
    public SandboxSettings settings; // Assign in Inspector for height reference

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool placed = false;

    void Start()
    {
        if (joystickUIRoot != null)
            joystickUIRoot.SetActive(false);
    }

    void Update()
    {
        if (placed) return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            TryPlaceAt(Input.mousePosition);
#else
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
            TryPlaceAt(Input.GetTouch(0).position);
#endif
    }

    private void TryPlaceAt(Vector2 screenPosition)
    {
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
        {
            Pose pose = hits[0].pose;
            PlaceSavedEnvironment(pose.position);
            placed = true;
        }
    }

    private void PlaceSavedEnvironment(Vector3 position)
    {
        string envKey = PlayerPrefs.GetString("SelectedEnvironmentKey");
        string json = PlayerPrefs.GetString(envKey);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("No environment data found for key: " + envKey);
            return;
        }

        EnvironmentData data = JsonUtility.FromJson<EnvironmentData>(json);
        if (data == null)
        {
            Debug.LogError("Failed to parse environment data.");
            return;
        }

        data.MigrateFromLegacy();

        int layerTotal = data.layerCount > 0 ? data.layerCount : 3;

        // Root sits exactly at AR anchor position
        GameObject root = new GameObject(data.environmentName);
        root.transform.position = position;
        root.transform.rotation = Quaternion.identity;

        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);
        }

        // Spawn environment plane at root — always at local zero
        if (!string.IsNullOrEmpty(data.environmentPlanePrefabName))
        {
            GameObject planePrefab =
                actorDatabase.GetActorByName(data.environmentPlanePrefabName);
            if (planePrefab != null)
            {
                GameObject plane = Instantiate(planePrefab, root.transform);
                plane.transform.localPosition = Vector3.zero;
                plane.transform.localRotation = Quaternion.identity;
            }
        }

        bool mainPlayerFound = false;

        // Collect all actors across all layers
        List<PlacedActorData> allActors = new List<PlacedActorData>();
        for (int i = 0; i < layerTotal; i++)
            allActors.AddRange(data.GetLayerActors(i));

        Debug.Log($"Spawning {allActors.Count} actors across {layerTotal} layers.");

        foreach (var actor in allActors)
        {
            GameObject prefab = actorDatabase.GetActorByName(actor.prefabName);
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found: {actor.prefabName}");
                continue;
            }

            GameObject go = Instantiate(prefab, root.transform);

            // Calculate local Y height for this layer
            // Layer 0 = top = highest, last layer = bottom = 0
            float localY = GetLocalLayerHeight(actor.layerIndex, layerTotal);

            // X and Z from saved local position, Y from layer calculation
            go.transform.position = new Vector3(
      root.transform.position.x + actor.localPosition.x,
      root.transform.position.y + localY,
      root.transform.position.z + actor.localPosition.z
  );
            go.transform.localRotation = actor.localRotation;

            if (string.IsNullOrEmpty(actor.uniqueID))
                actor.uniqueID = System.Guid.NewGuid().ToString();

            go.name = actor.uniqueID;
            go.tag = "Actor";

            ActorIdentity identity = go.AddComponent<ActorIdentity>();
            identity.uniqueId = actor.uniqueID;

            if (actor.isMainPlayer)
            {
                var controller = go.AddComponent<MovementController>();
                controller.joystick = joystick;
                mainPlayerFound = true;
                Debug.Log($"Main player: {actor.prefabName} Layer {actor.layerIndex}");
            }

            if (actor.addedScripts != null &&
                actor.addedScripts.Contains("Food Consumption"))
            {
                FoodConsumer fc = go.AddComponent<FoodConsumer>();
                fc.foodTargetUniqueID = actor.foodTargetUniqueID;
            }

            Debug.Log($"Spawned: {actor.prefabName} " +
                      $"Layer {actor.layerIndex} localY={localY}");
        }

        if (joystickUIRoot != null)
            joystickUIRoot.SetActive(mainPlayerFound);

        Debug.Log("Environment placed successfully.");
    }

    // Calculate local Y height relative to root
    // Layer 0 = top, last layer = bottom at Y=0
    float GetLocalLayerHeight(int layerIndex, int totalLayers)
    {
        float spacing = settings != null ? settings.layerSpacing : 1.5f;

        // Bottom layer sits just above the environment plane
        float bottomY = 0.1f;

        // Top layer is highest, bottom layer is lowest
        float topY = bottomY + (totalLayers - 1) * spacing;

        // Layer 0 = top = topY, last layer = bottom = bottomY
        return topY - (layerIndex * spacing);
    }

    float GetLayerHeight(int layerIndex, int totalLayers)
    {
        if (settings != null)
            return settings.GetLayerHeight(layerIndex, totalLayers);

        float bottomY = 0f;
        float spacing = 1.5f;
        float topY = bottomY + (totalLayers - 1) * spacing;
        return topY - (layerIndex * spacing);
    }
  
}*/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementController : MonoBehaviour
{
    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    public ARPlaneManager planeManager;

    [Header("Environment Prefabs")]
    public ActorDatabase actorDatabase;

    [Header("UI")]
    public FloatingJoystick joystick;
    public GameObject joystickUIRoot;

    [Header("Layer Settings")]
    public SandboxSettings settings;

    [Header("Ability System")]
    [Tooltip("ScriptableObject mapping prefab names to their abilities")]
    public ActorAbilityConfig abilityConfig;

    [Tooltip("The panel that shows ability buttons in the AR scene")]
    public AbilityUIPanel abilityUIPanel;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool placed = false;

    void Start()
    {
        if (joystickUIRoot != null)
            joystickUIRoot.SetActive(false);

        // Hide ability panel until a main player with abilities is spawned
        if (abilityUIPanel != null)
            abilityUIPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (placed) return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            TryPlaceAt(Input.mousePosition);
#else
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
            TryPlaceAt(Input.GetTouch(0).position);
#endif
    }

    private void TryPlaceAt(Vector2 screenPosition)
    {
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
        {
            Pose pose = hits[0].pose;
            PlaceSavedEnvironment(pose.position);
            placed = true;
        }
    }

    private void PlaceSavedEnvironment(Vector3 position)
    {
        string envKey = PlayerPrefs.GetString("SelectedEnvironmentKey");
        string json = PlayerPrefs.GetString(envKey);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("No environment data found for key: " + envKey);
            return;
        }

        EnvironmentData data = JsonUtility.FromJson<EnvironmentData>(json);
        if (data == null)
        {
            Debug.LogError("Failed to parse environment data.");
            return;
        }

        data.MigrateFromLegacy();

        int layerTotal = data.layerCount > 0 ? data.layerCount : 3;

        GameObject root = new GameObject(data.environmentName);
        root.transform.position = position;
        root.transform.rotation = Quaternion.identity;

        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);
        }

        // Spawn environment plane
        if (!string.IsNullOrEmpty(data.environmentPlanePrefabName))
        {
            GameObject planePrefab =
                actorDatabase.GetActorByName(data.environmentPlanePrefabName);
            if (planePrefab != null)
            {
                GameObject plane = Instantiate(planePrefab, root.transform);
                plane.transform.localPosition = Vector3.zero;
                plane.transform.localRotation = Quaternion.identity;
            }
        }

        bool mainPlayerFound = false;
        ActorAbilityManager mainPlayerAbilityManager = null;

        // Collect all actors across all layers
        List<PlacedActorData> allActors = new List<PlacedActorData>();
        for (int i = 0; i < layerTotal; i++)
            allActors.AddRange(data.GetLayerActors(i));

        Debug.Log($"Spawning {allActors.Count} actors across {layerTotal} layers.");

        foreach (var actor in allActors)
        {
            GameObject prefab = actorDatabase.GetActorByName(actor.prefabName);
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found: {actor.prefabName}");
                continue;
            }

            GameObject go = Instantiate(prefab, root.transform);

            float localY = GetLocalLayerHeight(actor.layerIndex, layerTotal);
            go.transform.position = new Vector3(
                root.transform.position.x + actor.localPosition.x,
                root.transform.position.y + localY,
                root.transform.position.z + actor.localPosition.z
            );
            go.transform.localRotation = actor.localRotation;

            if (string.IsNullOrEmpty(actor.uniqueID))
                actor.uniqueID = System.Guid.NewGuid().ToString();

            go.name = actor.uniqueID;
            go.tag = "Actor";

            ActorIdentity identity = go.AddComponent<ActorIdentity>();
            identity.uniqueId = actor.uniqueID;

            // Main player setup
            if (actor.isMainPlayer)
            {
                var controller = go.AddComponent<MovementController>();
                controller.joystick = joystick;
                mainPlayerFound = true;

                // --- Ability System ---
                // Attach abilities defined in ActorAbilityConfig for this prefab
                mainPlayerAbilityManager = AttachAbilities(go, actor.prefabName);

                Debug.Log($"Main player spawned: {actor.prefabName} Layer {actor.layerIndex}");
            }

            // Food consumption behaviour
            if (actor.addedScripts != null &&
                actor.addedScripts.Contains("Food Consumption"))
            {
                FoodConsumer fc = go.AddComponent<FoodConsumer>();
                fc.foodTargetUniqueID = actor.foodTargetUniqueID;
            }

            Debug.Log($"Spawned: {actor.prefabName} Layer {actor.layerIndex} localY={localY}");
        }

        if (joystickUIRoot != null)
            joystickUIRoot.SetActive(mainPlayerFound);

        // Setup ability UI panel for the main player
        if (abilityUIPanel != null)
            abilityUIPanel.SetupForActor(mainPlayerAbilityManager);

        Debug.Log("Environment placed successfully.");
    }

    /// <summary>
    /// Attaches ActorAbility components to the spawned actor based on ActorAbilityConfig.
    /// Then attaches ActorAbilityManager so the UI can query abilities.
    /// Returns the ActorAbilityManager, or null if no abilities were registered.
    /// </summary>
    private ActorAbilityManager AttachAbilities(GameObject actorGO, string prefabName)
    {
        if (abilityConfig == null)
        {
            Debug.LogWarning("[ARPlacementController] No ActorAbilityConfig assigned. " +
                             "Ability buttons will not appear.");
            return null;
        }

        List<string> abilityTypeNames = abilityConfig.GetAbilityNamesForPrefab(prefabName);

        if (abilityTypeNames == null || abilityTypeNames.Count == 0)
        {
            Debug.Log($"[ARPlacementController] No abilities configured for '{prefabName}'.");
            return null;
        }

        bool anyAttached = false;

        foreach (string typeName in abilityTypeNames)
        {
            // Resolve class name to System.Type at runtime
            System.Type abilityType = System.Type.GetType(typeName);

            if (abilityType == null)
            {
                Debug.LogError($"[ARPlacementController] Ability type '{typeName}' not found. " +
                               $"Make sure the class name exactly matches.");
                continue;
            }

            if (!typeof(ActorAbility).IsAssignableFrom(abilityType))
            {
                Debug.LogError($"[ARPlacementController] '{typeName}' does not extend ActorAbility.");
                continue;
            }

            actorGO.AddComponent(abilityType);
            Debug.Log($"[ARPlacementController] Attached ability '{typeName}' to {prefabName}.");
            anyAttached = true;
        }

        if (!anyAttached) return null;

        // ActorAbilityManager auto-discovers all ActorAbility components via GetComponents
        ActorAbilityManager manager = actorGO.AddComponent<ActorAbilityManager>();
        return manager;
    }

    float GetLocalLayerHeight(int layerIndex, int totalLayers)
    {
        float spacing = settings != null ? settings.layerSpacing : 1.5f;
        float bottomY = 0.1f;
        float topY = bottomY + (totalLayers - 1) * spacing;
        return topY - (layerIndex * spacing);
    }

    float GetLayerHeight(int layerIndex, int totalLayers)
    {
        if (settings != null)
            return settings.GetLayerHeight(layerIndex, totalLayers);

        float bottomY = 0f;
        float spacing = 1.5f;
        float topY = bottomY + (totalLayers - 1) * spacing;
        return topY - (layerIndex * spacing);
    }
}