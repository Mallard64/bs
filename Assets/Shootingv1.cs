using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MouseShootingv1 : MonoBehaviour
{
    public GameObject bulletPrefab;
    public GameObject superPrefab;
    public GameObject smallbulletPrefab;
    public Transform firePoint;
    public Camera playerCamera;  // Use this to directly reference the player's camera
    public float bulletSpeed = 10f;
    public float maxShootDistance = 50f;
    public int maxAmmo = 3;
    public float bulletLifetime = 3f;
    public LineRenderer lineRenderer;
    public float reloadTime = 2f;
    public int playerHP = 100;
    public int maxSuper;
    public int superCharge;
    public int q = 3;
    public int e = 3;

    public float shotTimerOriginal;
    public float shotTimer;

    public float qTimerOriginal;
    public float qTimer;

    public float eTimerOriginal;
    public float eTimer;

    public string AnimationName;

    public GameObject weapon;

    // UI Elements
    public TextMeshProUGUI ammoText;
    public TextMeshProUGUI superText;
    public TextMeshProUGUI qText;
    public TextMeshProUGUI eText;
    public GameObject aimingSprite;

    public int currentAmmo;

    public bool isReloading = false;

    void Start()
    {
        playerCamera = Camera.main;
        aimingSprite.SetActive(false);

        currentAmmo = maxAmmo; // Initialize ammo for the local player
    }

    void Update()
    {

        if (currentAmmo > 0)
        {
            ShowAimingSprite();
        }
        else
        {
            aimingSprite.SetActive(false);  // Hide the aiming sprite if no ammo
        }

        // Fire when clicking and not reloading
        if (Input.GetKeyDown(KeyCode.Mouse0) && currentAmmo > 0 && !isReloading && shotTimer <= 0)
        {
            Debug.Log("Player tried to shoot");  // Debugging log

            // Calculate mouse position and direction on the client side

            Shoot();
        }

        if (Input.GetKeyDown(KeyCode.Mouse1) && superCharge >= maxSuper)
        {
            Debug.Log("Player tried to SUPER");  // Debugging log

            // Calculate mouse position and direction on the client side

            Super();
        }

        if (Input.GetKeyDown(KeyCode.Q) && q > 0 && qTimer <= 0)
        {
            Debug.Log("Player tried to use Q");  // Debugging log

            // Calculate mouse position and direction on the client side
            q--;
            Q();
        }

        if (Input.GetKeyDown(KeyCode.E) && e > 0 && eTimer <= 0)
        {
            Debug.Log("Player tried to use Q");  // Debugging log

            // Calculate mouse position and direction on the client side
            e--;
            E();
        }

        // Start reloading if ammo is depleted and not already reloading
        if (currentAmmo <= 0 && !isReloading)
        {
            StartCoroutine(AutoReload());
        }

        shotTimer -= Time.deltaTime;
        qTimer -= Time.deltaTime;
        eTimer -= Time.deltaTime;
    }

    public virtual void Shoot()
    {
        if (weapon != null)
        {
            // Get the Animator component from the target object
            Animator targetAnimator = weapon.GetComponent<Animator>();

            // Check if the target object has an Animator
            if (targetAnimator != null)
            {
                targetAnimator.Play(AnimationName);  // Play the specified animation
            }
            else
            {
                Debug.LogWarning("No Animator found on the target GameObject: " + weapon.name);
            }
        }
        currentAmmo--;
        Vector3 mousePosition = playerCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, playerCamera.nearClipPlane));
        mousePosition.z = 0f;  // Set Z position for 2D calculations

        Debug.Log("Mouse Position: " + mousePosition);
        Debug.Log("Fire Point Position: " + firePoint.position);
        Vector3 direction = (mousePosition - firePoint.position).normalized;
        Debug.Log("Direction: " + direction);
        CmdShoot(direction);
        shotTimer = shotTimerOriginal;
    }

    void Super()
    {
        if (weapon != null)
        {
            // Get the Animator component from the target object
            Animator targetAnimator = weapon.GetComponent<Animator>();

            // Check if the target object has an Animator
            if (targetAnimator != null)
            {
                targetAnimator.Play(AnimationName);  // Play the specified animation
            }
            else
            {
                Debug.LogWarning("No Animator found on the target GameObject: " + weapon.name);
            }
        }
        superCharge = 0;
        Vector3 mousePosition = playerCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, playerCamera.nearClipPlane));
        mousePosition.z = 0f;  // Set Z position for 2D calculations

        Debug.Log("Mouse Position: " + mousePosition);
        Debug.Log("Fire Point Position: " + firePoint.position);
        Vector3 direction = (mousePosition - firePoint.position).normalized;
        Debug.Log("Direction: " + direction);
        CmdSuper(direction);
    }

    public virtual void Q()
    {
        Vector3 mousePosition = playerCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, playerCamera.nearClipPlane));
        mousePosition.z = 0f;  // Set Z position for 2D calculations

        Debug.Log("Mouse Position: " + mousePosition);
        Debug.Log("Fire Point Position: " + firePoint.position);
        Vector3 direction = (mousePosition - firePoint.position).normalized;
        transform.position = transform.position + direction * 10;
        qTimer = qTimerOriginal;
    }

    public virtual void E()
    {
        Vector3 mousePosition = playerCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, playerCamera.nearClipPlane));
        mousePosition.z = 0f;  // Set Z position for 2D calculations

        Debug.Log("Mouse Position: " + mousePosition);
        Debug.Log("Fire Point Position: " + firePoint.position);
        Vector3 direction = (mousePosition - firePoint.position).normalized;
        // Create the bullet on the server
        for (int i = 0; i < 12; i++)
        {
            // Rotate the direction by 90 degrees around the Z-axis for 2D
            Vector3 rotatedDirection = Quaternion.Euler(0, 0, 30 * i) * direction;
            Vector3 spawnPosition = firePoint.position + rotatedDirection * 0.6f;

            // Instantiate the bullet
            GameObject bullet = Instantiate(smallbulletPrefab, spawnPosition, Quaternion.identity);
            Debug.Log("Instantiated bullet at angle: " + (90 * i));

            // Set the bullet's velocity and assign shooter ID
            bullet.GetComponent<Rigidbody2D>().velocity = rotatedDirection * bulletSpeed;
            bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

            // Spawn the bullet on the server
            Instantiate(bullet);

            // Destroy the bullet after a set lifetime to avoid memory issues
            Destroy(bullet, bulletLifetime);
        }
        eTimer = eTimerOriginal;
    }

    public virtual void CmdShoot(Vector3 direction)
    {
        // Create the bullet on the server
        Vector3 spawnPosition = firePoint.position + direction * 0.6f;
        GameObject bullet = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);
        bullet.GetComponent<Rigidbody2D>().velocity = direction * bulletSpeed;

        // Assign the player's ID to the bullet's shooterId
        bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId; // Assuming the player has an 'Enemy' script attached with an ID

        Instantiate(bullet);

        Destroy(bullet, bulletLifetime);
    }

    public virtual void CmdSuper(Vector3 direction)
    {
        // Create the bullet on the server
        Vector3 spawnPosition = firePoint.position + direction * 0.6f;
        GameObject bullet = Instantiate(superPrefab, spawnPosition, Quaternion.identity);
        bullet.GetComponent<Rigidbody2D>().velocity = direction * bulletSpeed * 5;

        // Assign the player's ID to the bullet's shooterId
        bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

        Instantiate(bullet);

        bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

        Destroy(bullet, bulletLifetime);
    }

    // Coroutine to handle automatic reloading
    IEnumerator AutoReload()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;  // Reset ammo
        isReloading = false;
    }

    // This hook is called whenever the ammo count changes (SyncVar)
    void OnAmmoChanged(int oldAmmo, int newAmmo)
    {
    }

    // Show the aiming sprite with rotation based on mouse position
    public virtual void ShowAimingSprite()
    {
        if (playerCamera == null) return;

        Vector3 mousePosition = playerCamera.ScreenPointToRay(Input.mousePosition).GetPoint(10f);
        Vector3 direction = (mousePosition - firePoint.position).normalized;
        aimingSprite.SetActive(true);
        aimingSprite.transform.position = firePoint.position;
        weapon.transform.position = firePoint.position;

        // Calculate the angle and rotate the aiming sprite
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        aimingSprite.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
        if ((angle + 360) % 360 > 180)
        {
            weapon.GetComponent<SpriteRenderer>().flipY = true;
        }
        weapon.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
    }
}
