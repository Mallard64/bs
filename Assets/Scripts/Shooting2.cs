using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Mirror;

public class MouseShooting2 : MouseShooting
{
    public GameObject explosionRadius;
    // Command that runs on the server to handle shooting, now receives direction from client
    [Command]
    public override void CmdShoot(Vector3 direction)
    {
        System.Random r = new System.Random();
        // Create the bullet on the server
        for (int i = 0; i < 8; i++)
        {
            // Rotate the direction by 90 degrees around the Z-axis for 2D
            Vector3 rotatedDirection = Quaternion.Euler(0, 0, r.Next(-30,30)) * direction;
            Vector3 spawnPosition = firePoint.position + rotatedDirection * 0.6f;

            // Instantiate the bullet
            GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

            // Set the bullet's velocity and assign shooter ID
            bullet.GetComponent<Rigidbody2D>().velocity = rotatedDirection * bulletSpeed;
            bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

            // Spawn the bullet on the server
            NetworkServer.Spawn(bullet);

            // Destroy the bullet after a set lifetime to avoid memory issues
            Destroy(bullet, bulletLifetime);
        }
    }
    // Show the aiming sprite with rotation based on mouse position
    public override void ShowAimingSprite()
    {
        if (playerCamera == null) return;

        Vector3 mousePosition = playerCamera.ScreenPointToRay(Input.mousePosition).GetPoint(10f);
        Vector3 direction = (mousePosition - firePoint.position).normalized;
        aimingSprite.SetActive(true);
        aimingSprite.transform.position = firePoint.position;

        // Calculate the angle and rotate the aiming sprite
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        aimingSprite.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle+90));
    }

    [Command]
    public override void CmdSuper(Vector3 direction)
    {
        GetComponent<PlayerMovement>().moveSpeed = GetComponent<PlayerMovement>().moveSpeed * 1.5f;
    }


    [Command]
    public override void Q()
    {
        currentAmmo++;
        qTimer = qTimerOriginal;
    }

    [Command]
    public override void E()
    {
        Vector3 spawnPosition = firePoint.position;
        GameObject bullet = Instantiate(explosionRadius, spawnPosition, Quaternion.identity);

        // Assign the player's ID to the bullet's shooterId
        bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

        GetComponent<Enemy>().health = 0;

        NetworkServer.Spawn(bullet);

        bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

        Destroy(bullet, 0.2f);
        eTimer = eTimerOriginal;
    }
}
