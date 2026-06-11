using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attached to actor GameObjects at runtime when they are spawned as main player.
/// Holds references to all ActorAbility components on this actor.
/// The AbilityButtonUI queries this to build the ability button panel.
/// 
/// To add a new ability to an actor:
///   1. Create a class extending ActorAbility
///   2. Add it to the actor's prefab (or attach at runtime in ARPlacementController)
///   3. ActorAbilityManager will auto-discover it via GetComponents
/// </summary>
public class ActorAbilityManager : MonoBehaviour
{
    // Auto-populated from components on this GameObject and children
    private List<ActorAbility> abilities = new List<ActorAbility>();

    void Awake()
    {
        RefreshAbilities();
    }

    /// <summary>
    /// Scans this actor for all ActorAbility components.
    /// Call this after dynamically adding abilities at runtime.
    /// </summary>
    public void RefreshAbilities()
    {
        abilities.Clear();
        abilities.AddRange(GetComponentsInChildren<ActorAbility>(includeInactive: true));
        Debug.Log($"[ActorAbilityManager] Found {abilities.Count} abilities on {gameObject.name}");
    }

    public List<ActorAbility> GetAbilities() => abilities;

    /// <summary>
    /// Activate an ability by name. Used by UI buttons.
    /// </summary>
    public void ActivateAbility(string abilityName)
    {
        ActorAbility ability = abilities.Find(a => a.AbilityName == abilityName);
        if (ability != null)
            ability.Activate();
        else
            Debug.LogWarning($"[ActorAbilityManager] Ability '{abilityName}' not found on {gameObject.name}");
    }

    /// <summary>
    /// Deactivate all abilities (e.g. on scene transition).
    /// </summary>
    public void DeactivateAll()
    {
        foreach (var ability in abilities)
            ability.Deactivate();
    }
}
