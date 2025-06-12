using UnityEngine;
using System.Collections;
using Mirror;

public class ExplosiveBarrel : NetworkBehaviour
{
    [Header("Explosion Settings")]
    public float explosionRadius = 5f;
    public float explosionDamage = 50f;
    public float explosionForce = 15f;
    public float chainReactionDelay = 0.1f;
    
    [Header("Visual Effects")]
    public GameObject explosionEffectPrefab;
    public GameObject fireEffectPrefab;
    public Color warningColor = Color.red;
    public float warningDuration = 1f;
    
    [Header("Audio")]
    public AudioClip explosionSound;
    public AudioClip warningSound;
    
    [SyncVar]
    private bool hasExploded = false;
    [SyncVar]
    private bool isWarning = false;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine warningCoroutine;
    private AudioSource audioSource;
    
    // Health system
    [SyncVar]
    private float currentHealth = 100f;
    private float maxHealth = 100f;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        // Add Hittable component if it doesn't exist
        if (GetComponent<Hittable>() == null)
        {
            gameObject.AddComponent<Hittable>();
        }
        
        currentHealth = maxHealth;
    }
    
    [Server]
    public void TakeDamage(int damage)
    {
        if (hasExploded) return;
        
        currentHealth -= damage;
        
        // Show damage effect
        RpcShowDamageEffect();
        
        if (currentHealth <= 0)
        {
            TriggerExplosion();
        }
        else if (currentHealth <= maxHealth * 0.3f && !isWarning)
        {
            // Start warning when health is low
            StartWarning();
        }
    }
    
    // Implement Hittable interface - float version
    public void TakeDamage(float damage)
    {
        if (isServer)
        {
            // Call the server method directly to avoid recursion
            currentHealth -= damage;
            
            // Show damage effect
            RpcShowDamageEffect();
            
            if (currentHealth <= 0)
            {
                TriggerExplosion();
            }
            else if (currentHealth <= maxHealth * 0.3f && !isWarning)
            {
                StartWarning();
            }
        }
    }
    
    [Server]
    void StartWarning()
    {
        isWarning = true;
        RpcStartWarning();
        
        // Auto-explode after warning duration
        Invoke(nameof(TriggerExplosion), warningDuration);
    }
    
    [Server]
    void TriggerExplosion()
    {
        if (hasExploded) return;
        
        hasExploded = true;
        
        // Find all objects in explosion radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        
        foreach (Collider2D collider in colliders)
        {
            // Damage hittable objects
            Hittable hittable = collider.GetComponent<Hittable>();
            if (hittable != null && collider.gameObject != gameObject)
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                int actualDamage = Mathf.RoundToInt(explosionDamage * damageMultiplier);
                
                // Try to damage the object using various methods
                var explosiveBarrel = collider.GetComponent<ExplosiveBarrel>();
                if (explosiveBarrel != null)
                {
                    explosiveBarrel.TakeDamage(actualDamage);
                }
                else
                {
                    // Try other components that might have TakeDamage
                    var damageableComponents = collider.GetComponents<MonoBehaviour>();
                    foreach (var comp in damageableComponents)
                    {
                        var method = comp.GetType().GetMethod("TakeDamage", new[] { typeof(int) });
                        if (method != null)
                        {
                            method.Invoke(comp, new object[] { actualDamage });
                            break;
                        }
                    }
                }
            }
            
            // Apply physics force
            Rigidbody2D rb = collider.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 direction = (collider.transform.position - transform.position).normalized;
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                float forceMultiplier = 1f - (distance / explosionRadius);
                
                rb.AddForce(direction * explosionForce * forceMultiplier, ForceMode2D.Impulse);
            }
            
            // Chain reaction with other explosive barrels
            ExplosiveBarrel otherBarrel = collider.GetComponent<ExplosiveBarrel>();
            if (otherBarrel != null && !otherBarrel.hasExploded && otherBarrel != this)
            {
                // Delay for dramatic effect
                otherBarrel.Invoke(nameof(otherBarrel.TriggerExplosion), chainReactionDelay);
            }
        }
        
        // Show explosion effects
        RpcExplode();
        
        // Screen shake
        ScreenShake.ShakeExplosion(0.5f);
        
        // Destroy the barrel after explosion
        Invoke(nameof(DestroyBarrel), 2f);
    }
    
    [ClientRpc]
    void RpcStartWarning()
    {
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }
        warningCoroutine = StartCoroutine(WarningEffect());
        
        // Play warning sound
        if (audioSource != null && warningSound != null)
        {
            audioSource.PlayOneShot(warningSound);
        }
    }
    
    [ClientRpc]
    void RpcShowDamageEffect()
    {
        // Flash white when damaged
        StartCoroutine(DamageFlash());
    }
    
    [ClientRpc]
    void RpcExplode()
    {
        // Spawn explosion effect
        if (explosionEffectPrefab != null)
        {
            GameObject explosion = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            Destroy(explosion, 3f);
        }
        
        // Hide the barrel sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // Play explosion sound
        if (audioSource != null && explosionSound != null)
        {
            audioSource.PlayOneShot(explosionSound);
        }
        
        // Spawn fire effect
        if (fireEffectPrefab != null)
        {
            GameObject fire = Instantiate(fireEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fire, 5f);
        }
    }
    
    IEnumerator WarningEffect()
    {
        float elapsed = 0f;
        
        while (elapsed < warningDuration && !hasExploded)
        {
            // Pulsing red warning effect
            float intensity = Mathf.Sin(elapsed * 20f) * 0.5f + 0.5f;
            Color currentColor = Color.Lerp(originalColor, warningColor, intensity);
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = currentColor;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    
    IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.white;
            
            yield return new WaitForSeconds(0.1f);
            
            spriteRenderer.color = originalColor;
        }
    }
    
    void DestroyBarrel()
    {
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
    
    
    // Show explosion radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius * 0.5f);
    }
    
    // Static method to create explosive barrels
    public static GameObject CreateExplosiveBarrel(Vector3 position)
    {
        GameObject barrel = new GameObject("ExplosiveBarrel");
        barrel.transform.position = position;
        
        // Add components
        barrel.AddComponent<SpriteRenderer>();
        barrel.AddComponent<CircleCollider2D>();
        barrel.AddComponent<Rigidbody2D>();
        barrel.AddComponent<AudioSource>();
        barrel.AddComponent<NetworkIdentity>();
        barrel.AddComponent<Hittable>(); // Add Hittable component
        barrel.AddComponent<ExplosiveBarrel>();
        
        return barrel;
    }
}