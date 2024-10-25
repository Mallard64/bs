using UnityEngine;
using Mirror;
using System.Collections;

public class Enemy : NetworkBehaviour
{
    public int id;
    [SyncVar(hook = nameof(OnHealthChanged))] // Hook to call when health changes
    public int health;

    [SyncVar(hook = nameof(OnVisibilityChanged))] // SyncVar to synchronize visibility state
    public bool isVisible = true;

    public int maxHealth = 3;
    public SpriteRenderer spriteRenderer;
    public float respawnTime = 3f; // Time to wait before respawning

    private CustomNetworkManager1 networkManager; // Reference to your custom network manager

    System.Random r = new System.Random();

    void Start()
    {
        id = r.Next(100000);
        health = maxHealth;

        // Update color based on initial health
        UpdateHealthColor();

        // Find the network manager
        networkManager = (CustomNetworkManager1)NetworkManager.singleton;
    }

    // Method that is called when health changes
    void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log("Health changed from " + oldHealth + " to " + newHealth);
        UpdateHealthColor();
    }

    void OnVisibilityChanged(bool oldVisibility, bool newVisibility)
    {
        spriteRenderer.enabled = newVisibility;
    }

    [Server]  // Only the server can handle damage
    public void TakeDamage(int damage)
    {
        if (health <= 0) return;

        health -= damage;

        if (gameObject.GetComponent<Bushes>().inBush)
        {
            Debug.Log("Got hit in bush");
            //in bush
            SetVisibility(true); // Temporarily set visibility to true for the player
            StartCoroutine(ShowFace());
        }

        Debug.Log(gameObject.name + " took " + damage + " damage. Remaining health: " + health);

        if (health <= 0)
        {
            StartCoroutine(RespawnPlayer());
        }
    }

    [Server]
    IEnumerator RespawnPlayer()
    {
        // Disable the player while respawning
        RpcDisablePlayer();

        // Wait for the respawn time
        yield return new WaitForSeconds(respawnTime);

        // Respawn the player by moving them to a spawn point and restoring health
        Transform spawnPoint = networkManager.AssignSpawnPoint(connectionToClient); // Get a random spawn point from the network manager

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.position; // Move to the spawn point
            health = maxHealth; // Reset health

            // Re-enable the player on all clients
            RpcEnablePlayer();
        }
    }

    IEnumerator ShowFace()
    {
        yield return new WaitForSeconds(0.5f);

        if (gameObject.GetComponent<Bushes>().inBush)
        {
            //in bush
            SetVisibility(false); // Hide again after delay
        }
    }

    // Command to change visibility across all clients
    [Server]
    void SetVisibility(bool state)
    {
        isVisible = state; // This will trigger OnVisibilityChanged on all clients
    }

    [ClientRpc]
    void RpcDisablePlayer()
    {
        // Disable the player's renderer and collider on all clients
        spriteRenderer.enabled = false;
        GetComponent<Collider2D>().enabled = false;
        GetComponent<PlayerMovement>().enabled = false;
        if (GetComponent<MouseShooting>() != null)
        {
            GetComponent<MouseShooting>().enabled = false;
        }
        if (GetComponent<MouseShooting1>() != null)
        {
            GetComponent<MouseShooting1>().enabled = false;
        }
        if (GetComponent<MouseShooting2>() != null)
        {
            GetComponent<MouseShooting2>().enabled = false;
        }
    }

    [ClientRpc]
    void RpcEnablePlayer()
    {
        // Re-enable the player's renderer and collider on all clients
        spriteRenderer.enabled = true;
        GetComponent<Collider2D>().enabled = true;
        GetComponent<PlayerMovement>().enabled = true;
        if (GetComponent<MouseShooting>() != null)
        {
            GetComponent<MouseShooting>().enabled = true;
        }
        if (GetComponent<MouseShooting1>() != null)
        {
            GetComponent<MouseShooting1>().enabled = true;
        }
        if (GetComponent<MouseShooting2>() != null)
        {
            GetComponent<MouseShooting2>().enabled = true;
        }

        // Update health color to reset the color based on the restored health
        UpdateHealthColor();
    }

    void UpdateHealthColor()
    {
        // Lerp from red to green based on health percentage
        float healthPercentage = (float)health / maxHealth;
        spriteRenderer.color = Color.Lerp(Color.red, Color.green, healthPercentage);
    }
}




