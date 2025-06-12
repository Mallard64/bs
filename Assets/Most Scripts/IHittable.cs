using UnityEngine;

// Interface for objects that can take damage
public interface IHittable
{
    void TakeDamage(int damage);
    void TakeDamage(float damage);
}

// Alternative: If Hittable is a MonoBehaviour, we can use component-based approach
public abstract class HittableComponent : MonoBehaviour
{
    public abstract void TakeDamage(int damage);
    
    public virtual void TakeDamage(float damage)
    {
        TakeDamage(Mathf.RoundToInt(damage));
    }
}