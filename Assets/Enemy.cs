using UnityEngine;
using Mirror;

public class Enemy : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnHealthChanged))] // Hook to call when health changes
    public int health;

    public int id;
    public int maxHealth = 3;
    public SpriteRenderer spriteRenderer;

    void Start()
    {
        System.Random h = new System.Random();
        id = h.Next(10000);  // Generate a unique ID for each player
        health = maxHealth;

        // Update color based on initial health
        UpdateHealthColor();
    }

    // Method that is called when health changes
    void OnHealthChanged(int oldHealth, int newHealth)
    {
        Debug.Log("Health changed from " + oldHealth + " to " + newHealth);
        UpdateHealthColor();
    }

    [Server]  // Only the server can handle damage
    public void TakeDamage(int damage)
    {
        if (health <= 0) return;

        health -= damage;
        Debug.Log(gameObject.name + " took " + damage + " damage. Remaining health: " + health);

        if (health <= 0)
        {
            Die();
        }
    }

    [Server]
    void Die()
    {
        Debug.Log(gameObject.name + " died.");
        NetworkServer.Destroy(gameObject);  // Destroy the enemy across clients
    }

    void UpdateHealthColor()
    {
        // Lerp from red to green based on health percentage
        float healthPercentage = (float)health / maxHealth;
        spriteRenderer.color = Color.Lerp(Color.red, Color.green, healthPercentage);
    }
}
