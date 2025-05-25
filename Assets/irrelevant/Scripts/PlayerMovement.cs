using UnityEngine;
using Mirror;
using TMPro;

public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;       // Movement speed
    public Rigidbody2D rb;             // Rigidbody2D reference
    public FixedJoystick joystick;     // Reference to the Rigid Joystick from Joystick Pack

    private Vector2 movement;          // Movement vector
    private Animator animator;         // Animator reference
    private SpriteRenderer spriteRenderer;
    public Transform cameraTransform;  // Camera transform (if needed)
    public string name;                // Player name (for animations)
    public MouseShooting ms;
    public TextMeshProUGUI t;
    public bool isKeyboard;

    void Start()
    {
        isKeyboard = !Application.isMobilePlatform;
        animator = GetComponent<Animator>();
        ms = GetComponent<MouseShooting>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Ensure Rigidbody2D has the correct settings
        rb.gravityScale = 0; // Disable gravity for 2D movement
    }

    void Update()
    {

        if (!isLocalPlayer || !isClient) return;
        // Keep camera rotation fixed

        if (cameraTransform != null)
        {
            
            cameraTransform.rotation = Quaternion.Euler(0, 0, 0);
        }

        // Get movement input from the Rigid Joystick
        float moveX, moveY;
        if (!isKeyboard)
        {
            moveX = joystick.Horizontal;
            moveY = joystick.Vertical;
        }
        else
        {
            moveX = Input.GetAxis("Horizontal");
            moveY = Input.GetAxis("Vertical");
        }

        if (ms.isFlipped)
        {
            moveX = -moveX;
            moveY = -moveY;
        }

        // Normalize movement vector for smooth movement
        movement = new Vector2(moveX, moveY).normalized;

        // Handle animation & sprite flipping
        if (movement.magnitude > 0)
        { 
            if (movement.x > 0.05 || movement.x < -0.05)
            {
                if (ms.isFlipped)
                {
                    spriteRenderer.flipX = movement.x > 0;
                }
                else
                {
                    spriteRenderer.flipX = movement.x < 0;
                }
            }
            
            
            animator.Play(name + "_walk");         // Play walking animation
        }
        else
        {
            animator.Play(name + "_idle");         // Play idle animation
        }
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer || !isClient) return; // Only the local player can move

        // Move the player using Rigidbody2D physics
        rb.velocity = movement * moveSpeed;
    }
}
