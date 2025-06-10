using TMPro;
using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class Enemy : NetworkBehaviour
{
    public bool isEnemy = false;

    [SyncVar]
    public float health;

    GameObject playerCamera;

    [SyncVar(hook = nameof(OnVisibilityChanged))]
    public bool isVisible = true;

    [SyncVar(hook = nameof(OnSpawnPointChanged))]
    public Vector3 assignedSpawnPoint;

    [SyncVar]
    public int playerNum;

    public float maxHealth = 250f;
    public SpriteRenderer spriteRenderer;
    public float respawnTime = 3f;

    private PlayerMovement mover;
    private MouseShooting ms;
    public TextMeshPro t;

    public int weight = 100;
    public int connectionId;

    [SyncVar]
    public float hitstuntimer = 0f;

    [SyncVar]
    public float hstmax;

    public GameObject hitfx;

    private bool hasPlayedHitfAnimation = false;

    void Awake()
    {
        GameObject playerCamera = GameObject.FindWithTag("MainCamera");
        spriteRenderer = GetComponent<SpriteRenderer>();
        mover = GetComponent<PlayerMovement>();
        ms = GetComponent<MouseShooting>();
        connectionId = (new System.Random()).Next();
    }

    #region Server-side setup
    public override void OnStartServer()
    {
        base.OnStartServer();

        var networkManager = (CustomNetworkManager1)NetworkManager.singleton;
        assignedSpawnPoint = networkManager.AssignSpawnPoint(connectionToClient.connectionId);
        transform.position = assignedSpawnPoint;

        playerNum = (connectionToClient.connectionId % 2) + 1;

        Debug.Log($"[Server] Player spawned with playerNum={playerNum} (connId={connectionToClient.connectionId})");
    }
    #endregion

    #region Client-side setup
    public override void OnStartClient()
    {
        base.OnStartClient();
        transform.position = assignedSpawnPoint;
    }

    void OnSpawnPointChanged(Vector3 _, Vector3 newPoint) => transform.position = newPoint;
    void OnVisibilityChanged(bool _, bool newVis)
    {
        spriteRenderer.enabled = newVis;
        var spawnedDict = isServer ? NetworkServer.spawned : NetworkClient.spawned;
        if (spawnedDict.TryGetValue(ms.weaponNetId, out var go))
            go.GetComponent<SpriteRenderer>().enabled = newVis;
    }
    #endregion

    void Update()
    {
        
        ms.isFlipped = assignedSpawnPoint.y > 0;

        if (hitstuntimer <= 0.01f)
        {
            mover.enabled = true;
            ms.enabled = true;
            hasPlayedHitfAnimation = false;

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

            // Reset movement animation state when hitstun ends
            if (isLocalPlayer)
            {
                mover.ResetAnimationState();
            }
        }
        else
        {
            hitstuntimer -= Time.deltaTime;

            // Play hitf animation once during the middle of hitstun
            if (hitstuntimer >= 0.01f && hitstuntimer <= hstmax - 0.25f && !hasPlayedHitfAnimation)
            {
                GetComponent<Animator>().Play(GetComponent<PlayerMovement>().name + "_hitf");
                hasPlayedHitfAnimation = true;
            }
        }

        if (SceneManager.GetActiveScene().name != "Knockout") return;
        if (transform.position.y >= 10 || transform.position.y <= -18 || transform.position.x >= 19 || transform.position.x <= -19)
        {
            CmdDieAndRespawn();
        }

        t.text = $"{health:0}%";

        
    }

    #region Damage & Knockback
    [Server]
    public void TakeDamage(int damage)
    {
        health += damage;
        SetVisibility(true);
        RpcShowDamageEffect();
    }

    [ClientRpc]
    void RpcShowDamageEffect()
    {
        StartCoroutine(DamageTextEffect());
    }

    private IEnumerator DamageTextEffect()
    {
        if (t == null) yield break;

        // Store original color
        Color originalColor = t.color;

        // Set to red
        t.color = Color.red;

        // Shake for 0.1 seconds
        float shakeTimer = 0.1f;
        float shakeIntensity = 0.05f;

        Vector3 th = t.transform.position;

        while (shakeTimer > 0)
        {
            shakeTimer -= 0.01f;
            yield return new WaitForSeconds(0.01f);
        }


        // Return to appropriate color based on health
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
    }

    [Server]
    void SetVisibility(bool state) => isVisible = state;

    [Server]
    public void ApplyKnockback(Vector2 force, float stunDuration)
    {
        // Call TargetRpc for physics, ClientRpc for animation
        TargetApplyPhysics(connectionToClient, force);
        RpcPlayHitAnim(stunDuration);
    }

    [TargetRpc]
    private void TargetApplyPhysics(NetworkConnection target, Vector2 force)
    {
        var rb = GetComponent<Rigidbody2D>();
        rb?.AddForce(force, ForceMode2D.Impulse);
        mover.enabled = false;
        ms.enabled = false;
    }

    [ClientRpc]
    public void RpcPlayHitAnim(float stunDuration)
    {
        // Animation and hitstun sync to all clients
        hitstuntimer = (stunDuration < hitstuntimer) ? hitstuntimer : stunDuration;
        hstmax = hitstuntimer;
        hasPlayedHitfAnimation = false; // Reset flag for new hitstun
        GetComponent<Animator>().Play(GetComponent<PlayerMovement>().name + "_hit");
    }
    #endregion

    #region Wall-Death & Respawn
    void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.collider.CompareTag("Wall") || !isLocalPlayer) return;

        var v = GetComponent<Rigidbody2D>().velocity.magnitude;
        if (v < 30f) return;

        if (v < 55f)
        {
            GetComponent<Rigidbody2D>().velocity = -GetComponent<Rigidbody2D>().velocity * 0.3f;
            return;
        }

        CmdDieAndRespawn();
    }

    [Command]
    private void CmdDieAndRespawn()
    {
        int other = (playerNum == 1 ? 2 : 1);
        ScoreManager.Instance.AddKill(other);

        // Trigger death effect on all clients
        RpcPlayDeathEffect();

        // Disable player after a short delay to let death animation play
        Invoke(nameof(DisableAndRespawn), 0.5f);
    }

    [ClientRpc]
    void RpcPlayDeathEffect()
    {
        StartCoroutine(DeathEffectSequence());
    }

    private IEnumerator DeathEffectSequence()
    {
        // Play death animation
        GetComponent<Animator>().Play(GetComponent<PlayerMovement>().name + "_dead");

        // Zoom camera effect (only for local player cameras)
        var allCameras = FindObjectOfType<CameraFollow>();
        float originalSize = 0f;
        Camera targetCamera = null;

        targetCamera = allCameras.GetComponent<Camera>();

        // Zoom in effect
        if (targetCamera != null)
        {
            float zoomTimer = 0.5f;
            float targetZoom = originalSize * 0.7f; // Zoom in to 70% of original size

            while (zoomTimer > 0)
            {
                float progress = 1f - (zoomTimer / 0.5f);
                targetCamera.orthographicSize = Mathf.Lerp(originalSize, targetZoom, progress);

                zoomTimer -= Time.deltaTime;
                yield return null;
            }

            // Zoom back out
            zoomTimer = 0.3f;
            while (zoomTimer > 0)
            {
                float progress = 1f - (zoomTimer / 0.3f);
                targetCamera.orthographicSize = Mathf.Lerp(targetZoom, originalSize, progress);

                zoomTimer -= Time.deltaTime;
                yield return null;
            }

            // Ensure we're back to original size
            targetCamera.orthographicSize = originalSize;
        }
    }

    [Server]
    private void DisableAndRespawn()
    {
        RpcDisablePlayer();
        Invoke(nameof(ServerRespawn), respawnTime);
    }

    // In CameraFollow.cs - Add method to get zoom access
    public void SetZoom(float size)
    {
        if (isLocalPlayer && playerCamera != null)
        {
            playerCamera.GetComponent<Camera>().orthographicSize = size;
        }
    }

    public float GetZoom()
    {
        if (isLocalPlayer && playerCamera != null)
        {
            return playerCamera.GetComponent<Camera>().orthographicSize;
        }
        return 5f; // Default fallback
    }

    [Server]
    private void ServerRespawn()
    {
        health = 0f;
        transform.position = assignedSpawnPoint;
        RpcEnablePlayer();
    }

    [ClientRpc]
    private void RpcDisablePlayer()
    {
        spriteRenderer.enabled = false;
        mover.enabled = false;
        ms.enabled = false;
        GetComponent<Collider2D>().enabled = false;
    }

    [ClientRpc]
    private void RpcEnablePlayer()
    {
        transform.position = assignedSpawnPoint;
        spriteRenderer.enabled = true;
        mover.enabled = true;
        ms.enabled = true;
        GetComponent<Collider2D>().enabled = true;

        // Reset hitstun and animation state
        hitstuntimer = 0f;
        hstmax = 0f;
        hasPlayedHitfAnimation = false;

        if (isLocalPlayer)
        {
            mover.ResetAnimationState();
        }
    }
    #endregion

    [Command]
    void CmdPlayAnimation(string animationName)
    {
        RpcPlayAnimation(animationName);
    }

    [ClientRpc]
    void RpcPlayAnimation(string animationName)
    {
        GetComponent<Animator>().Play(animationName);
    }

    #region Win/Lose/Tie
    public void Win() => SceneManager.LoadScene("Win");
    public void Lose() => SceneManager.LoadScene("Lose");
    public void Tie() => SceneManager.LoadScene("Tie");
    #endregion
}