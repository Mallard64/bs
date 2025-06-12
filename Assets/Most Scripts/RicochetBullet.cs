using UnityEngine;
using Mirror;
using System.Collections;

public class RicochetBullet : NetworkBehaviour
{
    [Header("Ricochet Settings")]
    public int maxBounces = 3;
    public float speedMultiplierPerBounce = 0.9f; // Bullet slows down with each bounce
    public float damageMultiplierPerBounce = 0.8f; // Damage reduces with each bounce
    public LayerMask wallLayerMask = 1; // Which layers count as walls
    public bool seekEnemiesAfterBounce = true;
    public float enemySeekRange = 5f;
    
    [Header("Visual Effects")]
    public GameObject bounceEffectPrefab;
    public TrailRenderer bulletTrail;
    public Color bounceColor = Color.yellow;
    
    private int currentBounces = 0;
    private Rigidbody2D rb;
    private Bullet bulletComponent;
    private float originalSpeed;
    private float originalDamage;
    private Vector2 lastVelocity;
    
    // Audio
    private AudioSource audioSource;
    public AudioClip bounceSound;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        bulletComponent = GetComponent<Bullet>();
        audioSource = GetComponent<AudioSource>();
        
        if (rb != null)
        {
            originalSpeed = rb.velocity.magnitude;
            lastVelocity = rb.velocity;
        }
        
        if (bulletComponent != null)
        {
            originalDamage = bulletComponent.damage;
        }
        
        // Set up trail renderer
        if (bulletTrail != null)
        {
            bulletTrail.material.color = Color.white;
        }
    }
    
    void FixedUpdate()
    {
        if (rb != null)
        {
            lastVelocity = rb.velocity;
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if we hit a wall (not an enemy or other bullet)
        if (IsWall(collision.gameObject) && currentBounces < maxBounces)
        {
            PerformRicochet(collision);
        }
    }
    
    bool IsWall(GameObject obj)
    {
        // Check if the object is on the wall layer
        return ((1 << obj.layer) & wallLayerMask) != 0;
    }
    
    [Server]
    void PerformRicochet(Collision2D collision)
    {
        currentBounces++;
        
        // Calculate reflection vector
        Vector2 incomingVector = lastVelocity.normalized;
        Vector2 wallNormal = collision.contacts[0].normal;
        Vector2 reflectedVector = Vector2.Reflect(incomingVector, wallNormal);
        
        // Apply speed reduction
        float newSpeed = originalSpeed * Mathf.Pow(speedMultiplierPerBounce, currentBounces);
        
        // Check for enemy seeking after bounce
        if (seekEnemiesAfterBounce && currentBounces > 0)
        {
            Vector2 enemyDirection = FindNearestEnemy();
            if (enemyDirection != Vector2.zero)
            {
                // Blend reflected direction with enemy direction
                reflectedVector = Vector2.Lerp(reflectedVector, enemyDirection, 0.6f).normalized;
            }
        }
        
        // Apply new velocity
        rb.velocity = reflectedVector * newSpeed;
        
        // Reduce damage
        if (bulletComponent != null)
        {
            bulletComponent.damage = originalDamage * Mathf.Pow(damageMultiplierPerBounce, currentBounces);
        }
        
        // Visual and audio effects
        RpcPlayBounceEffects(collision.contacts[0].point, wallNormal);
        
        // Change bullet appearance after bounces
        UpdateBulletAppearance();
        
        // Screen shake for dramatic bounces
        if (currentBounces >= 2)
        {
            ScreenShake.Shake(0.1f, 0.05f * currentBounces);
        }
    }
    
    Vector2 FindNearestEnemy()
    {
        Collider2D[] enemies = Physics2D.OverlapCircleAll(transform.position, enemySeekRange);
        
        float closestDistance = float.MaxValue;
        Vector2 direction = Vector2.zero;
        
        foreach (Collider2D enemy in enemies)
        {
            // Check if it's an enemy (has Hittable component and isn't the shooter)
            if (enemy.GetComponent<Hittable>() != null && enemy.gameObject != gameObject)
            {
                float distance = Vector2.Distance(transform.position, enemy.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    direction = (enemy.transform.position - transform.position).normalized;
                }
            }
        }
        
        return direction;
    }
    
    [ClientRpc]
    void RpcPlayBounceEffects(Vector2 bouncePoint, Vector2 normal)
    {
        // Spawn bounce effect
        if (bounceEffectPrefab != null)
        {
            GameObject effect = Instantiate(bounceEffectPrefab, bouncePoint, Quaternion.LookRotation(normal));
            Destroy(effect, 2f);
        }
        
        // Play bounce sound
        if (audioSource != null && bounceSound != null)
        {
            audioSource.pitch = Random.Range(0.8f, 1.2f); // Vary pitch for more interesting sound
            audioSource.PlayOneShot(bounceSound);
        }
        
        // Screen flash effect
        StartCoroutine(BounceFlashEffect());
    }
    
    IEnumerator BounceFlashEffect()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = bounceColor;
            
            yield return new WaitForSeconds(0.1f);
            
            spriteRenderer.color = originalColor;
        }
    }
    
    void UpdateBulletAppearance()
    {
        // Update trail color based on bounces
        if (bulletTrail != null)
        {
            Color trailColor = Color.Lerp(Color.white, bounceColor, (float)currentBounces / maxBounces);
            bulletTrail.material.color = trailColor;
            
            // Make trail more prominent after bounces
            bulletTrail.widthMultiplier = 1f + (currentBounces * 0.2f);
        }
        
        // Scale bullet slightly with each bounce
        transform.localScale = Vector3.one * (1f + currentBounces * 0.1f);
    }
    
    // Method to make any bullet a ricochet bullet
    public static void MakeRicochet(GameObject bullet, int maxBounces = 3)
    {
        if (bullet.GetComponent<RicochetBullet>() == null)
        {
            RicochetBullet ricochet = bullet.AddComponent<RicochetBullet>();
            ricochet.maxBounces = maxBounces;
            
            // Add trail renderer if it doesn't exist
            if (bullet.GetComponent<TrailRenderer>() == null)
            {
                TrailRenderer trail = bullet.AddComponent<TrailRenderer>();
                trail.time = 0.3f;
                trail.widthMultiplier = 0.1f;
                trail.material = new Material(Shader.Find("Sprites/Default"));
                trail.material.color = Color.yellow;
                ricochet.bulletTrail = trail;
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Show enemy seek range in editor
        if (seekEnemiesAfterBounce)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, enemySeekRange);
        }
    }
}