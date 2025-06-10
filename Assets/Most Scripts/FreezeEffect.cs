using UnityEngine;
using Mirror;
using System.Collections;

public class FreezeEffect : NetworkBehaviour
{
    [SyncVar]
    private float freezeDuration;
    [SyncVar]
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
        RpcShowFreezeEffect();
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
    void RpcShowFreezeEffect()
    {
        // Create visual freeze effect (ice-blue tint and particles)
        StartCoroutine(FreezeVisualEffect());
    }
    
    IEnumerator FreezeVisualEffect()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Color freezeColor = Color.Lerp(originalColor, Color.cyan, 0.7f);
        
        float elapsed = 0f;
        while (elapsed < freezeDuration && isFrozen)
        {
            // Apply ice-blue tint to indicate freezing
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(freezeColor, Color.white, 
                    0.3f + 0.7f * Mathf.Sin(Time.time * 5f));
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
        
        // End freeze effect
        isFrozen = false;
        RestoreOriginalState();
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
    
    [ClientRpc]
    void RpcEndFreezeEffect()
    {
        // Clean up any visual effects
        StopAllCoroutines();
        
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
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