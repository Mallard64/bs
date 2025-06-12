using UnityEngine;
using Mirror;
using System.Collections;

public class BurnEffect : NetworkBehaviour
{
    [SyncVar]
    private float burnDamage;
    [SyncVar]
    private float burnDuration;
    [SyncVar]
    private float burnInterval;
    [SyncVar(hook = nameof(OnBurningStateChanged))]
    private bool isBurning = false;
    
    private Coroutine burnCoroutine;
    private Enemy targetEnemy;
    private ActualEnemy targetActualEnemy;
    
    void Start()
    {
        targetEnemy = GetComponent<Enemy>();
        targetActualEnemy = GetComponent<ActualEnemy>();
    }
    
    [Server]
    public void ApplyBurn(float damage, float duration, float interval)
    {
        if (isBurning)
        {
            // Refresh burn duration if already burning
            burnDuration = Mathf.Max(burnDuration, duration);
            burnDamage = Mathf.Max(burnDamage, damage);
        }
        else
        {
            burnDamage = damage;
            burnDuration = duration;
            burnInterval = interval;
            isBurning = true;
            
            if (burnCoroutine != null)
            {
                StopCoroutine(burnCoroutine);
            }
            burnCoroutine = StartCoroutine(BurnRoutine());
        }
        
        // Show burn effect to all clients (including owner)
        try
        {
            if (isServer && NetworkServer.active)
            {
                RpcShowBurnEffect(burnDuration);
            }
            else
            {
                // Fallback: show effect locally if networking isn't available
                StartCoroutine(BurnVisualEffect(burnDuration));
            }
        }
        catch
        {
            // Fallback: show effect locally if any networking error occurs
            StartCoroutine(BurnVisualEffect(burnDuration));
        }
    }
    
    [ClientRpc]
    void RpcShowBurnEffect(float duration)
    {
        // Create visual burn effect visible to ALL clients (including the affected player)
        StartCoroutine(BurnVisualEffect(duration));
        
        // Add extra visual effects for OTHER players to clearly see this player is burning
        if (!isLocalPlayer)
        {
            Debug.Log($"🔥 Other players can see {gameObject.name} is burning with enhanced fire effect");
            StartCoroutine(EnhancedBurnEffectForOthers(duration));
        }
        else
        {
            Debug.Log($"🔥 You are burning and can see your own burn effect");
        }
    }
    
    IEnumerator BurnVisualEffect(float duration)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        
        float elapsed = 0f;
        while (elapsed < duration && isBurning)
        {
            // Enhanced burn effect with multiple colors and intensity variation
            if (spriteRenderer != null)
            {
                float time = Time.time;
                float fastFlash = 0.5f + 0.5f * Mathf.Sin(time * 15f); // Fast red flash
                float slowPulse = 0.3f + 0.7f * Mathf.Sin(time * 3f);  // Slow intensity pulse
                float orangeFlash = 0.3f + 0.4f * Mathf.Sin(time * 8f); // Orange undertone
                
                // Mix between red and orange for more realistic fire effect
                Color fireColor = Color.Lerp(new Color(1f, 0.3f, 0f, 1f), Color.red, fastFlash);
                Color finalColor = Color.Lerp(originalColor, fireColor, slowPulse * 0.8f);
                
                // Add slight brightness boost during intense moments
                finalColor.r = Mathf.Min(1f, finalColor.r + orangeFlash * 0.2f);
                finalColor.g = Mathf.Min(1f, finalColor.g + orangeFlash * 0.1f);
                
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
    IEnumerator BurnRoutine()
    {
        float elapsed = 0f;
        
        while (elapsed < burnDuration && isBurning)
        {
            yield return new WaitForSeconds(burnInterval);
            elapsed += burnInterval;
            
            // Apply burn damage
            if (targetEnemy != null)
            {
                targetEnemy.TakeDamage((int)burnDamage);
            }
            else if (targetActualEnemy != null)
            {
                targetActualEnemy.TakeDamage((int)burnDamage);
            }
            
            // Show burn damage effect
            try
            {
                if (isServer && NetworkServer.active)
                {
                    RpcShowBurnDamage((int)burnDamage);
                }
            }
            catch
            {
                // Skip damage text if networking fails
            }
        }
        
        // End burn effect
        isBurning = false;
        try
        {
            if (isServer && NetworkServer.active)
            {
                RpcEndBurnEffect();
            }
        }
        catch
        {
            // Skip end effect RPC if networking fails
        }
    }
    
    [ClientRpc(includeOwner = true)]
    void RpcShowBurnDamage(int damage)
    {
        // Create floating damage text for burn damage
        var damageTextPrefab = Resources.Load<GameObject>("DamageText");
        if (damageTextPrefab != null)
        {
            var damageText = Instantiate(damageTextPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            var textMesh = damageText.GetComponent<TMPro.TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = $"{damage}";
                textMesh.color = Color.red;
                textMesh.fontSize = 3f;
            }
            
            StartCoroutine(AnimateBurnDamageText(damageText));
        }
    }
    
    IEnumerator EnhancedBurnEffectForOthers(float duration)
    {
        // More dramatic visual effects for other players to clearly see this player is on fire
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        GameObject fireParticleEffect = null;
        
        // Create a simple fire effect using a colored GameObject as a particle
        if (spriteRenderer != null)
        {
            // Create a small fire effect above the player
            fireParticleEffect = new GameObject("FireEffect");
            fireParticleEffect.transform.SetParent(transform);
            fireParticleEffect.transform.localPosition = Vector3.up * 0.5f;
            
            SpriteRenderer fireSprite = fireParticleEffect.AddComponent<SpriteRenderer>();
            fireSprite.color = Color.red;
            fireSprite.sprite = spriteRenderer.sprite; // Use same sprite but smaller and red
            fireParticleEffect.transform.localScale = Vector3.one * 0.3f;
        }
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Pulsing fire effect for other players
            if (fireParticleEffect != null)
            {
                float pulse = Mathf.Sin(Time.time * 8f) * 0.5f + 0.5f;
                fireParticleEffect.transform.localScale = Vector3.one * (0.2f + pulse * 0.2f);
                
                // Flicker the fire sprite
                var fireSprite = fireParticleEffect.GetComponent<SpriteRenderer>();
                if (fireSprite != null)
                {
                    fireSprite.color = Color.Lerp(Color.red, Color.yellow, pulse);
                }
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Clean up fire effect
        if (fireParticleEffect != null)
        {
            Destroy(fireParticleEffect);
        }
    }
    
    // SyncVar hook - called when isBurning changes
    void OnBurningStateChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"🔥 Burning state changed from {oldValue} to {newValue} for {gameObject.name}");
        
        if (!newValue && oldValue) // Just stopped burning
        {
            // Make sure visual effects are cleaned up on client
            StopAllCoroutines();
            
            SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            
            // Clean up any fire effects
            Transform fireEffect = transform.Find("FireEffect");
            if (fireEffect != null)
            {
                Destroy(fireEffect.gameObject);
            }
        }
    }
    
    [ClientRpc]
    void RpcEndBurnEffect()
    {
        Debug.Log($"🔥 Client: Ending burn effect for {gameObject.name}");
        
        // Clean up any visual effects
        StopAllCoroutines();
        
        // Force end burn state on client
        isBurning = false;
        
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        
        // Clean up any fire effects for other players
        Transform fireEffect = transform.Find("FireEffect");
        if (fireEffect != null)
        {
            Destroy(fireEffect.gameObject);
        }
    }
    
    IEnumerator AnimateBurnDamageText(GameObject damageText)
    {
        Vector3 startPos = damageText.transform.position;
        Vector3 endPos = startPos + Vector3.up * 1f;
        
        float duration = 1f;
        float elapsed = 0f;
        
        var textMesh = damageText.GetComponent<TMPro.TextMeshPro>();
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            damageText.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            if (textMesh != null)
            {
                Color color = textMesh.color;
                color.a = 1f - t;
                textMesh.color = color;
            }
            
            yield return null;
        }
        
        Destroy(damageText);
    }
    
    void OnDestroy()
    {
        if (burnCoroutine != null)
        {
            StopCoroutine(burnCoroutine);
        }
    }
}