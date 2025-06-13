using UnityEngine;
using Mirror;
using TMPro;

/// <summary>
/// Adapter component that adds lobby functionality to existing player prefab
/// This works with MouseShooting and PlayerMovement components
/// </summary>
public class LobbyPlayerAdapter : NetworkBehaviour
{
    [Header("Lobby Settings")]
    public bool isInLobby = true;
    public float lobbyMoveSpeed = 3f;
    public float lobbyRunSpeedMultiplier = 1.5f;
    
    [Header("Interaction")]
    public float interactionRange = 2f;
    public LayerMask interactableLayerMask = -1;
    
    [Header("UI")]
    public GameObject lobbyUI;
    public TextMeshProUGUI interactionPrompt;
    public TextMeshProUGUI playerNameText;
    
    [SyncVar(hook = nameof(OnPlayerNameChanged))]
    public string playerDisplayName = "Player";
    
    // Component references
    private MouseShooting mouseShooting;
    private PlayerMovement playerMovement;
    private AdvancedMovementSystem advancedMovement;
    private NetworkIdentity netIdentity;
    private Camera lobbyCamera;
    
    // Lobby state
    private bool isRunning;
    private GameObject currentInteractable;
    private Vector2 moveInput;
    
    // Original values to restore when leaving lobby
    private float originalMoveSpeed;
    private bool originalMouseShootingEnabled;
    private bool originalAdvancedMovementEnabled;
    
    void Start()
    {
        // Get component references
        mouseShooting = GetComponent<MouseShooting>();
        playerMovement = GetComponent<PlayerMovement>();
        advancedMovement = GetComponent<AdvancedMovementSystem>();
        netIdentity = GetComponent<NetworkIdentity>();
        
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
        
        if (isInLobby)
        {
            EnterLobbyMode();
        }
    }
    
    void SetupLocalPlayer()
    {
        // Setup camera follow
        lobbyCamera = Camera.main;
        if (lobbyCamera != null)
        {
            var cameraFollow = lobbyCamera.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.target = transform;
            }
        }
        
        // Enable lobby UI
        if (lobbyUI != null)
        {
            lobbyUI.SetActive(true);
        }
        
        // Show welcome message
        if (NotificationSystem.Instance != null)
        {
            NotificationSystem.Instance.ShowNotification(
                "Welcome to the Lobby! Use WASD to move, Hold Shift to run, E to interact, Tab for leaderboard", 
                5f, 
                NotificationType.Info
            );
        }
    }
    
    void SetupRemotePlayer()
    {
        // Disable UI for remote players
        if (lobbyUI != null)
        {
            lobbyUI.SetActive(false);
        }
    }
    
    public void EnterLobbyMode()
    {
        isInLobby = true;
        
        if (playerMovement != null)
        {
            // Store original speed and set lobby speed
            originalMoveSpeed = playerMovement.moveSpeed;
            playerMovement.moveSpeed = lobbyMoveSpeed;
        }
        
        if (mouseShooting != null)
        {
            // Disable shooting in lobby
            originalMouseShootingEnabled = mouseShooting.enabled;
            mouseShooting.isWorking = false; // Use the existing isWorking flag
        }
        
        if (advancedMovement != null)
        {
            // Disable advanced movement features in lobby
            originalAdvancedMovementEnabled = advancedMovement.enabled;
            advancedMovement.enabled = false;
        }
        
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
        
        Debug.Log($"🏛️ Player {playerDisplayName} entered lobby mode");
    }
    
    public void ExitLobbyMode()
    {
        isInLobby = false;
        
        if (playerMovement != null)
        {
            // Restore original movement speed
            playerMovement.moveSpeed = originalMoveSpeed;
        }
        
        if (mouseShooting != null)
        {
            // Re-enable shooting
            mouseShooting.isWorking = originalMouseShootingEnabled;
        }
        
        if (advancedMovement != null)
        {
            // Re-enable advanced movement
            advancedMovement.enabled = originalAdvancedMovementEnabled;
        }
        
        Debug.Log($"⚔️ Player {playerDisplayName} exited lobby mode");
    }
    
    void Update()
    {
        if (!isLocalPlayer || !isInLobby) return;
        
        HandleLobbyInput();
        CheckForInteractables();
        HandleInteraction();
        UpdateUI();
    }
    
    void HandleLobbyInput()
    {
        // Running input
        isRunning = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        
        // Apply run speed multiplier
        if (playerMovement != null)
        {
            float targetSpeed = lobbyMoveSpeed;
            if (isRunning && playerMovement.rb != null && playerMovement.rb.velocity.magnitude > 0.1f)
            {
                targetSpeed *= lobbyRunSpeedMultiplier;
            }
            playerMovement.moveSpeed = targetSpeed;
        }
        
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
        
        if (isInLobby)
        {
            EnterLobbyMode();
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
    
    // Public methods for other systems to use
    public string GetPlayerName()
    {
        return playerDisplayName;
    }
    
    public bool IsInLobby()
    {
        return isInLobby;
    }
    
    // Command to change player name
    [Command]
    public void CmdSetPlayerName(string newName)
    {
        if (!string.IsNullOrEmpty(newName) && newName.Length <= 20)
        {
            playerDisplayName = newName;
        }
    }
}