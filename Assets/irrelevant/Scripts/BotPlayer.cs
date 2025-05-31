using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BotPlayer : MonoBehaviour
{
    public Transform player;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float moveSpeed = 3f;
    public float shootingRange = 8f;
    public float fireRate = 1.5f;
    private float nextFireTime;
    public float dist = 4f;
    public System.Random r;

    private bool isAttacking = false;
    public float attackCooldown = 2f;

    // Natural movement variables
    private Vector2 inputDirection = Vector2.zero;
    private float directionChangeTimer = 0f;
    private float directionChangeInterval = 2f;

    // Prediction and reaction
    private Vector3 lastPlayerPos;
    private Vector3 playerVelocity;
    private float reactionDelay = 0.3f;
    private float nextReactionTime = 0f;

    // Movement state
    private MovementState currentState = MovementState.Approach;
    private float stateTimer = 0f;
    private float stateChangeCooldown = 3f;

    // Physics
    private Rigidbody2D rb;

    // Smart shooting
    private int consecutiveShots = 0;
    private bool isReloading = false;

    enum MovementState
    {
        Approach,    // Move toward player
        Retreat,     // Move away from player
        Strafe,      // Move sideways around player
        Hold         // Stay in position
    }

    private void Start()
    {
        r = new System.Random();
        rb = GetComponent<Rigidbody2D>();
        player = FindAnyObjectByType<PlayerMovement>().transform;
        lastPlayerPos = player.position;
        ChooseNewDirection();
    }

    private void Update()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerMovement>().transform;
            return;
        }

        UpdatePlayerTracking();
        UpdateMovementState();
        HandleMovementInput();

        if (!isAttacking && Time.time >= nextReactionTime)
        {
            if (ShouldAttack())
            {
                StartCoroutine(DecideAttack());
            }
        }
    }

    void FixedUpdate()
    {
        // Apply movement using velocity (physics-based)
        rb.velocity = inputDirection * moveSpeed;
    }

    void UpdatePlayerTracking()
    {
        playerVelocity = (player.position - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = player.position;
    }

    void UpdateMovementState()
    {
        stateTimer += Time.deltaTime;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Force state changes based on distance - maintain good fighting distance
        if (distanceToPlayer < 2f)
        {
            currentState = MovementState.Retreat;
            stateTimer = 0f;
        }
        else if (distanceToPlayer > 10f)
        {
            currentState = MovementState.Approach;
            stateTimer = 0f;
        }
        else if (distanceToPlayer > 7f && currentState == MovementState.Retreat)
        {
            // Stop retreating when we're at good distance
            currentState = MovementState.Strafe;
            stateTimer = 0f;
        }
        else if (stateTimer >= stateChangeCooldown)
        {
            // Only randomly choose new state if we're in good range (3-7 units)
            if (distanceToPlayer >= 3f && distanceToPlayer <= 7f)
            {
                ChooseNewMovementState();
            }
            stateTimer = 0f;
        }
    }

    void ChooseNewMovementState()
    {
        float rand = (float)r.NextDouble();

        // Prefer strafing when at good distance
        if (rand < 0.2f)
            currentState = MovementState.Approach;
        else if (rand < 0.3f)
            currentState = MovementState.Retreat;
        else if (rand < 0.8f)
            currentState = MovementState.Strafe;
        else
            currentState = MovementState.Hold;

        stateChangeCooldown = (float)(r.NextDouble() * 2f + 1.5f); // 1.5-3.5 seconds
    }

    void HandleMovementInput()
    {
        directionChangeTimer += Time.deltaTime;

        if (directionChangeTimer >= directionChangeInterval)
        {
            ChooseNewDirection();
        }

        Vector2 desiredDirection = Vector2.zero;
        Vector3 dirToPlayer = (player.position - transform.position).normalized;

        switch (currentState)
        {
            case MovementState.Approach:
                desiredDirection = dirToPlayer;
                // Add slight side movement for natural feel
                Vector2 sideMovement = new Vector2(-dirToPlayer.y, dirToPlayer.x) * (float)(r.NextDouble() - 0.5) * 0.3f;
                desiredDirection += sideMovement;

                // Slow down when getting close to avoid overshooting
                float distanceToPlayer = Vector3.Distance(transform.position, player.position);
                if (distanceToPlayer < 4f)
                {
                    desiredDirection *= 0.5f; // Move slower when close
                }
                break;

            case MovementState.Retreat:
                desiredDirection = -dirToPlayer;
                // Don't retreat too far - maintain fighting distance
                float currentDistance = Vector3.Distance(transform.position, player.position);
                if (currentDistance > 6f)
                {
                    desiredDirection *= 0.3f; // Slow retreat when already far enough
                }
                break;

            case MovementState.Strafe:
                Vector2 perpendicular = new Vector2(-dirToPlayer.y, dirToPlayer.x);
                float strafeDir = r.NextDouble() > 0.5 ? 1f : -1f;
                desiredDirection = perpendicular * strafeDir;

                // Add slight approach/retreat component to maintain good distance
                float strafeDistance = Vector3.Distance(transform.position, player.position);
                if (strafeDistance < 3f)
                {
                    desiredDirection += (Vector2)(-dirToPlayer) * 0.3f; // Retreat slightly while strafing
                }
                else if (strafeDistance > 6f)
                {
                    desiredDirection += (Vector2)dirToPlayer * 0.3f; // Approach slightly while strafing
                }
                break;

            case MovementState.Hold:
                desiredDirection = Vector2.zero;
                // Tiny random movements to simulate human "holding position"
                desiredDirection += new Vector2(
                    (float)(r.NextDouble() - 0.5) * 0.2f,
                    (float)(r.NextDouble() - 0.5) * 0.2f
                );
                break;
        }

        // Smooth input changes (like human gradually changing direction)
        inputDirection = Vector2.Lerp(inputDirection, desiredDirection.normalized, Time.deltaTime * 3f);

        // Add wall avoidance
        AvoidWalls();
    }

    void AvoidWalls()
    {
        // Check for walls in movement direction
        float checkDistance = 1.5f;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, inputDirection, checkDistance);

        if (hit.collider != null && hit.collider.CompareTag("Wall"))
        {
            // Wall detected - adjust direction
            Vector2 wallNormal = hit.normal;
            Vector2 avoidDirection = Vector2.Reflect(inputDirection, wallNormal);
            inputDirection = Vector2.Lerp(inputDirection, avoidDirection, Time.deltaTime * 5f);
        }

        // Also check sides for better wall avoidance
        Vector2 leftCheck = new Vector2(-inputDirection.y, inputDirection.x);
        Vector2 rightCheck = new Vector2(inputDirection.y, -inputDirection.x);

        RaycastHit2D leftHit = Physics2D.Raycast(transform.position, leftCheck, 0.8f);
        RaycastHit2D rightHit = Physics2D.Raycast(transform.position, rightCheck, 0.8f);

        if (leftHit.collider != null && leftHit.collider.CompareTag("Wall"))
        {
            inputDirection += rightCheck * 0.5f;
        }
        if (rightHit.collider != null && rightHit.collider.CompareTag("Wall"))
        {
            inputDirection += leftCheck * 0.5f;
        }
    }

    void ChooseNewDirection()
    {
        directionChangeTimer = 0f;
        directionChangeInterval = (float)(r.NextDouble() * 2f + 1f); // 1-3 seconds
    }

    bool ShouldAttack()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > shootingRange) return false;
        if (isReloading) return false;
        if (!HasClearShot()) return false;

        // Simple attack chance
        float attackChance = 0.6f - (distanceToPlayer / shootingRange) * 0.3f;
        return r.NextDouble() < attackChance;
    }

    bool HasClearShot()
    {
        Vector3 directionToPlayer = (player.position - firePoint.position).normalized;
        float distanceToPlayer = Vector3.Distance(firePoint.position, player.position);

        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, directionToPlayer, distanceToPlayer);
        return hit.collider == null || !hit.collider.CompareTag("Wall");
    }

    IEnumerator DecideAttack()
    {
        isAttacking = true;
        nextReactionTime = Time.time + reactionDelay;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Attack selection
        if (distanceToPlayer < 10f && r.NextDouble() > 0.85) // 15% chance for teleport
        {
            yield return StartCoroutine(AttackA());
        }
        else if (r.NextDouble() > 0.6) // 40% chance for spread
        {
            yield return StartCoroutine(AttackB());
        }
        else
        {
            yield return StartCoroutine(SingleShot());
        }

        // Reload behavior
        consecutiveShots++;
        if (consecutiveShots >= UnityEngine.Random.Range(4, 7))
        {
            yield return StartCoroutine(ReloadBehavior());
        }

        yield return new WaitForSeconds(attackCooldown + (float)(r.NextDouble() * 1f));
        isAttacking = false;
    }

    IEnumerator SingleShot()
    {
        if (HasClearShot())
        {
            AimAtPlayer();
            FireBullet();
            consecutiveShots++;
        }
        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator AttackA()
    {
        ShowIndicator();
        yield return new WaitForSeconds(1.5f);
        HideIndicator();

        // Teleport near player
        Vector3 offset = new Vector3(
            (float)(r.NextDouble() - 0.5) * 6f,
            (float)(r.NextDouble() - 0.5) * 6f,
            0
        );

        Vector3 teleportPos = player.position + offset;

        // Check if teleport position is safe
        Collider2D overlap = Physics2D.OverlapPoint(teleportPos);
        if (overlap == null || !overlap.CompareTag("Wall"))
        {
            transform.position = teleportPos;
            rb.velocity = Vector2.zero; // Stop momentum
        }

        AimAtPlayer();
        FireBullet();
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator AttackB()
    {
        int shots = UnityEngine.Random.Range(3, 6);

        for (int i = 0; i < shots; i++)
        {
            if (HasClearShot())
            {
                AimAtPlayer();
                float spread = (i - shots / 2f) * 12f;
                firePoint.rotation *= Quaternion.Euler(0, 0, spread);
                FireBullet();
            }
            yield return new WaitForSeconds(0.1f);
        }
        consecutiveShots += shots;
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator ReloadBehavior()
    {
        isReloading = true;
        GetComponent<SpriteRenderer>().color = Color.blue;
        yield return new WaitForSeconds(1.5f);
        GetComponent<SpriteRenderer>().color = Color.white;
        consecutiveShots = 0;
        isReloading = false;
    }

    void AimAtPlayer()
    {
        // Simple prediction
        Vector3 predictedPosition = player.position + playerVelocity * 0.25f;

        // Add slight inaccuracy
        predictedPosition += new Vector3(
            (float)(r.NextDouble() - 0.5) * 0.8f,
            (float)(r.NextDouble() - 0.5) * 0.8f,
            0
        );

        Vector3 direction = (predictedPosition - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        firePoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }

    void ShowIndicator()
    {
        GetComponent<SpriteRenderer>().color = Color.red;
    }

    void HideIndicator()
    {
        GetComponent<SpriteRenderer>().color = Color.white;
    }

    void FireBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        Bullet bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
        {
            bulletScript.shooterId = GetComponent<ActualEnemy>().connectionId;
            bulletScript.shotByEnemy = true;
        }

        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.velocity = firePoint.right * 10f;

        Destroy(bullet, 3f);
    }
}