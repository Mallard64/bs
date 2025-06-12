using UnityEngine;
using System.Collections;
using Mirror;

public class BulletTrailSystem : NetworkBehaviour
{
    [Header("Trail Settings")]
    public bool enableTrail = true;
    public float trailTime = 0.5f;
    public float trailWidth = 0.1f;
    public Material trailMaterial;
    public Color trailColor = Color.white;
    public AnimationCurve trailWidthCurve = AnimationCurve.Linear(0, 1, 1, 0);
    
    [Header("Particle Effects")]
    public bool enableParticles = true;
    public GameObject particleTrailPrefab;
    public int particleCount = 10;
    public float particleLifetime = 1f;
    public float particleSize = 0.1f;
    
    [Header("Weapon-Specific Colors")]
    public Color fireTrailColor = Color.red;
    public Color iceTrailColor = Color.cyan;
    public Color lightningTrailColor = Color.yellow;
    public Color poisonTrailColor = Color.green;
    public Color explosiveTrailColor = new Color(1f, 0.5f, 0f); // Orange color
    
    private TrailRenderer bulletTrail;
    private ParticleSystem trailParticles;
    private Bullet bulletComponent;
    
    void Start()
    {
        bulletComponent = GetComponent<Bullet>();
        
        if (enableTrail)
        {
            SetupTrailRenderer();
        }
        
        if (enableParticles)
        {
            SetupParticleSystem();
        }
        
        // Configure trail based on bullet type
        ConfigureTrailForBulletType();
    }
    
    void SetupTrailRenderer()
    {
        bulletTrail = gameObject.GetComponent<TrailRenderer>();
        
        if (bulletTrail == null)
        {
            bulletTrail = gameObject.AddComponent<TrailRenderer>();
        }
        
        // Configure trail
        bulletTrail.time = trailTime;
        bulletTrail.startWidth = trailWidth;
        bulletTrail.endWidth = 0f;
        bulletTrail.widthCurve = trailWidthCurve;
        bulletTrail.startColor = trailColor;
        bulletTrail.endColor = trailColor;
        bulletTrail.numCapVertices = 10;
        bulletTrail.numCornerVertices = 10;
        
        // Set material
        if (trailMaterial != null)
        {
            bulletTrail.material = trailMaterial;
        }
        else
        {
            // Create default additive material
            bulletTrail.material = CreateDefaultTrailMaterial();
        }
    }
    
    void SetupParticleSystem()
    {
        if (particleTrailPrefab != null)
        {
            GameObject particleObj = Instantiate(particleTrailPrefab, transform);
            trailParticles = particleObj.GetComponent<ParticleSystem>();
        }
        
        if (trailParticles == null)
        {
            // Create particle system
            GameObject particleObj = new GameObject("BulletTrailParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.zero;
            trailParticles = particleObj.AddComponent<ParticleSystem>();
            
            ConfigureParticleSystem();
        }
    }
    
    void ConfigureParticleSystem()
    {
        if (trailParticles == null) return;
        
        var main = trailParticles.main;
        main.startLifetime = particleLifetime;
        main.startSpeed = 0.5f;
        main.startSize = particleSize;
        main.startColor = trailColor;
        main.maxParticles = particleCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        
        var emission = trailParticles.emission;
        emission.rateOverTime = particleCount / particleLifetime;
        
        var shape = trailParticles.shape;
        shape.enabled = false;
        
        var velocityOverLifetime = trailParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(-1f, 1f);
        
        var sizeOverLifetime = trailParticles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0f);
        
        var colorOverLifetime = trailParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;
    }
    
    void ConfigureTrailForBulletType()
    {
        if (bulletComponent == null) return;
        
        Color selectedColor = trailColor;
        float selectedWidth = trailWidth;
        float selectedTime = trailTime;
        
        // Configure based on bullet effect type
        switch (bulletComponent.effectType)
        {
            case BulletEffectType.BurnDamage:
                selectedColor = fireTrailColor;
                selectedWidth *= 1.2f;
                break;
                
            case BulletEffectType.SlowEffect:
                selectedColor = iceTrailColor;
                selectedTime *= 1.5f;
                break;
                
            case BulletEffectType.LightningBolt:
                selectedColor = lightningTrailColor;
                selectedWidth *= 1.5f;
                selectedTime *= 0.8f;
                StartCoroutine(LightningTrailEffect());
                break;
                
            case BulletEffectType.ExplosionDamage:
                selectedColor = explosiveTrailColor;
                selectedWidth *= 1.3f;
                break;
                
            default:
                // Use default trail for unknown effect types
                break;
        }
        
        // Apply configuration
        ApplyTrailConfiguration(selectedColor, selectedWidth, selectedTime);
    }
    
    void ConfigureTrailForWeaponId(int weaponId)
    {
        Color selectedColor = trailColor;
        float selectedWidth = trailWidth;
        
        switch (weaponId)
        {
            case 4: // Elemental Staff
                selectedColor = Color.magenta;
                selectedWidth *= 1.2f;
                break;
                
            case 6: // Spirit Bow
                selectedColor = Color.green;
                selectedWidth *= 0.8f;
                break;
                
            case 7: // War Hammer
                selectedColor = Color.red;
                selectedWidth *= 1.5f;
                break;
                
            case 9: // Ninja Kunai
                selectedColor = Color.gray;
                selectedWidth *= 0.6f;
                break;
                
            case 11: // Lightning Gun
                selectedColor = lightningTrailColor;
                selectedWidth *= 1.8f;
                StartCoroutine(LightningTrailEffect());
                break;
        }
        
        ApplyTrailConfiguration(selectedColor, selectedWidth, trailTime);
    }
    
    void ApplyTrailConfiguration(Color color, float width, float time)
    {
        trailColor = color;
        
        if (bulletTrail != null)
        {
            bulletTrail.startWidth = width;
            bulletTrail.time = time;
            bulletTrail.startColor = color;
            bulletTrail.endColor = color;
        }
        
        if (trailParticles != null)
        {
            var main = trailParticles.main;
            main.startColor = color;
        }
    }
    
    IEnumerator LightningTrailEffect()
    {
        while (bulletTrail != null && gameObject != null)
        {
            // Flickering lightning effect
            float flicker = Random.Range(0.5f, 1f);
            Color flickerColor = trailColor * flicker;
            
            if (bulletTrail != null)
            {
                bulletTrail.material.color = flickerColor;
            }
            
            yield return new WaitForSeconds(0.05f);
        }
    }
    
    Material CreateDefaultTrailMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = trailColor;
        return mat;
    }
    
    // Method to enable/disable trail effects
    public void SetTrailEnabled(bool enabled)
    {
        enableTrail = enabled;
        
        if (bulletTrail != null)
        {
            bulletTrail.enabled = enabled;
        }
    }
    
    // Method to change trail color dynamically
    public void SetTrailColor(Color color)
    {
        trailColor = color;
        
        if (bulletTrail != null)
        {
            bulletTrail.startColor = color;
            bulletTrail.endColor = color;
        }
        
        if (trailParticles != null)
        {
            var main = trailParticles.main;
            main.startColor = color;
        }
    }
    
    // Method to enhance trail for special effects
    public void EnhanceTrail(float multiplier)
    {
        if (bulletTrail != null)
        {
            bulletTrail.startWidth = trailWidth * multiplier;
            bulletTrail.time = trailTime * multiplier;
        }
    }
    
    // Static method to add trail to any bullet
    public static void AddTrailToBullet(GameObject bullet, Color color, float width = 0.1f)
    {
        BulletTrailSystem trailSystem = bullet.GetComponent<BulletTrailSystem>();
        
        if (trailSystem == null)
        {
            trailSystem = bullet.AddComponent<BulletTrailSystem>();
        }
        
        trailSystem.trailColor = color;
        trailSystem.trailWidth = width;
        trailSystem.enableTrail = true;
        trailSystem.SetupTrailRenderer();
    }
    
    void OnDestroy()
    {
        // Clean up particle effects
        if (trailParticles != null)
        {
            trailParticles.Stop();
        }
    }
}