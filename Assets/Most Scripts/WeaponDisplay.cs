using System.Collections;
using UnityEngine;
using TMPro;

public class WeaponDisplay : MonoBehaviour
{
    [Header("Display Settings")]
    public GameObject[] weaponPrefabs;
    public Transform displayPoint;
    public float rotationSpeed = 45f;
    public float switchInterval = 5f;
    
    [Header("UI")]
    public TextMeshProUGUI weaponNameText;
    public TextMeshProUGUI weaponDescriptionText;
    public Canvas weaponInfoUI;
    
    [Header("Effects")]
    public ParticleSystem displayEffects;
    public Light displayLight;
    public AudioClip switchSound;
    
    private GameObject currentWeaponDisplay;
    private int currentWeaponIndex = 0;
    private AudioSource audioSource;
    private bool isRotating = false;
    
    // Weapon information
    private string[] weaponNames = {
        "Sniper Rifle", "Shotgun", "Sword", "Assault Rifle", "Elemental Staff",
        "Morph Cannon", "Spirit Bow", "War Hammer", "Plasma Rifle", "Ninja Kunai",
        "Chaos Orb", "Lightning Gun", "Rocket Launcher", "Flame Thrower", "Ice Beam",
        "Boomerang", "Laser Cannon", "Gravity Gun", "Venom Spitter"
    };
    
    private string[] weaponDescriptions = {
        "High damage, long range precision weapon",
        "Close range spread damage dealer", 
        "Versatile melee weapon with multiple modes",
        "Balanced automatic weapon for sustained fire",
        "Magical staff with fire, ice, and lightning modes",
        "Transforming weapon with rocket, beam, and grenade modes",
        "Mystical bow with piercing, explosive, and homing arrows",
        "Heavy weapon with slam, throw, and spin attacks",
        "Energy weapon with burst, charge, and overload modes",
        "Stealth weapon with shadow, poison, and teleport abilities",
        "Unpredictable orb with random, portal, and gravity effects",
        "Instant lightning that chains between enemies",
        "Explosive projectile with area damage",
        "Continuous fire stream with burn effects",
        "Freezing beam that slows and immobilizes",
        "Returning projectile that comes back to you",
        "Piercing laser that goes through all enemies",
        "Creates gravity wells that pull enemies in",
        "Spreads poison in a wide arc"
    };
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        StartRotation();
        StartCoroutine(WeaponSwitchRoutine());
    }
    
    public void StartRotation()
    {
        if (!isRotating)
        {
            isRotating = true;
            DisplayWeapon(currentWeaponIndex);
            StartCoroutine(RotationRoutine());
        }
    }
    
    public void StopRotation()
    {
        isRotating = false;
    }
    
    IEnumerator RotationRoutine()
    {
        while (isRotating && currentWeaponDisplay != null)
        {
            currentWeaponDisplay.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
            
            // Add floating motion
            float bobOffset = Mathf.Sin(Time.time * 2f) * 0.1f;
            Vector3 basePos = displayPoint.position;
            currentWeaponDisplay.transform.position = new Vector3(basePos.x, basePos.y + bobOffset, basePos.z);
            
            yield return null;
        }
    }
    
    IEnumerator WeaponSwitchRoutine()
    {
        while (gameObject != null)
        {
            yield return new WaitForSeconds(switchInterval);
            SwitchToNextWeapon();
        }
    }
    
    void SwitchToNextWeapon()
    {
        currentWeaponIndex = (currentWeaponIndex + 1) % weaponPrefabs.Length;
        DisplayWeapon(currentWeaponIndex);
        PlaySwitchEffect();
    }
    
    void DisplayWeapon(int weaponIndex)
    {
        // Destroy current display
        if (currentWeaponDisplay != null)
        {
            StartCoroutine(FadeOutWeapon(currentWeaponDisplay));
        }
        
        // Create new weapon display
        if (weaponIndex < weaponPrefabs.Length && weaponPrefabs[weaponIndex] != null)
        {
            currentWeaponDisplay = Instantiate(weaponPrefabs[weaponIndex], displayPoint.position, Quaternion.identity);
            
            // Remove any network components from display
            var networkComponents = currentWeaponDisplay.GetComponents<MonoBehaviour>();
            foreach (var component in networkComponents)
            {
                if (component.GetType().Name.Contains("Network"))
                {
                    Destroy(component);
                }
            }
            
            // Scale for display
            currentWeaponDisplay.transform.localScale = Vector3.one * 1.5f;
            
            // Add glow effect
            AddGlowEffect(currentWeaponDisplay);
            
            // Update UI
            UpdateWeaponInfo(weaponIndex);
            
            StartCoroutine(FadeInWeapon(currentWeaponDisplay));
        }
    }
    
    void AddGlowEffect(GameObject weapon)
    {
        var spriteRenderer = weapon.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            // Add outline/glow shader effect
            var glowObject = new GameObject("Glow");
            glowObject.transform.SetParent(weapon.transform);
            glowObject.transform.localPosition = Vector3.zero;
            glowObject.transform.localScale = Vector3.one * 1.1f;
            
            var glowRenderer = glowObject.AddComponent<SpriteRenderer>();
            glowRenderer.sprite = spriteRenderer.sprite;
            glowRenderer.color = new Color(1f, 1f, 1f, 0.3f);
            glowRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
            
            // Animate glow
            StartCoroutine(AnimateGlow(glowRenderer));
        }
    }
    
    IEnumerator AnimateGlow(SpriteRenderer glowRenderer)
    {
        while (glowRenderer != null)
        {
            float alpha = Mathf.PingPong(Time.time * 2f, 0.5f) + 0.2f;
            Color color = glowRenderer.color;
            color.a = alpha;
            glowRenderer.color = color;
            yield return null;
        }
    }
    
    IEnumerator FadeInWeapon(GameObject weapon)
    {
        var renderers = weapon.GetComponentsInChildren<SpriteRenderer>();
        
        float timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, timer / 0.5f);
            
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    Color color = renderer.color;
                    color.a = alpha;
                    renderer.color = color;
                }
            }
            
            yield return null;
        }
    }
    
    IEnumerator FadeOutWeapon(GameObject weapon)
    {
        var renderers = weapon.GetComponentsInChildren<SpriteRenderer>();
        
        float timer = 0f;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / 0.3f);
            
            foreach (var renderer in renderers)
            {
                if (renderer != null)
                {
                    Color color = renderer.color;
                    color.a = alpha;
                    renderer.color = color;
                }
            }
            
            yield return null;
        }
        
        Destroy(weapon);
    }
    
    void UpdateWeaponInfo(int weaponIndex)
    {
        if (weaponNameText != null && weaponIndex < weaponNames.Length)
        {
            weaponNameText.text = weaponNames[weaponIndex];
        }
        
        if (weaponDescriptionText != null && weaponIndex < weaponDescriptions.Length)
        {
            weaponDescriptionText.text = weaponDescriptions[weaponIndex];
        }
    }
    
    void PlaySwitchEffect()
    {
        // Play sound
        if (switchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(switchSound);
        }
        
        // Play particles
        if (displayEffects != null)
        {
            displayEffects.Play();
        }
        
        // Flash light
        if (displayLight != null)
        {
            StartCoroutine(FlashLight());
        }
    }
    
    IEnumerator FlashLight()
    {
        float originalIntensity = displayLight.intensity;
        
        // Flash bright
        displayLight.intensity = originalIntensity * 2f;
        yield return new WaitForSeconds(0.1f);
        
        // Fade back
        float timer = 0f;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            displayLight.intensity = Mathf.Lerp(originalIntensity * 2f, originalIntensity, timer / 0.5f);
            yield return null;
        }
        
        displayLight.intensity = originalIntensity;
    }
    
    public void OnWeaponClicked()
    {
        // Show detailed weapon stats or allow preview
        Debug.Log($"🗡️ Clicked on {weaponNames[currentWeaponIndex]}");
        
        // Could open a weapon details panel
        // WeaponDetailsPanel.Instance.ShowWeapon(currentWeaponIndex);
    }
}