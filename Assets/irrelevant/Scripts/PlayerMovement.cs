using UnityEngine;
using Mirror;
using TMPro;

public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 5f;
    public Rigidbody2D rb;
    public FixedJoystick joystick;
    private Vector2 movement;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    public Transform cameraTransform;
    public string name;
    public MouseShooting ms;
    public TextMeshProUGUI t;
    public bool isKeyboard;

    private Enemy enemyScript;
    private string currentAnimation = "";
    private bool isWalking = false;

    void Start()
    {
        isKeyboard = !Application.isMobilePlatform;
        animator = GetComponent<Animator>();
        ms = GetComponent<MouseShooting>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyScript = GetComponent<Enemy>();
        rb.gravityScale = 0;
    }

    void Update()
    {
        if (!isLocalPlayer || !isClient) return;

        if (cameraTransform != null)
        {
            cameraTransform.rotation = Quaternion.Euler(0, 0, 0);
        }

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

        movement = new Vector2(moveX, moveY).normalized;

        // Only handle movement animations when NOT in hitstun
        if (enemyScript.hitstuntimer <= 0.01f)
        {
            bool shouldWalk = movement.magnitude > 0;

            // Handle sprite flipping
            if (shouldWalk && (movement.x > 0.05f || movement.x < -0.05f))
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

            // Only change animation when movement state changes
            if (shouldWalk && !isWalking)
            {
                PlayAnimation(name + "_walk");
                isWalking = true;
            }
            else if (!shouldWalk && isWalking)
            {
                PlayAnimation(name + "_idle");
                isWalking = false;
            }
        }
        // When in hitstun, don't change isWalking state or play movement animations
    }

    private void PlayAnimation(string animName)
    {
        if (currentAnimation != animName)
        {
            animator.Play(animName);
            currentAnimation = animName;
        }
    }

    // Called by Enemy script when hitstun ends to reset animation state
    public void ResetAnimationState()
    {
        currentAnimation = "";
        isWalking = false;
    }

    void FixedUpdate()
    {
        if (!isLocalPlayer || !isClient) return;
        
        // Apply movement with current speed (accounts for slow effects)
        rb.velocity = movement * moveSpeed;
    }
}