using UnityEngine;
using Mirror;
using TMPro;

/// <summary>
/// DEPRECATED: Use LobbyPlayerAdapter instead
/// This script is kept for reference but should not be used with existing player prefabs
/// </summary>
public class LobbyPlayer : NetworkBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float runSpeedMultiplier = 1.5f;
    
    [Header("Interaction")]
    public float interactionRange = 2f;
    public LayerMask interactableLayerMask = -1;
    
    [Header("UI")]
    public Canvas playerUI;
    public TextMeshProUGUI interactionPrompt;
    public TextMeshProUGUI playerNameText;
    
    [Header("Visual")]
    public SpriteRenderer characterSprite;
    public Animator characterAnimator;
    public ParticleSystem walkDust;
    
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string playerDisplayName = "Player";
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool isRunning;
    private GameObject currentInteractable;
    private Camera lobbyCamera;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (isLocalPlayer)
        {
            SetupLocalPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }
        
        // Set random player name for demonstration
        if (isServer)
        {
            playerDisplayName = $"Player_{Random.Range(1000, 9999)}";
        }
    }
    
    void SetupLocalPlayer()
    {
        // Enable camera and UI for local player
        lobbyCamera = Camera.main;
        if (lobbyCamera != null)
        {
            var cameraFollow = lobbyCamera.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.target = transform;
            }
        }
        
        if (playerUI != null)
        {
            playerUI.gameObject.SetActive(true);
        }
        
        // Show controls hint
        if (NotificationSystem.Instance != null)
        {
            NotificationSystem.Instance.ShowNotification(
                "Welcome to the Lobby! Use WASD to move, Hold Shift to run, E to interact", 
                5f, 
                NotificationType.Info
            );
        }
    }
    
    void SetupRemotePlayer()
    {
        // Disable UI for remote players
        if (playerUI != null)
        {
            playerUI.gameObject.SetActive(false);
        }
    }
    
    void Update()
    {
        if (!isLocalPlayer) return;
        
        HandleInput();
        CheckForInteractables();
        HandleInteraction();
        UpdateUI();
    }
    
    void HandleInput()
    {
        // Movement input
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput = moveInput.normalized;
        
        // Running input
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        // Interaction input
        if (Input.GetKeyDown(KeyCode.E) && currentInteractable != null)
        {
            InteractWithObject(currentInteractable);
        }
        
        // Toggle leaderboard
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var lobbyManager = FindObjectOfType<LobbyManager>();
            if (lobbyManager != null)
            {
                lobbyManager.ToggleLeaderboard();
            }
        }
    }
    
    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        
        MovePlayer();
    }
    
    void MovePlayer()
    {
        float currentSpeed = moveSpeed;
        if (isRunning && moveInput.magnitude > 0)
        {
            currentSpeed *= runSpeedMultiplier;
        }
        
        Vector2 velocity = moveInput * currentSpeed;
        rb.velocity = velocity;
        
        // Update animations
        if (characterAnimator != null)
        {
            characterAnimator.SetFloat("Speed", velocity.magnitude);
            characterAnimator.SetBool("IsRunning", isRunning && velocity.magnitude > 0.1f);
            
            // Flip sprite based on movement direction
            if (velocity.x != 0 && characterSprite != null)
            {
                characterSprite.flipX = velocity.x < 0;
            }
        }
        
        // Dust particles when moving
        if (walkDust != null)
        {
            if (velocity.magnitude > 0.1f)
            {
                if (!walkDust.isPlaying)
                    walkDust.Play();
                    
                var emission = walkDust.emission;
                emission.rateOverTime = isRunning ? 20f : 10f;
            }
            else
            {
                if (walkDust.isPlaying)
                    walkDust.Stop();
            }
        }
    }
    
    void CheckForInteractables()
    {
        // Find nearby interactable objects
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, interactionRange, interactableLayerMask);
        
        GameObject closestInteractable = null;
        float closestDistance = float.MaxValue;
        
        foreach (var obj in nearbyObjects)
        {
            var interactable = obj.GetComponent<IInteractable>();
            if (interactable != null)
            {
                float distance = Vector2.Distance(transform.position, obj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestInteractable = obj.gameObject;
                }
            }
        }
        
        currentInteractable = closestInteractable;
    }
    
    void HandleInteraction()
    {
        if (currentInteractable != null)
        {
            var interactable = currentInteractable.GetComponent<IInteractable>();
            if (interactable != null && interactionPrompt != null)
            {
                interactionPrompt.text = $"Press E to {interactable.GetInteractionText()}";
                interactionPrompt.gameObject.SetActive(true);
            }
        }
        else
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.gameObject.SetActive(false);
            }
        }
    }
    
    void InteractWithObject(GameObject obj)
    {
        var interactable = obj.GetComponent<IInteractable>();
        if (interactable != null)
        {
            interactable.Interact(gameObject);
        }
    }
    
    void UpdateUI()
    {
        // Update any dynamic UI elements here
    }
    
    void OnPlayerNameChanged(string oldName, string newName)
    {
        playerDisplayName = newName;
        
        if (playerNameText != null)
        {
            playerNameText.text = playerDisplayName;
        }
    }
    
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        
        // Spawn player at available lobby spawn point
        if (isServer)
        {
            var lobbyManager = FindObjectOfType<LobbyManager>();
            if (lobbyManager != null)
            {
                var spawnPoint = lobbyManager.GetAvailableSpawnPoint();
                transform.position = spawnPoint.position;
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}