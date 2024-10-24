using UnityEngine;
using Mirror;  // Add Mirror namespace

public class PlayerMovement : NetworkBehaviour  // Extend NetworkBehaviour instead of MonoBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody2D rb;

    Vector2 movement;  // Change this to Vector2

    void Update()
    {
        // Only allow the local player to move their own character
        if (!isLocalPlayer)
        {
            return;  // Exit if this is not the local player's object
        }

        // Get input from WASD or arrow keys
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");  // Change moveZ to moveY

        // Update movement using Vector2 (for 2D)
        movement = new Vector2(moveX, moveY).normalized;
    }

    void FixedUpdate()
    {
        // Only move if this is the local player's object
        if (!isLocalPlayer)
        {
            return;
        }

        // Apply movement to Rigidbody2D
        rb.MovePosition(rb.position + movement * moveSpeed * Time.fixedDeltaTime);
    }
}
