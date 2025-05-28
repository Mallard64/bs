using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class BotPlayer : MonoBehaviour
{
    public Transform player;            // Reference to Player 1
    public GameObject bulletPrefab;     // The bullet prefab that the bot will shoot
    public Transform firePoint;         // The point where the bullet is fired from
    public float moveSpeed = 0.03f;        // Speed at which the bot moves
    public float shootingRange = 5f;    // Range within which the bot will shoot
    public float fireRate = 2f;         // Time between shots
    private float nextFireTime;         // Tracks when the bot can shoot again
    public float dist = 5f;
    public System.Random r;


    private void Start()
    {
        r = new System.Random();
        player = FindAnyObjectByType<PlayerMovement>().transform;
    }

    private void Update()
    {
        //if (GetComponent<Enemy>().health >= 50)
        //{

        //}
        //else
        //{
        //    RunLogicAggro();
        //}
        RunLogicAggro();
    }

    void RunLogicAggro()
    {
        MoveTowardsPlayer(dist);
        StartCoroutine(DecideAttack());
    }

    IEnumerator DecideAttack()
    {
        if (r.NextDouble() > 0.9)
        {
            StartCoroutine(AttackA());
        }
        else
        {
            StartCoroutine(AttackB());
        }
        yield return new WaitForSeconds(5f);
    }

    void ShowIndicator()
    {
        Debug.Log("Gonna attack!");
        GetComponent<SpriteRenderer>().color = Color.red;
    }

    void HideIndicator()
    {
        Debug.Log("You're cooked!");
        GetComponent<SpriteRenderer>().color = Color.white;
    }

    IEnumerator AttackA()
    {
        ShowIndicator();
        yield return new WaitForSeconds(2f);
        HideIndicator();
        transform.position = player.transform.position;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if the bot is within range and the cooldown allows for shooting
        if (distanceToPlayer <= shootingRange && Time.time >= nextFireTime)
        {
            // Aim at the player
            Vector3 direction = (player.position - firePoint.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

            // Fire a bullet
            FireBullet();

            // Set the next fire time
            nextFireTime = Time.time + fireRate;
        }
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator AttackB()
    {
        for (int i = -2; i <= 2; i++)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= shootingRange && Time.time >= nextFireTime)
            {
                // Aim at the player
                Vector3 direction = (player.position - firePoint.position).normalized;
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + i * 10;
                firePoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

                // Fire a bullet
                FireBullet();

                // Set the next fire time
                nextFireTime = Time.time + fireRate;
            }
        }
        yield return new WaitForSeconds(0.5f);
    }
    

    // Bot moves toward the player
    void MoveTowardsPlayer(float dist)
    {
        // Calculate the direction to move towards the player
        Vector3 direction = (player.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, player.position);

        Debug.Log("Bot Y Position: " + transform.position.y);
        Debug.Log("Player Y Position: " + player.position.y);

        // Move the bot towards the player if it's out of shooting range
        if (distance > dist)
        {
            transform.position += direction * moveSpeed * Time.deltaTime;
        }

    }

    // Bot shoots at the player when within range
    void ShootAtPlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Check if the bot is within range and the cooldown allows for shooting
        if (distanceToPlayer <= shootingRange && Time.time >= nextFireTime)
        {
            // Aim at the player
            Vector3 direction = (player.position - firePoint.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(new Vector3(0, 0, angle));

            // Fire a bullet
            FireBullet();

            // Set the next fire time
            nextFireTime = Time.time + fireRate;
        }
    }

    // Instantiates a bullet and fires it towards the player
    void FireBullet()
    {
        // Create the bullet at the fire point
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);

        // Give the bullet velocity towards the player
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.velocity = firePoint.right * 10f;  // Adjust the speed of the bullet as needed

        // Destroy the bullet after a few seconds to avoid memory issues
        Destroy(bullet, 3f);
    }
}
