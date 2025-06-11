using UnityEngine;
using Mirror;
using System.Collections;
using TMPro;

public enum BulletEffectType
{
    None,
    BurnDamage,
    SlowEffect,
    FreezeEffect,
    LightningBolt,
    ExplosionDamage,
    PoisonDamage,
    GravityPull
}

public class Bullet : NetworkBehaviour
{
    public int bulletDamage = 10;
    public float damage = 10f; // New damage property for combo system
    public float knockbackBase = 18f;
    public float knockbackScaling = 1.4f;
    public int shooterId;
    public bool shotByEnemy = false;  // Track if shot by enemy or player
    private Vector2 lastPosition;
    private Vector2 moveDirection;
    
    [Header("Special Effects")]
    public BulletEffectType effectType = BulletEffectType.None;
    public float effectDuration = 3f;
    public float effectDamage = 5f; // For burn damage
    public float slowAmount = 0.5f; // Multiplier for movement speed (0.5 = 50% speed)
    public float lightningRange = 2f; // Range for lightning bolt effect
    public int lightningTargets = 3; // Max targets for lightning chains
    
    [Header("Bullet Properties")]
    public bool isPiercing = false; // Bullet goes through enemies
    public bool canGoThroughWalls = false; // Bullet goes through walls
    public int maxPierceTargets = -1; // -1 = unlimited, 0+ = limited piercing
    private int pierceCount = 0;

    [SerializeField] private float outwardOffset = 5f;
    public GameObject parent;
    Rigidbody2D rb;

    void Awake() => rb = GetComponent<Rigidbody2D>();

    [ServerCallback]
    void OnCollisionEnter2D(Collision2D col)
    {
        // Try to get Enemy component (used for both players and enemies)
        var enemy = col.gameObject.GetComponent<Enemy>();
        var actualEnemy = col.gameObject.GetComponent<ActualEnemy>();

        bool shouldDamage = false;

        // Case 1: Bullet shot by player hits another player
        if (!shotByEnemy && enemy != null && enemy.connectionToClient != null &&
            !enemy.isEnemy && shooterId != enemy.connectionId)
        {
            shouldDamage = true;
            DamagePlayer(enemy, col);
        }
        // Case 2: Bullet shot by player hits an enemy NPC
        else if (!shotByEnemy && actualEnemy != null && actualEnemy.isEnemy)
        {
            shouldDamage = true;
            DamageActualEnemy(actualEnemy, col);
        }
        // Case 3: Bullet shot by enemy hits a player
        else if (shotByEnemy && enemy != null && enemy.connectionToClient != null && !enemy.isEnemy)
        {
            shouldDamage = true;
            DamagePlayer(enemy, col);
        }
        // Case 4: Bullet shot by enemy hits different enemy (optional friendly fire)
        else if (shotByEnemy && actualEnemy != null && actualEnemy.isEnemy && shooterId != actualEnemy.connectionId)
        {
            // Uncomment for enemy friendly fire:
            // shouldDamage = true;
            // DamageActualEnemy(actualEnemy, col);
        }
        // Case 5: Hit environment/walls
        else if (col.gameObject.CompareTag("Wall"))
        {
            // Only destroy if bullet can't go through walls
            if (!canGoThroughWalls)
            {
                NetworkServer.Destroy(gameObject);
                return;
            }
        }

        if (shouldDamage)
        {
            // 1) Grab the contact point
            Vector3 hitPos = col.contacts[0].point;

            // 2) Apply special effects
            ApplySpecialEffect(col.gameObject, hitPos);

            // 3) Handle piercing logic
            if (isPiercing)
            {
                pierceCount++;
                // Only destroy if we've hit max pierce targets (if limit is set)
                if (maxPierceTargets >= 0 && pierceCount >= maxPierceTargets)
                {
                    NetworkServer.Destroy(gameObject);
                }
                // If unlimited piercing or haven't hit limit, don't destroy
            }
            else
            {
                // Non-piercing bullets destroy on any hit
                NetworkServer.Destroy(gameObject);
            }
        }
    }

    void DamagePlayer(Enemy player, Collision2D col)
    {
        // Get damage multipliers from shooter
        float comboMultiplier = GetShooterComboMultiplier();
        float ultimateMultiplier = GetShooterUltimateMultiplier();
        float totalMultiplier = comboMultiplier * ultimateMultiplier;
        
        // 1) Calculate final damage with all multipliers
        float finalDamage = damage > 0 ? damage * totalMultiplier : bulletDamage * totalMultiplier;
        player.TakeDamage((int)finalDamage);
        
        // 2) Add overcharge for dealing damage
        AddShooterOvercharge(finalDamage);

        // 2) Knockback calculation using final damage
        float d = finalDamage;
        float w = player.weight;
        float dm = player.health;
        float baseComp = (d * 0.1f) + (d * dm / 20f);
        float kb = ((baseComp * knockbackScaling) * (200f / (w + 100f))) + knockbackBase;

        // 3) Get knockback direction
        Vector2 dir = GetKnockbackDirection(col);

        // 4) Apply knockback
        float stun = (kb * 0.4f) / 60f;
        player.ApplyKnockback(dir*kb, stun/10f);
        
        // 5) Special effects are now handled in main collision logic
        
        // 6) Show damage number if combo multiplier > 1
        if (comboMultiplier > 1f)
        {
            ShowComboHitEffect(player.transform.position, finalDamage, comboMultiplier);
        }
    }

    void DamageActualEnemy(ActualEnemy actualEnemy, Collision2D col)
    {
        // 1) Damage
        actualEnemy.TakeDamage(bulletDamage);

        // 2) Knockback calculation (same formula)
        float d = bulletDamage;
        float w = actualEnemy.weight;
        float dm = actualEnemy.health;
        float baseComp = (d * 0.1f) + (d * dm / 20f);
        float kb = ((baseComp * knockbackScaling) * (200f / (w + 100f))) + knockbackBase;

        // 3) Get knockback direction
        Vector2 dir = GetKnockbackDirection(col);

        // 4) Apply knockback (ClientRpc for enemies)
        float stun = (kb * 0.4f) / 60f;
        actualEnemy.ApplyKnockback(dir * kb, stun / 15f);
        
        // 5) Special effects are now handled in main collision logic
    }

    Vector2 GetKnockbackDirection(Collision2D col)
    {
        // Choose direction: parent movement > bullet velocity > collision normal
        if (parent != null && moveDirection.sqrMagnitude > 0.1f)
        {
            return moveDirection;
        }
        else if (rb.velocity.sqrMagnitude > 0.1f)
        {
            return rb.velocity.normalized;
        }
        else
        {
            return -col.contacts[0].normal;
        }
    }

    private void Update()
    {
        if (parent != null)
        {
            // 1) Get parent's current position
            Vector2 parentPos = parent.transform.position;

            // 2) Compute movement delta
            Vector2 delta = parentPos - lastPosition;
            if (delta.sqrMagnitude > 0.001f)
                moveDirection = delta.normalized;
            lastPosition = parentPos;

            // 3) Position bullet relative to parent
            transform.position = parentPos + moveDirection * outwardOffset;
        }
    }
    
    float GetShooterComboMultiplier()
    {
        // Find the shooter by connection ID and get their combo multiplier
        foreach (var player in FindObjectsOfType<Enemy>())
        {
            if (player.connectionId == shooterId)
            {
                var comboSystem = player.GetComponent<WeaponComboSystem>();
                if (comboSystem != null)
                {
                    return comboSystem.GetDamageMultiplier();
                }
                break;
            }
        }
        return 1f; // Default multiplier if no combo system found
    }
    
    float GetShooterUltimateMultiplier()
    {
        // Find the shooter by connection ID and get their ultimate multiplier
        foreach (var player in FindObjectsOfType<Enemy>())
        {
            if (player.connectionId == shooterId)
            {
                var overchargeSystem = player.GetComponent<WeaponOverchargeSystem>();
                if (overchargeSystem != null)
                {
                    return overchargeSystem.GetUltimateDamageMultiplier();
                }
                break;
            }
        }
        return 1f; // Default multiplier if no overcharge system found
    }
    
    void AddShooterOvercharge(float damageDealt)
    {
        // Find the shooter and add overcharge for damage dealt
        foreach (var player in FindObjectsOfType<Enemy>())
        {
            if (player.connectionId == shooterId)
            {
                var overchargeSystem = player.GetComponent<WeaponOverchargeSystem>();
                if (overchargeSystem != null)
                {
                    overchargeSystem.AddDamageOvercharge(damageDealt);
                }
                break;
            }
        }
    }
    
    [ClientRpc]
    void ShowComboHitEffect(Vector3 position, float damage, float multiplier)
    {
        // Create floating damage text with combo styling
        var damageTextPrefab = Resources.Load<GameObject>("DamageText");
        if (damageTextPrefab != null)
        {
            var damageText = Instantiate(damageTextPrefab, position + Vector3.up * 0.5f, Quaternion.identity);
            var textMesh = damageText.GetComponent<TextMeshPro>();
            if (textMesh != null)
            {
                textMesh.text = $"{damage:F0}";
                textMesh.color = multiplier >= 2.5f ? Color.red : 
                                multiplier >= 2f ? Color.magenta : 
                                multiplier >= 1.5f ? Color.yellow : Color.white;
                textMesh.fontSize = 4f + (multiplier - 1f) * 2f; // Scale with multiplier
            }
            
            // Animate the text
            StartCoroutine(AnimateDamageText(damageText));
        }
    }
    
    IEnumerator AnimateDamageText(GameObject damageText)
    {
        Vector3 startPos = damageText.transform.position;
        Vector3 endPos = startPos + Vector3.up * 2f;
        
        float duration = 1.5f;
        float elapsed = 0f;
        
        var textMesh = damageText.GetComponent<TextMeshPro>();
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Move upward
            damageText.transform.position = Vector3.Lerp(startPos, endPos, t);
            
            // Fade out
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
    
    void ApplySpecialEffect(GameObject target, Vector3 hitPosition)
    {
        switch (effectType)
        {
            case BulletEffectType.BurnDamage:
                ApplyBurnEffect(target);
                break;
            case BulletEffectType.SlowEffect:
                ApplySlowEffect(target);
                break;
            case BulletEffectType.FreezeEffect:
                ApplyFreezeEffect(target);
                break;
            case BulletEffectType.LightningBolt:
                ApplyLightningEffect(target, hitPosition);
                break;
            case BulletEffectType.ExplosionDamage:
                ApplyExplosionEffect(hitPosition);
                break;
            case BulletEffectType.PoisonDamage:
                ApplyPoisonEffect(target);
                break;
            case BulletEffectType.GravityPull:
                ApplyGravityEffect(target, hitPosition);
                break;
        }
    }
    
    [Server]
    void ApplyBurnEffect(GameObject target)
    {
        var burnEffect = target.GetComponent<BurnEffect>();
        if (burnEffect == null)
        {
            burnEffect = target.AddComponent<BurnEffect>();
        }
        burnEffect.ApplyBurn(effectDamage, effectDuration, 1f); // 1 second intervals
    }
    
    [Server]
    void ApplySlowEffect(GameObject target)
    {
        var slowEffect = target.GetComponent<SlowEffect>();
        if (slowEffect == null)
        {
            slowEffect = target.AddComponent<SlowEffect>();
        }
        slowEffect.ApplySlow(slowAmount, effectDuration);
    }
    
    [Server]
    void ApplyFreezeEffect(GameObject target)
    {
        var freezeEffect = target.GetComponent<FreezeEffect>();
        if (freezeEffect == null)
        {
            freezeEffect = target.AddComponent<FreezeEffect>();
        }
        freezeEffect.ApplyFreeze(effectDuration);
    }
    
    void ApplyLightningEffect(GameObject target, Vector3 position)
    {
        // Add random variance to lightning position
        Vector3 lightningPos = position + new Vector3(
            Random.Range(-lightningRange * 0.5f, lightningRange * 0.5f),
            Random.Range(-lightningRange * 0.5f, lightningRange * 0.5f),
            0f
        );
        
        // Create lightning bolt prefab at position with startup delay
        StartCoroutine(SpawnLightningBolt(lightningPos));
    }
    
    IEnumerator SpawnLightningBolt(Vector3 position)
    {
        // High startup delay for lightning
        yield return new WaitForSeconds(0.8f);
        
        // Find all enemies within lightning range
        Collider2D[] targets = Physics2D.OverlapCircleAll(position, lightningRange);
        int hitCount = 0;
        
        foreach (var collider in targets)
        {
            if (hitCount >= lightningTargets) break;
            
            var enemy = collider.GetComponent<Enemy>();
            var actualEnemy = collider.GetComponent<ActualEnemy>();
            
            bool shouldHit = false;
            
            // Check if valid target (same logic as bullet collision)
            if (!shotByEnemy && enemy != null && enemy.connectionToClient != null && 
                !enemy.isEnemy && shooterId != enemy.connectionId)
            {
                shouldHit = true;
                enemy.TakeDamage((int)effectDamage);
            }
            else if (!shotByEnemy && actualEnemy != null && actualEnemy.isEnemy)
            {
                shouldHit = true;
                actualEnemy.TakeDamage((int)effectDamage);
            }
            else if (shotByEnemy && enemy != null && enemy.connectionToClient != null && !enemy.isEnemy)
            {
                shouldHit = true;
                enemy.TakeDamage((int)effectDamage);
            }
            
            if (shouldHit)
            {
                hitCount++;
                // Spawn visual lightning effect
                RpcSpawnLightningVisual(position, collider.transform.position);
            }
        }
    }
    
    [ClientRpc]
    void RpcSpawnLightningVisual(Vector3 startPos, Vector3 endPos)
    {
        // Try to use lightning sprite first, fallback to LineRenderer
        var lightningSprite = Resources.Load<Sprite>("Effect 2 - Sprite Sheet_0"); // First frame of lightning
        
        if (lightningSprite != null)
        {
            // Create lightning bolt using sprite
            GameObject lightningBolt = new GameObject("LightningBolt");
            lightningBolt.transform.position = Vector3.Lerp(startPos, endPos, 0.5f); // Center between points
            
            var spriteRenderer = lightningBolt.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = lightningSprite;
            spriteRenderer.color = new Color(1f, 1f, 0.8f, 0.9f); // Bright yellow-white
            spriteRenderer.sortingOrder = 15;
            
            // Scale and rotate to match direction
            Vector3 direction = endPos - startPos;
            float distance = direction.magnitude;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            
            lightningBolt.transform.rotation = Quaternion.Euler(0, 0, angle);
            lightningBolt.transform.localScale = new Vector3(distance * 0.5f, 1.5f, 1f); // Scale to span distance
            
            // Animate with flickering effect
            StartCoroutine(AnimateLightningBolt(lightningBolt, 0.3f));
        }
        else
        {
            // Fallback to LineRenderer if sprite not found
            GameObject lightningLine = new GameObject("Lightning");
            LineRenderer lr = lightningLine.AddComponent<LineRenderer>();
            
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.color = Color.yellow;
            lr.startWidth = 0.15f;
            lr.endWidth = 0.08f;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.sortingOrder = 10;
            
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, endPos);
            
            StartCoroutine(DestroyLightningVisual(lightningLine, 0.2f));
        }
    }
    
    IEnumerator AnimateLightningBolt(GameObject lightningBolt, float duration)
    {
        var spriteRenderer = lightningBolt.GetComponent<SpriteRenderer>();
        Color originalColor = spriteRenderer.color;
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Flickering effect
            float flicker = Mathf.Sin(Time.time * 50f) * 0.3f + 0.7f;
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, 
                                           originalColor.a * flicker * (1f - t));
            
            // Slight random rotation for energy effect
            lightningBolt.transform.rotation *= Quaternion.Euler(0, 0, Random.Range(-2f, 2f));
            
            yield return null;
        }
        
        Destroy(lightningBolt);
    }
    
    IEnumerator DestroyLightningVisual(GameObject lightning, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (lightning != null)
        {
            Destroy(lightning);
        }
    }
    
    [Server]
    void ApplyExplosionEffect(Vector3 position)
    {
        // Create explosion that damages all nearby enemies
        Collider2D[] targets = Physics2D.OverlapCircleAll(position, lightningRange);
        
        foreach (var collider in targets)
        {
            var enemy = collider.GetComponent<Enemy>();
            var actualEnemy = collider.GetComponent<ActualEnemy>();
            
            bool shouldHit = false;
            
            if (!shotByEnemy && enemy != null && enemy.connectionToClient != null && 
                !enemy.isEnemy && shooterId != enemy.connectionId)
            {
                shouldHit = true;
                enemy.TakeDamage((int)effectDamage);
            }
            else if (!shotByEnemy && actualEnemy != null && actualEnemy.isEnemy)
            {
                shouldHit = true;
                actualEnemy.TakeDamage((int)effectDamage);
            }
            else if (shotByEnemy && enemy != null && enemy.connectionToClient != null && !enemy.isEnemy)
            {
                shouldHit = true;
                enemy.TakeDamage((int)effectDamage);
            }
            
            if (shouldHit)
            {
                // Apply knockback from explosion
                var targetRb = collider.GetComponent<Rigidbody2D>();
                if (targetRb != null)
                {
                    Vector2 forceDirection = (collider.transform.position - position).normalized;
                    targetRb.AddForce(forceDirection * 300f, ForceMode2D.Impulse);
                }
            }
        }
        
        // Spawn explosion visual effect
        SpawnExplosionVisual(position);
    }
    
    [Server]
    void ApplyPoisonEffect(GameObject target)
    {
        var burnEffect = target.GetComponent<BurnEffect>();
        if (burnEffect == null)
        {
            burnEffect = target.AddComponent<BurnEffect>();
        }
        // Reuse burn effect for poison with different parameters
        burnEffect.ApplyBurn(effectDamage, effectDuration, 1.5f); // Slower ticks for poison
    }
    
    [Server]
    void ApplyGravityEffect(GameObject target, Vector3 position)
    {
        var targetRb = target.GetComponent<Rigidbody2D>();
        if (targetRb != null)
        {
            // Apply continuous pull toward gravity center
            StartCoroutine(GravityPullRoutine(targetRb, position, effectDuration));
        }
    }
    
    [ClientRpc]
    void SpawnExplosionVisual(Vector3 position)
    {
        // Create simple explosion effect with expanding circle
        GameObject explosion = new GameObject("Explosion");
        explosion.transform.position = position;
        
        var renderer = explosion.AddComponent<SpriteRenderer>();
        renderer.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange explosion color
        renderer.sortingOrder = 10;
        
        StartCoroutine(AnimateExplosion(explosion));
    }
    
    IEnumerator AnimateExplosion(GameObject explosion)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * 3f;
        
        var renderer = explosion.GetComponent<SpriteRenderer>();
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            explosion.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 1f - t;
                renderer.color = color;
            }
            
            yield return null;
        }
        
        Destroy(explosion);
    }
    
    IEnumerator GravityPullRoutine(Rigidbody2D targetRb, Vector3 gravityCenter, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration && targetRb != null)
        {
            Vector3 pullDirection = (gravityCenter - targetRb.transform.position).normalized;
            targetRb.AddForce(pullDirection * 100f * Time.fixedDeltaTime);
            
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
    }
}