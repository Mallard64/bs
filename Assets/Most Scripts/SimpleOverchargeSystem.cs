using UnityEngine;
using System.Collections;

// Simplified overcharge system that doesn't require Mirror networking
public class SimpleOverchargeSystem : MonoBehaviour
{
    [Header("Overcharge Settings")]
    public float overchargeCapacity = 100f;
    public float chargeRate = 10f;
    public float decayRate = 5f;
    public float overchargeDuration = 8f;
    public float cooldownDuration = 12f;
    
    [Header("Overcharge Bonuses")]
    public float damageMultiplier = 2.5f;
    public float fireRateMultiplier = 1.8f;
    public bool unlimitedAmmo = true;
    public bool piercingShots = true;
    
    private float currentCharge = 0f;
    private bool isOvercharged = false;
    private bool isOnCooldown = false;
    private float lastShotTime = 0f;
    
    void Update()
    {
        // Natural charge decay when not shooting
        if (Time.time - lastShotTime > 1f && currentCharge > 0 && !isOvercharged)
        {
            currentCharge = Mathf.Max(0, currentCharge - decayRate * Time.deltaTime);
        }
        
        // Check if we should activate overcharge
        if (currentCharge >= overchargeCapacity && !isOvercharged && !isOnCooldown)
        {
            ActivateOvercharge();
        }
    }
    
    public void OnWeaponFired()
    {
        lastShotTime = Time.time;
        
        // Build charge when shooting (but not during overcharge or cooldown)
        if (!isOvercharged && !isOnCooldown)
        {
            currentCharge = Mathf.Min(overchargeCapacity, currentCharge + chargeRate);
        }
    }
    
    void ActivateOvercharge()
    {
        isOvercharged = true;
        currentCharge = 0f;
        
        Debug.Log("OVERCHARGE ACTIVATED! Weapon systems at maximum power!");
        
        // Screen shake
        if (ScreenShake.Instance != null)
        {
            ScreenShake.Shake(0.8f, 0.2f);
        }
        
        // Start overcharge duration
        Invoke(nameof(EndOvercharge), overchargeDuration);
    }
    
    void EndOvercharge()
    {
        isOvercharged = false;
        isOnCooldown = true;
        
        Debug.Log("Overcharge ended. Systems returning to normal.");
        
        // Start cooldown
        Invoke(nameof(EndCooldown), cooldownDuration);
    }
    
    void EndCooldown()
    {
        isOnCooldown = false;
        Debug.Log("Overcharge ready!");
    }
    
    // Public getters
    public bool IsOvercharged() => isOvercharged;
    public bool IsOnCooldown() => isOnCooldown;
    public float GetCharge() => currentCharge;
    public float GetChargePercentage() => currentCharge / overchargeCapacity;
    public float GetDamageMultiplier() => isOvercharged ? damageMultiplier : 1f;
    public float GetFireRateMultiplier() => isOvercharged ? fireRateMultiplier : 1f;
    public bool HasUnlimitedAmmo() => isOvercharged && unlimitedAmmo;
    public bool HasPiercingShots() => isOvercharged && piercingShots;
    public bool HasRicochetShots() => isOvercharged;
    
    public void ForceOvercharge()
    {
        if (!isOnCooldown)
        {
            currentCharge = overchargeCapacity;
            ActivateOvercharge();
        }
    }
    
    public void AddCharge(float amount)
    {
        if (!isOvercharged && !isOnCooldown)
        {
            currentCharge = Mathf.Min(overchargeCapacity, currentCharge + amount);
        }
    }
}