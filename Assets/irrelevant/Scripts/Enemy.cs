
using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

/// <summary>
/// Player script combining Smash‐style percent damage, knockback RPCs,
/// and wall‐death respawn, plus SyncVar spawn‐point replication.
/// </summary>
public class Enemy : NetworkBehaviour
{
    [SyncVar]
    public float health;           // Percent damage meter (0–∞)

    [SyncVar(hook = nameof(OnVisibilityChanged))]
    public bool isVisible = true;

    [SyncVar(hook = nameof(OnSpawnPointChanged))]
    public Vector3 assignedSpawnPoint;

    public int connectionId;

    public float maxHealth = 250f;
    public SpriteRenderer spriteRenderer;
    public float respawnTime = 3f;

    private CustomNetworkManager1 networkManager;
    private PlayerMovement mover;
    private GameStatsManager pt;
    private MouseShooting ms;
    private bool hx;
    public float weight = 100f;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mover = GetComponent<PlayerMovement>();
        pt = FindObjectOfType<GameStatsManager>();
        ms = GetComponent<MouseShooting>();
        hx = PlayerPrefs.GetInt("team") % 2 == 0;
    }

    void Start()
    {
        connectionId = Random.Range(1, int.MaxValue);
    }

    #region Server‐side setup
    public override void OnStartServer()
    {
        base.OnStartServer();

        networkManager = (CustomNetworkManager1)NetworkManager.singleton;

        // Assign a unique connectionId & spawn point on the server
        connectionToClient.connectionId.ToString();  // ensure connectionToClient exists
        assignedSpawnPoint = networkManager.AssignSpawnPoint(connectionToClient.connectionId);

        // Position immediately on the server
        transform.position = assignedSpawnPoint;
    }
    #endregion

    #region Client‐side setup
    public override void OnStartClient()
    {
        base.OnStartClient();
        // At the moment the client spawns this object, move it to the replicated point:
        transform.position = assignedSpawnPoint;
    }

    void OnSpawnPointChanged(Vector3 oldPoint, Vector3 newPoint)
    {
        // whenever SyncVar changes, update position locally
        transform.position = newPoint;
    }

    void OnVisibilityChanged(bool oldVis, bool newVis)
    {
        spriteRenderer.enabled = newVis;
        if (!NetworkServer.spawned.TryGetValue(GetComponent<MouseShooting>().weaponNetId, out var pi)) return;
        pi.GetComponent<SpriteRenderer>().enabled = newVis;
    }
    #endregion

    void Update()
    {
        if (assignedSpawnPoint.y > 0)
        {
            ms.isFlipped = true;
        }
        else
        {
            ms.isFlipped = false;
        }
    }

    void UpdateColor()
    {
        float t = Mathf.Clamp01(health / maxHealth);
        spriteRenderer.color = Color.Lerp(Color.green, Color.red, t);
    }

    #region Damage & Knockback
    [Server]
    public void TakeDamage(int damage)
    {
        health += damage;
        // reveal if hidden
        SetVisibility(true);
    }

    private IEnumerator HitstunCoroutine(float duration)
    {
        mover.enabled = false;
        yield return new WaitForSeconds(duration);
        mover.enabled = true;
    }

    [Server]
    void SetVisibility(bool state) => isVisible = state;

    [TargetRpc]
    public void TargetApplyKnockback(NetworkConnection target, Vector2 force, float stunDuration)
    {
        var rb = GetComponent<Rigidbody2D>();
        rb?.AddForce(force, ForceMode2D.Impulse);
        StartCoroutine(HitstunCoroutine(stunDuration));
    }
    #endregion

    #region Wall‐Death & Respawn
    void OnCollisionEnter2D(Collision2D col)
    {
        if (!col.collider.CompareTag("Wall") || !isLocalPlayer) return;
        Vector2 v = GetComponent<Rigidbody2D>().velocity;
        if (v.magnitude < 30f) return;
        if (v.magnitude < 60f)
        {
            GetComponent<Rigidbody2D>().velocity = -v;
            return;
        }
        CmdDieAndRespawn();
    }

    [Command]
    private void CmdDieAndRespawn()
    {
        RpcDisablePlayer();
        Invoke(nameof(ServerRespawn), respawnTime);
    }

    [Server]
    private void ServerRespawn()
    {
        health = 0f;
        // move on the server
        transform.position = assignedSpawnPoint;
        RpcEnablePlayer();
    }

    [ClientRpc]
    private void RpcDisablePlayer()
    {
        spriteRenderer.enabled = false;
        mover.enabled = false;
    }

    [ClientRpc]
    private void RpcEnablePlayer()
    {
        // force the client to the correct spot
        transform.position = assignedSpawnPoint;

        spriteRenderer.enabled = true;
        mover.enabled = true;
        UpdateColor();
    }
    #endregion

    #region Win/Lose/Tie
    public void Win() => SceneManager.LoadScene("Win");
    public void Lose() => SceneManager.LoadScene("Lose");
    public void Tie() => SceneManager.LoadScene("Tie");
    #endregion
}
