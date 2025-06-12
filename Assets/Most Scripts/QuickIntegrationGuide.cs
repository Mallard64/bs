using UnityEngine;

// QUICK INTEGRATION GUIDE - Fun Features
// =======================================
// 
// SUPER EASY METHOD (3 steps):
// 1. Drag "EasyFunFeatures" script onto your Player prefab
// 2. Check "Auto Setup Features" in the inspector  
// 3. Done! All features will auto-activate when the game starts
//
// MANUAL INTEGRATION:
// If you want more control, here's how to integrate each feature:
//
// IN YOUR SHOOTING SCRIPT:
// Add this line where you shoot bullets:
// GetComponent<EasyFunFeatures>()?.OnWeaponFired(weaponId, mode, direction);
// GetComponent<EasyFunFeatures>()?.EnhanceBullet(bulletGameObject);
//
// TO USE DAMAGE MULTIPLIERS:
// float baseDamage = 25f;
// float multiplier = GetComponent<EasyFunFeatures>()?.GetTotalDamageMultiplier() ?? 1f;
// float finalDamage = baseDamage * multiplier;
//
// TO CHECK OVERCHARGE BONUSES:
// EasyFunFeatures features = GetComponent<EasyFunFeatures>();
// if (features != null && features.HasUnlimitedAmmo()) { /* Don't consume ammo */ }
//
// TO TRIGGER SCREEN SHAKE:
// GetComponent<EasyFunFeatures>()?.TriggerScreenShake(0.5f, 0.8f);
// ScreenShake.ShakeExplosion(0.3f);  // Explosion shake
// ScreenShake.ShakeLightning(0.4f);  // Lightning shake
// ScreenShake.ShakeGunshot(0.1f);    // Gunshot shake
//
// KEYBOARD SHORTCUTS (for testing):
// F1 - Toggle Screen Shake
// F2 - Toggle Weapon Combos
// F3 - Toggle Weapon Overcharge  
// F4 - Toggle Bullet Trails
// F5 - Toggle Ricochet Bullets
// F6 - Toggle Explosive Barrels
// F7 - Force Overcharge

public class QuickIntegrationGuide : MonoBehaviour
{
    [Header("Integration Guide")]
    [TextArea(5, 10)]
    public string guide = "See the comments at the top of this script file for detailed integration instructions!";
    
    [Header("Quick Setup")]
    [Tooltip("Drag your player prefab here to auto-setup")]
    public GameObject playerPrefab;
    
    [ContextMenu("Auto-Setup Player Prefab")]
    void AutoSetupPlayerPrefab()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Please assign a player prefab first!");
            return;
        }
        
        // Add EasyFunFeatures if not present
        EasyFunFeatures features = playerPrefab.GetComponent<EasyFunFeatures>();
        if (features == null)
        {
            features = playerPrefab.AddComponent<EasyFunFeatures>();
            Debug.Log("Added EasyFunFeatures to player prefab!");
        }
        
        // Configure for best experience
        features.autoSetupFeatures = true;
        features.addScreenShake = true;
        features.addWeaponCombos = true;
        features.addWeaponOvercharge = true;
        features.addBulletTrails = true;
        features.addRicochetChance = false; // Start conservative
        features.ricochetChance = 0.1f;
        
        Debug.Log("Player prefab configured with fun features!");
        Debug.Log("Remember to call OnWeaponFired() and EnhanceBullet() in your shooting script!");
    }
    
    [ContextMenu("Create Test Explosive Barrel")]
    void CreateTestBarrel()
    {
        Vector3 spawnPos = transform.position + Vector3.right * 3f;
        GameObject barrel = ExplosiveBarrel.CreateExplosiveBarrel(spawnPos);
        Debug.Log("Created test explosive barrel at " + spawnPos);
    }
}