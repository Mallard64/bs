using UnityEngine;
using Mirror;
using System.Collections;

public class Enemy : NetworkBehaviour
{
    [SyncVar] public int health;
    [SyncVar(hook = nameof(OnVisibilityChanged))] public bool isVisible = true;

    public int maxHealth = 3;
    public SpriteRenderer spriteRenderer;
    public float respawnTime = 3f;

    private CustomNetworkManager1 networkManager;
    public int connectionId; // Store the player's connection ID for consistency
    public Vector3 assignedSpawnPoint; // Store the player's assigned spawn point

    void Start()
    {
        health = maxHealth;
        networkManager = (CustomNetworkManager1)NetworkManager.singleton;

        System.Random r = new System.Random();
        connectionId = r.Next(10000000);

        assignedSpawnPoint = networkManager.AssignSpawnPoint(connectionId);
        Debug.Log("ASSSIGNED SPAWN POINT: " + assignedSpawnPoint);
        if (assignedSpawnPoint != null)
        {
            transform.position = assignedSpawnPoint;
        }
        else
        {
            Debug.LogError("Failed to assign a spawn point.");
        }
    }

    void Update()
    {
        UpdateHealthColor();
        if (health <= 0)
        {
            transform.position = assignedSpawnPoint;
            StartCoroutine(RespawnPlayer());
        }
    }

    void OnVisibilityChanged(bool oldVisibility, bool newVisibility)
    {
        spriteRenderer.enabled = newVisibility;
    }

    [Server]
    public void TakeDamage(int damage)
    {
        if (health <= 0) return;

        health -= damage;

        if (gameObject.GetComponent<Bushes>().inBush)
        {
            SetVisibility(true);
            StartCoroutine(ShowFace());
        }

        if (health <= 0)
        {
            transform.position = assignedSpawnPoint;
            StartCoroutine(RespawnPlayer());
        }
    }

    [Server]
    IEnumerator RespawnPlayer()
    {
        RpcDisablePlayer();
        yield return new WaitForSeconds(respawnTime);
        health = maxHealth;
        
        Debug.Log("RESPAWN LOCATION: " + transform.position);
        RpcEnablePlayer();
    }

    IEnumerator ShowFace()
    {
        yield return new WaitForSeconds(0.5f);
        if (gameObject.GetComponent<Bushes>().inBush)
        {
            SetVisibility(false);
        }
    }

    [Server]
    void SetVisibility(bool state)
    {
        isVisible = state;
    }

    [ClientRpc]
    void RpcDisablePlayer()
    {
        spriteRenderer.enabled = false;
        GetComponent<Collider2D>().enabled = false;
        GetComponent<PlayerMovement>().enabled = false;
    }

    [ClientRpc]
    void RpcEnablePlayer()
    {
        spriteRenderer.enabled = true;
        GetComponent<Collider2D>().enabled = true;
        GetComponent<PlayerMovement>().enabled = true;
        UpdateHealthColor();
    }

    void UpdateHealthColor()
    {
        float healthPercentage = (float)health / maxHealth;
        spriteRenderer.color = Color.Lerp(Color.red, Color.green, healthPercentage);
    }
}