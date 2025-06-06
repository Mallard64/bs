using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BotPlayer : MonoBehaviour
{
    public Transform player;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float moveSpeed = 4f;
    public float shootingRange = 10f;
    public float fireRate = 1.2f;
    private float nextFireTime;
    public float dist = 5f;
    public System.Random r;

    private bool isAttacking = false;
    public float attackCooldown = 1.5f;

    // Enhanced movement variables
    private Vector2 inputDirection = Vector2.zero;
    private float directionChangeTimer = 0f;
    private float directionChangeInterval = 1.5f;

    // Prediction and reaction
    private Vector3 lastPlayerPos;
    private Vector3 playerVelocity;
    private float reactionDelay = 0.25f;
    private float nextReactionTime = 0f;

    // Enhanced movement state
    private MovementState currentState = MovementState.Hunt;
    private float stateTimer = 0f;
    private float stateChangeCooldown = 2.5f;

    // Physics
    private Rigidbody2D rb;

    // Enhanced combat system
    private int consecutiveShots = 0;
    private bool isReloading = false;
    private float aggressionLevel = 1f; // Increases over time
    private int attackPattern = 0;

    // Special abilities
    private bool canDash = true;
    private float dashCooldown = 8f;
    private float lastDashTime = 0f;

    // Visual feedback
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    enum MovementState
    {
        Hunt,        // Aggressively pursue player
        Circle,      // Circle around player at medium distance
        Retreat,     // Back away temporarily
        Ambush,      // Hold position and wait
        Berserk      // Fast, erratic movement when low health or high aggression
    }

    private void Start()
    {
        r = new System.Random();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
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
        UpdateAggressionLevel();
        UpdateMovementState();
        HandleMovementInput();

        if (!isAttacking && Time.time >= nextReactionTime)
        {
            if (ShouldAttack())
            {
                StartCoroutine(DecideAttack());
            }
        }

        UpdateSpecialAbilities();
    }

    void FixedUpdate()
    {
        // Apply movement with speed scaling based on state
        float speedMultiplier = GetSpeedMultiplier();
        rb.velocity = inputDirection * moveSpeed * speedMultiplier;
    }

    float GetSpeedMultiplier()
    {
        switch (currentState)
        {
            case MovementState.Berserk: return 1.5f;
            case MovementState.Hunt: return 1.2f;
            case MovementState.Retreat: return 1.3f;
            case MovementState.Circle: return 1f;
            case MovementState.Ambush: return 0.3f;
            default: return 1f;
        }
    }

    void UpdatePlayerTracking()
    {
        playerVelocity = (player.position - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = player.position;
    }

    void UpdateAggressionLevel()
    {
        // Increase aggression over time (makes boss more dangerous)
        aggressionLevel += Time.deltaTime * 0.1f;
        aggressionLevel = Mathf.Clamp(aggressionLevel, 1f, 3f);

        // Visual feedback for aggression
        if (aggressionLevel > 2f)
        {
            spriteRenderer.color = Color.Lerp(originalColor, Color.red, 0.3f);
        }
    }

    void UpdateMovementState()
    {
        stateTimer += Time.deltaTime;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Force state changes based on conditions
        if (aggressionLevel > 2.5f && r.NextDouble() > 0.8f)
        {
            currentState = MovementState.Berserk;
            stateTimer = 0f;
        }
        else if (distanceToPlayer < 2f && currentState != MovementState.Retreat)
        {
            currentState = MovementState.Retreat;
            stateTimer = 0f;
        }
        else if (distanceToPlayer > 12f)
        {
            currentState = MovementState.Hunt;
            stateTimer = 0f;
        }
        else if (stateTimer >= stateChangeCooldown)
        {
            ChooseNewMovementState(distanceToPlayer);
            stateTimer = 0f;
        }
    }

    void ChooseNewMovementState(float distanceToPlayer)
    {
        float rand = (float)r.NextDouble();

        // State selection based on distance and aggression
        if (distanceToPlayer < 4f)
        {
            if (rand < 0.4f) currentState = MovementState.Circle;
            else if (rand < 0.7f) currentState = MovementState.Retreat;
            else currentState = MovementState.Hunt;
        }
        else if (distanceToPlayer < 8f)
        {
            if (rand < 0.5f) currentState = MovementState.Circle;
            else if (rand < 0.8f) currentState = MovementState.Hunt;
            else currentState = MovementState.Ambush;
        }
        else
        {
            currentState = MovementState.Hunt;
        }

        stateChangeCooldown = (float)(r.NextDouble() * 2f + 1f) / aggressionLevel;
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
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case MovementState.Hunt:
                desiredDirection = dirToPlayer;
                // Predict player movement for interception
                Vector3 predictedPos = player.position + playerVelocity * 0.5f;
                desiredDirection = (predictedPos - transform.position).normalized;
                break;

            case MovementState.Circle:
                Vector2 perpendicular = new Vector2(-dirToPlayer.y, dirToPlayer.x);
                float circleDir = Mathf.Sign(Vector3.Cross(dirToPlayer, transform.right).z);
                desiredDirection = perpendicular * circleDir;

                // Maintain optimal distance while circling
                if (distanceToPlayer < 4f)
                    desiredDirection += (Vector2)(-dirToPlayer) * 0.4f;
                else if (distanceToPlayer > 7f)
                    desiredDirection += (Vector2)dirToPlayer * 0.4f;
                break;

            case MovementState.Retreat:
                desiredDirection = -dirToPlayer;
                // Add evasive side movement
                Vector2 evasion = new Vector2(-dirToPlayer.y, dirToPlayer.x) * (float)(r.NextDouble() - 0.5) * 2f;
                desiredDirection += evasion * 0.5f;
                break;

            case MovementState.Ambush:
                // Stay mostly still but track player
                desiredDirection = Vector2.zero;
                break;

            case MovementState.Berserk:
                // Erratic, aggressive movement
                Vector2 randomComponent = new Vector2(
                    (float)(r.NextDouble() - 0.5) * 2f,
                    (float)(r.NextDouble() - 0.5) * 2f
                );
                desiredDirection = (dirToPlayer + (Vector3)randomComponent * 0.7f).normalized;
                break;
        }

        GetComponent<Animator>().Play("walk");

        // Smooth input changes
        inputDirection = Vector2.Lerp(inputDirection, desiredDirection.normalized, Time.deltaTime * 4f);

        // Enhanced wall avoidance
        AvoidWalls();
    }

    void AvoidWalls()
    {
        float checkDistance = 2f;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, inputDirection, checkDistance);

        if (hit.collider != null && hit.collider.CompareTag("Wall"))
        {
            Vector2 wallNormal = hit.normal;
            Vector2 avoidDirection = Vector2.Reflect(inputDirection, wallNormal);
            inputDirection = Vector2.Lerp(inputDirection, avoidDirection, Time.deltaTime * 6f);
        }
    }

    void UpdateSpecialAbilities()
    {
        // Dash ability cooldown
        if (Time.time - lastDashTime > dashCooldown)
        {
            canDash = true;
        }
    }

    void ChooseNewDirection()
    {
        directionChangeTimer = 0f;
        directionChangeInterval = (float)(r.NextDouble() * 1.5f + 0.5f) / aggressionLevel;
    }

    bool ShouldAttack()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer > shootingRange) return false;
        if (isReloading) return false;
        if (!HasClearShot()) return false;

        // Aggression affects attack frequency
        float attackChance = (0.6f + aggressionLevel * 0.2f) - (distanceToPlayer / shootingRange) * 0.3f;
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
        nextReactionTime = Time.time + reactionDelay / aggressionLevel;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Choose attack pattern based on aggression and situation
        if (canDash && distanceToPlayer > 6f && r.NextDouble() > 0.7f)
        {
            yield return StartCoroutine(DashAttack());
        }
        else if (aggressionLevel > 2f && r.NextDouble() > 0.6f)
        {
            yield return StartCoroutine(RapidFire());
        }
        else if (distanceToPlayer < 8f && r.NextDouble() > 0.8f)
        {
            yield return StartCoroutine(TeleportStrike());
        }
        else if (r.NextDouble() > 0.5f)
        {
            yield return StartCoroutine(SpreadShot());
        }
        else
        {
            yield return StartCoroutine(PrecisionShot());
        }

        // Reload behavior
        consecutiveShots++;
        if (consecutiveShots >= UnityEngine.Random.Range(5, 9))
        {
            yield return StartCoroutine(ReloadBehavior());
        }

        float cooldown = attackCooldown / aggressionLevel;
        yield return new WaitForSeconds(cooldown + (float)(r.NextDouble() * 0.5f));
        isAttacking = false;
    }

    IEnumerator PrecisionShot()
    {
        AimAtPredictedPosition();
        ShowAttackIndicator(Color.yellow, 0.3f);
        yield return new WaitForSeconds(0.3f);

        if (HasClearShot())
        {
            FireBullet();
            consecutiveShots++;
        }
        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator SpreadShot()
    {
        int shots = UnityEngine.Random.Range(3, 6);
        ShowAttackIndicator(Color.red, 0.5f);
        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < shots; i++)
        {
            AimAtPredictedPosition();
            float spread = (i - shots / 2f) * 15f;
            firePoint.rotation *= Quaternion.Euler(0, 0, spread);
            FireBullet();
            yield return new WaitForSeconds(0.08f);
        }
        consecutiveShots += shots;
        firePoint.rotation = Quaternion.Euler(0, 0, 0);
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator RapidFire()
    {
        int shots = UnityEngine.Random.Range(6, 10);
        ShowAttackIndicator(Color.red, 0.8f);
        yield return new WaitForSeconds(0.8f);

        for (int i = 0; i < shots; i++)
        {
            if (HasClearShot())
            {
                AimAtPredictedPosition();
                FireBullet();
            }
            yield return new WaitForSeconds(0.12f);
        }
        consecutiveShots += shots;
        yield return new WaitForSeconds(0.4f);
    }

    IEnumerator TeleportStrike()
    {
        ShowAttackIndicator(Color.magenta, 1.2f);
        yield return new WaitForSeconds(1.2f);

        // Teleport near player
        Vector3 offset = new Vector3(
            (float)(r.NextDouble() - 0.5) * 5f,
            (float)(r.NextDouble() - 0.5) * 5f,
            0
        );

        Vector3 teleportPos = player.position + offset;
        Collider2D overlap = Physics2D.OverlapPoint(teleportPos);

        if (overlap == null || !overlap.CompareTag("Wall"))
        {
            // Teleport effect
            spriteRenderer.color = Color.white;
            transform.position = teleportPos;
            rb.velocity = Vector2.zero;

            // Quick shot after teleport
            yield return new WaitForSeconds(0.2f);
            AimAtPredictedPosition();
            FireBullet();
            FireBullet(); // Double shot
        }

        yield return new WaitForSeconds(0.4f);
    }

    IEnumerator DashAttack()
    {
        canDash = false;
        lastDashTime = Time.time;

        ShowAttackIndicator(Color.cyan, 0.8f);
        Vector3 dashDirection = (player.position - transform.position).normalized;

        yield return new WaitForSeconds(0.8f);

        // Dash toward player
        rb.velocity = dashDirection * moveSpeed * 3f;
        spriteRenderer.color = Color.cyan;

        yield return new WaitForSeconds(0.3f);

        // Fire shots during dash
        for (int i = 0; i < 3; i++)
        {
            AimAtPredictedPosition();
            FireBullet();
            yield return new WaitForSeconds(0.1f);
        }

        consecutiveShots += 3;
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator ReloadBehavior()
    {
        isReloading = true;
        ShowAttackIndicator(Color.blue, 2f);
        yield return new WaitForSeconds(2f);
        consecutiveShots = 0;
        isReloading = false;
    }

    void ShowAttackIndicator(Color color, float duration)
    {
        StartCoroutine(FlashColor(color, duration));
    }

    IEnumerator FlashColor(Color color, float duration)
    {
        spriteRenderer.color = color;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = originalColor;
    }

    void AimAtPredictedPosition()
    {
        float bulletSpeed = 10f;
        float timeToTarget = Vector3.Distance(firePoint.position, player.position) / bulletSpeed;
        Vector3 predictedPosition = player.position + playerVelocity * timeToTarget * 0.8f;

        // Add inaccuracy that decreases with aggression
        float inaccuracy = 1f / aggressionLevel;
        predictedPosition += new Vector3(
            (float)(r.NextDouble() - 0.5) * inaccuracy,
            (float)(r.NextDouble() - 0.5) * inaccuracy,
            0
        );

        Vector3 direction = (predictedPosition - firePoint.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        firePoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
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

        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        bulletRb.velocity = firePoint.right * 8f;

        Destroy(bullet, 3f);
    }
}