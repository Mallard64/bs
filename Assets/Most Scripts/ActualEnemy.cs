using TMPro;
using System.Collections;
using UnityEngine;
using Mirror;

public class ActualEnemy : NetworkBehaviour
{
    public bool isEnemy = true;  // This is an actual enemy

    [SyncVar]
    public float health;

    [SyncVar(hook = nameof(OnVisibilityChanged))]
    public bool isVisible = true;

    [SyncVar]
    public Vector3 spawnPoint;

    public float maxHealth = 150f;
    public SpriteRenderer spriteRenderer;
    public float respawnTime = 5f;

    private BotPlayer botAI;  // AI controller instead of player movement
    public TextMeshPro t;

    public int weight = 120;
    public int connectionId;
    public float hitstuntimer = 0f;
    public GameObject hitfx;

    // Enemy-specific properties
    public bool isAlive = true;
    public float deathDuration = 3f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        botAI = GetComponent<BotPlayer>();
        connectionId = (new System.Random()).Next();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        health = 0f;  // Start with 0 damage
        spawnPoint = transform.position;  // Set spawn point to current position
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        transform.position = spawnPoint;
    }

    void OnVisibilityChanged(bool _, bool newVis)
    {
        spriteRenderer.enabled = newVis;
    }

    void Update()
    {
        // Update health display
        t.text = $"{health:0}%";

        if (health < 40)
        {
            t.color = Color.green;
        }
        else if (health < 70)
        {
            t.color = Color.yellow;
        }
        else
        {
            t.color = Color.red;
        }

        // Handle hitstun
        if (hitstuntimer <= 0.01f)
        {
            if (botAI != null) botAI.enabled = true;
        }
        else
        {
            hitstuntimer -= Time.deltaTime;
            if (botAI != null) botAI.enabled = false;
        }

        // Check if enemy should die from high damage
        if (health >= maxHealth && isAlive)
        {
            Die();
        }

        // Death boundary check (optional - if you want enemies to die from falling off)
        if (transform.position.y >= 15 || transform.position.y <= -25 ||
            transform.position.x >= 25 || transform.position.x <= -25)
        {
            if (isServer) Die();
        }
    }

    #region Damage & Knockback
    [Server]
    public void TakeDamage(int damage)
    {
        if (!isAlive) return;

        health += damage;
        SetVisibility(true);

        // Trigger hit effect
        RpcShowHitEffect();
    }

    [ClientRpc]
    void RpcShowHitEffect()
    {
        if (hitfx != null)
        {
            StartCoroutine(HitstunCoroutine(0.2f));
        }
    }

    private IEnumerator HitstunCoroutine(float duration)
    {
        if (hitfx != null) hitfx.SetActive(true);

        yield return new WaitForSeconds(duration);

        if (hitfx != null) hitfx.SetActive(false);
    }

    [Server]
    void SetVisibility(bool state) => isVisible = state;

    [ClientRpc]
    public void ApplyKnockback(Vector2 force, float stunDuration)
    {
        if (!isAlive) return;

        var rb = GetComponent<Rigidbody2D>();
        rb?.AddForce(force, ForceMode2D.Impulse);

        if (botAI != null) botAI.enabled = false;
        hitstuntimer = (stunDuration < hitstuntimer) ? hitstuntimer : stunDuration;
    }
    #endregion

    #region Death & Respawn
    [Server]
    public void Die()
    {
        if (!isAlive) return;

        isAlive = false;
        RpcDisableEnemy();

        // Award points to players (optional)
        // ScoreManager.Instance.AddKill(/* player who killed this enemy */);

        Invoke(nameof(ServerRespawn), respawnTime);
    }

    [Server]
    private void ServerRespawn()
    {
        health = 0f;
        isAlive = true;
        transform.position = spawnPoint;
        RpcEnableEnemy();
    }

    [ClientRpc]
    private void RpcDisableEnemy()
    {
        spriteRenderer.enabled = false;
        if (botAI != null) botAI.enabled = false;

        // Disable colliders
        var colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
            col.enabled = false;
    }

    [ClientRpc]
    private void RpcEnableEnemy()
    {
        transform.position = spawnPoint;
        spriteRenderer.enabled = true;
        if (botAI != null) botAI.enabled = true;

        // Re-enable colliders
        var colliders = GetComponents<Collider2D>();
        foreach (var col in colliders)
            col.enabled = true;
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.collider.CompareTag("Wall") || !isAlive) return;

        var v = GetComponent<Rigidbody2D>().velocity.magnitude;
        if (v < 25f) return;

        if (v < 40f)
        {
            // Bounce off wall
            GetComponent<Rigidbody2D>().velocity = -GetComponent<Rigidbody2D>().velocity * 0.4f;
            return;
        }

        // Die from wall impact
        if (isServer) Die();
    }
    #endregion
}