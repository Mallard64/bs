// MouseShooting.cs - Complete updated version
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
    public bool isWorking = true;

    public bool isKeyboard = false;

    public Button swapMode;

    [SyncVar]
    public int swapModeNum = 0;

    public int swapModeNumMax = 0;

    public bool hasSpecial = false;

    public GameObject[] staffBullets;

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
    
    [Header("🎮 Fun Features")]
    public bool enableFunFeatures = true;
    public bool enableScreenShake = true;
    public bool enableBulletTrails = true;
    public bool enableRicochet = true;
    public bool enableCombos = true;
    public bool enableOvercharge = true;
    [Range(0f, 1f)]
    public float ricochetChance = 0.15f;
    
    // Fun features components
    private SimpleComboSystem comboSystem;
    private SimpleOverchargeSystem overchargeSystem;
    private bool funFeaturesInitialized = false;

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
        deactivateMobile(!isKeyboard);
        
        // Initialize fun features
        InitializeFunFeatures();
    }

    private void InitializeFunFeatures()
    {
        if (!enableFunFeatures || funFeaturesInitialized) return;
        
        Debug.Log($"🎮 Initializing Fun Features on {(isServer ? "Server" : "Client")}...");
        
        // Add combo system (each client manages their own for local player)
        if (enableCombos && comboSystem == null && isLocalPlayer)
        {
            comboSystem = gameObject.AddComponent<SimpleComboSystem>();
            Debug.Log("✅ Combo system added");
        }
        
        // Add overcharge system (each client manages their own for local player)
        if (enableOvercharge && overchargeSystem == null && isLocalPlayer)
        {
            overchargeSystem = gameObject.AddComponent<SimpleOverchargeSystem>();
            Debug.Log("✅ Overcharge system added");
        }
        
        // Setup screen shake (all clients need this for visual effects)
        if (enableScreenShake && Camera.main != null)
        {
            if (Camera.main.GetComponent<ScreenShake>() == null)
            {
                Camera.main.gameObject.AddComponent<ScreenShake>();
                Debug.Log("✅ Screen shake added to main camera");
            }
        }
        
        funFeaturesInitialized = true;
        Debug.Log($"🎉 Fun Features fully initialized on {(isServer ? "Server" : "Client")}!");
    }
    
    private void OnWeaponFiredFunFeatures(int weaponId, int mode = 0)
    {
        if (!enableFunFeatures) return;
        
        Debug.Log($"🎮 Fun Features: Weapon fired! ID={weaponId}, Mode={mode}");
        
        // Register with combo system
        if (comboSystem != null && enableCombos)
        {
            comboSystem.RegisterWeaponFire(weaponId);
        }
        
        // Register with overcharge system  
        if (overchargeSystem != null && enableOvercharge)
        {
            overchargeSystem.OnWeaponFired();
        }
        
        // Screen shake based on weapon
        if (enableScreenShake && ScreenShake.Instance != null)
        {
            switch (weaponId)
            {
                case 0: // Sniper
                    ScreenShake.Shake(0.4f, 0.15f);
                    break;
                case 1: // Shotgun
                    ScreenShake.Shake(0.3f, 0.2f);
                    break;
                case 2: // AR
                    ScreenShake.Shake(0.2f, 0.1f);
                    break;
                case 4: // Lightning Staff
                    ScreenShake.ShakeLightning();
                    break;
                default:
                    ScreenShake.Shake(0.2f, 0.1f);
                    break;
            }
        }
    }
    
    private void EnhanceBulletFunFeatures(GameObject bullet)
    {
        if (!enableFunFeatures || bullet == null) return;
        
        Debug.Log($"🎮 Server enhancing bullet: {bullet.name}");
        
        // Add ricochet chance (server-side only for physics)
        if (enableRicochet && bullet.GetComponent<RicochetBullet>() == null)
        {
            if (UnityEngine.Random.Range(0f, 1f) < ricochetChance)
            {
                var ricochet = bullet.AddComponent<RicochetBullet>();
                ricochet.maxBounces = 2;
                ricochet.speedMultiplierPerBounce = 0.85f;
                Debug.Log("🎯 Added ricochet to bullet!");
            }
        }
        
        // Trigger client-side visual enhancements
        var bulletNetworkIdentity = bullet.GetComponent<NetworkIdentity>();
        if (bulletNetworkIdentity != null)
        {
            RpcEnhanceBullet(bulletNetworkIdentity.netId);
        }
    }
    
    public float GetFunFeaturesDamageMultiplier()
    {
        if (!enableFunFeatures) return 1f;
        
        float comboMult = comboSystem != null ? comboSystem.GetDamageMultiplier() : 1f;
        float overchargeMult = overchargeSystem != null ? overchargeSystem.GetDamageMultiplier() : 1f;
        return comboMult * overchargeMult;
    }
    
    [ClientRpc]
    private void RpcTriggerFunFeatures(int weaponId, int mode)
    {
        if (!enableFunFeatures) return;
        
        Debug.Log($"🎮 Client Fun Features: Weapon fired! ID={weaponId}, Mode={mode}");
        
        // Only trigger for local player's own shots
        if (isLocalPlayer)
        {
            // Register with local combo system
            if (comboSystem != null && enableCombos)
            {
                comboSystem.RegisterWeaponFire(weaponId);
            }
            
            // Register with local overcharge system  
            if (overchargeSystem != null && enableOvercharge)
            {
                overchargeSystem.OnWeaponFired();
            }
        }
        
        // Screen shake on all clients (everyone sees/feels the effects)
        if (enableScreenShake && ScreenShake.Instance != null)
        {
            switch (weaponId)
            {
                case 0: // Sniper
                    ScreenShake.Shake(0.4f, 0.15f);
                    break;
                case 1: // Shotgun
                    ScreenShake.Shake(0.3f, 0.2f);
                    break;
                case 2: // AR
                    ScreenShake.Shake(0.2f, 0.1f);
                    break;
                case 4: // Lightning Staff
                    ScreenShake.ShakeLightning();
                    break;
                default:
                    ScreenShake.Shake(0.2f, 0.1f);
                    break;
            }
        }
    }
    
    [ClientRpc]
    private void RpcEnhanceBullet(uint bulletNetId)
    {
        if (!enableFunFeatures) return;
        
        // Find bullet by network ID and enhance on client
        if (NetworkClient.spawned.TryGetValue(bulletNetId, out var bulletNi))
        {
            GameObject bullet = bulletNi.gameObject;
            Debug.Log($"🎮 Client enhancing bullet: {bullet.name}");
            
            // Add trail effect on client
            if (enableBulletTrails && bullet.GetComponent<BulletTrailSystem>() == null)
            {
                var trailSystem = bullet.AddComponent<BulletTrailSystem>();
                trailSystem.enableTrail = true;
                trailSystem.enableParticles = true;
            }
            
            // Note: Ricochet physics should only be on server to avoid conflicts
        }
    }
    
    [ClientRpc]
    private void RpcStaffFireEffect(Vector3 position, float angle)
    {
        if (!enableFunFeatures) return;
        
        Debug.Log("🔥 Client: Fire staff visual effect triggered");
        
        // Add fire particle effects, screen shake, etc.
        if (enableScreenShake && ScreenShake.Instance != null)
        {
            ScreenShake.Shake(0.25f, 0.1f);
        }
        
        // Could add fire particle system here
        // ParticleSystem.Instantiate(fireParticles, position, Quaternion.Euler(0, 0, angle));
    }
    
    [ClientRpc]
    private void RpcStaffIceEffect(Vector3 position, float angle)
    {
        if (!enableFunFeatures) return;
        
        Debug.Log("❄️ Client: Ice staff visual effect triggered");
        
        // Add ice particle effects, screen shake, etc.
        if (enableScreenShake && ScreenShake.Instance != null)
        {
            ScreenShake.Shake(0.2f, 0.12f);
        }
        
        // Could add ice crystal particle system here
        // ParticleSystem.Instantiate(iceParticles, position, Quaternion.Euler(0, 0, angle));
    }
    
    [ClientRpc]
    private void RpcStaffLightningEffect(Vector3 startPos, float angle, float distance, Vector3 direction)
    {
        if (!enableFunFeatures) return;
        
        Debug.Log($"⚡ Client: Lightning staff visual effect - distance: {distance}, angle: {angle}");
        
        // Screen shake for lightning
        if (enableScreenShake && ScreenShake.Instance != null)
        {
            ScreenShake.ShakeLightning(0.3f);
        }
        
        // Could enhance the actual lightning bullet on client
        StartCoroutine(EnhanceLightningVisualCoroutine(startPos, angle, distance, direction));
    }
    
    private System.Collections.IEnumerator EnhanceLightningVisualCoroutine(Vector3 startPos, float angle, float distance, Vector3 direction)
    {
        // Wait a frame for the networked lightning bolt to spawn
        yield return null;
        
        // Find recently spawned lightning bolts near the start position
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(startPos, 2f);
        
        foreach (var obj in nearbyObjects)
        {
            if (obj.name.Contains("Lightning") || obj.GetComponent<Bullet>()?.effectType == BulletEffectType.LightningBolt)
            {
                GameObject lightning = obj.gameObject;
                Debug.Log($"⚡ Client: Found lightning bolt to enhance: {lightning.name}");
                
                // Apply client-side visual enhancements
                var spriteRenderer = lightning.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    // Add flickering effect
                    StartCoroutine(LightningFlickerEffect(spriteRenderer));
                }
                break;
            }
        }
    }
    
    private System.Collections.IEnumerator LightningFlickerEffect(SpriteRenderer spriteRenderer)
    {
        Color originalColor = spriteRenderer.color;
        float flickerTime = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < flickerTime && spriteRenderer != null)
        {
            // Flicker between bright white and original color
            float alpha = Mathf.PingPong(Time.time * 20f, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, Color.white, alpha * 0.5f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    private void deactivateMobile(bool i)
    {
        movement.SetActive(i);
        shoot.SetActive(i);
    }
    
    
    void OnGUI()
    {
        if (!isLocalPlayer || !enableFunFeatures) return;
        
        GUILayout.BeginArea(new Rect(10, 300, 300, 150));
        GUILayout.Label("🎮 Fun Features Status:");
        
        // Show local systems only (each client manages their own)
        if (comboSystem != null)
        {
            GUILayout.Label($"Combo Level: {comboSystem.GetComboLevel()} (x{comboSystem.GetDamageMultiplier():F1})");
        }
        
        if (overchargeSystem != null)
        {
            GUILayout.Label($"⚡ Charge: {overchargeSystem.GetChargePercentage()*100:F0}%");
            if (overchargeSystem.IsOvercharged())
            {
                GUILayout.Label("🌟 OVERCHARGED!");
            }
        }
        
        GUILayout.Label($"💥 Total Damage: x{GetFunFeaturesDamageMultiplier():F1}");
        GUILayout.Label("F1=Test Shake, F2=Force Overcharge, F3=Debug");
        
        GUILayout.EndArea();
    }

    #region Initialization
    public override void OnStartClient()
    {
        base.OnStartClient();
        
        // Initialize fun features for clients
        InitializeFunFeatures();
        
        // Register all weapon prefabs so they can spawn
        foreach (var prefab in weapons)
            if (prefab != null)
                NetworkClient.RegisterPrefab(prefab);

        // Register all bullet prefabs
        foreach (var bulletPrefab in bulletPrefabs)
            if (bulletPrefab != null)
                NetworkClient.RegisterPrefab(bulletPrefab);

        foreach (var bulletPrefab in staffBullets)
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

        swapMode.gameObject.SetActive(hasSpecial);

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
        
        // Fun features testing (F1-F3 keys) - only for local player
        if (enableFunFeatures && isLocalPlayer)
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                ScreenShake.Shake(0.5f, 0.3f);
                Debug.Log("🧪 Client screen shake test");
            }
            
            if (Input.GetKeyDown(KeyCode.F2) && overchargeSystem != null)
            {
                overchargeSystem.ForceOvercharge();
                Debug.Log("🧪 Client force overcharge test");
            }
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                Debug.Log($"🧪 Client damage multiplier: x{GetFunFeaturesDamageMultiplier():F1}");
                Debug.Log($"🧪 IsLocalPlayer: {isLocalPlayer}, ComboSystem: {comboSystem != null}, OverchargeSystem: {overchargeSystem != null}");
            }
        }
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
        // Handle old weapon
        if (oldId != 0 && NetworkClient.spawned.TryGetValue(oldId, out var oldNi))
        {
            oldNi.gameObject.SetActive(false);
        }
        
        // Handle new weapon
        if (newId != 0 && NetworkClient.spawned.TryGetValue(newId, out var newNi))
        {
            weaponInstance = newNi.gameObject;
            
            // Keep NetworkTransform enabled for proper synchronization
            // Don't parent - let the Weapon script handle positioning
            
            weaponInstance.SetActive(true);
            
            Debug.Log($"🔧 Activated weapon {weaponInstance.name} with NetworkTransform synchronization");
        }
        wantsPickup = false;
    }
    
    // Method to properly detach weapon (for drops, disconnects, etc.)
    public void DetachCurrentWeapon()
    {
        if (weaponInstance != null)
        {
            Debug.Log($"🔧 Detaching weapon {weaponInstance.name}");
            
            // No need to change NetworkTransform or parenting since we're not using parenting
            weaponInstance = null;
        }
    }
    
    // Called when player disconnects to clean up weapons
    public override void OnStopClient()
    {
        base.OnStopClient();
        DetachCurrentWeapon();
    }
    
    #endregion

    public void SwapWeaponNum()
    {
        if (isLocalPlayer)
        {
            CmdSwapWeaponMode();
        }
    }

    [Command]
    void CmdSwapWeaponMode()
    {
        swapModeNum = (swapModeNum + 1) % swapModeNumMax;
    }

    private void HandleShootingInput()
    {
        if (!isWorking) return;
        if (!canShoot) return;
        if (isShooting) return;
        if (!isLocalPlayer) return;
        if (weaponNetId == 0) return;
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

            if (mag <= 0.1f && isPressed && CanShoot())
            {
                Debug.Log("autoaim");
                d = GetComponent<Autoaim>().FindTarget();
                if (d == Vector3.zero)
                {
                    d = dir;
                }
                isAuto = true;
                PerformAutoAim(d);
                return;
            }
            else
            {
                isAuto = false;
            }

            if (mag >= 0.6f && CanShoot())
            {
                PerformShoot(dir);
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

        // Handle ammo consumption based on weapon type and mode
        if (weaponNetId != 0 && NetworkClient.spawned.TryGetValue(weaponNetId, out var weaponNi))
        {
            var weapon = weaponNi.GetComponent<Weapon>();
            bool consumeAmmo = true;
            
            switch (weapon.id)
            {
                case 2: // Sword - only throw mode consumes ammo
                    consumeAmmo = (swapModeNum == 2);
                    break;
                case 7: // War Hammer - only throw mode consumes ammo
                    consumeAmmo = (swapModeNum == 1);
                    break;
                default:
                    consumeAmmo = true;
                    break;
            }
            
            if (consumeAmmo)
            {
                currentAmmo--;
            }
        }

        isAuto = true;
        CmdPerformShoot(fallbackDir);
        shotCooldown = shotCooldownTime;
    }

    private void PerformShoot(Vector3 shootDir)
    {
        if (!isLocalPlayer) return;

        // Handle ammo consumption based on weapon type and mode
        if (weaponNetId != 0 && NetworkClient.spawned.TryGetValue(weaponNetId, out var weaponNi))
        {
            var weapon = weaponNi.GetComponent<Weapon>();
            bool consumeAmmo = true;
            
            switch (weapon.id)
            {
                case 2: // Sword - only throw mode consumes ammo
                    consumeAmmo = (swapModeNum == 2);
                    break;
                case 7: // War Hammer - only throw mode consumes ammo
                    consumeAmmo = (swapModeNum == 1);
                    break;
                default:
                    consumeAmmo = true;
                    break;
            }
            
            if (consumeAmmo)
            {
                currentAmmo--;
            }
        }

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

        // Register combo action
        var comboSystem = GetComponent<WeaponComboSystem>();
        if (comboSystem != null)
        {
            comboSystem.RegisterWeaponFire(weapon.id, swapModeNum, direction);
        }
        
        // Add overcharge for weapon use
        var overchargeSystem = GetComponent<WeaponOverchargeSystem>();
        if (overchargeSystem != null)
        {
            overchargeSystem.AddOvercharge(2f); // Base overcharge for weapon use
        }
        
        // Trigger integrated fun features on server
        OnWeaponFiredFunFeatures(weapon.id, swapModeNum);
        
        // Trigger fun features on all clients
        RpcTriggerFunFeatures(weapon.id, swapModeNum);

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
                ShootSword(direction);
                break;
            case 3:
                ShootAR(direction);
                break;
            case 4:
                ShootElementalStaff(direction);
                break;
            case 5:
                ShootMorphCannon(direction);
                break;
            case 6:
                ShootSpiritBow(direction);
                break;
            case 7:
                ShootWarHammer(direction);
                break;
            case 8:
                ShootPlasmaRifle(direction);
                break;
            case 9:
                ShootNinjaKunai(direction);
                break;
            case 10:
                ShootChaosOrb(direction);
                break;
            case 11:
                ShootLightningGun(direction);
                break;
            case 12:
                ShootRocketLauncher(direction);
                break;
            case 13:
                ShootFlameThrower(direction);
                break;
            case 14:
                ShootIceBeam(direction);
                break;
            case 15:
                ShootBoomerang(direction);
                break;
            case 16:
                ShootLaserCannon(direction);
                break;
            case 17:
                ShootGravityGun(direction);
                break;
            case 18:
                ShootVenomSpitter(direction);
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
        if (bulletPrefabs.Length == 0) return;
        
        Vector3 spawnPosition = firePoint.position + direction.normalized * 0.6f;
        GameObject bullet = Instantiate(bulletPrefabs[0], spawnPosition, Quaternion.identity);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        bullet.transform.rotation = Quaternion.Euler(0, 0, angle);

        bullet.GetComponent<Rigidbody2D>().velocity = direction.normalized * bulletSpeed;
        
        var bulletScript = bullet.GetComponent<Bullet>();
        if (bulletScript != null)
            bulletScript.shooterId = GetComponent<Enemy>().connectionId;

        NetworkServer.Spawn(bullet);
        
        // Enhance bullet with integrated fun features
        EnhanceBulletFunFeatures(bullet);
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

            var bulletScript = bullet.GetComponent<Bullet>();
            if (bulletScript != null)
                bulletScript.shooterId = GetComponent<Enemy>().connectionId;

            NetworkServer.Spawn(bullet);
            
            // Enhance bullet with integrated fun features
            EnhanceBulletFunFeatures(bullet);
            
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
        
        // Enhance bullet with integrated fun features
        EnhanceBulletFunFeatures(hitbox);
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
        
        // Enhance bullet with integrated fun features
        EnhanceBulletFunFeatures(bullet);
        Destroy(bullet, bulletLifetime);
    }

    private void ShootSword(Vector3 direction)
    {
        switch (swapModeNum)
        {
            case 0: // Stab mode
                Vector3 stabPos = firePoint.position + direction.normalized * 0.7f;
                var stabHitbox = Instantiate(bulletPrefabs[2], stabPos, Quaternion.identity);
                float stabAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                stabHitbox.transform.rotation = Quaternion.Euler(0, 0, stabAngle);
                stabHitbox.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                stabHitbox.GetComponent<Bullet>().parent = gameObject;
                NetworkServer.Spawn(stabHitbox);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(stabHitbox);
                Destroy(stabHitbox, 0.15f);
                break;

            case 1: // Slice mode (wide arc)
                int slices = 5;
                float sliceSpread = 60f;

                for (int i = -2; i <= 2; i++)
                {
                    float angleOffset = i * (sliceSpread / 4f);

                    // Rotate the original direction vector
                    Vector3 sliceDir = Quaternion.Euler(0, 0, angleOffset) * direction;

                    Vector3 slicePos = firePoint.position + sliceDir.normalized * 0.7f;
                    var sliceHitbox = Instantiate(bulletPrefabs[2], slicePos, Quaternion.identity);

                    float sliceAngle = Mathf.Atan2(sliceDir.y, sliceDir.x) * Mathf.Rad2Deg;
                    sliceHitbox.transform.rotation = Quaternion.Euler(0, 0, sliceAngle);

                    sliceHitbox.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                    sliceHitbox.GetComponent<Bullet>().parent = gameObject;
                    NetworkServer.Spawn(sliceHitbox);
                    
                    // Enhance bullet with integrated fun features
                    EnhanceBulletFunFeatures(sliceHitbox);
                    Destroy(sliceHitbox, 0.1f);
                }
                break;

            case 2: // Throwing mode
                Vector3 throwPos = firePoint.position + direction.normalized * 0.7f;
                GameObject thrownSword = Instantiate(bulletPrefabs[4], throwPos, Quaternion.identity);
                float throwAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                thrownSword.transform.rotation = Quaternion.Euler(0, 0, throwAngle);
                var rb = thrownSword.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = direction.normalized * bulletSpeed;
                    rb.angularVelocity = 360f;
                }
                thrownSword.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                thrownSword.GetComponent<Bullet>().parent = gameObject;
                NetworkServer.Spawn(thrownSword);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(thrownSword);
                Destroy(thrownSword, bulletLifetime * 1.2f);
                break;
        }
    }

    // ===== NEW MULTI-MODE WEAPONS =====

    public void ShootElementalStaff(Vector3 direction)
    {
        // Elemental Staff - Fire/Ice/Lightning modes inspired by Brawl Stars
        switch (swapModeNum)
        {
            case 0: // Fire Mode - Fast, burning DOT projectile
                Vector3 firePos = firePoint.position + direction.normalized * 1.0f;
                GameObject fireBolt = Instantiate(staffBullets[0], firePos, Quaternion.identity);
                
                float fireAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                fireBolt.transform.rotation = Quaternion.Euler(0, 0, fireAngle);
                
                var fireRb = fireBolt.GetComponent<Rigidbody2D>();
                fireRb.velocity = direction.normalized * (bulletSpeed * 1.2f);
                
                var fireBullet = fireBolt.GetComponent<Bullet>();
                fireBullet.shooterId = GetComponent<Enemy>().connectionId;
                fireBullet.damage = 15f; // Lower direct damage but adds burn
                fireBullet.effectType = BulletEffectType.BurnDamage;
                fireBullet.effectDuration = 4f; // 4 seconds of burning
                fireBullet.effectDamage = 3f; // 3 damage per second
                
                NetworkServer.Spawn(fireBolt);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(fireBolt);
                
                // Trigger fire visual effects on all clients
                RpcStaffFireEffect(firePos, fireAngle);
                
                Destroy(fireBolt, bulletLifetime * 0.8f);
                break;

            case 1: // Ice Mode - Slowing projectile with area effect
                Vector3 icePos = firePoint.position + direction.normalized * 1.0f;
                GameObject iceShard = Instantiate(staffBullets[1], icePos, Quaternion.identity);
                
                float iceAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                iceShard.transform.rotation = Quaternion.Euler(0, 0, iceAngle);
                
                var iceRb = iceShard.GetComponent<Rigidbody2D>();
                iceRb.velocity = direction.normalized * (bulletSpeed * 0.9f);
                
                var iceBullet = iceShard.GetComponent<Bullet>();
                iceBullet.shooterId = GetComponent<Enemy>().connectionId;
                iceBullet.damage = 20f; // Medium damage + slow
                iceBullet.effectType = BulletEffectType.SlowEffect;
                iceBullet.effectDuration = 3f; // 3 seconds of slowing
                iceBullet.slowAmount = 0.4f; // Reduce movement to 40% of normal speed
                
                NetworkServer.Spawn(iceShard);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(iceShard);
                
                // Trigger ice visual effects on all clients
                RpcStaffIceEffect(icePos, iceAngle);
                
                Destroy(iceShard, bulletLifetime);
                break;

            case 2: // Lightning Mode - Instant lightning bolt that stretches to target
                Vector3 targetDir = GetComponent<Autoaim>().FindTarget();
                Vector3 finalDirection;
                float lightningDistance = 6f; // Max lightning stretch distance for staff
                
                if (targetDir != Vector3.positiveInfinity && targetDir.magnitude > 0.1f && targetDir.magnitude < 50f)
                {
                    // Target found - stretch lightning to target
                    finalDirection = targetDir.normalized;
                    lightningDistance = Mathf.Min(targetDir.magnitude, lightningDistance);
                }
                else
                {
                    // No target - shoot in mouse direction
                    finalDirection = direction.magnitude > 0.1f ? direction.normalized : Vector3.right;
                }
                
                // Clamp lightning distance to prevent physics errors
                lightningDistance = Mathf.Clamp(lightningDistance, 1f, 6f);
                
                // Position lightning with pivot at left center, starting from firePoint
                Vector3 lightningStartPos = firePoint.position;
                
                // Spawn lightning bolt at starting position
                GameObject lightning = Instantiate(staffBullets[2], lightningStartPos, Quaternion.identity);
                
                float lightningAngle = Mathf.Atan2(finalDirection.y, finalDirection.x) * Mathf.Rad2Deg;
                lightning.transform.rotation = Quaternion.Euler(0, 0, lightningAngle);
                
                // Scale the lightning bolt to stretch to target distance
                // Since pivot is at left center, scaling X will extend it in the right direction
                var spriteRenderer = lightning.GetComponent<SpriteRenderer>();
                var boxCollider = lightning.GetComponent<BoxCollider2D>();
                
                if (spriteRenderer != null)
                {
                    Vector3 scale = lightning.transform.localScale;
                    scale.x = lightningDistance; // Stretch along the bolt direction
                    lightning.transform.localScale = scale;
                }
                
                // Adjust BoxCollider to match the stretched lightning
                if (boxCollider != null)
                {
                    Vector2 colliderSize = boxCollider.size;
                    colliderSize.x = lightningDistance;
                    boxCollider.size = colliderSize;
                    
                    // Center the collider along the lightning bolt
                    Vector2 colliderOffset = boxCollider.offset;
                    colliderOffset.x = lightningDistance * 0.5f;
                    boxCollider.offset = colliderOffset;
                }
                
                // Set lightning at rest (no velocity - instant hit)
                var lightningRb = lightning.GetComponent<Rigidbody2D>();
                if (lightningRb != null)
                {
                    lightningRb.velocity = Vector2.zero;
                    lightningRb.isKinematic = true; // Prevent physics movement
                }
                
                var lightningBullet = lightning.GetComponent<Bullet>();
                lightningBullet.shooterId = GetComponent<Enemy>().connectionId;
                lightningBullet.damage = 35f; // High damage, can chain
                lightningBullet.effectType = BulletEffectType.LightningBolt;
                lightningBullet.effectDuration = 0f; // Instant effect
                lightningBullet.effectDamage = 25f; // Lightning bolt damage
                lightningBullet.lightningRange = 3f; // 3 unit range for lightning
                lightningBullet.lightningTargets = 4; // Up to 4 additional targets
                
                NetworkServer.Spawn(lightning);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(lightning);
                
                // Trigger lightning visual effects on all clients
                RpcStaffLightningEffect(lightningStartPos, lightningAngle, lightningDistance, finalDirection);
                
                // Lightning bolt lingers for animation to complete (0.5s animation)
                Destroy(lightning, 0.6f);
                break;
        }
    }

    public void ShootMorphCannon(Vector3 direction)
    {
        // Morph Cannon - Rocket/Beam/Grenade modes inspired by Smash Bros items
        switch (swapModeNum)
        {
            case 0: // Rocket Mode - Fast seeking projectile
                Vector3 rocketPos = firePoint.position + direction.normalized * 0.8f;
                GameObject rocket = Instantiate(bulletPrefabs[0], rocketPos, Quaternion.identity);
                
                float rocketAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                rocket.transform.rotation = Quaternion.Euler(0, 0, rocketAngle);
                
                var rocketRb = rocket.GetComponent<Rigidbody2D>();
                rocketRb.velocity = direction.normalized * (bulletSpeed * 1.3f);
                
                rocket.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                if (rocket.GetComponent<Bullet>() != null)
                {
                    rocket.GetComponent<Bullet>().damage = 40f;
                }
                
                NetworkServer.Spawn(rocket);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(rocket);
                Destroy(rocket, bulletLifetime);
                break;

            case 1: // Beam Mode - Continuous laser
                for (int i = 1; i <= 8; i++)
                {
                    Vector3 beamPos = firePoint.position + (direction.normalized * i * 0.5f);
                    GameObject beamSegment = Instantiate(bulletPrefabs[2], beamPos, Quaternion.identity);
                    
                    float beamAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    beamSegment.transform.rotation = Quaternion.Euler(0, 0, beamAngle);
                    
                    beamSegment.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                    if (beamSegment.GetComponent<Bullet>() != null)
                    {
                        beamSegment.GetComponent<Bullet>().damage = 8f; // Lower per-segment damage
                    }
                    
                    NetworkServer.Spawn(beamSegment);
                    
                    // Enhance bullet with integrated fun features
                    EnhanceBulletFunFeatures(beamSegment);
                    Destroy(beamSegment, 0.3f);
                }
                break;

            case 2: // Grenade Mode - Bouncing explosive
                Vector3 grenadePos = firePoint.position + direction.normalized * 0.6f;
                GameObject grenade = Instantiate(bulletPrefabs[1], grenadePos, Quaternion.identity);
                
                float grenadeAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                grenade.transform.rotation = Quaternion.Euler(0, 0, grenadeAngle);
                
                var grenadeRb = grenade.GetComponent<Rigidbody2D>();
                grenadeRb.velocity = direction.normalized * (bulletSpeed * 0.7f);
                
                grenade.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                if (grenade.GetComponent<Bullet>() != null)
                {
                    grenade.GetComponent<Bullet>().damage = 45f; // High damage, area effect
                }
                
                NetworkServer.Spawn(grenade);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(grenade);
                Destroy(grenade, bulletLifetime * 1.5f);
                break;
        }
    }

    public void ShootSpiritBow(Vector3 direction)
    {
        // Spirit Bow - Piercing/Explosive/Homing modes
        switch (swapModeNum)
        {
            case 0: // Piercing Mode - Arrow goes through enemies
                Vector3 piercePos = firePoint.position + direction.normalized * 0.6f;
                GameObject pierceArrow = Instantiate(bulletPrefabs[0], piercePos, Quaternion.identity);
                
                float pierceAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                pierceArrow.transform.rotation = Quaternion.Euler(0, 0, pierceAngle);
                
                var pierceRb = pierceArrow.GetComponent<Rigidbody2D>();
                pierceRb.velocity = direction.normalized * (bulletSpeed * 1.4f);
                
                var pierceBullet = pierceArrow.GetComponent<Bullet>();
                if (pierceBullet != null)
                {
                    pierceBullet.shooterId = GetComponent<Enemy>().connectionId;
                    pierceBullet.damage = 25f;
                    pierceBullet.isPiercing = true; // Enable piercing
                    pierceBullet.maxPierceTargets = -1; // Unlimited piercing
                }
                
                NetworkServer.Spawn(pierceArrow);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(pierceArrow);
                Destroy(pierceArrow, bulletLifetime * 1.2f);
                break;

            case 1: // Explosive Mode - Arrow explodes on impact
                Vector3 explosivePos = firePoint.position + direction.normalized * 0.6f;
                GameObject explosiveArrow = Instantiate(bulletPrefabs[1], explosivePos, Quaternion.identity);
                
                float explosiveAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                explosiveArrow.transform.rotation = Quaternion.Euler(0, 0, explosiveAngle);
                
                var explosiveRb = explosiveArrow.GetComponent<Rigidbody2D>();
                explosiveRb.velocity = direction.normalized * bulletSpeed;
                
                explosiveArrow.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                if (explosiveArrow.GetComponent<Bullet>() != null)
                {
                    explosiveArrow.GetComponent<Bullet>().damage = 35f;
                }
                
                NetworkServer.Spawn(explosiveArrow);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(explosiveArrow);
                Destroy(explosiveArrow, bulletLifetime);
                break;

            case 2: // Homing Mode - Arrow seeks nearest enemy
                Vector3 homingPos = firePoint.position + direction.normalized * 0.6f;
                GameObject homingArrow = Instantiate(bulletPrefabs[2], homingPos, Quaternion.identity);
                
                float homingAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                homingArrow.transform.rotation = Quaternion.Euler(0, 0, homingAngle);
                
                var homingRb = homingArrow.GetComponent<Rigidbody2D>();
                homingRb.velocity = direction.normalized * (bulletSpeed * 0.9f);
                
                homingArrow.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                if (homingArrow.GetComponent<Bullet>() != null)
                {
                    homingArrow.GetComponent<Bullet>().damage = 30f;
                }
                
                NetworkServer.Spawn(homingArrow);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(homingArrow);
                Destroy(homingArrow, bulletLifetime * 2f);
                break;
        }
    }

    public void ShootWarHammer(Vector3 direction)
    {
        // War Hammer - Slam/Throw/Spin modes inspired by Smash Bros
        switch (swapModeNum)
        {
            case 0: // Slam Mode - Close range shockwave
                for (int i = -2; i <= 2; i++)
                {
                    float slamAngle = i * 30f;
                    Vector3 slamDir = Quaternion.Euler(0, 0, slamAngle) * direction;
                    Vector3 slamPos = firePoint.position + slamDir.normalized * (0.5f + Mathf.Abs(i) * 0.3f);
                    
                    GameObject shockwave = Instantiate(bulletPrefabs[2], slamPos, Quaternion.identity);
                    float shockAngle = Mathf.Atan2(slamDir.y, slamDir.x) * Mathf.Rad2Deg;
                    shockwave.transform.rotation = Quaternion.Euler(0, 0, shockAngle);
                    
                    shockwave.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                    if (shockwave.GetComponent<Bullet>() != null)
                    {
                        shockwave.GetComponent<Bullet>().damage = 30f;
                    }
                    
                    NetworkServer.Spawn(shockwave);
                    
                    // Enhance bullet with integrated fun features
                    EnhanceBulletFunFeatures(shockwave);
                    Destroy(shockwave, 0.2f);
                }
                break;

            case 1: // Throw Mode - Boomerang effect
                Vector3 throwPos = firePoint.position + direction.normalized * 0.7f;
                GameObject thrownHammer = Instantiate(bulletPrefabs[1], throwPos, Quaternion.identity);
                
                float throwAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                thrownHammer.transform.rotation = Quaternion.Euler(0, 0, throwAngle);
                
                var throwRb = thrownHammer.GetComponent<Rigidbody2D>();
                throwRb.velocity = direction.normalized * (bulletSpeed * 1.1f);
                throwRb.angularVelocity = 540f; // Fast spinning
                
                thrownHammer.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                if (thrownHammer.GetComponent<Bullet>() != null)
                {
                    thrownHammer.GetComponent<Bullet>().damage = 40f;
                }
                
                NetworkServer.Spawn(thrownHammer);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(thrownHammer);
                Destroy(thrownHammer, bulletLifetime * 1.3f);
                break;

            case 2: // Spin Mode - 360 degree attack
                for (int i = 0; i < 12; i++)
                {
                    float spinAngle = i * 30f;
                    Vector3 spinDir = Quaternion.Euler(0, 0, spinAngle) * Vector3.right;
                    Vector3 spinPos = firePoint.position + spinDir * 0.8f;
                    
                    GameObject spinHitbox = Instantiate(bulletPrefabs[2], spinPos, Quaternion.identity);
                    spinHitbox.transform.rotation = Quaternion.Euler(0, 0, spinAngle);
                    
                    spinHitbox.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                    if (spinHitbox.GetComponent<Bullet>() != null)
                    {
                        spinHitbox.GetComponent<Bullet>().damage = 20f;
                    }
                    
                    NetworkServer.Spawn(spinHitbox);
                    
                    // Enhance bullet with integrated fun features
                    EnhanceBulletFunFeatures(spinHitbox);
                    Destroy(spinHitbox, 0.15f);
                }
                break;
        }
    }

    private void ShootPlasmaRifle(Vector3 direction)
    {
        // Plasma Rifle - Burst/Charge/Overload modes
        switch (swapModeNum)
        {
            case 0: // Burst Mode - 3-shot burst
                for (int i = 0; i < 3; i++)
                {
                    StartCoroutine(DelayedPlasmaShot(direction, i * 0.1f, 15f));
                }
                break;

            case 1: // Charge Mode - Single powerful shot
                Vector3 chargePos = firePoint.position + direction.normalized * 0.8f;
                GameObject chargeShot = Instantiate(bulletPrefabs[0], chargePos, Quaternion.identity);
                
                float chargeAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                chargeShot.transform.rotation = Quaternion.Euler(0, 0, chargeAngle);
                
                var chargeRb = chargeShot.GetComponent<Rigidbody2D>();
                chargeRb.velocity = direction.normalized * (bulletSpeed * 0.8f);
                
                chargeShot.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                if (chargeShot.GetComponent<Bullet>() != null)
                {
                    chargeShot.GetComponent<Bullet>().damage = 60f; // Very high damage
                }
                
                NetworkServer.Spawn(chargeShot);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(chargeShot);
                Destroy(chargeShot, bulletLifetime);
                break;

            case 2: // Overload Mode - Spread shot with self-damage risk
                for (int i = -4; i <= 4; i++)
                {
                    float overloadAngle = i * 10f;
                    Vector3 overloadDir = Quaternion.Euler(0, 0, overloadAngle) * direction;
                    
                    Vector3 overloadPos = firePoint.position + overloadDir.normalized * 0.6f;
                    GameObject overloadShot = Instantiate(bulletPrefabs[1], overloadPos, Quaternion.identity);
                    
                    var overloadRb = overloadShot.GetComponent<Rigidbody2D>();
                    overloadRb.velocity = overloadDir.normalized * (bulletSpeed * 1.2f);
                    
                    overloadShot.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                    if (overloadShot.GetComponent<Bullet>() != null)
                    {
                        overloadShot.GetComponent<Bullet>().damage = 18f;
                    }
                    
                    NetworkServer.Spawn(overloadShot);
                    
                    // Enhance bullet with integrated fun features
                    EnhanceBulletFunFeatures(overloadShot);
                    Destroy(overloadShot, bulletLifetime * 0.7f);
                }
                break;
        }
    }

    private IEnumerator DelayedPlasmaShot(Vector3 direction, float delay, float damage)
    {
        yield return new WaitForSeconds(delay);
        
        Vector3 burstPos = firePoint.position + direction.normalized * 0.6f;
        GameObject burstShot = Instantiate(bulletPrefabs[0], burstPos, Quaternion.identity);
        
        float burstAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        burstShot.transform.rotation = Quaternion.Euler(0, 0, burstAngle);
        
        var burstRb = burstShot.GetComponent<Rigidbody2D>();
        burstRb.velocity = direction.normalized * bulletSpeed;
        
        burstShot.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
        if (burstShot.GetComponent<Bullet>() != null)
        {
            burstShot.GetComponent<Bullet>().damage = damage;
        }
        
        NetworkServer.Spawn(burstShot);
        
        // Enhance bullet with integrated fun features
        EnhanceBulletFunFeatures(burstShot);
        Destroy(burstShot, bulletLifetime);
    }

    public void ShootNinjaKunai(Vector3 direction)
    {
        // Ninja Kunai - Shadow/Poison/Teleport modes
        switch (swapModeNum)
        {
            case 0: // Shadow Mode - Multiple kunai from different angles
                for (int i = -1; i <= 1; i++)
                {
                    float shadowAngle = i * 15f;
                    Vector3 shadowDir = Quaternion.Euler(0, 0, shadowAngle) * direction;
                    
                    Vector3 shadowPos = firePoint.position + shadowDir.normalized * 0.5f;
                    GameObject shadowKunai = Instantiate(bulletPrefabs[2], shadowPos, Quaternion.identity);
                    
                    float kunaiAngle = Mathf.Atan2(shadowDir.y, shadowDir.x) * Mathf.Rad2Deg;
                    shadowKunai.transform.rotation = Quaternion.Euler(0, 0, kunaiAngle);
                    
                    var shadowRb = shadowKunai.GetComponent<Rigidbody2D>();
                    shadowRb.velocity = shadowDir.normalized * (bulletSpeed * 1.3f);
                    
                    shadowKunai.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                    if (shadowKunai.GetComponent<Bullet>() != null)
                    {
                        shadowKunai.GetComponent<Bullet>().damage = 20f;
                    }
                    
                    NetworkServer.Spawn(shadowKunai);
                    
                    // Enhance bullet with integrated fun features
                    EnhanceBulletFunFeatures(shadowKunai);
                    Destroy(shadowKunai, bulletLifetime);
                }
                break;

            case 1: // Poison Mode - DoT kunai
                Vector3 poisonPos = firePoint.position + direction.normalized * 0.5f;
                GameObject poisonKunai = Instantiate(bulletPrefabs[2], poisonPos, Quaternion.identity);
                
                float poisonAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                poisonKunai.transform.rotation = Quaternion.Euler(0, 0, poisonAngle);
                
                var poisonRb = poisonKunai.GetComponent<Rigidbody2D>();
                poisonRb.velocity = direction.normalized * bulletSpeed;
                
                poisonKunai.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                if (poisonKunai.GetComponent<Bullet>() != null)
                {
                    poisonKunai.GetComponent<Bullet>().damage = 15f; // Lower direct damage, poison effect
                }
                
                NetworkServer.Spawn(poisonKunai);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(poisonKunai);
                Destroy(poisonKunai, bulletLifetime);
                break;

            case 2: // Teleport Mode - Instant hit at target location
                Vector3 teleportTarget = firePoint.position + direction.normalized * 4f;
                GameObject teleportKunai = Instantiate(bulletPrefabs[2], teleportTarget, Quaternion.identity);
                
                float teleportAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                teleportKunai.transform.rotation = Quaternion.Euler(0, 0, teleportAngle);
                
                teleportKunai.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                if (teleportKunai.GetComponent<Bullet>() != null)
                {
                    teleportKunai.GetComponent<Bullet>().damage = 35f;
                }
                
                NetworkServer.Spawn(teleportKunai);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(teleportKunai);
                Destroy(teleportKunai, 0.1f);
                break;
        }
    }

    public void ShootChaosOrb(Vector3 direction)
    {
        // Chaos Orb - Random/Portal/Gravity modes
        switch (swapModeNum)
        {
            case 0: // Random Mode - Unpredictable bouncing orb
                Vector3 randomPos = firePoint.position + direction.normalized * 0.6f;
                GameObject chaosOrb = Instantiate(bulletPrefabs[1], randomPos, Quaternion.identity);
                
                // Add random deviation to direction
                float randomAngle = UnityEngine.Random.Range(-30f, 30f);
                Vector3 chaosDir = Quaternion.Euler(0, 0, randomAngle) * direction;
                
                float orbAngle = Mathf.Atan2(chaosDir.y, chaosDir.x) * Mathf.Rad2Deg;
                chaosOrb.transform.rotation = Quaternion.Euler(0, 0, orbAngle);
                
                var chaosRb = chaosOrb.GetComponent<Rigidbody2D>();
                chaosRb.velocity = chaosDir.normalized * (bulletSpeed * UnityEngine.Random.Range(0.8f, 1.4f));
                chaosRb.angularVelocity = UnityEngine.Random.Range(-360f, 360f);
                
                chaosOrb.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                if (chaosOrb.GetComponent<Bullet>() != null)
                {
                    chaosOrb.GetComponent<Bullet>().damage = UnityEngine.Random.Range(20f, 45f);
                }
                
                NetworkServer.Spawn(chaosOrb);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(chaosOrb);
                Destroy(chaosOrb, bulletLifetime * UnityEngine.Random.Range(0.8f, 1.5f));
                break;

            case 1: // Portal Mode - Creates temporary portal
                Vector3 portalPos = firePoint.position + direction.normalized * 3f;
                GameObject portal = Instantiate(bulletPrefabs[2], portalPos, Quaternion.identity);
                
                portal.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                if (portal.GetComponent<Bullet>() != null)
                {
                    portal.GetComponent<Bullet>().damage = 25f;
                }
                
                NetworkServer.Spawn(portal);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(portal);
                Destroy(portal, 2f); // Portal lasts 2 seconds
                break;

            case 2: // Gravity Mode - Pulls enemies toward center
                Vector3 gravityPos = firePoint.position + direction.normalized * 2f;
                GameObject gravityOrb = Instantiate(bulletPrefabs[1], gravityPos, Quaternion.identity);
                
                gravityOrb.GetComponent<Bullet>().shooterId = GetComponent<Enemy>().connectionId;
                if (gravityOrb.GetComponent<Bullet>() != null)
                {
                    gravityOrb.GetComponent<Bullet>().damage = 30f;
                }
                
                NetworkServer.Spawn(gravityOrb);
                
                // Enhance bullet with integrated fun features
                EnhanceBulletFunFeatures(gravityOrb);
                Destroy(gravityOrb, 3f); // Gravity effect lasts 3 seconds
                break;
        }
    }

    // ===== NEW UNIQUE WEAPONS =====

    public void ShootLightningGun(Vector3 direction)
    {
        // Lightning Gun - Instant lightning bolt that stretches to target
        Vector3 targetDir = GetComponent<Autoaim>().FindTarget();
        Vector3 finalDirection;
        float lightningDistance = 8f; // Max lightning stretch distance
        
        if (targetDir != Vector3.positiveInfinity && targetDir.magnitude > 0.1f && targetDir.magnitude < 50f)
        {
            // Target found - stretch lightning to target
            finalDirection = targetDir.normalized;
            lightningDistance = Mathf.Min(targetDir.magnitude, lightningDistance);
        }
        else
        {
            // No target - shoot in mouse direction
            finalDirection = direction.magnitude > 0.1f ? direction.normalized : Vector3.right;
        }
        
        // Clamp lightning distance to prevent physics errors
        lightningDistance = Mathf.Clamp(lightningDistance, 1f, 8f);
        
        // Position lightning with pivot at left center, starting from firePoint
        Vector3 lightningStartPos = firePoint.position;
        
        // Spawn lightning bolt at starting position
        GameObject lightning = Instantiate(bulletPrefabs[0], lightningStartPos, Quaternion.identity);
        
        float lightningAngle = Mathf.Atan2(finalDirection.y, finalDirection.x) * Mathf.Rad2Deg;
        lightning.transform.rotation = Quaternion.Euler(0, 0, lightningAngle);
        
        // Scale the lightning bolt to stretch to target distance
        // Since pivot is at left center, scaling X will extend it in the right direction
        var spriteRenderer = lightning.GetComponent<SpriteRenderer>();
        var boxCollider = lightning.GetComponent<BoxCollider2D>();
        
        if (spriteRenderer != null)
        {
            Vector3 scale = lightning.transform.localScale;
            scale.x = lightningDistance; // Stretch along the bolt direction
            lightning.transform.localScale = scale;
        }
        
        // Adjust BoxCollider to match the stretched lightning
        if (boxCollider != null)
        {
            Vector2 colliderSize = boxCollider.size;
            colliderSize.x = lightningDistance;
            boxCollider.size = colliderSize;
            
            // Center the collider along the lightning bolt
            Vector2 colliderOffset = boxCollider.offset;
            colliderOffset.x = lightningDistance * 0.5f;
            boxCollider.offset = colliderOffset;
        }
        
        // Set lightning at rest (no velocity - instant hit)
        var lightningRb = lightning.GetComponent<Rigidbody2D>();
        if (lightningRb != null)
        {
            lightningRb.velocity = Vector2.zero;
            lightningRb.isKinematic = true; // Prevent physics movement
        }
        
        var lightningBullet = lightning.GetComponent<Bullet>();
        lightningBullet.shooterId = GetComponent<Enemy>().connectionId;
        lightningBullet.damage = 30f;
        lightningBullet.effectType = BulletEffectType.LightningBolt;
        lightningBullet.effectDamage = 20f; // Chain damage
        lightningBullet.lightningRange = 4f; // Chain range
        lightningBullet.lightningTargets = 5; // Max chain targets
        
        NetworkServer.Spawn(lightning);
        
        // Enhance bullet with integrated fun features
        EnhanceBulletFunFeatures(lightning);
        
        // Screen shake for lightning impact
        ScreenShake.ShakeLightning(0.4f);
        
        // Lightning bolt lingers for animation to complete (0.5s animation)
        Destroy(lightning, 0.6f);
    }

    public void ShootRocketLauncher(Vector3 direction)
    {
        // Rocket Launcher - Explosive projectile with area damage
        Vector3 rocketPos = firePoint.position + direction.normalized * 0.8f;
        GameObject rocket = Instantiate(bulletPrefabs[1], rocketPos, Quaternion.identity);
        
        float rocketAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rocket.transform.rotation = Quaternion.Euler(0, 0, rocketAngle);
        
        var rocketRb = rocket.GetComponent<Rigidbody2D>();
        rocketRb.velocity = direction.normalized * (bulletSpeed * 0.9f);
        
        var rocketBullet = rocket.GetComponent<Bullet>();
        rocketBullet.shooterId = GetComponent<Enemy>().connectionId;
        rocketBullet.damage = 50f; // High direct damage
        
        // Create explosion effect on destruction
        StartCoroutine(CreateExplosion(rocket, direction, 2f, 35f));
        
        NetworkServer.Spawn(rocket);
        
        // Enhance bullet with integrated fun features
        EnhanceBulletFunFeatures(rocket);
        Destroy(rocket, bulletLifetime);
    }

    public void ShootFlameThrower(Vector3 direction)
    {
        // Flame Thrower - Continuous stream of fire with burn effect
        int flames = 6;
        for (int i = 0; i < flames; i++)
        {
            float flameDistance = (i + 1) * 0.4f;
            Vector3 flamePos = firePoint.position + direction.normalized * flameDistance;
            
            // Add slight spread for realistic flame effect
            float spreadAngle = UnityEngine.Random.Range(-8f, 8f);
            Vector3 flameDir = Quaternion.Euler(0, 0, spreadAngle) * direction;
            flamePos += new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), UnityEngine.Random.Range(-0.2f, 0.2f), 0);
            
            GameObject flame = Instantiate(bulletPrefabs[2], flamePos, Quaternion.identity);
            
            float flameAngle = Mathf.Atan2(flameDir.y, flameDir.x) * Mathf.Rad2Deg;
            flame.transform.rotation = Quaternion.Euler(0, 0, flameAngle);
            
            var flameBullet = flame.GetComponent<Bullet>();
            flameBullet.shooterId = GetComponent<Enemy>().connectionId;
            flameBullet.damage = 12f; // Lower direct damage, DoT effect
            flameBullet.effectType = BulletEffectType.BurnDamage;
            flameBullet.effectDuration = 5f; // Long burn duration
            flameBullet.effectDamage = 4f; // Burn damage per tick
            
            NetworkServer.Spawn(flame);
            
            // Enhance bullet with integrated fun features
            EnhanceBulletFunFeatures(flame);
            Destroy(flame, 0.4f); // Short lifetime for flame segments
        }
    }

    public void ShootIceBeam(Vector3 direction)
    {
        // Ice Beam - Continuous beam that slows and freezes
        for (int i = 1; i <= 10; i++)
        {
            Vector3 icePos = firePoint.position + (direction.normalized * i * 0.3f);
            GameObject iceSegment = Instantiate(bulletPrefabs[2], icePos, Quaternion.identity);
            
            float iceAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            iceSegment.transform.rotation = Quaternion.Euler(0, 0, iceAngle);
            
            var iceBullet = iceSegment.GetComponent<Bullet>();
            iceBullet.shooterId = GetComponent<Enemy>().connectionId;
            iceBullet.damage = 8f; // Low damage per segment
            iceBullet.effectType = BulletEffectType.FreezeEffect;
            iceBullet.effectDuration = 2f; // Freeze duration
            
            NetworkServer.Spawn(iceSegment);
            
            // Enhance bullet with integrated fun features
            EnhanceBulletFunFeatures(iceSegment);
            Destroy(iceSegment, 0.5f);
        }
    }

    public void ShootBoomerang(Vector3 direction)
    {
        // Boomerang - Returns to player after traveling
        Vector3 boomerangPos = firePoint.position + direction.normalized * 0.6f;
        GameObject boomerang = Instantiate(bulletPrefabs[1], boomerangPos, Quaternion.identity);
        
        float boomerangAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        boomerang.transform.rotation = Quaternion.Euler(0, 0, boomerangAngle);
        
        var boomerangRb = boomerang.GetComponent<Rigidbody2D>();
        boomerangRb.velocity = direction.normalized * bulletSpeed;
        boomerangRb.angularVelocity = 720f; // Fast spinning
        
        var boomerangBullet = boomerang.GetComponent<Bullet>();
        boomerangBullet.shooterId = GetComponent<Enemy>().connectionId;
        boomerangBullet.damage = 25f;
        
        // Create return behavior
        StartCoroutine(BoomerangReturn(boomerang, 1.5f));
        
        NetworkServer.Spawn(boomerang);
        
        // Enhance bullet with integrated fun features
        EnhanceBulletFunFeatures(boomerang);
        Destroy(boomerang, bulletLifetime * 2f); // Double lifetime for return trip
    }

    public void ShootLaserCannon(Vector3 direction)
    {
        // Laser Cannon - Piercing beam that goes through multiple enemies
        for (int i = 1; i <= 15; i++)
        {
            Vector3 laserPos = firePoint.position + (direction.normalized * i * 0.4f);
            GameObject laserSegment = Instantiate(bulletPrefabs[0], laserPos, Quaternion.identity);
            
            float laserAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            laserSegment.transform.rotation = Quaternion.Euler(0, 0, laserAngle);
            
            var laserBullet = laserSegment.GetComponent<Bullet>();
            if (laserBullet != null)
            {
                laserBullet.shooterId = GetComponent<Enemy>().connectionId;
                laserBullet.damage = 18f; // High damage, pierces through
                laserBullet.isPiercing = true; // Laser pierces through all enemies
                laserBullet.maxPierceTargets = -1; // Unlimited piercing
            }
            
            NetworkServer.Spawn(laserSegment);
            
            // Enhance bullet with integrated fun features
            EnhanceBulletFunFeatures(laserSegment);
            Destroy(laserSegment, 0.2f);
        }
    }

    public void ShootGravityGun(Vector3 direction)
    {
        // Gravity Gun - Creates gravity well that pulls enemies
        Vector3 gravityPos = firePoint.position + direction.normalized * 3f;
        GameObject gravityWell = Instantiate(bulletPrefabs[2], gravityPos, Quaternion.identity);
        
        var gravityBullet = gravityWell.GetComponent<Bullet>();
        gravityBullet.shooterId = GetComponent<Enemy>().connectionId;
        gravityBullet.damage = 15f; // Continuous damage
        gravityBullet.effectType = BulletEffectType.SlowEffect;
        gravityBullet.effectDuration = 4f; // Pull effect duration
        gravityBullet.slowAmount = 0.3f; // Strong slow effect
        
        // Create pull effect for nearby enemies
        StartCoroutine(GravityPullEffect(gravityWell, 4f));
        
        NetworkServer.Spawn(gravityWell);
        
        // Enhance bullet with integrated fun features
        EnhanceBulletFunFeatures(gravityWell);
        Destroy(gravityWell, 4f); // Gravity well lasts 4 seconds
    }

    public void ShootVenomSpitter(Vector3 direction)
    {
        // Venom Spitter - Spreads poison in an arc
        int venomGlobs = 5;
        float maxSpread = 30f;
        
        for (int i = -2; i <= 2; i++)
        {
            float venomAngle = i * (maxSpread / 4f);
            Vector3 venomDir = Quaternion.Euler(0, 0, venomAngle) * direction.normalized;
            
            Vector3 venomPos = firePoint.position + venomDir * 0.5f;
            GameObject venomGlob = Instantiate(bulletPrefabs[1], venomPos, Quaternion.identity);
            
            var venomRb = venomGlob.GetComponent<Rigidbody2D>();
            venomRb.velocity = venomDir * (bulletSpeed * 0.8f);
            
            var venomBullet = venomGlob.GetComponent<Bullet>();
            venomBullet.shooterId = GetComponent<Enemy>().connectionId;
            venomBullet.damage = 20f;
            venomBullet.effectType = BulletEffectType.BurnDamage; // Reuse burn effect for poison
            venomBullet.effectDuration = 6f; // Long poison duration
            venomBullet.effectDamage = 3f; // Poison damage per tick
            
            NetworkServer.Spawn(venomGlob);
            
            // Enhance bullet with integrated fun features
            EnhanceBulletFunFeatures(venomGlob);
            Destroy(venomGlob, bulletLifetime);
        }
    }

    // Helper coroutines for special weapon effects
    private IEnumerator CreateExplosion(GameObject rocket, Vector3 direction, float explosionRadius, float explosionDamage)
    {
        // Wait for rocket to be destroyed or hit something (with timeout)
        float elapsed = 0f;
        float maxWait = bulletLifetime + 1f;
        while (rocket != null && elapsed < maxWait)
        {
            elapsed += 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        
        // Create explosion effect at last known position
        Vector3 explosionPos = rocket?.transform.position ?? (firePoint.position + direction.normalized * 3f);
        
        // Find all entities in explosion radius
        Collider2D[] targets = Physics2D.OverlapCircleAll(explosionPos, explosionRadius);
        
        foreach (var target in targets)
        {
            var enemy = target.GetComponent<Enemy>();
            var actualEnemy = target.GetComponent<ActualEnemy>();
            
            if (enemy != null && !enemy.isEnemy && enemy.connectionId != GetComponent<Enemy>().connectionId)
            {
                enemy.TakeDamage((int)explosionDamage);
            }
            else if (actualEnemy != null && actualEnemy.isEnemy)
            {
                actualEnemy.TakeDamage((int)explosionDamage);
            }
        }
    }

    private IEnumerator BoomerangReturn(GameObject boomerang, float outwardTime)
    {
        yield return new WaitForSeconds(outwardTime);
        
        if (boomerang != null)
        {
            var rb = boomerang.GetComponent<Rigidbody2D>();
            
            // Reverse direction and increase speed for return
            Vector3 returnDirection = (firePoint.position - boomerang.transform.position).normalized;
            rb.velocity = returnDirection * (bulletSpeed * 1.5f);
            rb.angularVelocity = -720f; // Reverse spin
        }
    }

    private IEnumerator GravityPullEffect(GameObject gravityWell, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration && gravityWell != null)
        {
            // Find enemies within gravity range
            Collider2D[] targets = Physics2D.OverlapCircleAll(gravityWell.transform.position, 3f);
            
            foreach (var target in targets)
            {
                var enemy = target.GetComponent<Enemy>();
                var actualEnemy = target.GetComponent<ActualEnemy>();
                var targetRb = target.GetComponent<Rigidbody2D>();
                
                if (targetRb != null)
                {
                    bool validTarget = false;
                    
                    if (enemy != null && !enemy.isEnemy && enemy.connectionId != GetComponent<Enemy>().connectionId)
                        validTarget = true;
                    else if (actualEnemy != null && actualEnemy.isEnemy)
                        validTarget = true;
                    
                    if (validTarget)
                    {
                        // Apply gravity pull
                        Vector3 pullDirection = (gravityWell.transform.position - target.transform.position).normalized;
                        targetRb.AddForce(pullDirection * 150f * Time.fixedDeltaTime);
                    }
                }
            }
            
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
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
        
        // Keep NetworkTransform enabled for proper synchronization
        // Don't parent - let NetworkTransform handle position sync
        
        instance.GetComponent<Weapon>().slotnum = weapId;
        instance.GetComponent<Weapon>().SetParent(gameObject);
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
    void UpdateUI() { }
    #endregion

    #region SyncVar Hooks
    void OnWeaponIdAChanged(uint oldId, uint newId) { Debug.Log($"WeaponIdA: {oldId} -> {newId}"); }
    void OnWeaponIdBChanged(uint oldId, uint newId) { Debug.Log($"WeaponIdB: {oldId} -> {newId}"); }
    void OnWeaponIdCChanged(uint oldId, uint newId) { Debug.Log($"WeaponIdC: {oldId} -> {newId}"); }
    #endregion
}

