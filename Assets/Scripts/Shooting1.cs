using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Mirror;

public class MouseShooting1 : MouseShooting
{
    // Command that runs on the server to handle shooting, now receives direction from client
    [Command]
    public override void CmdShoot(Vector3 direction)
    {
        // Create the bullet on the server
        for (int i = 0; i < 4; i++)
        {
            // Rotate the direction by 90 degrees around the Z-axis for 2D
            Vector3 rotatedDirection = Quaternion.Euler(0, 0, 90 * i) * direction;
            Vector3 spawnPosition = firePoint.position + rotatedDirection * 0.6f;

            // Instantiate the bullet
            GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
            Debug.Log("Instantiated bullet at angle: " + (90 * i));

            // Set the bullet's velocity and assign shooter ID
            bullet.GetComponent<Rigidbody2D>().velocity = rotatedDirection * bulletSpeed;
            bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

            // Spawn the bullet on the server
            NetworkServer.Spawn(bullet);

            // Destroy the bullet after a set lifetime to avoid memory issues
            Destroy(bullet, bulletLifetime);
        }
    }

    [Command]
    public override void CmdSuper(Vector3 direction)
    {
        for (int j = 0; j < 10; j++)
        {
            for (int i = 0; i < 4; i++)
            {
                // Rotate the direction by 90 degrees around the Z-axis for 2D
                Vector3 rotatedDirection = Quaternion.Euler(0, 0, (90 + 36 * j) * i) * direction;
                Vector3 spawnPosition = firePoint.position + rotatedDirection * 0.6f;

                // Instantiate the bullet
                GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
                Debug.Log("Instantiated bullet at angle: " + (90 * i));

                // Set the bullet's velocity and assign shooter ID
                bullet.GetComponent<Rigidbody2D>().velocity = rotatedDirection * (bulletSpeed * 5);
                bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

                // Spawn the bullet on the server
                NetworkServer.Spawn(bullet);

                // Destroy the bullet after a set lifetime to avoid memory issues
                Destroy(bullet, bulletLifetime);
            }
        }
        
    }


    [Command]
    public override void Q()
    {
        currentAmmo += 2;
        qTimer = qTimerOriginal;
    }

    [Command]
    public override void E()
    {
        GetComponent<PlayerMovement>().moveSpeed = GetComponent<PlayerMovement>().moveSpeed * 1.25f;
    }
}
