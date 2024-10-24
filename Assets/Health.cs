using UnityEngine;
using Mirror;

public class Health : NetworkBehaviour
{
    [SyncVar] // Synchronize health across all clients
    public int currentHealth;

    public int maxHealth = 100;

    void Start()
    {
        currentHealth = maxHealth;
    }

    [Server] // Run this method only on the server
    public void TakeDamage(int damage)
    {
        if (currentHealth <= 0) return;  // If already dead, do nothing

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    [Server] // Server handles the destruction
    void Die()
    {
        // Handle death logic (e.g., respawn or destroy)
        NetworkServer.Destroy(gameObject); // Destroy the object across clients
    }
}
