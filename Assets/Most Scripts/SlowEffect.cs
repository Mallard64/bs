using UnityEngine;
using Mirror;
using System.Collections;

public class SlowEffect : NetworkBehaviour
{
    [SyncVar]
    private float slowMultiplier = 1f;
    [SyncVar]
    private float slowDuration;
    [SyncVar(hook = nameof(OnSlowedStateChanged))]
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
        try
        {
            if (isServer && NetworkServer.active)
            {
                RpcShowSlowEffect(slowDuration);
            }
            else
            {
                // Fallback: show effect locally if networking isn't available
                StartCoroutine(SlowVisualEffect(slowDuration));
            }
        }
        catch
        {
            // Fallback: show effect locally if any networking error occurs
            StartCoroutine(SlowVisualEffect(slowDuration));
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
    void RpcShowSlowEffect(float duration)
    {
        // Create visual slow effect visible to ALL clients (including the affected player)
        StartCoroutine(SlowVisualEffect(duration));
        
        // Add extra visual effects for OTHER players to clearly see this player is slowed
        if (!isLocalPlayer)
        {
            Debug.Log($"🟣 Other players can see {gameObject.name} is slowed with enhanced slow effect");
            StartCoroutine(EnhancedSlowEffectForOthers(duration));
        }
        else
        {
            Debug.Log($"🟣 You are slowed and can see your own slow effect");
        }
    }
    
    IEnumerator SlowVisualEffect(float duration)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        
        float elapsed = 0f;
        while (elapsed < duration && isSlowed)
        {
            // Enhanced slow effect with purple/blue gradient and pulsing to show lethargy
            if (spriteRenderer != null)
            {
                float time = Time.time;
                float slowPulse = 0.5f + 0.5f * Mathf.Sin(time * 1.5f);      // Very slow pulse (sluggish feeling)
                float heavyBreath = 0.3f + 0.4f * Mathf.Sin(time * 0.8f);    // Even slower breathing effect
                float sluggishWave = 0.2f + 0.3f * Mathf.Sin(time * 0.5f);   // Ultra slow wave (emphasizes slowness)
                
                // Create colors that suggest weight/sluggishness - darker blues and purples
                Color deepBlue = new Color(0.3f, 0.5f, 0.9f, 1f);            // Deep blue
                Color slugPurple = new Color(0.5f, 0.3f, 0.8f, 1f);          // Sluggish purple
                Color heavyGray = new Color(0.6f, 0.6f, 0.8f, 1f);           // Heavy grayish-blue
                
                // Layer the effects to create a "weighed down" appearance
                Color baseColor = Color.Lerp(deepBlue, slugPurple, slowPulse);
                Color weightedColor = Color.Lerp(baseColor, heavyGray, heavyBreath * 0.4f);
                
                // Apply slow intensity based on the slow multiplier (stronger slow = more intense effect)
                float slowIntensity = Mathf.Lerp(0.4f, 0.7f, 1f - slowMultiplier);  // Slower = more intense
                Color finalColor = Color.Lerp(originalColor, weightedColor, slowIntensity + sluggishWave * 0.1f);
                
                // Slightly darken to emphasize the "heavy" feeling
                finalColor.r *= 0.9f;
                finalColor.g *= 0.9f;
                finalColor.b = Mathf.Min(1f, finalColor.b * 1.1f); // Keep blue component strong
                
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
    IEnumerator SlowRoutine()
    {
        yield return new WaitForSeconds(slowDuration);
        
        // End slow effect
        isSlowed = false;
        RestoreOriginalSpeed();
        try
        {
            if (isServer && NetworkServer.active)
            {
                RpcEndSlowEffect();
            }
        }
        catch
        {
            // Skip end effect RPC if networking fails
        }
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
    
    IEnumerator EnhancedSlowEffectForOthers(float duration)
    {
        // More dramatic visual effects for other players to clearly see this player is slowed
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        GameObject slowEffect = null;
        
        // Create slow motion "aura" effect for other players
        if (spriteRenderer != null)
        {
            // Create a slow motion visual indicator
            slowEffect = new GameObject("SlowEffect");
            slowEffect.transform.SetParent(transform);
            slowEffect.transform.localPosition = Vector3.down * 0.3f; // Below the player
            
            SpriteRenderer slowSprite = slowEffect.AddComponent<SpriteRenderer>();
            slowSprite.color = new Color(0.5f, 0.3f, 0.8f, 0.4f); // Purple translucent
            slowSprite.sprite = spriteRenderer.sprite; // Use same sprite but purple
            slowEffect.transform.localScale = Vector3.one * 0.8f;
        }
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Slow pulsing effect for other players
            if (slowEffect != null)
            {
                float pulse = Mathf.Sin(Time.time * 2f) * 0.2f + 0.8f; // Slower pulse to represent slowness
                slowEffect.transform.localScale = Vector3.one * (0.6f + pulse * 0.2f);
                
                // Fade the slow aura
                var slowSprite = slowEffect.GetComponent<SpriteRenderer>();
                if (slowSprite != null)
                {
                    slowSprite.color = new Color(0.5f, 0.3f, 0.8f, 0.2f + pulse * 0.2f);
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Clean up slow effect
        if (slowEffect != null)
        {
            Destroy(slowEffect);
        }
    }
    
    // SyncVar hook - called when isSlowed changes
    void OnSlowedStateChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"🟣 Slowed state changed from {oldValue} to {newValue} for {gameObject.name}");
        
        if (!newValue && oldValue) // Just stopped being slowed
        {
            // Make sure visual effects are cleaned up on client
            StopAllCoroutines();
            
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            
            // Clean up any slow effects
            Transform slowEffect = transform.Find("SlowEffect");
            if (slowEffect != null)
            {
                Destroy(slowEffect.gameObject);
            }
        }
    }
    
    [ClientRpc]
    void RpcEndSlowEffect()
    {
        Debug.Log($"🟣 Client: Ending slow effect for {gameObject.name}");
        
        // Clean up any visual effects
        StopAllCoroutines();
        
        // Force end slow state on client
        isSlowed = false;
        
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        
        // Clean up any slow effects for other players
        Transform slowEffect = transform.Find("SlowEffect");
        if (slowEffect != null)
        {
            Destroy(slowEffect.gameObject);
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