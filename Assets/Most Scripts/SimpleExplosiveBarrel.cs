using UnityEngine;
using System.Collections;
using Mirror;

// Simplified explosive barrel that doesn't rely on Hittable inheritance
public class SimpleExplosiveBarrel : NetworkBehaviour
{
    [Header("Explosion Settings")]
    public float explosionRadius = 5f;
    public float explosionDamage = 50f;
    public float explosionForce = 15f;
    
    [Header("Health")]
    public float maxHealth = 100f;
    
    [SyncVar]
    private float currentHealth = 100f;
    [SyncVar]
    private bool hasExploded = false;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
        
        currentHealth = maxHealth;
        
        // Make sure we have a Hittable component for compatibility
        if (GetComponent<Hittable>() == null)
        {
            gameObject.AddComponent<Hittable>();
        }
    }
    
    // Public method that can be called by bullets or other damage sources
    public void TakeDamage(int damage)
    {
        if (isServer)
        {
            TakeDamageServer(damage);
        }
    }
    
    public void TakeDamage(float damage)
    {
        if (isServer)
        {
            TakeDamageServer(Mathf.RoundToInt(damage));
        }
    }
    
    [Server]
    void TakeDamageServer(int damage)
    {
        if (hasExploded) return;
        
        currentHealth -= damage;
        
        // Show damage effect
        RpcShowDamageEffect();
        
        if (currentHealth <= 0)
        {
            Explode();
        }
    }
    
    [Server]
    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        // Find all objects in explosion radius
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
        
        foreach (Collider2D collider in colliders)
        {
            // Try to damage objects that can take damage
            var barrel = collider.GetComponent<SimpleExplosiveBarrel>();
            if (barrel != null && barrel != this && !barrel.hasExploded)
            {
                barrel.TakeDamage(25); // Chain reaction damage
                continue;
            }
            
            // Try other damage methods
            var hittable = collider.GetComponent<Hittable>();
            if (hittable != null)
            {
                float distance = Vector2.Distance(transform.position, collider.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                int actualDamage = Mathf.RoundToInt(explosionDamage * damageMultiplier);
                
                // Try to call TakeDamage if the method exists
                var damageMethod = hittable.GetType().GetMethod("TakeDamage", new[] { typeof(int) });
                if (damageMethod != null)
                {
                    damageMethod.Invoke(hittable, new object[] { actualDamage });
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
        }
        
        // Show explosion effects
        RpcExplode();
        
        // Screen shake
        if (ScreenShake.Instance != null)
        {
            ScreenShake.ShakeExplosion(0.5f);
        }
        
        // Destroy after a delay
        Invoke(nameof(DestroyBarrel), 2f);
    }
    
    [ClientRpc]
    void RpcShowDamageEffect()
    {
        StartCoroutine(DamageFlash());
    }
    
    [ClientRpc]
    void RpcExplode()
    {
        // Hide sprite
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // You can add explosion effects here
        Debug.Log($"💥 Barrel exploded at {transform.position}!");
    }
    
    IEnumerator DamageFlash()
    {
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            spriteRenderer.color = original;
        }
    }
    
    void DestroyBarrel()
    {
        if (isServer)
        {
            NetworkServer.Destroy(gameObject);
        }
    }
    
    // Static method to create a simple barrel
    public static GameObject CreateSimpleBarrel(Vector3 position)
    {
        GameObject barrel = new GameObject("SimpleExplosiveBarrel");
        barrel.transform.position = position;
        
        // Add basic components
        var sprite = barrel.AddComponent<SpriteRenderer>();
        sprite.color = Color.red; // Temporary red color
        
        var collider = barrel.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        
        var rb = barrel.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        
        barrel.AddComponent<Hittable>();
        barrel.AddComponent<NetworkIdentity>();
        barrel.AddComponent<SimpleExplosiveBarrel>();
        
        return barrel;
    }
    
    // Show explosion radius in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}