using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    public int bulletDamage = 10;
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
            NetworkServer.Destroy(gameObject);
        }
    }

    void DamagePlayer(Enemy player, Collision2D col)
    {
        // 1) Damage
        player.TakeDamage(bulletDamage);

        // 2) Knockback calculation
        float d = bulletDamage;
        float w = player.weight;
        float dm = player.health;
        float baseComp = (d * 0.1f) + (d * dm / 20f);
        float kb = ((baseComp * knockbackScaling) * (200f / (w + 100f))) + knockbackBase;

        // 3) Get knockback direction
        Vector2 dir = GetKnockbackDirection(col);

        // 4) Apply knockback
        float stun = (kb * 0.4f) / 60f;
        player.TargetApplyKnockback(player.connectionToClient, dir * kb, stun / 10f);
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
}