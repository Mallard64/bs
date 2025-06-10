using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class AdvancedMovementSystem : NetworkBehaviour
{
    [Header("Dash Settings")]
    public float dashDistance = 3f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 2f;
    public int maxDashCharges = 2;
    
    [Header("Wall Jump Settings")]
    public float wallJumpForce = 400f;
    public float wallSlideSpeed = 2f;
    public LayerMask wallLayer = 1 << 6;
    
    [Header("Momentum Settings")]
    public float momentumDecay = 0.95f;
    public float maxMomentum = 15f;
    public float momentumDamageBonus = 0.1f;
    
    [Header("Perfect Dodge Settings")]
    public float perfectDodgeWindow = 0.3f;
    public float perfectDodgeReward = 1.5f; // Damage multiplier for next attack
    
    [Header("UI References")]
    public GameObject[] dashChargeIndicators;
    public TextMeshProUGUI momentumText;
    public GameObject perfectDodgeEffect;
    
    [SyncVar(hook = nameof(OnDashChargesChanged))]
    public int currentDashCharges;
    
    [SyncVar(hook = nameof(OnMomentumChanged))]
    public float currentMomentum = 0f;
    
    [SyncVar]
    public bool isPerfectDodgeActive = false;
    
    [SyncVar]
    public bool isDashing = false;
    
    [SyncVar]
    public bool isWallSliding = false;
    
    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private float lastDashTime;
    private Vector2 dashDirection;
    private Coroutine dashCoroutine;
    private Coroutine dashRechargeCoroutine;
    
    // Movement tracking for momentum
    private Vector2 lastPosition;
    private float lastMoveTime;
    private List<Vector2> recentMovements = new List<Vector2>();
    
    // Perfect dodge tracking
    private float lastDamageTime;
    private Coroutine perfectDodgeCoroutine;
    
    // Wall detection
    private bool isNearWall = false;
    private Vector2 wallNormal;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        currentDashCharges = maxDashCharges;
        lastPosition = transform.position;
        
        if (!isLocalPlayer)
        {
            if (momentumText != null) momentumText.gameObject.SetActive(false);
            if (perfectDodgeEffect != null) perfectDodgeEffect.SetActive(false);
            foreach (var indicator in dashChargeIndicators)
            {
                if (indicator != null) indicator.SetActive(false);
            }
        }
        
        if (isServer)
        {
            StartCoroutine(MomentumUpdateRoutine());
        }
    }
    
    void Update()
    {
        if (!isLocalPlayer) return;
        
        HandleDashInput();
        HandleWallInteraction();
        UpdateMovementTracking();
    }
    
    void HandleDashInput()
    {
        // Double-tap movement key or dedicated dash button
        if (Input.GetKeyDown(KeyCode.LeftShift) && CanDash())
        {
            Vector2 dashDir = GetDashDirection();
            if (dashDir != Vector2.zero)
            {
                CmdPerformDash(dashDir);
            }
        }
    }
    
    Vector2 GetDashDirection()
    {
        Vector2 inputDir = Vector2.zero;
        
        // Get movement input
        if (Input.GetKey(KeyCode.W)) inputDir.y += 1;
        if (Input.GetKey(KeyCode.S)) inputDir.y -= 1;
        if (Input.GetKey(KeyCode.A)) inputDir.x -= 1;
        if (Input.GetKey(KeyCode.D)) inputDir.x += 1;
        
        // If no input, dash toward mouse
        if (inputDir == Vector2.zero)
        {
            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorld.z = 0;
            inputDir = (mouseWorld - transform.position).normalized;
        }
        
        return inputDir.normalized;
    }
    
    bool CanDash()
    {
        return currentDashCharges > 0 && 
               Time.time - lastDashTime >= 0.1f && 
               !isDashing;
    }
    
    [Command]
    void CmdPerformDash(Vector2 direction)
    {
        if (!CanDash()) return;
        
        currentDashCharges--;
        lastDashTime = Time.time;
        isDashing = true;
        dashDirection = direction;
        
        // Start dash coroutine
        if (dashCoroutine != null) StopCoroutine(dashCoroutine);
        dashCoroutine = StartCoroutine(DashRoutine());
        
        // Start recharge if this was the last charge
        if (currentDashCharges == 0 && dashRechargeCoroutine == null)
        {
            dashRechargeCoroutine = StartCoroutine(DashRechargeRoutine());
        }
        
        // Add momentum for dash
        AddMomentum(dashDistance * 2f);
        
        RpcPlayDashEffect(direction);
    }
    
    [Server]
    IEnumerator DashRoutine()
    {
        float elapsed = 0f;
        Vector2 startPos = transform.position;
        Vector2 targetPos = startPos + dashDirection * dashDistance;
        
        // Make player invulnerable during dash
        var enemy = GetComponent<Enemy>();
        bool wasInvulnerable = false;
        if (enemy != null)
        {
            wasInvulnerable = enemy.isInvulnerable;
            enemy.isInvulnerable = true;
        }
        
        while (elapsed < dashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / dashDuration;
            
            // Smooth dash movement
            Vector2 currentPos = Vector2.Lerp(startPos, targetPos, t);
            transform.position = currentPos;
            
            yield return null;
        }
        
        // Restore vulnerability
        if (enemy != null)
        {
            enemy.isInvulnerable = wasInvulnerable;
        }
        
        isDashing = false;
        dashCoroutine = null;
    }
    
    [Server]
    IEnumerator DashRechargeRoutine()
    {
        while (currentDashCharges < maxDashCharges)
        {
            yield return new WaitForSeconds(dashCooldown);
            currentDashCharges++;
            RpcPlayDashRechargeEffect();
        }
        
        dashRechargeCoroutine = null;
    }
    
    void HandleWallInteraction()
    {
        // Check for walls
        CheckWallCollision();
        
        if (isNearWall && Input.GetKeyDown(KeyCode.Space))
        {
            CmdPerformWallJump();
        }
    }
    
    void CheckWallCollision()
    {
        float wallCheckDistance = 0.6f;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.right, wallCheckDistance, wallLayer);
        
        if (hit.collider == null)
        {
            hit = Physics2D.Raycast(transform.position, Vector2.left, wallCheckDistance, wallLayer);
        }
        
        if (hit.collider != null)
        {
            isNearWall = true;
            wallNormal = hit.normal;
            
            // Apply wall sliding if moving toward wall
            if (rb.velocity.y < 0 && Vector2.Dot(rb.velocity.normalized, -wallNormal) > 0.5f)
            {
                CmdStartWallSlide();
            }
        }
        else
        {
            isNearWall = false;
            if (isWallSliding)
            {
                CmdStopWallSlide();
            }
        }
    }
    
    [Command]
    void CmdPerformWallJump()
    {
        if (!isNearWall) return;
        
        Vector2 jumpDirection = wallNormal + Vector2.up;
        jumpDirection.Normalize();
        
        rb.velocity = jumpDirection * wallJumpForce;
        
        // Add momentum for wall jump
        AddMomentum(wallJumpForce * 0.01f);
        
        RpcPlayWallJumpEffect(jumpDirection);
    }
    
    [Command]
    void CmdStartWallSlide()
    {
        if (isWallSliding) return;
        
        isWallSliding = true;
        RpcStartWallSlideEffect();
    }
    
    [Command]
    void CmdStopWallSlide()
    {
        if (!isWallSliding) return;
        
        isWallSliding = false;
        RpcStopWallSlideEffect();
    }
    
    void UpdateMovementTracking()
    {
        Vector2 currentPos = transform.position;
        Vector2 movement = currentPos - lastPosition;
        
        if (movement.magnitude > 0.01f)
        {
            recentMovements.Add(movement);
            if (recentMovements.Count > 10)
            {
                recentMovements.RemoveAt(0);
            }
            
            lastMoveTime = Time.time;
        }
        
        lastPosition = currentPos;
    }
    
    [Server]
    IEnumerator MomentumUpdateRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.1f);
            
            // Calculate momentum based on recent movement
            float movementMagnitude = 0f;
            foreach (var movement in recentMovements)
            {
                movementMagnitude += movement.magnitude;
            }
            
            float targetMomentum = Mathf.Min(movementMagnitude * 10f, maxMomentum);
            
            // Decay momentum if not moving
            if (Time.time - lastMoveTime > 0.5f)
            {
                targetMomentum *= momentumDecay;
            }
            
            currentMomentum = Mathf.Lerp(currentMomentum, targetMomentum, 0.1f);
        }
    }
    
    [Server]
    public void AddMomentum(float amount)
    {
        currentMomentum = Mathf.Min(currentMomentum + amount, maxMomentum);
    }
    
    [Server]
    public void OnTakeDamage()
    {
        float timeSinceLastDamage = Time.time - lastDamageTime;
        lastDamageTime = Time.time;
        
        // Check for perfect dodge (took damage very recently after previous damage)
        if (timeSinceLastDamage <= perfectDodgeWindow && !isPerfectDodgeActive)
        {
            ActivatePerfectDodge();
        }
    }
    
    [Server]
    void ActivatePerfectDodge()
    {
        isPerfectDodgeActive = true;
        
        if (perfectDodgeCoroutine != null)
        {
            StopCoroutine(perfectDodgeCoroutine);
        }
        
        perfectDodgeCoroutine = StartCoroutine(PerfectDodgeRoutine());
        RpcActivatePerfectDodgeEffect();
    }
    
    [Server]
    IEnumerator PerfectDodgeRoutine()
    {
        yield return new WaitForSeconds(3f); // Perfect dodge buff lasts 3 seconds
        isPerfectDodgeActive = false;
        RpcDeactivatePerfectDodgeEffect();
    }
    
    // Public methods for other systems to use
    public float GetMomentumDamageBonus()
    {
        return 1f + (currentMomentum / maxMomentum) * momentumDamageBonus;
    }
    
    public float GetPerfectDodgeMultiplier()
    {
        return isPerfectDodgeActive ? perfectDodgeReward : 1f;
    }
    
    public bool IsHighMomentum()
    {
        return currentMomentum > maxMomentum * 0.7f;
    }
    
    public bool CanPhaseThrough()
    {
        return isDashing; // Can phase through certain attacks while dashing
    }
    
    // Client RPCs for visual effects
    [ClientRpc]
    void RpcPlayDashEffect(Vector2 direction)
    {
        if (!isLocalPlayer) return;
        
        // Create dash trail effect
        var trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.enabled = true;
            trail.time = dashDuration;
        }
        
        StartCoroutine(DisableDashEffect());
    }
    
    IEnumerator DisableDashEffect()
    {
        yield return new WaitForSeconds(dashDuration + 0.1f);
        
        var trail = GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.enabled = false;
        }
    }
    
    [ClientRpc]
    void RpcPlayDashRechargeEffect()
    {
        if (!isLocalPlayer) return;
        
        // Play dash recharge sound/visual
    }
    
    [ClientRpc]
    void RpcPlayWallJumpEffect(Vector2 direction)
    {
        if (!isLocalPlayer) return;
        
        // Create wall jump particle effect
    }
    
    [ClientRpc]
    void RpcStartWallSlideEffect()
    {
        if (!isLocalPlayer) return;
        
        // Start wall slide particle effect
    }
    
    [ClientRpc]
    void RpcStopWallSlideEffect()
    {
        if (!isLocalPlayer) return;
        
        // Stop wall slide effect
    }
    
    [ClientRpc]
    void RpcActivatePerfectDodgeEffect()
    {
        if (!isLocalPlayer) return;
        
        if (perfectDodgeEffect != null)
        {
            perfectDodgeEffect.SetActive(true);
        }
        
        // Play perfect dodge sound
    }
    
    [ClientRpc]
    void RpcDeactivatePerfectDodgeEffect()
    {
        if (!isLocalPlayer) return;
        
        if (perfectDodgeEffect != null)
        {
            perfectDodgeEffect.SetActive(false);
        }
    }
    
    // SyncVar hooks
    void OnDashChargesChanged(int oldCharges, int newCharges)
    {
        if (!isLocalPlayer) return;
        
        for (int i = 0; i < dashChargeIndicators.Length; i++)
        {
            if (dashChargeIndicators[i] != null)
            {
                dashChargeIndicators[i].SetActive(i < newCharges);
            }
        }
    }
    
    void OnMomentumChanged(float oldMomentum, float newMomentum)
    {
        if (!isLocalPlayer) return;
        
        if (momentumText != null)
        {
            if (newMomentum > 0.1f)
            {
                momentumText.text = $"Momentum: {newMomentum:F1}";
                momentumText.gameObject.SetActive(true);
                
                // Color based on momentum level
                float momentumRatio = newMomentum / maxMomentum;
                if (momentumRatio > 0.8f)
                    momentumText.color = Color.red;
                else if (momentumRatio > 0.5f)
                    momentumText.color = Color.yellow;
                else
                    momentumText.color = Color.white;
            }
            else
            {
                momentumText.gameObject.SetActive(false);
            }
        }
    }
    
    void FixedUpdate()
    {
        if (!isServer) return;
        
        // Apply wall sliding physics
        if (isWallSliding && rb.velocity.y < 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, -wallSlideSpeed);
        }
    }
}