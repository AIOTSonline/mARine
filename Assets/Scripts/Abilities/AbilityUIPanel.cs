using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages the ability button panel in the AR scene.
/// After the main player is spawned, call SetupForActor() to populate buttons.
/// 
/// In your AR Canvas, create:
///   AbilityButtonPanel (this script) 
///     └─ [buttons are instantiated here at runtime]
/// 
/// Assign abilityButtonPrefab in the Inspector — it needs an AbilityButtonUI component.
/// </summary>
public class AbilityUIPanel : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab with Button + AbilityButtonUI component")]
    public GameObject abilityButtonPrefab;

    [Tooltip("Parent transform where buttons are instantiated")]
    public Transform buttonContainer;

    private List<GameObject> spawnedButtons = new List<GameObject>();

    /// <summary>
    /// Call this from ARPlacementController after spawning the main player.
    /// Clears any existing buttons and builds new ones for the actor's abilities.
    /// </summary>
    public void SetupForActor(ActorAbilityManager abilityManager)
    {
        ClearButtons();

        if (abilityManager == null)
        {
            gameObject.SetActive(false);
            return;
        }

        List<ActorAbility> abilities = abilityManager.GetAbilities();

        if (abilities == null || abilities.Count == 0)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        foreach (ActorAbility ability in abilities)
        {
            GameObject btnObj = Instantiate(abilityButtonPrefab, buttonContainer);
            AbilityButtonUI btnUI = btnObj.GetComponent<AbilityButtonUI>();

            if (btnUI != null)
                btnUI.Bind(ability);
            else
                Debug.LogError("[AbilityUIPanel] abilityButtonPrefab is missing AbilityButtonUI component!");

            spawnedButtons.Add(btnObj);
        }

        Debug.Log($"[AbilityUIPanel] Built {abilities.Count} ability button(s).");
    }

    void ClearButtons()
    {
        foreach (var btn in spawnedButtons)
        {
            if (btn != null) Destroy(btn);
        }
        spawnedButtons.Clear();
    }
}
