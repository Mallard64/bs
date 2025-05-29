using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BotPlayer : MonoBehaviour
{
    public Transform player;
    public GameObject bulletPrefab;
    public Transform firePoint;
    public float moveSpeed = 0.03f;
    public float shootingRange = 8f;
    public float fireRate = 1.5f;
    private float nextFireTime;
    public float dist = 4f;
    public System.Random r;

    private bool isAttacking = false;
    public float attackCooldown = 2f;

    // Human-like behavior variables
    private Vector3 targetPosition;
    private float repositionTimer = 0f;
    private float repositionInterval;
    private bool isRepositioning = false;

    // Prediction and reaction
    private Vector3 lastPlayerPos;
    private Vector3 playerVelocity;
    private float reactionDelay = 0.2f;
    private float nextReactionTime = 0f;

    // Movement patterns
    private float strafeDirection = 1f;
    private float strafeTimer = 0f;
    private float strafeDuration;

    // Smart shooting
    private LayerMask wallLayerMask = -1; // Set this in inspector to wall layer
    private float lastShotTime = 0f;
    private int consecutiveShots = 0;
    private bool isReloading = false;

    private void Start()
    {
        r = new System.Random();
        player = FindAnyObjectByType<PlayerMovement>().transform;
        targetPosition = transform.position;
        lastPlayerPos = player.position;
        RandomizeRepositionInterval();
        RandomizeStrafePattern();
    }

    private void Update()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<PlayerMovement>().transform;
            return;
        }

        UpdatePlayerTracking();
        RunHumanLikeLogic();
    }

    void UpdatePlayerTracking()
    {
        // Calculate player velocity for prediction
        playerVelocity = (player.position - lastPlayerPos) / Time.deltaTime;
        lastPlayerPos = player.position;
    }

    void RunHumanLikeLogic()
    {
        // Human-like positioning
        HandleRepositioning();
        MoveTowardsTarget();

        // Human-like attacking with smart timing
        if (!isAttacking && Time.time >= nextReactionTime)
        {
            //if (ShouldAttack())
            //{
            //    StartCoroutine(DecideAttack());
            //}
            StartCoroutine(DecideAttack());
        }
    }

    void HandleRepositioning()
    {
        repositionTimer += Time.deltaTime;

        if (repositionTimer >= repositionInterval || isRepositioning)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            // Choose new position based on situation
            if (distanceToPlayer < 2f) // Too close - back away
            {
                Vector3 awayFromPlayer = (transform.position - player.position).normalized;
                targetPosition = transform.position + awayFromPlayer * 3f;
            }
            else if (distanceToPlayer > 10f) // Too far - get closer
            {
                Vector3 towardsPlayer = (player.position - transform.position).normalized;
                targetPosition = player.position - towardsPlayer * dist;
            }
            else // Good distance - strafe around player
            {
                Vector3 dirToPlayer = (player.position - transform.position).normalized;
                Vector3 perpendicular = new Vector3(-dirToPlayer.y, dirToPlayer.x, 0);
                float strafeDistance = UnityEngine.Random.Range(2f, 4f);
                targetPosition = transform.position + perpendicular * strafeDirection * strafeDistance;
            }

            // Add some randomness to make it less predictable
            targetPosition += new Vector3(
                (float)(r.NextDouble() - 0.5) * 2f,
                (float)(r.NextDouble() - 0.5) * 2f,
                0
            );

            repositionTimer = 0f;
            isRepositioning = true;
            RandomizeRepositionInterval();
        }

        // Check if reached target position
        if (Vector3.Distance(transform.position, targetPosition) < 0.5f)
        {
            isRepositioning = false;
        }
    }

    void MoveTowardsTarget()
    {
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance > 0.1f)
        {
            // Add some human-like imperfection to movement
            float wobble = Mathf.Sin(Time.time * 3f) * 0.1f;
            Vector3 wobbleOffset = new Vector3(wobble, wobble * 0.5f, 0);

            transform.position += (direction + wobbleOffset) * moveSpeed * Time.deltaTime;
        }

        // Handle strafing when near player
        HandleStrafing();
    }

    void HandleStrafing()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= shootingRange && distanceToPlayer >= 2f)
        {
            strafeTimer += Time.deltaTime;

            if (strafeTimer >= strafeDuration)
            {
                RandomizeStrafePattern();
            }

            // Strafe perpendicular to player
            Vector3 dirToPlayer = (player.position - transform.position).normalized;
            Vector3 strafeDir = new Vector3(-dirToPlayer.y, dirToPlayer.x, 0) * strafeDirection;
            transform.position += strafeDir * moveSpeed * 0.5f * Time.deltaTime;
        }
    }

    void RandomizeRepositionInterval()
    {
        repositionInterval = (float)(r.NextDouble() * 3f + 1f); // 1-4 seconds
    }

    void RandomizeStrafePattern()
    {
        strafeDirection = r.NextDouble() > 0.5 ? 1f : -1f;
        strafeDuration = (float)(r.NextDouble() * 2f + 1f); // 1-3 seconds
        strafeTimer = 0f;
    }

    bool ShouldAttack()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Don't attack if too far
        if (distanceToPlayer > shootingRange) return false;

        // Don't attack if reloading (human-like reload behavior)
        if (isReloading) return false;

        // Check if we have clear shot (no walls in the way)
        if (!HasClearShot()) return false;

        // Don't attack if player is moving too fast (harder to hit)
        if (playerVelocity.magnitude > 8f && r.NextDouble() > 0.3) return false;

        // More likely to attack if player is closer
        float attackChance = 1f - (distanceToPlayer / shootingRange);
        attackChance += 0.3f; // Base chance

        return r.NextDouble() < attackChance;
    }

    bool HasClearShot()
    {
        Vector3 directionToPlayer = (player.position - firePoint.position).normalized;
        float distanceToPlayer = Vector3.Distance(firePoint.position, player.position);

        RaycastHit2D hit = Physics2D.Raycast(firePoint.position, directionToPlayer, distanceToPlayer, wallLayerMask);
        return hit.collider == null; // No wall in the way
    }

    IEnumerator DecideAttack()
    {
        isAttacking = true;
        nextReactionTime = Time.time + reactionDelay;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Choose attack based on distance and situation
        if (distanceToPlayer < 3f && r.NextDouble() > 0.7)
        {
            yield return StartCoroutine(AttackA()); // Teleport attack for close range
        }
        else if (consecutiveShots < 3 && r.NextDouble() > 0.4)
        {
            yield return StartCoroutine(AttackB()); // Spread shot
        }
        else
        {
            yield return StartCoroutine(SingleShot()); // Single precise shot
        }

        // Human-like reload behavior
        consecutiveShots++;
        if (consecutiveShots >= 5)
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
            AimAtPredictedPosition();
            FireBullet();
            consecutiveShots++;
        }
        yield return new WaitForSeconds(0.1f);
    }

    IEnumerator AttackA()
    {
        ShowIndicator();
        yield return new WaitForSeconds(1.5f); // Shorter telegraph
        HideIndicator();

        // Only teleport if it makes sense
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer < 6f && HasClearShot())
        {
            // Teleport near player but not exactly on them
            
        }
        Vector3 offset = new Vector3(
                (float)(r.NextDouble() - 0.5) * 4f,
                (float)(r.NextDouble() - 0.5) * 4f,
                0
            );
        transform.position = player.position + offset;

        AimAtPredictedPosition();
        FireBullet();
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator AttackB()
    {
        int shots = UnityEngine.Random.Range(3, 6); // Variable shot count

        for (int i = 0; i < shots; i++)
        {
            if (HasClearShot())
            {
                
            }
            AimAtPredictedPosition();
            float spread = (i - shots / 2f) * 15f; // Spread shots
            firePoint.rotation *= Quaternion.Euler(0, 0, spread);
            FireBullet();
            yield return new WaitForSeconds(0.1f);
        }
        consecutiveShots += shots;
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator ReloadBehavior()
    {
        isReloading = true;
        GetComponent<SpriteRenderer>().color = Color.blue; // Visual indicator
        yield return new WaitForSeconds(2f); // Reload time
        GetComponent<SpriteRenderer>().color = Color.white;
        consecutiveShots = 0;
        isReloading = false;
    }

    void AimAtPredictedPosition()
    {
        // Predict where player will be
        float bulletSpeed = 10f;
        float timeToTarget = Vector3.Distance(firePoint.position, player.position) / bulletSpeed;
        Vector3 predictedPosition = player.position + playerVelocity * timeToTarget;

        // Add some human imperfection
        predictedPosition += new Vector3(
            (float)(r.NextDouble() - 0.5) * 1f,
            (float)(r.NextDouble() - 0.5) * 1f,
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
        lastShotTime = Time.time;
    }
}