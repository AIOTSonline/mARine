using UnityEngine;

/// <summary>
/// Abstract base class for all actor-specific abilities.
/// Extend this to add new species behaviors (camouflage, speed burst, ink spray, etc.)
/// Attach concrete implementations to actor prefabs via ActorAbilityManager.
/// </summary>
public abstract class ActorAbility : MonoBehaviour
{
    // Display name shown on the UI button
    public abstract string AbilityName { get; }

    // Optional icon name for future UI icon support
    public virtual string IconName => "";

    // Whether this ability is currently active/toggled on
    public bool IsActive { get; protected set; } = false;

    /// <summary>
    /// Called when the ability button is pressed.
    /// Override to implement toggle or one-shot behavior.
    /// </summary>
    public abstract void Activate();

    /// <summary>
    /// Called when the ability is force-deactivated (e.g. actor destroyed, scene change).
    /// Override to clean up any effects.
    /// </summary>
    public virtual void Deactivate()
    {
        IsActive = false;
    }

    protected virtual void OnDestroy()
    {
        Deactivate();
    }
}
