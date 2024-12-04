using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Mirror;

public class MouseShooting3 : MouseShooting
{
    public GameObject wall;
    public int shotmultiplier = 1;
    public int speedmultiplier = 1;
    // Command that runs on the server to handle shooting, now receives direction from client

    public override void Shoot()
    {
        currentAmmo--;
        Vector3 mousePosition = playerCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, playerCamera.nearClipPlane));
        mousePosition.z = 0f;  // Set Z position for 2D calculations

        Vector3 direction = (playerCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, playerCamera.nearClipPlane)) - firePoint.position).normalized;
        if (Vector3.Distance(playerCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, playerCamera.nearClipPlane)), firePoint.position) > maxShootDistance)
        {
            return;
        }
        
        if (!isLocalPlayer) return;
        CmdShoot(direction);
        shotTimer = shotTimerOriginal;
    }


    [Command]
    public override void CmdShoot(Vector3 direction)
    {
        Vector3 rotatedDirection = Quaternion.Euler(0, 0, 0) * direction;
        Vector3 spawnPosition = firePoint.position + direction * 0.6f;
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

        // Set the bullet's velocity and assign shooter ID
        bullet.GetComponent<Rigidbody2D>().velocity = rotatedDirection * bulletSpeed * speedmultiplier;
        bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
        bullet.GetComponent<Bullet>().bulletDamage *= shotmultiplier;

        // Spawn the bullet on the server
        NetworkServer.Spawn(bullet);

        // Destroy the bullet after a set lifetime to avoid memory issues
        Destroy(bullet, bulletLifetime);
        bullet.GetComponent<BoxCollider2D>().enabled = false;
        StartCoroutine(Throwable(bullet));
    }

    IEnumerator Throwable(GameObject bullet)
    {
        
        yield return new WaitForSeconds(0.9f);
        bullet.GetComponent<BoxCollider2D>().enabled = true;
    }

    // Show the aiming sprite with rotation based on mouse position
    public override void ShowAimingSprite()
    {
        if (playerCamera == null) return;

        // Get the mouse position in world coordinates
        Vector3 mousePosition = playerCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, playerCamera.nearClipPlane));

        // Calculate direction from firePoint to mousePosition
        Vector3 direction = (mousePosition - firePoint.position).normalized;

        // Calculate the distance from firePoint to mousePosition
        float distance = Vector3.Distance(firePoint.position, mousePosition);

        // Clamp the position if it's beyond maxShootDistance
        if (distance > maxShootDistance)
        {
            aimingSprite.SetActive(false);
            return;
        }

        // Update the aiming sprite's position
        aimingSprite.transform.position = mousePosition;

        // Calculate the angle for rotation
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Set the rotation of the aiming sprite
        aimingSprite.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle + 90)); // Adjust by +90 degrees if needed

        // Show the aiming sprite
        aimingSprite.SetActive(true);

    }

    [Command]
    public override void CmdSuper(Vector3 direction)
    {
        Vector3 spawnPosition = firePoint.position;
        GameObject bullet = Instantiate(wall, spawnPosition, Quaternion.identity);

        // Assign the player's ID to the bullet's shooterId
        bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

        NetworkServer.Spawn(bullet);

        bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

        Destroy(bullet, 20f);
        eTimer = eTimerOriginal;

    }

    [Command]
    public override void Q()
    {
        shotmultiplier *= 3;
    }

    [Command]
    public override void E()
    {
        speedmultiplier *= 3;
    }
}
