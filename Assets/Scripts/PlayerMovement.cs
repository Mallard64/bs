using UnityEngine;
using Mirror;  // Add Mirror namespace

public class PlayerMovement : NetworkBehaviour  // Extend NetworkBehaviour
{
    public float moveSpeed = 5f;    // Movement speed
    public Rigidbody2D rb;          // Reference to Rigidbody2D

    private Vector2 movement;       // Movement input
    private Animator animator;      // Animator reference
    private SpriteRenderer spriteRenderer;
    public Transform cameraTransform; // To lock camera rotation
    public string name;

    void Start()
    {
        // Get the Animator component
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (movement.magnitude > 0)
        {
            if (movement.x > 0) spriteRenderer.flipX = false;  // Face right
            else if (movement.x < 0) spriteRenderer.flipX = true;  // Face left
        }
        if (!isLocalPlayer)
        {
            return; // Exit if this is not the local player's object
        }
        // Lock the camera's rotation permanently at 0, 0, 0
        if (cameraTransform != null)
        {
            cameraTransform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // Only allow the local player to move and animate their own character
        

        // Get input from WASD or Arrow Keys
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        // Update movement vector
        movement = new Vector2(moveX, moveY).normalized;

        // Check if moving and set the appropriate animation
        if (movement.magnitude > 0)
        {
            if (movement.x > 0) spriteRenderer.flipX = false;  // Face right
            else if (movement.x < 0) spriteRenderer.flipX = true;  // Face left
            animator.Play(name+"_walk");  // Play walk animation
        }
        else
        {
            animator.Play(name+"_idle");  // Play idle animation
        }
    }

    void FixedUpdate()
    {
        // Only move if this is the local player's object
        if (!isLocalPlayer)
        {
            return;
        }

        // Move the player using Rigidbody2D
        rb.velocity = movement * moveSpeed;
    }
}
