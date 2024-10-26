using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Mirror;

public class MouseShooting : NetworkBehaviour
{
    public GameObject bulletPrefab;
    public Transform firePoint;
    public Camera playerCamera;  // Use this to directly reference the player's camera
    public float bulletSpeed = 10f;
    public float maxShootDistance = 50f;
    public int maxAmmo = 3;
    public float bulletLifetime = 3f;
    public LineRenderer lineRenderer;
    public float reloadTime = 2f;
    public int playerHP = 100;

    // UI Elements
    public TextMeshProUGUI ammoText;
    public GameObject aimingSprite;

    [SyncVar(hook = nameof(OnAmmoChanged))]  // Sync the current ammo across clients
    public int currentAmmo;

    private bool isReloading = false;

    void Start()
    {
        aimingSprite.SetActive(false);
        if (!isLocalPlayer) return;  // Only set up camera for the local player

        // Enable the camera only for the local player
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            playerCamera.GetComponent<CameraFollow>().target = transform; // Attach the camera to follow the player
        }

        currentAmmo = maxAmmo; // Initialize ammo for the local player
        UpdateUI();
    }

    void Update()
    {
        if (!isLocalPlayer) return;  // Only control the local player

        if (currentAmmo > 0)
        {
            ShowAimingSprite();
        }
        else
        {
            aimingSprite.SetActive(false);  // Hide the aiming sprite if no ammo
        }

        // Fire when clicking and not reloading
        if (Input.GetButtonDown("Fire1") && currentAmmo > 0 && !isReloading)
        {
            Debug.Log("Player tried to shoot");  // Debugging log

            // Calculate mouse position and direction on the client side
            
            Shoot();
        }

        // Start reloading if ammo is depleted and not already reloading
        if (currentAmmo <= 0 && !isReloading)
        {
            StartCoroutine(AutoReload());
        }

        UpdateUI();  // Update the UI with ammo info
    }

    void Shoot()
    {
        currentAmmo--;
        Vector3 mousePosition = playerCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, playerCamera.nearClipPlane));
        mousePosition.z = 0f;  // Set Z position for 2D calculations

        Debug.Log("Mouse Position: " + mousePosition);
        Debug.Log("Fire Point Position: " + firePoint.position);
        Vector3 direction = (mousePosition - firePoint.position).normalized;
        Debug.Log("Direction: " + direction);
        if (!isLocalPlayer) return;
        CmdShoot(direction);
    }

    // Command that runs on the server to handle shooting, now receives direction from client
    [Command]
    void CmdShoot(Vector3 direction)
    {
        // Create the bullet on the server
        Vector3 spawnPosition = firePoint.position + direction * 0.6f;
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        bullet.GetComponent<Rigidbody2D>().velocity = direction * bulletSpeed;

        // Assign the player's ID to the bullet's shooterId
        bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId; // Assuming the player has an 'Enemy' script attached with an ID

        NetworkServer.Spawn(bullet);

        Destroy(bullet, bulletLifetime);
    }


    // Coroutine to handle automatic reloading
    IEnumerator AutoReload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;  // Reset ammo
        isReloading = false;
        UpdateUI();
    }

    // Update UI with current ammo count
    void UpdateUI()
    {
        if (ammoText != null && isLocalPlayer)
        {
            ammoText.text = "Ammo: " + currentAmmo + " / " + maxAmmo;
        }
    }

    // This hook is called whenever the ammo count changes (SyncVar)
    void OnAmmoChanged(int oldAmmo, int newAmmo)
    {
        UpdateUI();  // Update UI when ammo changes
    }

    // Show the aiming sprite with rotation based on mouse position
    void ShowAimingSprite()
    {
        if (playerCamera == null) return;

        Vector3 mousePosition = playerCamera.ScreenPointToRay(Input.mousePosition).GetPoint(10f);
        Vector3 direction = (mousePosition - firePoint.position).normalized;
        aimingSprite.SetActive(true);
        aimingSprite.transform.position = firePoint.position;

        // Calculate the angle and rotate the aiming sprite
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        aimingSprite.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }
}
