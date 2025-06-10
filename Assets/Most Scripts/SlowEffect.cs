using UnityEngine;
using Mirror;
using System.Collections;

public class SlowEffect : NetworkBehaviour
{
    [SyncVar]
    private float slowMultiplier = 1f;
    [SyncVar]
    private float slowDuration;
    [SyncVar]
    private bool isSlowed = false;
    
    private Coroutine slowCoroutine;
    private Enemy targetEnemy;
    private ActualEnemy targetActualEnemy;
    private PlayerMovement playerMovement;
    private BotPlayer botPlayer;
    private float originalMoveSpeed;
    private bool originalSpeedStored = false;
    
    void Start()
    {
        targetEnemy = GetComponent<Enemy>();
        targetActualEnemy = GetComponent<ActualEnemy>();
        playerMovement = GetComponent<PlayerMovement>();
        botPlayer = GetComponent<BotPlayer>();
    }
    
    [Server]
    public void ApplySlow(float multiplier, float duration)
    {
        if (isSlowed)
        {
            // Refresh slow duration if already slowed, use strongest slow effect
            slowDuration = Mathf.Max(slowDuration, duration);
            slowMultiplier = Mathf.Min(slowMultiplier, multiplier);
        }
        else
        {
            slowMultiplier = multiplier;
            slowDuration = duration;
            isSlowed = true;
            
            // Store original speed
            StoreOriginalSpeed();
            
            if (slowCoroutine != null)
            {
                StopCoroutine(slowCoroutine);
            }
            slowCoroutine = StartCoroutine(SlowRoutine());
        }
        
        // Apply speed reduction immediately
        ApplySpeedReduction();
        
        // Show slow effect to all clients
        RpcShowSlowEffect();
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
    
    void ApplySpeedReduction()
    {
        if (playerMovement != null)
        {
            playerMovement.moveSpeed = originalMoveSpeed * slowMultiplier;
        }
        else if (botPlayer != null)
        {
            botPlayer.moveSpeed = originalMoveSpeed * slowMultiplier;
        }
    }
    
    [ClientRpc]
    void RpcShowSlowEffect()
    {
        // Create visual slow effect (simple blue tint)
        StartCoroutine(SlowVisualEffect());
    }
    
    IEnumerator SlowVisualEffect()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        Color slowColor = Color.Lerp(originalColor, Color.cyan, 0.3f);
        
        float elapsed = 0f;
        while (elapsed < slowDuration && isSlowed)
        {
            // Apply blue tint to indicate slowing
            if (spriteRenderer != null)
            {
                spriteRenderer.color = slowColor;
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
    IEnumerator SlowRoutine()
    {
        yield return new WaitForSeconds(slowDuration);
        
        // End slow effect
        isSlowed = false;
        RestoreOriginalSpeed();
        RpcEndSlowEffect();
    }
    
    void RestoreOriginalSpeed()
    {
        if (originalSpeedStored)
        {
            if (playerMovement != null)
            {
                playerMovement.moveSpeed = originalMoveSpeed;
            }
            else if (botPlayer != null)
            {
                botPlayer.moveSpeed = originalMoveSpeed;
            }
        }
    }
    
    [ClientRpc]
    void RpcEndSlowEffect()
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
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }
        
        // Restore speed when component is destroyed
        if (isSlowed)
        {
            RestoreOriginalSpeed();
        }
    }
}