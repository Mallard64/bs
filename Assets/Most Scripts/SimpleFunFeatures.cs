using UnityEngine;
using Mirror;

// Simplified version with minimal syntax requirements
public class SimpleFunFeatures : NetworkBehaviour
{
    [Header("Simple Fun Features - No Complex Dependencies")]
    public bool enableScreenShake = true;
    public bool enableBulletTrails = true;
    
    void Start()
    {
        if (enableScreenShake)
        {
            SetupScreenShake();
        }
        
        Debug.Log("✅ Simple Fun Features activated!");
    }
    
    void SetupScreenShake()
    {
        Camera cam = Camera.main;
        if (cam != null && cam.GetComponent<ScreenShake>() == null)
        {
            cam.gameObject.AddComponent<ScreenShake>();
            Debug.Log("📳 Screen shake added to camera!");
        }
    }
    
    // Simple method to add trails to bullets
    public void AddTrailToBullet(GameObject bullet)
    {
        if (bullet == null) return;
        
        TrailRenderer trail = bullet.GetComponent<TrailRenderer>();
        if (trail == null)
        {
            trail = bullet.AddComponent<TrailRenderer>();
            trail.time = 0.3f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.material.color = Color.yellow;
        }
    }
    
    // Simple screen shake trigger
    public void TriggerScreenShake(float intensity = 0.2f)
    {
        if (enableScreenShake)
        {
            ScreenShake.Shake(0.3f, intensity);
        }
    }
    
    // Call this when you fire a weapon
    public void OnWeaponFired(GameObject bullet)
    {
        if (enableBulletTrails)
        {
            AddTrailToBullet(bullet);
        }
        
        // Add screen shake for shooting
        TriggerScreenShake(0.1f);
    }
}