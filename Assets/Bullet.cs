using UnityEngine;
using Mirror;

public class Bullet : NetworkBehaviour
{
    public int bulletDamage = 10;  // Amount of damage the bullet will deal
    public int shooterId;  // The ID of the player who fired the bullet

    // This method is called when the bullet collides with something
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Only the server should handle damage
        if (!isServer) return;

        Debug.Log("Bullet collided with: " + collision.gameObject.name);

        // Check if the object hit has the Enemy component
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();

        // Ensure the collision target is not the player who fired the bullet
        if (enemy != null && enemy.connectionId != shooterId)
        {
            // Server applies the damage
            enemy.TakeDamage(bulletDamage);
            Debug.Log("Dealt " + bulletDamage + " damage to: " + enemy.gameObject.name);

            // Destroy the bullet after it hits the enemy
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Hit something else or the shooter itself: " + collision.gameObject.name);
        }
    }
}
