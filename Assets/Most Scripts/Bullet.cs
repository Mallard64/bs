using UnityEngine;
using Mirror;
using System.Collections;
using TMPro;

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
        // Case 4: Hit environment/walls
        else if (col.gameObject.CompareTag("Wall"))
        {
            NetworkServer.Destroy(gameObject);
            return;
        }

        if (shouldDamage)
        {
            // 1) Grab the contact point
            Vector3 hitPos = col.contacts[0].point;

            // 4) Destroy the bullet
            NetworkServer.Destroy(gameObject);
        }

        if (shouldDamage)
        {
            NetworkServer.Destroy(gameObject);
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
        
        // 5) Show damage number if combo multiplier > 1
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
}