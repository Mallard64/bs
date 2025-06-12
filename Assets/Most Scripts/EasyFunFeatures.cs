using UnityEngine;
using Mirror;

// Just drag this script onto your player GameObjects and it automatically adds all the fun features!
public class EasyFunFeatures : NetworkBehaviour
{
    [Header("🎮 One-Click Fun Features Setup!")]
    [Tooltip("Check this to automatically add all fun features to this player")]
    public bool autoSetupFeatures = true;
    
    [Header("✨ Individual Feature Toggles")]
    public bool addScreenShake = true;
    public bool addWeaponCombos = true;
    public bool addWeaponOvercharge = true;
    public bool addBulletTrails = true;
    public bool addRicochetChance = false; // Off by default - can be chaotic!
    
    [Header("🎯 Integration Settings")]
    [Tooltip("The main camera for screen shake (auto-found if null)")]
    public Camera playerCamera;
    
    [Tooltip("Chance for bullets to ricochet (0.0 = never, 1.0 = always)")]
    [Range(0f, 1f)]
    public float ricochetChance = 0.15f;
    
    // Added components
    private FunFeaturesManager featuresManager;
    private SimpleComboSystem comboSystem;
    private SimpleOverchargeSystem overchargeSystem;
    private ScreenShake screenShake;
    
    void Start()
    {
        if (autoSetupFeatures)
        {
            SetupAllFeatures();
        }
    }
    
    [ContextMenu("Setup All Fun Features")]
    public void SetupAllFeatures()
    {
        Debug.Log($"🎮 Setting up fun features for {gameObject.name}...");
        
        // Add main features manager
        if (featuresManager == null)
        {
            featuresManager = gameObject.AddComponent<FunFeaturesManager>();
            featuresManager.enableScreenShake = addScreenShake;
            featuresManager.enableWeaponCombos = addWeaponCombos;
            featuresManager.enableWeaponOvercharge = addWeaponOvercharge;
            featuresManager.enableBulletTrails = addBulletTrails;
            featuresManager.enableRicochetBullets = addRicochetChance;
            featuresManager.ricochetChance = ricochetChance;
            
            Debug.Log("✅ FunFeaturesManager added!");
        }
        
        // Setup Screen Shake
        if (addScreenShake)
        {
            SetupScreenShake();
        }
        
        // Setup Weapon Combos
        if (addWeaponCombos)
        {
            SetupWeaponCombos();
        }
        
        // Setup Weapon Overcharge
        if (addWeaponOvercharge)
        {
            SetupWeaponOvercharge();
        }
        
        Debug.Log("🎉 All fun features setup complete!");
    }
    
    void SetupScreenShake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        if (playerCamera != null && screenShake == null)
        {
            screenShake = playerCamera.gameObject.AddComponent<ScreenShake>();
            Debug.Log("📳 Screen Shake added to camera!");
        }
    }
    
    void SetupWeaponCombos()
    {
        if (comboSystem == null)
        {
            comboSystem = gameObject.AddComponent<SimpleComboSystem>();
            Debug.Log("Weapon Combo System added!");
        }
    }
    
    void SetupWeaponOvercharge()
    {
        if (overchargeSystem == null)
        {
            overchargeSystem = gameObject.AddComponent<SimpleOverchargeSystem>();
            Debug.Log("Weapon Overcharge System added!");
        }
    }
    
    // Call this from your shooting script to integrate with the systems
    public void OnWeaponFired(int weaponId, int mode = 0, Vector3 direction = default)
    {
        // Register with combo system
        if (comboSystem != null)
        {
            comboSystem.RegisterWeaponFire(weaponId);
        }
        
        // Register with overcharge system
        if (overchargeSystem != null)
        {
            overchargeSystem.OnWeaponFired();
        }
        
        // Add trails and ricochet to bullets if enabled
        if (addBulletTrails || addRicochetChance)
        {
            EnhanceNewBullets();
        }
    }
    
    // Call this to enhance bullets with fun effects
    public void EnhanceBullet(GameObject bullet)
    {
        if (bullet == null) return;
        
        // Add trail system
        if (addBulletTrails && bullet.GetComponent<BulletTrailSystem>() == null)
        {
            BulletTrailSystem trailSystem = bullet.AddComponent<BulletTrailSystem>();
            trailSystem.enableTrail = true;
            trailSystem.enableParticles = true;
        }
        
        // Add ricochet chance
        if (addRicochetChance && bullet.GetComponent<RicochetBullet>() == null)
        {
            if (Random.Range(0f, 1f) < ricochetChance)
            {
                RicochetBullet ricochet = bullet.AddComponent<RicochetBullet>();
                ricochet.maxBounces = 2;
                ricochet.speedMultiplierPerBounce = 0.85f;
            }
        }
    }
    
    void EnhanceNewBullets()
    {
        // Find bullets near this player and enhance them
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, 15f);
        
        foreach (Collider2D obj in nearbyObjects)
        {
            if (obj.GetComponent<Bullet>() != null)
            {
                EnhanceBullet(obj.gameObject);
            }
        }
    }
    
    // Get current bonuses for your weapon damage calculations
    public float GetTotalDamageMultiplier()
    {
        float comboMultiplier = comboSystem != null ? comboSystem.GetDamageMultiplier() : 1f;
        float overchargeMultiplier = overchargeSystem != null ? overchargeSystem.GetDamageMultiplier() : 1f;
        
        return comboMultiplier * overchargeMultiplier;
    }
    
    // Check if player has special bonuses
    public bool HasUnlimitedAmmo()
    {
        return overchargeSystem != null && overchargeSystem.HasUnlimitedAmmo();
    }
    
    public bool HasPiercingShots()
    {
        return overchargeSystem != null && overchargeSystem.HasPiercingShots();
    }
    
    public bool HasRicochetShots()
    {
        return overchargeSystem != null && overchargeSystem.HasRicochetShots();
    }
    
    public bool IsOvercharged()
    {
        return overchargeSystem != null && overchargeSystem.IsOvercharged();
    }
    
    // Easy access methods for other scripts
    public void TriggerScreenShake(float intensity = 0.2f, float duration = 0.3f)
    {
        if (screenShake != null)
        {
            ScreenShake.Shake(duration, intensity);
        }
    }
    
    public void ForceOvercharge()
    {
        if (overchargeSystem != null)
        {
            overchargeSystem.ForceOvercharge();
        }
    }
    
    // Debug info
    void OnGUI()
    {
        if (!isLocalPlayer) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("🎮 Fun Features Status:");
        
        if (comboSystem != null)
        {
            GUILayout.Label($"Combo: {comboSystem.GetComboLevel()} (x{comboSystem.GetDamageMultiplier():F1})");
        }
        
        if (overchargeSystem != null)
        {
            GUILayout.Label($"⚡ Charge: {overchargeSystem.GetChargePercentage()*100:F0}%");
            if (overchargeSystem.IsOvercharged())
            {
                GUILayout.Label("🌟 OVERCHARGED!");
            }
            if (overchargeSystem.IsOnCooldown())
            {
                GUILayout.Label("❄️ Cooling down...");
            }
        }
        
        GUILayout.Label($"💥 Damage Multiplier: x{GetTotalDamageMultiplier():F1}");
        
        GUILayout.EndArea();
    }
}