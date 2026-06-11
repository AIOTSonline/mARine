using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that maps actor prefab names to the ability components
/// they should receive when spawned as main player in the AR scene.
/// 
/// Create via: Assets → Create → MarineAR → Actor Ability Config
/// 
/// Add one entry per species. The abilityTypeNames are the exact class names
/// of ActorAbility subclasses (e.g. "CamouflageAbility").
/// 
/// This keeps ARPlacementController clean — no hardcoded prefab name checks.
/// To support a new species: just add an entry here.
/// </summary>
[CreateAssetMenu(menuName = "MarineAR/Actor Ability Config")]
public class ActorAbilityConfig : ScriptableObject
{
    [System.Serializable]
    public class ActorAbilityEntry
    {
        [Tooltip("Must exactly match the prefab name in ActorDatabase (e.g. 'Octo_Yellow')")]
        public string prefabName;

        [Tooltip("Class names of ActorAbility subclasses to attach at runtime " +
                 "(e.g. 'CamouflageAbility'). Must be exact C# class names.")]
        public List<string> abilityTypeNames = new List<string>();
    }

    public List<ActorAbilityEntry> entries = new List<ActorAbilityEntry>();

    /// <summary>
    /// Returns the ability type names for a given prefab name, or empty list if none.
    /// </summary>
    public List<string> GetAbilityNamesForPrefab(string prefabName)
    {
        ActorAbilityEntry entry = entries.Find(e => e.prefabName == prefabName);
        return entry != null ? entry.abilityTypeNames : new List<string>();
    }

    /// <summary>
    /// Returns true if this prefab has any registered abilities.
    /// </summary>
    public bool HasAbilities(string prefabName)
    {
        return entries.Exists(e => e.prefabName == prefabName && e.abilityTypeNames.Count > 0);
    }
}
