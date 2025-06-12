using UnityEngine;
using System.Collections;
using Mirror;

public class WeaponOverchargeSystem : NetworkBehaviour
{
    [Header("Overcharge Settings")]
    public float overchargeCapacity = 100f;
    public float chargeRate = 10f; // Charge per second when shooting
    public float decayRate = 5f; // Decay per second when not shooting
    public float overchargeDuration = 8f;
    public float cooldownDuration = 12f;
    
    [Header("Overcharge Bonuses")]
    public float damageMultiplier = 2.5f;
    public float fireRateMultiplier = 1.8f;
    public float reloadSpeedMultiplier = 3f;
    public bool unlimitedAmmo = true;
    public bool piercingShots = true;
    public bool ricochetShots = true;
    
    [Header("Visual Effects")]
    public GameObject overchargeAura;
    public ParticleSystem overchargeParticles;
    public Color overchargeColor = Color.cyan;
    
    [SyncVar(hook = nameof(OnChargeChanged))]
    private float currentCharge = 0f;
    
    [SyncVar(hook = nameof(OnOverchargeStateChanged))]
    private bool isOvercharged = false;
    
    [SyncVar]
    private bool isOnCooldown = false;
    
    private float lastShotTime;
    private float overchargeStartTime;
    private Coroutine overchargeCoroutine;
    private Coroutine cooldownCoroutine;
    
    // Audio
    private AudioSource audioSource;
    public AudioClip overchargeActivateSound;
    public AudioClip overchargeLoopSound;
    public AudioClip overchargeEndSound;
    
    // Components
    private SpriteRenderer playerSprite;
    private Color originalColor;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        playerSprite = GetComponent<SpriteRenderer>();
        
        if (playerSprite != null)
        {
            originalColor = playerSprite.color;
        }
        
        // Set up overcharge aura
        if (overchargeAura != null)
        {
            overchargeAura.SetActive(false);
        }
    }
    
    void Update()
    {
        // Only run on server, but safely check if networking is available
        try
        {
            if (isServer == false) return;
        }
        catch
        {
            // If networking isn't available, continue anyway for single-player
        }
        
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
    
    [Server]
    public void OnWeaponFired()
    {
        lastShotTime = Time.time;
        
        // Build charge when shooting (but not during overcharge or cooldown)
        if (!isOvercharged && !isOnCooldown)
        {
            currentCharge = Mathf.Min(overchargeCapacity, currentCharge + chargeRate);
        }
    }
    
    [Server]
    void ActivateOvercharge()
    {
        isOvercharged = true;
        overchargeStartTime = Time.time;
        currentCharge = 0f; // Reset charge
        
        // Start overcharge coroutine
        if (overchargeCoroutine != null)
        {
            StopCoroutine(overchargeCoroutine);
        }
        overchargeCoroutine = StartCoroutine(OverchargeRoutine());
        
        // Show effects on all clients
        RpcActivateOvercharge();
    }
    
    [Server]
    IEnumerator OverchargeRoutine()
    {
        yield return new WaitForSeconds(overchargeDuration);
        
        // End overcharge
        isOvercharged = false;
        isOnCooldown = true;
        
        // Show end effects
        RpcDeactivateOvercharge();
        
        // Start cooldown
        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
        }
        cooldownCoroutine = StartCoroutine(CooldownRoutine());
    }
    
    [Server]
    IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(cooldownDuration);
        
        isOnCooldown = false;
        RpcCooldownComplete();
    }
    
    [ClientRpc]
    void RpcActivateOvercharge()
    {
        // Visual effects
        if (overchargeAura != null)
        {
            overchargeAura.SetActive(true);
        }
        
        if (overchargeParticles != null)
        {
            overchargeParticles.Play();
        }
        
        // Screen shake
        ScreenShake.Shake(0.8f, 0.2f);
        
        // Player color change
        if (playerSprite != null)
        {
            StartCoroutine(OverchargeColorEffect());
        }
        
        // Audio
        if (audioSource != null && overchargeActivateSound != null)
        {
            audioSource.PlayOneShot(overchargeActivateSound);
        }
        
        if (audioSource != null && overchargeLoopSound != null)
        {
            audioSource.clip = overchargeLoopSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }
    
    [ClientRpc]
    void RpcDeactivateOvercharge()
    {
        // Visual effects
        if (overchargeAura != null)
        {
            overchargeAura.SetActive(false);
        }
        
        if (overchargeParticles != null)
        {
            overchargeParticles.Stop();
        }
        
        // Reset player color
        if (playerSprite != null)
        {
            playerSprite.color = originalColor;
        }
        
        // Stop loop audio and play end sound
        if (audioSource != null)
        {
            audioSource.loop = false;
            audioSource.Stop();
            
            if (overchargeEndSound != null)
            {
                audioSource.PlayOneShot(overchargeEndSound);
            }
        }
    }
    
    [ClientRpc]
    void RpcCooldownComplete()
    {
        // Maybe add a subtle effect when cooldown completes
        ScreenShake.Shake(0.1f, 0.05f);
    }
    
    IEnumerator OverchargeColorEffect()
    {
        while (isOvercharged && playerSprite != null)
        {
            // Pulsing color effect
            float pulse = Mathf.Sin(Time.time * 8f) * 0.5f + 0.5f;
            Color currentColor = Color.Lerp(originalColor, overchargeColor, pulse * 0.6f);
            playerSprite.color = currentColor;
            
            yield return null;
        }
    }
    
    void OnChargeChanged(float oldCharge, float newCharge)
    {
        // Update UI charge bar if you have one
        // UpdateChargeUI(newCharge / overchargeCapacity);
    }
    
    void OnOverchargeStateChanged(bool oldState, bool newState)
    {
        // Handle overcharge state change
        if (newState)
        {
            Debug.Log("OVERCHARGE ACTIVATED! Weapon systems at maximum power!");
        }
        else
        {
            Debug.Log("Overcharge ended. Systems returning to normal.");
        }
    }
    
    // Public getters for other systems
    public bool IsOvercharged()
    {
        return isOvercharged;
    }
    
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }
    
    public float GetCharge()
    {
        return currentCharge;
    }
    
    public float GetChargePercentage()
    {
        return currentCharge / overchargeCapacity;
    }
    
    public float GetDamageMultiplier()
    {
        return isOvercharged ? damageMultiplier : 1f;
    }
    
    public float GetFireRateMultiplier()
    {
        return isOvercharged ? fireRateMultiplier : 1f;
    }
    
    public float GetReloadSpeedMultiplier()
    {
        return isOvercharged ? reloadSpeedMultiplier : 1f;
    }
    
    public bool HasUnlimitedAmmo()
    {
        return isOvercharged && unlimitedAmmo;
    }
    
    public bool HasPiercingShots()
    {
        return isOvercharged && piercingShots;
    }
    
    public bool HasRicochetShots()
    {
        return isOvercharged && ricochetShots;
    }
    
    // Method to manually trigger overcharge (for testing or special abilities)
    [Server]
    public void ForceOvercharge()
    {
        if (!isOnCooldown)
        {
            currentCharge = overchargeCapacity;
            ActivateOvercharge();
        }
    }
    
    // Method to add charge (for pickups or special events)
    [Server]
    public void AddCharge(float amount)
    {
        if (!isOvercharged && !isOnCooldown)
        {
            currentCharge = Mathf.Min(overchargeCapacity, currentCharge + amount);
        }
    }
}