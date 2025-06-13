using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hittable : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 100;
    public int currentHealth;
    
    [Header("Effects")]
    public GameObject hitEffect;
    public AudioClip hitSound;
    
    private AudioSource audioSource;
    
    void Start()
    {
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
    }
    
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        // Play hit effect
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        
        // Play hit sound
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        
        // Check if destroyed
        if (currentHealth <= 0)
        {
            OnDestroyed();
        }
    }
    
    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
    
    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
    
    protected virtual void OnDestroyed()
    {
        // Override in subclasses for specific behavior
        Debug.Log($"{gameObject.name} was destroyed!");
    }
}
