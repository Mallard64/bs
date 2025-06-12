using UnityEngine;
using Mirror;

public class FunFeaturesManager : NetworkBehaviour
{
    [Header("Feature Toggles")]
    public bool enableScreenShake = true;
    public bool enableWeaponCombos = true;
    public bool enableWeaponOvercharge = true;
    public bool enableBulletTrails = true;
    public bool enableRicochetBullets = false; // Off by default to avoid chaos
    public bool enableExplosiveBarrels = true;
    
    [Header("Screen Shake Settings")]
    public Camera mainCamera;
    
    [Header("Bullet Enhancement Settings")]
    public float ricochetChance = 0.2f; // 20% chance for ricochet
    public LayerMask bulletLayer = 1;
    
    [Header("Barrel Spawn Settings")]
    public GameObject explosiveBarrelPrefab;
    public Transform[] barrelSpawnPoints;
    public bool autoSpawnBarrels = true;
    
    // Component references
    private ScreenShake screenShakeComponent;
    private WeaponComboSystem comboSystem;
    private WeaponOverchargeSystem overchargeSystem;
    
    void Start()
    {
        // Auto-setup all enabled features
        SetupEnabledFeatures();
        
        // Spawn barrels if enabled
        if (enableExplosiveBarrels && autoSpawnBarrels)
        {
            SpawnExplosiveBarrels();
        }
    }
    
    void SetupEnabledFeatures()
    {
        // Setup Screen Shake
        if (enableScreenShake)
        {
            EnableScreenShake();
        }
        
        // Setup Weapon Combos
        if (enableWeaponCombos)
        {
            EnableWeaponCombos();
        }
        
        // Setup Weapon Overcharge
        if (enableWeaponOvercharge)
        {
            EnableWeaponOvercharge();
        }
        
        // Setup Bullet Trails for existing bullets
        if (enableBulletTrails)
        {
            EnableBulletTrails();
        }
    }
    
    #region Screen Shake
    public void EnableScreenShake()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
        
        if (mainCamera != null && screenShakeComponent == null)
        {
            screenShakeComponent = mainCamera.gameObject.AddComponent<ScreenShake>();
            Debug.Log("✨ Screen Shake enabled!");
        }
    }
    
    public void DisableScreenShake()
    {
        if (screenShakeComponent != null)
        {
            Destroy(screenShakeComponent);
            screenShakeComponent = null;
            Debug.Log("❌ Screen Shake disabled!");
        }
    }
    
    public void ToggleScreenShake()
    {
        enableScreenShake = !enableScreenShake;
        if (enableScreenShake)
            EnableScreenShake();
        else
            DisableScreenShake();
    }
    #endregion
    
    #region Weapon Combos
    public void EnableWeaponCombos()
    {
        if (comboSystem == null)
        {
            comboSystem = gameObject.AddComponent<WeaponComboSystem>();
            Debug.Log("🔥 Weapon Combo System enabled!");
        }
    }
    
    public void DisableWeaponCombos()
    {
        if (comboSystem != null)
        {
            Destroy(comboSystem);
            comboSystem = null;
            Debug.Log("❌ Weapon Combo System disabled!");
        }
    }
    
    public void ToggleWeaponCombos()
    {
        enableWeaponCombos = !enableWeaponCombos;
        if (enableWeaponCombos)
            EnableWeaponCombos();
        else
            DisableWeaponCombos();
    }
    #endregion
    
    #region Weapon Overcharge
    public void EnableWeaponOvercharge()
    {
        if (overchargeSystem == null)
        {
            overchargeSystem = gameObject.AddComponent<WeaponOverchargeSystem>();
            Debug.Log("⚡ Weapon Overcharge System enabled!");
        }
    }
    
    public void DisableWeaponOvercharge()
    {
        if (overchargeSystem != null)
        {
            Destroy(overchargeSystem);
            overchargeSystem = null;
            Debug.Log("❌ Weapon Overcharge System disabled!");
        }
    }
    
    public void ToggleWeaponOvercharge()
    {
        enableWeaponOvercharge = !enableWeaponOvercharge;
        if (enableWeaponOvercharge)
            EnableWeaponOvercharge();
        else
            DisableWeaponOvercharge();
    }
    #endregion
    
    #region Bullet Trails
    public void EnableBulletTrails()
    {
        // Find all existing bullets and add trails
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject bullet in bullets)
        {
            AddTrailToBullet(bullet);
        }
        
        Debug.Log($"✨ Bullet Trails enabled for {bullets.Length} bullets!");
    }
    
    public void DisableBulletTrails()
    {
        // Remove trail components from all bullets
        BulletTrailSystem[] trailSystems = FindObjectsOfType<BulletTrailSystem>();
        foreach (BulletTrailSystem trail in trailSystems)
        {
            Destroy(trail);
        }
        
        Debug.Log("❌ Bullet Trails disabled!");
    }
    
    public void ToggleBulletTrails()
    {
        enableBulletTrails = !enableBulletTrails;
        if (enableBulletTrails)
            EnableBulletTrails();
        else
            DisableBulletTrails();
    }
    
    void AddTrailToBullet(GameObject bullet)
    {
        if (bullet.GetComponent<BulletTrailSystem>() == null)
        {
            BulletTrailSystem trailSystem = bullet.AddComponent<BulletTrailSystem>();
            trailSystem.enableTrail = true;
            trailSystem.enableParticles = true;
        }
    }
    #endregion
    
    #region Ricochet Bullets
    public void EnableRicochetBullets()
    {
        // Add ricochet to random bullets
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject bullet in bullets)
        {
            if (Random.Range(0f, 1f) < ricochetChance)
            {
                AddRicochetToBullet(bullet);
            }
        }
        
        Debug.Log($"🎯 Ricochet enabled for some bullets!");
    }
    
    public void DisableRicochetBullets()
    {
        RicochetBullet[] ricochetBullets = FindObjectsOfType<RicochetBullet>();
        foreach (RicochetBullet ricochet in ricochetBullets)
        {
            Destroy(ricochet);
        }
        
        Debug.Log("❌ Ricochet Bullets disabled!");
    }
    
    public void ToggleRicochetBullets()
    {
        enableRicochetBullets = !enableRicochetBullets;
        if (enableRicochetBullets)
            EnableRicochetBullets();
        else
            DisableRicochetBullets();
    }
    
    void AddRicochetToBullet(GameObject bullet)
    {
        if (bullet.GetComponent<RicochetBullet>() == null)
        {
            RicochetBullet ricochet = bullet.AddComponent<RicochetBullet>();
            ricochet.maxBounces = 2; // Conservative default
        }
    }
    #endregion
    
    #region Explosive Barrels
    public void SpawnExplosiveBarrels()
    {
        if (barrelSpawnPoints == null || barrelSpawnPoints.Length == 0)
        {
            // Auto-generate spawn points around the map
            CreateAutoBarrelSpawnPoints();
        }
        
        foreach (Transform spawnPoint in barrelSpawnPoints)
        {
            if (spawnPoint != null)
            {
                CreateExplosiveBarrel(spawnPoint.position);
            }
        }
        
        Debug.Log($"💥 Spawned {barrelSpawnPoints.Length} explosive barrels!");
    }
    
    void CreateAutoBarrelSpawnPoints()
    {
        // Create spawn points in a grid around the map center
        barrelSpawnPoints = new Transform[6];
        GameObject spawnParent = new GameObject("BarrelSpawnPoints");
        
        for (int i = 0; i < 6; i++)
        {
            GameObject spawnPoint = new GameObject($"BarrelSpawn_{i}");
            spawnPoint.transform.SetParent(spawnParent.transform);
            
            // Distribute around a circle
            float angle = (i * 60f) * Mathf.Deg2Rad;
            float radius = 10f;
            Vector3 position = new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0
            );
            spawnPoint.transform.position = position;
            barrelSpawnPoints[i] = spawnPoint.transform;
        }
    }
    
    GameObject CreateExplosiveBarrel(Vector3 position)
    {
        if (explosiveBarrelPrefab != null)
        {
            GameObject barrel = Instantiate(explosiveBarrelPrefab, position, Quaternion.identity);
            if (isServer)
            {
                NetworkServer.Spawn(barrel);
            }
            return barrel;
        }
        else
        {
            // Create basic barrel if no prefab provided
            return ExplosiveBarrel.CreateExplosiveBarrel(position);
        }
    }
    
    public void RemoveAllBarrels()
    {
        ExplosiveBarrel[] barrels = FindObjectsOfType<ExplosiveBarrel>();
        foreach (ExplosiveBarrel barrel in barrels)
        {
            if (isServer)
            {
                NetworkServer.Destroy(barrel.gameObject);
            }
            else
            {
                Destroy(barrel.gameObject);
            }
        }
        
        Debug.Log("💥 All explosive barrels removed!");
    }
    
    public void ToggleExplosiveBarrels()
    {
        enableExplosiveBarrels = !enableExplosiveBarrels;
        if (enableExplosiveBarrels)
            SpawnExplosiveBarrels();
        else
            RemoveAllBarrels();
    }
    #endregion
    
    #region Auto-Enhancement for New Objects
    void Update()
    {
        // Auto-enhance new bullets if features are enabled
        if (enableBulletTrails || enableRicochetBullets)
        {
            EnhanceNewBullets();
        }
    }
    
    void EnhanceNewBullets()
    {
        GameObject[] bullets = GameObject.FindGameObjectsWithTag("Bullet");
        foreach (GameObject bullet in bullets)
        {
            // Add trails if enabled and not already present
            if (enableBulletTrails && bullet.GetComponent<BulletTrailSystem>() == null)
            {
                AddTrailToBullet(bullet);
            }
            
            // Add ricochet if enabled and lucky roll
            if (enableRicochetBullets && bullet.GetComponent<RicochetBullet>() == null)
            {
                if (Random.Range(0f, 1f) < ricochetChance)
                {
                    AddRicochetToBullet(bullet);
                }
            }
        }
    }
    #endregion
    
    #region Public Interface - Easy Integration
    
    // Method to enhance specific bullets manually
    public void EnhanceBullet(GameObject bullet, bool addTrail = true, bool addRicochet = false)
    {
        if (addTrail && enableBulletTrails)
        {
            AddTrailToBullet(bullet);
        }
        
        if (addRicochet && enableRicochetBullets)
        {
            AddRicochetToBullet(bullet);
        }
    }
    
    // Method to register weapon fire for combo/overcharge systems
    public void OnWeaponFired(int weaponId, int mode = 0, Vector3 direction = default)
    {
        if (comboSystem != null && isServer)
        {
            comboSystem.RegisterWeaponFire(weaponId, mode, direction);
        }
        
        if (overchargeSystem != null && isServer)
        {
            overchargeSystem.OnWeaponFired();
        }
    }
    
    // Get current bonuses for weapons to use
    public float GetDamageMultiplier()
    {
        float comboMultiplier = comboSystem != null ? comboSystem.GetDamageMultiplier() : 1f;
        float overchargeMultiplier = overchargeSystem != null ? overchargeSystem.GetDamageMultiplier() : 1f;
        
        return comboMultiplier * overchargeMultiplier;
    }
    
    public bool HasUnlimitedAmmo()
    {
        return overchargeSystem != null && overchargeSystem.HasUnlimitedAmmo();
    }
    
    public bool HasPiercingShots()
    {
        return overchargeSystem != null && overchargeSystem.HasPiercingShots();
    }
    
    #endregion
    
    #region Debug Controls
    [Header("Debug Controls")]
    public KeyCode toggleScreenShakeKey = KeyCode.F1;
    public KeyCode toggleCombosKey = KeyCode.F2;
    public KeyCode toggleOverchargeKey = KeyCode.F3;
    public KeyCode toggleTrailsKey = KeyCode.F4;
    public KeyCode toggleRicochetKey = KeyCode.F5;
    public KeyCode toggleBarrelsKey = KeyCode.F6;
    public KeyCode forceOverchargeKey = KeyCode.F7;
    
    void LateUpdate()
    {
        // Debug key controls (only for testing)
        if (Input.GetKeyDown(toggleScreenShakeKey))
            ToggleScreenShake();
        
        if (Input.GetKeyDown(toggleCombosKey))
            ToggleWeaponCombos();
        
        if (Input.GetKeyDown(toggleOverchargeKey))
            ToggleWeaponOvercharge();
        
        if (Input.GetKeyDown(toggleTrailsKey))
            ToggleBulletTrails();
        
        if (Input.GetKeyDown(toggleRicochetKey))
            ToggleRicochetBullets();
        
        if (Input.GetKeyDown(toggleBarrelsKey))
            ToggleExplosiveBarrels();
        
        if (Input.GetKeyDown(forceOverchargeKey) && overchargeSystem != null)
            overchargeSystem.ForceOvercharge();
    }
    #endregion
}