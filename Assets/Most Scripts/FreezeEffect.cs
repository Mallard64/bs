using UnityEngine;
using Mirror;
using System.Collections;

public class FreezeEffect : NetworkBehaviour
{
    [SyncVar]
    private float freezeDuration;
    [SyncVar(hook = nameof(OnFrozenStateChanged))]
    private bool isFrozen = false;
    
    private Coroutine freezeCoroutine;
    private Enemy targetEnemy;
    private ActualEnemy targetActualEnemy;
    private PlayerMovement playerMovement;
    private BotPlayer botPlayer;
    private MouseShooting mouseShooting;
    private float originalMoveSpeed;
    private bool originalSpeedStored = false;
    
    void Start()
    {
        targetEnemy = GetComponent<Enemy>();
        targetActualEnemy = GetComponent<ActualEnemy>();
        playerMovement = GetComponent<PlayerMovement>();
        botPlayer = GetComponent<BotPlayer>();
        mouseShooting = GetComponent<MouseShooting>();
    }
    
    [Server]
    public void ApplyFreeze(float duration)
    {
        if (isFrozen)
        {
            // Refresh freeze duration if already frozen
            freezeDuration = Mathf.Max(freezeDuration, duration);
        }
        else
        {
            freezeDuration = duration;
            isFrozen = true;
            
            // Store original speed and disable movement
            StoreOriginalSpeed();
            ApplyFreeze();
            
            if (freezeCoroutine != null)
            {
                StopCoroutine(freezeCoroutine);
            }
            freezeCoroutine = StartCoroutine(FreezeRoutine());
        }
        
        // Show freeze effect to all clients
        try
        {
            if (isServer && NetworkServer.active)
            {
                RpcShowFreezeEffect(freezeDuration);
            }
            else
            {
                // Fallback: show effect locally if networking isn't available
                StartCoroutine(FreezeVisualEffect(freezeDuration));
            }
        }
        catch
        {
            // Fallback: show effect locally if any networking error occurs
            StartCoroutine(FreezeVisualEffect(freezeDuration));
        }
    }
    
    void StoreOriginalSpeed()
    {
        if (!originalSpeedStored)
        {
            if (playerMovement != null)
            {
                originalMoveSpeed = playerMovement.moveSpeed;
            }
            else if (botPlayer != null)
            {
                originalMoveSpeed = botPlayer.moveSpeed;
            }
            else
            {
                // Fallback: use a default speed
                originalMoveSpeed = 5f;
            }
            originalSpeedStored = true;
        }
    }
    
    void ApplyFreeze()
    {
        // Completely stop movement
        if (playerMovement != null)
        {
            playerMovement.moveSpeed = 0f;
            playerMovement.enabled = false;
        }
        if (botPlayer != null)
        {
            botPlayer.moveSpeed = 0f;
            botPlayer.enabled = false;
        }
        if (mouseShooting != null)
        {
            mouseShooting.enabled = false;
        }
        
        // Stop rigidbody movement
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
        }
    }
    
    [ClientRpc]
    void RpcShowFreezeEffect(float duration)
    {
        // Create visual freeze effect visible to ALL clients (including the affected player)
        StartCoroutine(FreezeVisualEffect(duration));
        
        // Add extra visual effects for OTHER players to clearly see this player is frozen
        if (!isLocalPlayer)
        {
            Debug.Log($"❄️ Other players can see {gameObject.name} is frozen with enhanced ice effect");
            StartCoroutine(EnhancedFreezeEffectForOthers(duration));
        }
        else
        {
            Debug.Log($"❄️ You are frozen and can see your own freeze effect");
        }
    }
    
    IEnumerator FreezeVisualEffect(float duration)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        
        float elapsed = 0f;
        while (elapsed < duration && isFrozen)
        {
            // Enhanced freeze effect with multiple ice colors and crystal-like shimmer
            if (spriteRenderer != null)
            {
                float time = Time.time;
                float slowPulse = 0.4f + 0.6f * Mathf.Sin(time * 2f);     // Slow breathing pulse
                float shimmer = 0.2f + 0.3f * Mathf.Sin(time * 8f);       // Fast shimmer effect
                float iceGlow = 0.3f + 0.4f * Mathf.Sin(time * 1.5f);     // Ultra slow glow
                
                // Create layered ice colors - cyan base with white highlights
                Color iceBlue = new Color(0.4f, 0.8f, 1f, 1f);           // Light ice blue
                Color iceCyan = new Color(0.2f, 1f, 1f, 1f);              // Bright cyan
                Color iceWhite = new Color(0.9f, 0.95f, 1f, 1f);          // Slightly blue-tinted white
                
                // Mix colors for realistic ice effect
                Color baseIce = Color.Lerp(iceBlue, iceCyan, slowPulse);
                Color shimmerIce = Color.Lerp(baseIce, iceWhite, shimmer * 0.6f);
                Color finalColor = Color.Lerp(originalColor, shimmerIce, 0.75f + iceGlow * 0.25f);
                
                // Add slight brightness boost during shimmer
                finalColor.g = Mathf.Min(1f, finalColor.g + shimmer * 0.1f);
                finalColor.b = Mathf.Min(1f, finalColor.b + shimmer * 0.15f);
                
                spriteRenderer.color = finalColor;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Restore original color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }
    
    [Server]
    IEnumerator FreezeRoutine()
    {
        yield return new WaitForSeconds(freezeDuration);
        
        // End freeze effect on server
        EndFreezeEffect();
    }
    
    [Server]
    public void EndFreezeEffect()
    {
        if (!isFrozen) return; // Already unfrozen
        
        Debug.Log($"❄️ Server: Ending freeze effect for {gameObject.name}");
        
        // End freeze effect
        isFrozen = false;
        RestoreOriginalState();
        
        // Tell all clients to end the freeze effect
        RpcEndFreezeEffect();
    }
    
    void RestoreOriginalState()
    {
        if (originalSpeedStored)
        {
            if (playerMovement != null)
            {
                playerMovement.moveSpeed = originalMoveSpeed;
                playerMovement.enabled = true;
            }
            if (botPlayer != null)
            {
                botPlayer.moveSpeed = originalMoveSpeed;
                botPlayer.enabled = true;
            }
            if (mouseShooting != null)
            {
                mouseShooting.enabled = true;
            }
            
            // Restore rigidbody
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
        }
    }
    
    IEnumerator EnhancedFreezeEffectForOthers(float duration)
    {
        // More dramatic visual effects for other players to clearly see this player is frozen
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        GameObject iceEffect = null;
        
        // Create ice crystal effect for other players
        if (spriteRenderer != null)
        {
            // Create ice crystals around the player
            iceEffect = new GameObject("IceEffect");
            iceEffect.transform.SetParent(transform);
            iceEffect.transform.localPosition = Vector3.zero;
            
            SpriteRenderer iceSprite = iceEffect.AddComponent<SpriteRenderer>();
            iceSprite.color = new Color(0.7f, 0.9f, 1f, 0.6f); // Translucent ice blue
            iceSprite.sprite = spriteRenderer.sprite; // Use same sprite but icy
            iceEffect.transform.localScale = Vector3.one * 1.1f; // Slightly larger
        }
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Subtle ice shimmer effect for other players
            if (iceEffect != null)
            {
                float shimmer = Mathf.Sin(Time.time * 3f) * 0.1f + 0.9f;
                var iceSprite = iceEffect.GetComponent<SpriteRenderer>();
                if (iceSprite != null)
                {
                    iceSprite.color = new Color(0.7f, 0.9f, 1f, 0.3f + shimmer * 0.3f);
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Clean up ice effect
        if (iceEffect != null)
        {
            Destroy(iceEffect);
        }
    }
    
    [ClientRpc]
    void RpcEndFreezeEffect()
    {
        Debug.Log($"❄️ Client: Ending freeze effect for {gameObject.name}");
        
        // Clean up any visual effects
        StopAllCoroutines();
        
        // Force end freeze state on client
        isFrozen = false;
        
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        
        // Clean up any ice effects for other players
        Transform iceEffect = transform.Find("IceEffect");
        if (iceEffect != null)
        {
            Destroy(iceEffect.gameObject);
        }
    }
    
    // SyncVar hook - called when isFrozen changes
    void OnFrozenStateChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"❄️ Frozen state changed from {oldValue} to {newValue} for {gameObject.name}");
        
        if (!newValue && oldValue) // Just got unfrozen
        {
            // Make sure visual effects are cleaned up on client
            StopAllCoroutines();
            
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            
            // Clean up any ice effects
            Transform iceEffect = transform.Find("IceEffect");
            if (iceEffect != null)
            {
                Destroy(iceEffect.gameObject);
            }
        }
    }
    
    // Public method to force unfreeze (for debugging or special cases)
    [ContextMenu("Force Unfreeze")]
    public void ForceUnfreeze()
    {
        if (isServer)
        {
            EndFreezeEffect();
        }
        else
        {
            Debug.Log("❄️ Can only force unfreeze from server");
        }
    }
    
    void OnDestroy()
    {
        if (freezeCoroutine != null)
        {
            StopCoroutine(freezeCoroutine);
        }
        
        // Restore state when component is destroyed
        if (isFrozen)
        {
            RestoreOriginalState();
        }
    }
}