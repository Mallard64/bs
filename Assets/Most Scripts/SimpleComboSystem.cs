using UnityEngine;
using System.Collections.Generic;
using System.Collections;

// Simplified combo system that doesn't require Mirror networking
public class SimpleComboSystem : MonoBehaviour
{
    [Header("Combo Settings")]
    public float comboWindow = 3f;
    public int maxComboLevel = 5;
    public float damageMultiplierPerLevel = 0.15f;
    
    private int currentCombo = 0;
    private float lastShotTime = 0f;
    private List<int> recentWeapons = new List<int>();
    private int lastWeaponUsed = -1;
    
    void Update()
    {
        // Decay combo over time
        if (Time.time - lastShotTime > comboWindow && currentCombo > 0)
        {
            ResetCombo();
        }
    }
    
    public void RegisterWeaponFire(int weaponId)
    {
        lastShotTime = Time.time;
        
        // Don't count same weapon repeatedly
        if (weaponId == lastWeaponUsed)
            return;
            
        // Add to recent weapons
        recentWeapons.Add(weaponId);
        lastWeaponUsed = weaponId;
        
        // Keep only recent weapons (last 3)
        if (recentWeapons.Count > 3)
        {
            recentWeapons.RemoveAt(0);
        }
        
        // Calculate combo level
        HashSet<int> uniqueWeapons = new HashSet<int>(recentWeapons);
        currentCombo = Mathf.Min(uniqueWeapons.Count, maxComboLevel);
        
        Debug.Log($"Combo Level: {currentCombo} - Damage x{GetDamageMultiplier():F1}");
    }
    
    void ResetCombo()
    {
        currentCombo = 0;
        recentWeapons.Clear();
        lastWeaponUsed = -1;
    }
    
    public float GetDamageMultiplier()
    {
        return 1f + (currentCombo * damageMultiplierPerLevel);
    }
    
    public int GetComboLevel()
    {
        return currentCombo;
    }
}