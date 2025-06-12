using UnityEngine;
using Mirror;

// Auto-adds fun features to any MouseShooting component
[RequireComponent(typeof(MouseShooting))]
public class AutoFunFeatures : NetworkBehaviour
{
    [Header("🎮 Auto Fun Features")]
    public bool enableScreenShake = true;
    public bool enableBulletTrails = true;
    public bool enableRicochet = true;
    public bool enableCombos = true;
    public bool enableOvercharge = true;
    
    [Range(0f, 1f)]
    public float ricochetChance = 0.15f;
    
    private SimpleComboSystem comboSystem;
    private SimpleOverchargeSystem overchargeSystem;
    private MouseShooting shooter;
    
    void Start()
    {
        shooter = GetComponent<MouseShooting>();
        
        if (enableCombos)
        {
            comboSystem = gameObject.AddComponent<SimpleComboSystem>();
            Debug.Log("✅ Auto Fun Features: Combo system added");
        }
        
        if (enableOvercharge)
        {
            overchargeSystem = gameObject.AddComponent<SimpleOverchargeSystem>();
            Debug.Log("✅ Auto Fun Features: Overcharge system added");
        }
        
        // Setup screen shake
        if (enableScreenShake && Camera.main != null)
        {
            if (Camera.main.GetComponent<ScreenShake>() == null)
            {
                Camera.main.gameObject.AddComponent<ScreenShake>();
                Debug.Log("✅ Auto Fun Features: Screen shake added to main camera");
            }
        }
        
        Debug.Log("🎮 Auto Fun Features initialized successfully!");
    }
    
    void Update()
    {
        // Test features with keyboard shortcuts (only for local player)
        if (!isLocalPlayer) return;
        
        if (Input.GetKeyDown(KeyCode.F1))
        {
            ScreenShake.Shake(0.5f, 0.3f);
            Debug.Log("🧪 Screen shake test");
        }
        
        if (Input.GetKeyDown(KeyCode.F2) && overchargeSystem != null)
        {
            overchargeSystem.ForceOvercharge();
            Debug.Log("🧪 Force overcharge test");
        }
    }
    
    // Call this from MouseShooting when weapon fires
    public void OnWeaponFired(int weaponId, int mode = 0)
    {
        Debug.Log($"🎮 AutoFunFeatures: Weapon fired! ID={weaponId}, Mode={mode}");
        
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
                default:
                    ScreenShake.Shake(0.2f, 0.1f);
                    break;
            }
        }
    }
    
    // Call this to enhance bullets
    public void EnhanceBullet(GameObject bullet)
    {
        if (bullet == null) return;
        
        Debug.Log($"🎮 AutoFunFeatures: Enhancing bullet {bullet.name}");
        
        // Add trail effect
        if (enableBulletTrails && bullet.GetComponent<BulletTrailSystem>() == null)
        {
            var trailSystem = bullet.AddComponent<BulletTrailSystem>();
            trailSystem.enableTrail = true;
            trailSystem.enableParticles = true;
        }
        
        // Add ricochet chance
        if (enableRicochet && bullet.GetComponent<RicochetBullet>() == null)
        {
            if (Random.Range(0f, 1f) < ricochetChance)
            {
                var ricochet = bullet.AddComponent<RicochetBullet>();
                ricochet.maxBounces = 2;
                ricochet.speedMultiplierPerBounce = 0.85f;
                Debug.Log("🎮 Added ricochet to bullet!");
            }
        }
    }
    
    // Get current damage multipliers
    public float GetDamageMultiplier()
    {
        float comboMult = comboSystem != null ? comboSystem.GetDamageMultiplier() : 1f;
        float overchargeMult = overchargeSystem != null ? overchargeSystem.GetDamageMultiplier() : 1f;
        return comboMult * overchargeMult;
    }
    
    public bool IsOvercharged()
    {
        return overchargeSystem != null && overchargeSystem.IsOvercharged();
    }
    
    void OnGUI()
    {
        if (!isLocalPlayer) return;
        
        GUILayout.BeginArea(new Rect(10, 300, 300, 150));
        GUILayout.Label("🎮 Auto Fun Features Status:");
        
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
        
        GUILayout.Label($"💥 Total Damage: x{GetDamageMultiplier():F1}");
        GUILayout.Label("F1=Test Shake, F2=Force Overcharge");
        
        GUILayout.EndArea();
    }
}