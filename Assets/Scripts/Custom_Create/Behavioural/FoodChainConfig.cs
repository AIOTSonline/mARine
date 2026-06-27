using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "MarineAR/Food Chain Config")]
public class FoodChainConfig : ScriptableObject
{
    [System.Serializable]
    public class SpeciesEntry
    {
        [Tooltip("Must exactly match the prefab name in ActorDatabase")]
        public string prefabName;

        [Range(1, 10)]
        [Tooltip("Food chain position. Higher = predator. 1 = bottom of chain.")]
        public int foodChainTier = 1;

        [Tooltip("Human-readable species name shown in UI (e.g. 'Great White Shark')")]
        public string displayName;

        [Tooltip("Additional behaviour class names to attach beyond tier-based ones. " +
                 "e.g. 'PatrolBehaviour', 'SchoolingBehaviour'")]
        public List<string> additionalBehaviours = new List<string>();
    }

    [Header("Species Registry")]
    public List<SpeciesEntry> species = new List<SpeciesEntry>();

    [Header("Tier Behaviour Rules")]
    [Tooltip("Minimum tier difference required to trigger hunting. " +
             "e.g. value=1 means tier 3 hunts tier 2 and below. " +
             "value=2 means tier 3 only hunts tier 1.")]
    public int huntTierDifference = 1;

    [Tooltip("Minimum tier difference required to trigger fleeing.")]
    public int fleeTierDifference = 1;

    [Header("Detection Ranges")]
    [Tooltip("Radius in Unity units within which a predator detects prey")]
    public float huntDetectionRadius = 3f;

    [Tooltip("Radius in Unity units within which prey detects a predator")]
    public float fleeDetectionRadius = 4f;

    [Tooltip("How close a predator must get to consume prey")]
    public float consumeDistance = 0.8f;

    [Header("Chance-Based Hunting")]
    [Range(0f, 1f)]
    [Tooltip("Probability (0-1) that a predator successfully lands a kill on each attempt. " +
             "0 = never kills, 1 = always kills. Default 0.6 = 60% chance each check.")]
    public float huntSuccessChance = 0.6f;

    [Header("Chance-Based Escape")]
    [Range(0f, 1f)]
    [Tooltip("Probability (0-1) that prey successfully triggers a flee response when a " +
             "predator enters detection range. 0 = never flees, 1 = always flees.")]
    public float escapeChance = 0.7f;

    [Header("Defense Ability (Camouflage etc.)")]
    [Range(0f, 1f)]
    [Tooltip("Probability (0-1) that a prey actor triggers its defense ability " +
             "(e.g. camouflage) when a predator is detected nearby. Default 0.8 = 80%.")]
    public float defenseTriggerChance = 0.8f;

    [Tooltip("How long (seconds) the predator is fully stunned after defense triggers")]
    public float predatorStunDuration = 1.5f;

    [Tooltip("How long (seconds) the predator is slowed after the stun wears off")]
    public float predatorSlowDuration = 3f;

    [Range(0f, 1f)]
    [Tooltip("Speed multiplier applied to the predator during the slow phase. " +
             "0.3 = moves at 30% of normal speed.")]
    public float predatorSlowMultiplier = 0.3f;

    [Header("Default Behaviours on Placement")]
    [Tooltip("If true, Hunt Prey is automatically added to addedScripts when an actor " +
             "is placed on the grid, based on its tier. User can remove it in BehaviorScene.")]
    public bool autoAssignHuntBehaviour = true;

    [Tooltip("If true, Flee Predators is automatically added to addedScripts when an actor " +
             "is placed on the grid, based on its tier. User can remove it in BehaviorScene.")]
    public bool autoAssignFleeBehaviour = true;


    // Lookup helpers

    public SpeciesEntry GetEntry(string prefabName)
    {
        return species.Find(s => s.prefabName == prefabName);
    }

    
    public int GetTier(string prefabName)
    {
        SpeciesEntry entry = GetEntry(prefabName);
        return entry != null ? entry.foodChainTier : 1;
    }


    public bool ShouldHunt(int predatorTier, int preyTier)
    {
        return predatorTier - preyTier >= huntTierDifference;
    }


    public bool ShouldFlee(int preyTier, int predatorTier)
    {
        return predatorTier - preyTier >= fleeTierDifference;
    }

    public List<string> GetAdditionalBehaviours(string prefabName)
    {
        SpeciesEntry entry = GetEntry(prefabName);
        return entry?.additionalBehaviours ?? new List<string>();
    }
}