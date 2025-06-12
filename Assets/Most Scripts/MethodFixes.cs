using UnityEngine;

// This script provides extension methods and fixes for missing method issues
public static class MethodFixes
{
    // Extension method for WeaponOverchargeSystem to provide missing methods
    public static float GetUltimateDamageMultiplier(this WeaponOverchargeSystem system)
    {
        return system.GetDamageMultiplier(); // Use the existing method
    }
    
    public static void AddDamageOvercharge(this WeaponOverchargeSystem system, float amount)
    {
        system.AddCharge(amount); // Use the existing AddCharge method
    }
    
    public static void AddOvercharge(this WeaponOverchargeSystem system, float amount)
    {
        system.AddCharge(amount); // Use the existing AddCharge method
    }
}

// Extension for Hittable to add TakeDamage methods if they don't exist
public static class HittableExtensions
{
    public static void TakeDamage(this Hittable hittable, int damage)
    {
        // Try to find a method that can handle damage
        var component = hittable.GetComponent<MonoBehaviour>();
        if (component != null)
        {
            // Try to invoke TakeDamage via reflection
            var method = component.GetType().GetMethod("TakeDamage");
            if (method != null)
            {
                method.Invoke(component, new object[] { damage });
            }
        }
    }
    
    public static void TakeDamage(this Hittable hittable, float damage)
    {
        hittable.TakeDamage(Mathf.RoundToInt(damage));
    }
}

// Safe TrailRenderer color setter
public static class TrailRendererExtensions
{
    public static void SetColor(this TrailRenderer trail, Color color)
    {
        trail.startColor = color;
        trail.endColor = color;
    }
}