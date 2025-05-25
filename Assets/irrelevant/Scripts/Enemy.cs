using TMPro;
using System.Collections;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class Enemy : NetworkBehaviour
{
    [SyncVar]
    public float health;           // Percent damage meter (0–∞)

    [SyncVar(hook = nameof(OnVisibilityChanged))]
    public bool isVisible = true;

    [SyncVar(hook = nameof(OnSpawnPointChanged))]
    public Vector3 assignedSpawnPoint;

    // --- NEW: our local player number (1 or 2) assigned on the server ---
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

    public GameObject hitfx;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        mover = GetComponent<PlayerMovement>();
        ms = GetComponent<MouseShooting>();
        connectionId = (new System.Random()).Next();
    }

    #region Server‐side setup
    public override void OnStartServer()
    {
        base.OnStartServer();

        // 1) Assign spawn point as before
        var networkManager = (CustomNetworkManager1)NetworkManager.singleton;
        assignedSpawnPoint = networkManager.AssignSpawnPoint(connectionToClient.connectionId);
        transform.position = assignedSpawnPoint;

        // 2) **NEW**: simple 2‐player ID (1 or 2)
        //    Mirror host is usually connectionId=0, first client =1, etc.
        playerNum = (connectionToClient.connectionId % 2) + 1;

        Debug.Log($"[Server] Player spawned with playerNum={playerNum} (connId={connectionToClient.connectionId})");
    }
    #endregion

    #region Client‐side setup
    public override void OnStartClient()
    {
        base.OnStartClient();
        transform.position = assignedSpawnPoint;
    }

    void OnSpawnPointChanged(Vector3 _, Vector3 newPoint) => transform.position = newPoint;
    void OnVisibilityChanged(bool _, bool newVis)
    {
        spriteRenderer.enabled = newVis;
        if (NetworkServer.spawned.TryGetValue(ms.weaponNetId, out var go))
            go.GetComponent<SpriteRenderer>().enabled = newVis;
    }
    #endregion

    void Update()
    {
        // update the percent display
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

        // flip logic
        ms.isFlipped = assignedSpawnPoint.y > 0;


        if (transform.position.y >= 10 || transform.position.y <= -18 || transform.position.x >= 19 || transform.position.x <= -19)
        {
            CmdDieAndRespawn();
        }
    }

    #region Damage & Knockback
    [Server]
    public void TakeDamage(int damage)
    {
        health += damage;
        SetVisibility(true);
    }

    private IEnumerator HitstunCoroutine(float duration)
    {
        //hitfx.SetActive(true);
        mover.enabled = false;
        ms.enabled = false;
        yield return new WaitForSeconds(duration);
        mover.enabled = true;
        ms.enabled = true;
        //hitfx.SetActive(false);
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

        var v = GetComponent<Rigidbody2D>().velocity.magnitude;
        if (v < 20f) return;

        if (v < 35f)
        {
            // a bounce
            GetComponent<Rigidbody2D>().velocity = -GetComponent<Rigidbody2D>().velocity * 0.6f;
            return;
        }

        // **WE DIED**
        CmdDieAndRespawn();
    }

    [Command]
    private void CmdDieAndRespawn()
    {
        // 1) credit the other player with a kill
        int other = (playerNum == 1 ? 2 : 1);
        ScoreManager.Instance.AddKill(other);

        // 2) disable & schedule respawn
        RpcDisablePlayer();
        Invoke(nameof(ServerRespawn), respawnTime);
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
    }

    [ClientRpc]
    private void RpcEnablePlayer()
    {
        transform.position = assignedSpawnPoint;
        spriteRenderer.enabled = true;
        mover.enabled = true;
    }
    #endregion

    #region Win/Lose/Tie
    public void Win() => SceneManager.LoadScene("Win");
    public void Lose() => SceneManager.LoadScene("Lose");
    public void Tie() => SceneManager.LoadScene("Tie");
    #endregion
}