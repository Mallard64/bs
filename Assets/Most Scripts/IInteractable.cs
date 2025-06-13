using UnityEngine;

/// <summary>
/// Interface for objects that can be interacted with in the lobby
/// </summary>
public interface IInteractable
{
    /// <summary>
    /// Called when a player interacts with this object
    /// </summary>
    /// <param name="player">The player GameObject that is interacting</param>
    void Interact(GameObject player);
    
    /// <summary>
    /// Returns the text to display in the interaction prompt
    /// </summary>
    /// <returns>Text describing the interaction (e.g., "open", "use", "enter")</returns>
    string GetInteractionText();
    
    /// <summary>
    /// Checks if the specified player can interact with this object
    /// </summary>
    /// <param name="player">The player GameObject to check</param>
    /// <returns>True if interaction is allowed, false otherwise</returns>
    bool CanInteract(GameObject player);
}

/// <summary>
/// Base implementation of IInteractable for common functionality
/// </summary>
public abstract class InteractableBase : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    public string interactionText = "interact";
    public bool isInteractable = true;
    public float cooldownTime = 1f;
    
    private float lastInteractionTime;
    
    public virtual void Interact(GameObject player)
    {
        if (!CanInteract(player)) return;
        
        lastInteractionTime = Time.time;
        OnInteract(player);
    }
    
    public virtual string GetInteractionText()
    {
        return interactionText;
    }
    
    public virtual bool CanInteract(GameObject player)
    {
        if (!isInteractable) return false;
        if (Time.time - lastInteractionTime < cooldownTime) return false;
        
        return CanInteractSpecific(player);
    }
    
    /// <summary>
    /// Override this method to implement specific interaction logic
    /// </summary>
    protected abstract void OnInteract(GameObject player);
    
    /// <summary>
    /// Override this method to implement specific interaction requirements
    /// </summary>
    protected virtual bool CanInteractSpecific(GameObject player)
    {
        return true;
    }
}