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
    [SyncVar]
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
        
        // Show burn effect to all clients
        RpcShowBurnEffect();
    }
    
    [ClientRpc]
    void RpcShowBurnEffect()
    {
        // Create visual burn effect (simple particle system or color change)
        StartCoroutine(BurnVisualEffect());
    }
    
    IEnumerator BurnVisualEffect()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        
        float elapsed = 0f;
        while (elapsed < burnDuration && isBurning)
        {
            // Flash red to indicate burning
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(originalColor, Color.red, 
                    0.5f + 0.5f * Mathf.Sin(Time.time * 10f));
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
            RpcShowBurnDamage((int)burnDamage);
        }
        
        // End burn effect
        isBurning = false;
        RpcEndBurnEffect();
    }
    
    [ClientRpc]
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
    
    [ClientRpc]
    void RpcEndBurnEffect()
    {
        // Clean up any visual effects
        StopAllCoroutines();
        
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
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