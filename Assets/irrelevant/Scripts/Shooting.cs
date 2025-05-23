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

    private void Start()
    {
        isKeyboard = !Application.isMobilePlatform;
        deactivateMobile(isKeyboard);
    }

    private void deactivateMobile(bool i)
    {
        //swap.SetActive(i);
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
            if (mag >= 0.11f || isKeyboard)
            {
                isAiming = true;

            }
            else
            {
                isAiming = false;

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
            // 3) Gather “fire” + raw direction + magnitude
            Vector3 rawDir = Vector3.zero;
            float mag = 0f;
            bool fire = false;

            // ——— DESKTOP: convert screen→world at the player’s Z-depth ———
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

            // 4) Zero out any stray Z
            rawDir.z = 0f;

            // 6) Bail if direction is still invalid
            if (float.IsNaN(rawDir.x) || float.IsNaN(rawDir.y))
                return;

            // 7) Cache it for animations / VFX
            v = rawDir;
            // 9) MANUAL SHOOT: big deadzone + fire
            const float manualThreshold = 10f;
            if (fire && CanShoot())
            {
                PerformShoot(rawDir);
            }


            // 11) Reload & cooldown
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

        // if your autolookup gives Vector3.zero on “no target,”
        // fall back to the player’s stick/mouse direction
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
        var wep = NetworkServer.spawned[weaponNetId].GetComponent<Weapon>();
        wep?.Shoot(direction);

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
    void UpdateUI() { ammoText.text = $"{currentAmmo} / {maxAmmo}"; }
    #endregion

    #region SyncVar Hooks
    void OnWeaponIdAChanged(uint oldId, uint newId) { Debug.Log($"WeaponIdA: {oldId} -> {newId}"); }
    void OnWeaponIdBChanged(uint oldId, uint newId) { Debug.Log($"WeaponIdB: {oldId} -> {newId}"); }
    void OnWeaponIdCChanged(uint oldId, uint newId) { Debug.Log($"WeaponIdC: {oldId} -> {newId}"); }
    #endregion
}

