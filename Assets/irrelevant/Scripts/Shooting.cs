// MouseShooting.cs
using System.Collections;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;
using System;

/// <summary>
/// Server-authoritative shooting and pickups, with client-side visuals synchronized via weaponNetId.
/// </summary>
public class MouseShooting : NetworkBehaviour
{
    public bool isKeyboard = false;

    [Header("Weapon Prefabs (0 = none)")]
    public GameObject[] weapons;

    [Header("Bullet Prefabs for each weapon")]
    public GameObject[] bulletPrefabs; // Array of bullet prefabs corresponding to weapon IDs

    [SyncVar]
    public uint weaponIdA = 0;

    [SyncVar]
    public uint weaponIdB = 0;

    [SyncVar]
    public uint weaponIdC = 0;

    public bool isFlipped = true;

    public bool wantsPickup = false;

    public GameObject PickupButton;

    [Header("References")]
    public Transform firePoint;
    public Camera playerCamera;
    public FixedJoystick attackJoystick;
    public TextMeshProUGUI ammoText;

    public bool isAuto = false;

    public Vector3 d = Vector3.zero;

    [Header("Stats")]
    [SyncVar]
    public int maxAmmo = 3;

    public float reloadTime = 2f;
    public float shotCooldownTime = 1f;

    [Header("Weapon Stats")]
    public float bulletSpeed = 5f;
    public float bulletLifetime = 5f;

    // Current weapon slot index (0=A,1=B,2=C)
    [SyncVar]
    public int weapId = 0;

    // NetId of the spawned weapon instance
    [SyncVar]
    public uint weaponNetId = 0;

    [SyncVar(hook = nameof(OnAmmoChanged))]
    public int currentAmmo = 0;

    private GameObject weaponInstance;
    private Rigidbody2D rb;
    private bool isReloading;

    [SyncVar]
    public float shotCooldown;

    public bool isAiming = false;
    public bool isPressed = false;
    public bool canShoot = true;
    public Vector3 v = Vector3.zero;

    public GameObject swap;
    public GameObject movement;
    public GameObject shoot;

    public Image PanelBar;
    public Image Bar;

    public bool isShooting = false;

    private void Start()
    {
        if (!isLocalPlayer)
        {
            Bar.gameObject.SetActive(false);
        }
        isKeyboard = !Application.isMobilePlatform;
        deactivateMobile(isKeyboard);
    }

    private void deactivateMobile(bool i)
    {
        movement.SetActive(i);
        shoot.SetActive(i);
    }

    #region Initialization
    public override void OnStartClient()
    {
        base.OnStartClient();
        // Register all weapon prefabs so they can spawn
        foreach (var prefab in weapons)
            if (prefab != null)
                NetworkClient.RegisterPrefab(prefab);

        // Register all bullet prefabs
        foreach (var bulletPrefab in bulletPrefabs)
            if (bulletPrefab != null)
                NetworkClient.RegisterPrefab(bulletPrefab);

        // Attach any existing weapon
        if (weaponNetId != 0)
            AttachWeaponClient(0, weaponNetId);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        rb = GetComponent<Rigidbody2D>();
        if (playerCamera != null)
        {
            playerCamera.enabled = true;
            playerCamera.GetComponent<CameraFollow>().target = transform;
        }
        currentAmmo = maxAmmo;
    }

    public void ActivatePickupButton()
    {
        PickupButton.SetActive(true);
    }

    public void DeActivatePickupButton()
    {
        PickupButton.SetActive(false);
    }

    public void swapPickup()
    {
        wantsPickup = true;
    }

    void Update()
    {
        if (isFlipped)
        {
            transform.localEulerAngles = new Vector3(0, 0, 0);
        }
        else
        {
            transform.localEulerAngles = new Vector3(0, 0, 180);
        }
        if (!isLocalPlayer) return;
        PanelBar.fillAmount = ((float)currentAmmo) / maxAmmo;
        if (isFlipped)
        {
            transform.localEulerAngles = new Vector3(0, 0, 180);
        }
        else
        {
            transform.localEulerAngles = new Vector3(0, 0, 0);
        }
        HandleShootingInput();
        UpdateUI();
    }
    #endregion

    public void Press()
    {
        if (weaponNetId == 0)
        {
            return;
        }
        isPressed = true;
    }

    #region Weapon Swapping
    [Command(requiresAuthority = false)]
    public void SwapWeapon()
    {
        if (shotCooldown > 0f) return;
        if (weaponIdA == 0 && weaponIdB == 0 && weaponIdC == 0) return;
        int nextSlot = (weapId + 1) % 3;
        ChangeWeaponSlot(nextSlot);
    }

    [Command(requiresAuthority = false)]
    public void SwapWeapon(int index)
    {
        if (shotCooldown > 0f) return;
        if (weaponIdA == 0 && weaponIdB == 0 && weaponIdC == 0) return;
        ChangeWeaponSlot(index % 3);
    }

    private void ChangeWeaponSlot(int slot)
    {
        if (shotCooldown >= 0f) return;
        uint oldId = weaponNetId;
        weapId = slot;
        uint newId = 0;
        switch (weapId)
        {
            case 0: newId = weaponIdA; break;
            case 1: newId = weaponIdB; break;
            case 2: newId = weaponIdC; break;
        }
        // Server deactivates old
        if (oldId != 0 && NetworkServer.spawned.TryGetValue(oldId, out var oldNi))
            oldNi.gameObject.SetActive(false);
        weaponNetId = newId;
        // Server activates new
        if (newId != 0 && NetworkServer.spawned.TryGetValue(newId, out var newNi))
        {
            newNi.gameObject.SetActive(true);
            currentAmmo = newNi.gameObject.GetComponent<Weapon>().maxAmmo;
            maxAmmo = currentAmmo;
        }

        // Notify clients of slot change
        wantsPickup = false;
        RpcOnWeaponChanged(oldId, newId);
    }

    [ClientRpc]
    void RpcOnWeaponChanged(uint oldId, uint newId)
    {
        AttachWeaponClient(oldId, newId);
    }

    void AttachWeaponClient(uint oldId, uint newId)
    {
        if (oldId != 0 && NetworkClient.spawned.TryGetValue(oldId, out var oldNi))
            oldNi.gameObject.SetActive(false);
        if (newId != 0 && NetworkClient.spawned.TryGetValue(newId, out var newNi))
        {
            weaponInstance = newNi.gameObject;
            weaponInstance.transform.SetParent(firePoint, false);
            weaponInstance.SetActive(true);
        }
        wantsPickup = false;
    }
    #endregion

    private void HandleShootingInput()
    {
        if (!canShoot) return;
        if (!isLocalPlayer) return;
        if (!isKeyboard)
        {
            if (weaponNetId == 0) return;
            float mag;
            Vector3 dir;
            mag = new Vector2(attackJoystick.Horizontal, attackJoystick.Vertical).magnitude;
            dir = new(attackJoystick.Horizontal, attackJoystick.Vertical, 0f);
            dir.Normalize();
            if (isFlipped)
            {
                dir = -dir;
            }
            v = dir;

            // Tell weapon to aim
            if (weaponNetId != 0 && NetworkClient.spawned.TryGetValue(weaponNetId, out var weaponNi))
            {
                var weapon = weaponNi.GetComponent<Weapon>();
                if (mag >= 0.11f)
                {
                    weapon.Aim(dir);
                    isAiming = true;
                }
                else
                {
                    weapon.StopAiming();
                    isAiming = false;
                }
            }

            if (mag <= 0.1f && isPressed && currentAmmo > 0 && !isReloading && shotCooldown <= 0f)
            {
                Debug.Log("autoaim");
                currentAmmo--;
                d = GetComponent<Autoaim>().FindTarget();
                if (d == Vector3.zero)
                {
                    d = dir;
                }
                isAuto = true;
                CmdPerformShoot(d);
                shotCooldown = shotCooldownTime;
                return;
            }
            else
            {
                isAuto = false;
            }

            if (mag >= 0.6f && currentAmmo > 0 && !isReloading && shotCooldown <= 0f)
            {
                currentAmmo--;
                CmdPerformShoot(dir);
                shotCooldown = shotCooldownTime;
            }

            if (currentAmmo <= 0 && !isReloading)
                StartCoroutine(ReloadRoutine());
            shotCooldown -= Time.deltaTime;
            isPressed = false;
        }
        else
        {
            if (weaponNetId == 0)
            {
                isAiming = false;
                return;
            }

            Vector3 rawDir = Vector3.zero;
            float mag = 0f;
            bool fire = false;

            Vector3 playerScreen = Camera.main.WorldToScreenPoint(transform.position);
            Vector3 mouseScreen = Input.mousePosition;
            mouseScreen.z = playerScreen.z;

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(mouseScreen);
            Vector3 delta = mouseWorld - transform.position;
            mag = delta.magnitude;
            if (mag > 0.001f)
                rawDir = delta / mag;

            fire = Input.GetMouseButtonDown(0);
            isAiming = true;
            rawDir.z = 0f;

            if (float.IsNaN(rawDir.x) || float.IsNaN(rawDir.y))
                return;

            v = rawDir;

            // Tell weapon to aim
            if (weaponNetId != 0 && NetworkClient.spawned.TryGetValue(weaponNetId, out var weaponNi))
            {
                var weapon = weaponNi.GetComponent<Weapon>();
                weapon.Aim(rawDir);
            }

            const float manualThreshold = 10f;
            if (fire && CanShoot())
            {
                PerformShoot(rawDir);
            }

            if (currentAmmo <= 0 && !isReloading)
                StartCoroutine(ReloadRoutine());

            shotCooldown -= Time.deltaTime;
            isPressed = false;
        }
    }

    private bool CanShoot()
    {
        return currentAmmo > 0
            && !isReloading
            && shotCooldown <= 0f;
    }

    private void PerformAutoAim(Vector3 fallbackDir)
    {
        if (!isLocalPlayer) return;
        currentAmmo--;
        Vector3 targetDir = GetComponent<Autoaim>().FindTarget();

        if (targetDir == Vector3.zero)
            targetDir = fallbackDir;

        isAuto = true;
        CmdPerformShoot(targetDir);
        shotCooldown = shotCooldownTime;
    }

    private void PerformShoot(Vector3 shootDir)
    {
        if (!isLocalPlayer) return;
        currentAmmo--;
        isAuto = false;
        CmdPerformShoot(shootDir);
        shotCooldown = shotCooldownTime;
    }

    [Command]
    void CmdPerformShoot(Vector3 direction)
    {
        if (weaponNetId == 0) return;
        if (!NetworkServer.spawned.TryGetValue(weaponNetId, out var weaponNi)) return;

        var weapon = weaponNi.GetComponent<Weapon>();
        if (weapon == null) return;

        // Start shooting sequence
        StartCoroutine(ShootingSequence(weapon.id, direction, weapon.startup, weapon.shot, weapon.end));

        // Tell weapon to play animation and rotate
        weapon.PlayShootAnimation();
        weapon.RotateToDirection(direction);
    }

    private IEnumerator ShootingSequence(int weaponId, Vector3 direction, float startup, float shot, float end)
    {
        isShooting = true;

        // Startup delay
        yield return new WaitForSeconds(startup);

        // Perform the actual shot based on weapon type
        switch (weaponId)
        {
            case 0:
                ShootSniper(direction);
                break;
            case 1:
                ShootShotgun(direction);
                break;
            case 2:
                ShootKnife(direction);
                break;
            default:
                ShootAR(direction);
                break;
        }

        // Shot duration
        yield return new WaitForSeconds(shot);

        // End delay
        yield return new WaitForSeconds(end);

        isShooting = false;
        isAuto = false;
    }

    private void ShootSniper(Vector3 direction)
    {
        Vector3 spawnPosition = firePoint.position + direction.normalized * 0.6f;
        GameObject bullet = Instantiate(bulletPrefabs[0], spawnPosition, Quaternion.identity);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        bullet.GetComponent<Rigidbody2D>().velocity = direction.normalized * bulletSpeed;
        bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

        NetworkServer.Spawn(bullet);
        Destroy(bullet, bulletLifetime);
    }

    private void ShootShotgun(Vector3 direction)
    {
        int pellets = 7;
        float maxSpread = 45f;

        for (int i = -3; i <= 3; i++)
        {
            float angle = i * (maxSpread / 3f);
            Vector3 spreadDir = Quaternion.Euler(0, 0, angle) * direction.normalized;

            Vector3 spawnPos = firePoint.position + spreadDir * 0.6f;
            GameObject bullet = Instantiate(bulletPrefabs[1], spawnPos, Quaternion.identity);

            var rb = bullet.GetComponent<Rigidbody2D>();
            rb.velocity = spreadDir * bulletSpeed;

            bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

            NetworkServer.Spawn(bullet);
            Destroy(bullet, bulletLifetime);
        }
    }

    private void ShootKnife(Vector3 direction)
    {
        Vector3 pos = firePoint.position + direction.normalized * 0.3f;
        var hitbox = Instantiate(bulletPrefabs[2], pos, Quaternion.identity);

        hitbox.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
        hitbox.GetComponent<Bullet>().parent = gameObject;

        NetworkServer.Spawn(hitbox);
        Destroy(hitbox, 0.1f);
    }

    private void ShootAR(Vector3 direction)
    {
        float maxSpread = 1.5f;
        float angle = ((float)(new System.Random()).NextDouble()) * (maxSpread);

        Vector3 spreadDir = Quaternion.Euler(0, 0, angle) * direction.normalized;

        Vector3 spawnPos = firePoint.position + spreadDir * 0.6f;
        GameObject bullet = Instantiate(bulletPrefabs[3], spawnPos, Quaternion.identity);

        var rb = bullet.GetComponent<Rigidbody2D>();
        rb.velocity = spreadDir * bulletSpeed;

        bullet.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;

        NetworkServer.Spawn(bullet);
        Destroy(bullet, bulletLifetime);
    }

    private IEnumerator ReloadRoutine()
    {
        isReloading = true;
        yield return new WaitForSeconds(reloadTime);
        currentAmmo = maxAmmo;
        isReloading = false;
    }

    #region Pickup Handling
    [Command(requiresAuthority = false)]
    public void CmdRequestPickup(int weaponIdx, uint pickupNetId)
    {
        if (shotCooldown > 0f) return;
        if (!NetworkServer.spawned.TryGetValue(pickupNetId, out var pi)) return;
        var pickup = pi.GetComponent<Pickup>();
        if (pickup == null) return;
        NetworkServer.Destroy(pickup.gameObject);
        var prefab = weapons[weaponIdx];
        var instance = Instantiate(prefab, firePoint.position, Quaternion.identity);
        var ni = instance.GetComponent<NetworkIdentity>();
        instance.transform.SetParent(firePoint, false);
        instance.GetComponent<Weapon>().slotnum = weapId;
        instance.GetComponent<Weapon>().parent = gameObject;
        NetworkServer.Spawn(instance, connectionToClient);
        if (weaponIdA != 0 && weaponIdB != 0 && weaponIdC != 0)
        {
            if (weaponNetId == weaponIdA)
            {
                if (!NetworkServer.spawned.TryGetValue(weaponNetId, out var aa)) return;
                NetworkServer.Destroy(aa.gameObject);
                weaponNetId = 0;
                weaponIdA = 0;
            }
            else if (weaponNetId == weaponIdB)
            {
                if (!NetworkServer.spawned.TryGetValue(weaponNetId, out var bb)) return;
                NetworkServer.Destroy(bb.gameObject);
                weaponNetId = 0;
                weaponIdB = 0;
            }
            else
            {
                if (!NetworkServer.spawned.TryGetValue(weaponNetId, out var cc)) return;
                NetworkServer.Destroy(cc.gameObject);
                weaponNetId = 0;
                weaponIdC = 0;
            }
        }

        weaponNetId = ni.netId;
        if (weaponIdA == 0)
        {
            weaponIdA = ni.netId;
            ChangeWeaponSlot(1);
            ChangeWeaponSlot(2);
            ChangeWeaponSlot(0);
        }
        else if (weaponIdB == 0)
        {
            weaponIdB = ni.netId;
            ChangeWeaponSlot(2);
            ChangeWeaponSlot(0);
            ChangeWeaponSlot(1);
        }
        else
        {
            weaponIdC = ni.netId;
            ChangeWeaponSlot(0);
            ChangeWeaponSlot(1);
            ChangeWeaponSlot(2);
        }

        currentAmmo = instance.GetComponent<Weapon>().maxAmmo;
        maxAmmo = instance.GetComponent<Weapon>().maxAmmo;
    }
    #endregion

    #region UI Updates
    void OnAmmoChanged(int oldVal, int newVal) { if (isLocalPlayer) UpdateUI(); }
    void UpdateUI() {}
    #endregion

    #region SyncVar Hooks
    void OnWeaponIdAChanged(uint oldId, uint newId) { Debug.Log($"WeaponIdA: {oldId} -> {newId}"); }
    void OnWeaponIdBChanged(uint oldId, uint newId) { Debug.Log($"WeaponIdB: {oldId} -> {newId}"); }
    void OnWeaponIdCChanged(uint oldId, uint newId) { Debug.Log($"WeaponIdC: {oldId} -> {newId}"); }
    #endregion
}