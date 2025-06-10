using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.UI;

public class WeaponOverchargeSystem : NetworkBehaviour
{
    [Header("Overcharge Settings")]
    public float maxOvercharge = 100f;
    public float overchargeDecayRate = 5f; // Points per second when not active
    public float damageToOverchargeRatio = 0.8f; // How much damage adds to overcharge
    public float killBonusOvercharge = 25f;
    
    [Header("UI References")]
    public Slider overchargeBar;
    public TextMeshProUGUI overchargeText;
    public GameObject ultimateReadyEffect;
    public Button ultimateButton;
    public Animator ultimateAnimator;
    
    [SyncVar(hook = nameof(OnOverchargeChanged))]
    public float currentOvercharge = 0f;
    
    [SyncVar(hook = nameof(OnUltimateReadyChanged))]
    public bool isUltimateReady = false;
    
    [SyncVar]
    public bool isUltimateActive = false;
    
    [SyncVar]
    public float ultimateDuration = 8f;
    
    private Coroutine ultimateCoroutine;
    private Coroutine decayCoroutine;
    
    // Ultimate effects per weapon
    private Dictionary<int, UltimateEffect> ultimateEffects = new Dictionary<int, UltimateEffect>()
    {
        // Elemental Staff - Elemental Mastery
        {4, new UltimateEffect("Elemental Mastery", 8f, "All elements fuse into devastating attacks!")},
        
        // Morph Cannon - Annihilation Mode
        {5, new UltimateEffect("Annihilation Mode", 6f, "All weapons fire simultaneously!")},
        
        // Spirit Bow - Hunter's Focus
        {6, new UltimateEffect("Hunter's Focus", 10f, "Perfect accuracy and piercing shots!")},
        
        // War Hammer - Berserker Fury
        {7, new UltimateEffect("Berserker Fury", 7f, "Unstoppable melee devastation!")},
        
        // Plasma Rifle - Energy Overflow
        {8, new UltimateEffect("Energy Overflow", 6f, "Infinite ammo and overcharged shots!")},
        
        // Ninja Kunai - Shadow Assassin
        {9, new UltimateEffect("Shadow Assassin", 8f, "Teleport and strike from the shadows!")},
        
        // Chaos Orb - Reality Distortion
        {10, new UltimateEffect("Reality Distortion", 10f, "Chaos reigns supreme!")},
        
        // Classic weapons
        {0, new UltimateEffect("Perfect Shot", 5f, "Every shot is a critical hit!")},
        {1, new UltimateEffect("Storm of Lead", 6f, "Unlimited pellets and range!")},
        {2, new UltimateEffect("Blade Dance", 7f, "Lightning-fast sword strikes!")},
        {3, new UltimateEffect("Suppressive Fire", 8f, "Infinite ammo and penetration!")}
    };
    
    [System.Serializable]
    public struct UltimateEffect
    {
        public string name;
        public float duration;
        public string description;
        
        public UltimateEffect(string n, float d, string desc)
        {
            name = n;
            duration = d;
            description = desc;
        }
    }

    void Start()
    {
        if (!isLocalPlayer)
        {
            if (overchargeBar != null) overchargeBar.gameObject.SetActive(false);
            if (overchargeText != null) overchargeText.gameObject.SetActive(false);
            if (ultimateButton != null) ultimateButton.gameObject.SetActive(false);
        }
        else
        {
            // Set up ultimate button
            if (ultimateButton != null)
            {
                ultimateButton.onClick.AddListener(TryActivateUltimate);
                ultimateButton.interactable = false;
            }
        }
        
        // Start decay coroutine
        if (isServer)
        {
            decayCoroutine = StartCoroutine(OverchargeDecayRoutine());
        }
    }

    [Server]
    public void AddOvercharge(float amount)
    {
        if (isUltimateActive) return; // No overcharge gain during ultimate
        
        currentOvercharge = Mathf.Min(currentOvercharge + amount, maxOvercharge);
        
        // Check if ultimate is ready
        if (currentOvercharge >= maxOvercharge && !isUltimateReady)
        {
            isUltimateReady = true;
            RpcShowUltimateReady();
        }
    }
    
    [Server]
    public void AddDamageOvercharge(float damage)
    {
        AddOvercharge(damage * damageToOverchargeRatio);
    }
    
    [Server]
    public void AddKillOvercharge()
    {
        AddOvercharge(killBonusOvercharge);
    }
    
    public void TryActivateUltimate()
    {
        if (!isLocalPlayer || !isUltimateReady) return;
        CmdActivateUltimate();
    }
    
    [Command]
    void CmdActivateUltimate()
    {
        if (!isUltimateReady) return;
        
        // Get current weapon
        var shooting = GetComponent<MouseShooting>();
        if (shooting == null || shooting.weaponNetId == 0) return;
        
        if (!NetworkServer.spawned.TryGetValue(shooting.weaponNetId, out var weaponNi)) return;
        var weapon = weaponNi.GetComponent<Weapon>();
        if (weapon == null) return;
        
        // Activate ultimate
        isUltimateReady = false;
        isUltimateActive = true;
        currentOvercharge = 0f;
        
        // Get ultimate effect for this weapon
        UltimateEffect effect = ultimateEffects.ContainsKey(weapon.id) 
            ? ultimateEffects[weapon.id] 
            : new UltimateEffect("Ultimate Power", 6f, "Overwhelming force!");
        
        ultimateDuration = effect.duration;
        
        // Start ultimate coroutine
        if (ultimateCoroutine != null)
        {
            StopCoroutine(ultimateCoroutine);
        }
        ultimateCoroutine = StartCoroutine(UltimateActiveRoutine(weapon.id, effect));
        
        // Notify clients
        RpcStartUltimate(weapon.id, effect.name, effect.description);
    }
    
    [Server]
    IEnumerator UltimateActiveRoutine(int weaponId, UltimateEffect effect)
    {
        float timeRemaining = effect.duration;
        
        while (timeRemaining > 0f)
        {
            yield return new WaitForSeconds(0.1f);
            timeRemaining -= 0.1f;
            
            // Apply weapon-specific ultimate effects
            ApplyUltimateEffects(weaponId);
        }
        
        // End ultimate
        isUltimateActive = false;
        RpcEndUltimate();
    }
    
    [Server]
    void ApplyUltimateEffects(int weaponId)
    {
        var shooting = GetComponent<MouseShooting>();
        if (shooting == null) return;
        
        switch (weaponId)
        {
            case 4: // Elemental Staff - cycle through all elements rapidly
                StartCoroutine(ElementalMasteryEffect());
                break;
                
            case 5: // Morph Cannon - all modes fire at once
                StartCoroutine(AnnihilationModeEffect());
                break;
                
            case 6: // Spirit Bow - guaranteed hits and piercing
                // Handled in shooting logic
                break;
                
            case 7: // War Hammer - rapid melee attacks
                StartCoroutine(BerserkerFuryEffect());
                break;
                
            case 8: // Plasma Rifle - infinite ammo mode
                shooting.currentAmmo = shooting.maxAmmo; // Infinite ammo
                break;
                
            case 9: // Ninja Kunai - teleport attacks
                StartCoroutine(ShadowAssassinEffect());
                break;
                
            case 10: // Chaos Orb - reality distortion
                StartCoroutine(RealityDistortionEffect());
                break;
        }
    }
    
    [Server]
    IEnumerator ElementalMasteryEffect()
    {
        var shooting = GetComponent<MouseShooting>();
        Vector3 forward = transform.up;
        
        // Fire all three elements in sequence
        for (int mode = 0; mode < 3; mode++)
        {
            int oldMode = shooting.swapModeNum;
            shooting.swapModeNum = mode;
            shooting.ShootElementalStaff(forward);
            shooting.swapModeNum = oldMode;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    [Server]
    IEnumerator AnnihilationModeEffect()
    {
        var shooting = GetComponent<MouseShooting>();
        Vector3 forward = transform.up;
        
        // Fire all cannon modes
        for (int mode = 0; mode < 3; mode++)
        {
            int oldMode = shooting.swapModeNum;
            shooting.swapModeNum = mode;
            shooting.ShootMorphCannon(forward);
            shooting.swapModeNum = oldMode;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    [Server]
    IEnumerator BerserkerFuryEffect()
    {
        var shooting = GetComponent<MouseShooting>();
        
        // Rapid slam attacks in all directions
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.up;
            
            int oldMode = shooting.swapModeNum;
            shooting.swapModeNum = 0; // Slam mode
            shooting.ShootWarHammer(direction);
            shooting.swapModeNum = oldMode;
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    [Server]
    IEnumerator ShadowAssassinEffect()
    {
        var shooting = GetComponent<MouseShooting>();
        
        // Teleport attacks to nearby enemies
        var enemies = FindObjectsOfType<Enemy>();
        foreach (var enemy in enemies)
        {
            if (enemy.gameObject == gameObject) continue; // Don't target self
            
            Vector3 direction = (enemy.transform.position - transform.position).normalized;
            
            int oldMode = shooting.swapModeNum;
            shooting.swapModeNum = 2; // Teleport mode
            shooting.ShootNinjaKunai(direction);
            shooting.swapModeNum = oldMode;
            
            yield return new WaitForSeconds(0.2f);
        }
    }
    
    [Server]
    IEnumerator RealityDistortionEffect()
    {
        var shooting = GetComponent<MouseShooting>();
        
        // Create chaos in all directions
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f;
            Vector3 direction = Quaternion.Euler(0, 0, angle) * Vector3.up;
            
            int randomMode = Random.Range(0, 3);
            int oldMode = shooting.swapModeNum;
            shooting.swapModeNum = randomMode;
            shooting.ShootChaosOrb(direction);
            shooting.swapModeNum = oldMode;
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    [Server]
    IEnumerator OverchargeDecayRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            
            if (!isUltimateActive && currentOvercharge > 0f)
            {
                currentOvercharge = Mathf.Max(0f, currentOvercharge - overchargeDecayRate);
                
                if (currentOvercharge < maxOvercharge && isUltimateReady)
                {
                    isUltimateReady = false;
                }
            }
        }
    }
    
    [ClientRpc]
    void RpcShowUltimateReady()
    {
        if (!isLocalPlayer) return;
        
        if (ultimateReadyEffect != null)
        {
            ultimateReadyEffect.SetActive(true);
        }
        
        if (ultimateAnimator != null)
        {
            ultimateAnimator.Play("UltimateReady");
        }
        
        // Play ready sound
        var audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            // Play ultimate ready sound
        }
    }
    
    [ClientRpc]
    void RpcStartUltimate(int weaponId, string ultimateName, string description)
    {
        if (!isLocalPlayer) return;
        
        StartCoroutine(ShowUltimateStartEffect(ultimateName, description));
        
        if (ultimateReadyEffect != null)
        {
            ultimateReadyEffect.SetActive(false);
        }
    }
    
    [ClientRpc]
    void RpcEndUltimate()
    {
        if (!isLocalPlayer) return;
        
        if (ultimateAnimator != null)
        {
            ultimateAnimator.Play("UltimateEnd");
        }
    }
    
    IEnumerator ShowUltimateStartEffect(string name, string description)
    {
        // Create ultimate start effect UI
        var ultimateUI = new GameObject("UltimateStartUI");
        ultimateUI.transform.SetParent(GameObject.Find("Canvas").transform, false);
        
        var text = ultimateUI.AddComponent<TextMeshProUGUI>();
        text.text = $"{name}\n{description}";
        text.fontSize = 36f;
        text.color = Color.cyan;
        text.alignment = TextAlignmentOptions.Center;
        
        var rect = ultimateUI.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(800, 200);
        
        // Animate
        for (float t = 0; t < 2f; t += Time.deltaTime)
        {
            float alpha = Mathf.Lerp(1f, 0f, t / 2f);
            text.color = new Color(text.color.r, text.color.g, text.color.b, alpha);
            
            float scale = Mathf.Lerp(1f, 1.2f, t / 2f);
            ultimateUI.transform.localScale = Vector3.one * scale;
            
            yield return null;
        }
        
        Destroy(ultimateUI);
    }
    
    void OnOverchargeChanged(float oldValue, float newValue)
    {
        if (!isLocalPlayer) return;
        
        if (overchargeBar != null)
        {
            overchargeBar.value = newValue / maxOvercharge;
        }
        
        if (overchargeText != null)
        {
            overchargeText.text = $"Overcharge: {newValue:F0}/{maxOvercharge:F0}";
        }
    }
    
    void OnUltimateReadyChanged(bool oldValue, bool newValue)
    {
        if (!isLocalPlayer) return;
        
        if (ultimateButton != null)
        {
            ultimateButton.interactable = newValue;
            ultimateButton.GetComponent<Image>().color = newValue ? Color.cyan : Color.gray;
        }
    }
    
    // Public method to check if ultimate provides special effects
    public bool HasUltimateEffect(string effectType)
    {
        if (!isUltimateActive) return false;
        
        var shooting = GetComponent<MouseShooting>();
        if (shooting == null || shooting.weaponNetId == 0) return false;
        
        if (!NetworkServer.spawned.TryGetValue(shooting.weaponNetId, out var weaponNi)) return false;
        var weapon = weaponNi.GetComponent<Weapon>();
        if (weapon == null) return false;
        
        // Check weapon-specific ultimate effects
        switch (weapon.id)
        {
            case 6 when effectType == "perfect_accuracy": return true;
            case 6 when effectType == "piercing": return true;
            case 8 when effectType == "infinite_ammo": return true;
            default: return false;
        }
    }
    
    public float GetUltimateDamageMultiplier()
    {
        return isUltimateActive ? 2.5f : 1f;
    }
}