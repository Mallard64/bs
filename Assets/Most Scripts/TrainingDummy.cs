using System.Collections;
using UnityEngine;
using TMPro;
using Mirror;

public class TrainingDummy : NetworkBehaviour, IHittable
{
    [Header("Training Dummy Settings")]
    public int maxHealth = 1000;
    public float resetTime = 5f;
    public bool showDamageNumbers = true;
    public bool trackDPS = true;
    
    [Header("Visual Effects")]
    public ParticleSystem hitEffect;
    public Animator dummyAnimator;
    public SpriteRenderer spriteRenderer;
    public AudioClip[] hitSounds;
    
    [Header("UI")]
    public Canvas dummyUI;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI dpsText;
    public UnityEngine.UI.Slider healthBar;
    public GameObject damageTextPrefab;
    
    [SyncVar(hook = nameof(OnHealthChanged))]
    private int currentHealth;
    
    [SyncVar]
    private float totalDamageDealt = 0f;
    
    [SyncVar]
    private float dpsTrackingStartTime;
    
    private AudioSource audioSource;
    private Color originalColor;
    private Coroutine resetCoroutine;
    private Coroutine dpsCoroutine;
    
    // DPS tracking
    private System.Collections.Generic.List<DamageEntry> recentDamage = new System.Collections.Generic.List<DamageEntry>();
    
    [System.Serializable]
    private struct DamageEntry
    {
        public float damage;
        public float timestamp;
    }
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        
        if (isServer)
        {
            ResetDummy();
        }
        
        if (trackDPS)
        {
            StartCoroutine(UpdateDPSDisplay());
        }
    }
    
    [Server]
    void ResetDummy()
    {
        currentHealth = maxHealth;
        totalDamageDealt = 0f;
        dpsTrackingStartTime = Time.time;
        recentDamage.Clear();
        
        RpcResetVisuals();
    }
    
    [ClientRpc]
    void RpcResetVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
        
        if (dummyAnimator != null)
        {
            dummyAnimator.SetBool("IsHit", false);
            dummyAnimator.SetBool("IsDestroyed", false);
        }
    }
    
    public void TakeDamage(int damage)
    {
        TakeDamage((float)damage, 0);
    }
    
    public void TakeDamage(float damage)
    {
        TakeDamage(damage, 0);
    }
    
    public void TakeDamage(float damage, uint shooterId = 0)
    {
        if (!isServer) return;
        
        // Record damage
        totalDamageDealt += damage;
        currentHealth -= Mathf.RoundToInt(damage);
        
        // Track for DPS
        if (trackDPS)
        {
            recentDamage.Add(new DamageEntry { damage = damage, timestamp = Time.time });
            
            // Remove old damage entries (older than 5 seconds)
            recentDamage.RemoveAll(entry => Time.time - entry.timestamp > 5f);
        }
        
        // Show effects
        RpcShowHitEffects(damage);
        
        // Check if dummy should reset
        if (currentHealth <= 0)
        {
            RpcShowDestroyedEffect();
            
            if (resetCoroutine != null)
                StopCoroutine(resetCoroutine);
            resetCoroutine = StartCoroutine(ResetAfterDelay());
        }
        else
        {
            // Cancel any pending reset
            if (resetCoroutine != null)
            {
                StopCoroutine(resetCoroutine);
                resetCoroutine = null;
            }
        }
    }
    
    IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(resetTime);
        ResetDummy();
    }
    
    [ClientRpc]
    void RpcShowHitEffects(float damage)
    {
        // Play hit animation
        if (dummyAnimator != null)
        {
            dummyAnimator.SetTrigger("Hit");
        }
        
        // Flash red
        if (spriteRenderer != null)
        {
            StartCoroutine(FlashRed());
        }
        
        // Play hit sound
        if (hitSounds.Length > 0 && audioSource != null)
        {
            var randomSound = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.PlayOneShot(randomSound);
        }
        
        // Play particle effect
        if (hitEffect != null)
        {
            hitEffect.Play();
        }
        
        // Show damage number
        if (showDamageNumbers && damageTextPrefab != null)
        {
            ShowDamageText(damage);
        }
    }
    
    [ClientRpc]
    void RpcShowDestroyedEffect()
    {
        if (dummyAnimator != null)
        {
            dummyAnimator.SetBool("IsDestroyed", true);
        }
        
        // Larger particle effect
        if (hitEffect != null)
        {
            var emission = hitEffect.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 50)
            });
            hitEffect.Play();
        }
        
        // Screen shake for nearby players
        ScreenShake.Shake(0.3f, 0.2f);
    }
    
    IEnumerator FlashRed()
    {
        if (spriteRenderer == null) yield break;
        
        spriteRenderer.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        
        float timer = 0f;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            spriteRenderer.color = Color.Lerp(Color.red, originalColor, timer / 0.3f);
            yield return null;
        }
        
        spriteRenderer.color = originalColor;
    }
    
    void ShowDamageText(float damage)
    {
        var damageText = Instantiate(damageTextPrefab, transform.position + Vector3.up, Quaternion.identity);
        var textMesh = damageText.GetComponent<TextMeshPro>();
        
        if (textMesh != null)
        {
            textMesh.text = damage.ToString("F0");
            textMesh.color = Color.red;
            textMesh.fontSize = 4f;
        }
        
        StartCoroutine(AnimateDamageText(damageText));
    }
    
    IEnumerator AnimateDamageText(GameObject damageText)
    {
        Vector3 startPos = damageText.transform.position;
        Vector3 endPos = startPos + Vector3.up * 2f + Vector3.right * Random.Range(-0.5f, 0.5f);
        
        var textMesh = damageText.GetComponent<TextMeshPro>();
        float timer = 0f;
        
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            
            damageText.transform.position = Vector3.Lerp(startPos, endPos, timer);
            
            if (textMesh != null)
            {
                Color color = textMesh.color;
                color.a = Mathf.Lerp(1f, 0f, timer);
                textMesh.color = color;
            }
            
            yield return null;
        }
        
        Destroy(damageText);
    }
    
    IEnumerator UpdateDPSDisplay()
    {
        while (gameObject != null)
        {
            yield return new WaitForSeconds(0.2f); // Update 5 times per second
            
            if (dpsText != null)
            {
                float currentDPS = CalculateCurrentDPS();
                dpsText.text = $"DPS: {currentDPS:F1}";
            }
        }
    }
    
    float CalculateCurrentDPS()
    {
        if (recentDamage.Count == 0) return 0f;
        
        float totalRecentDamage = 0f;
        float oldestTime = Time.time;
        
        foreach (var entry in recentDamage)
        {
            totalRecentDamage += entry.damage;
            if (entry.timestamp < oldestTime)
                oldestTime = entry.timestamp;
        }
        
        float timespan = Time.time - oldestTime;
        return timespan > 0 ? totalRecentDamage / timespan : 0f;
    }
    
    void OnHealthChanged(int oldHealth, int newHealth)
    {
        currentHealth = newHealth;
        
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
        
        if (healthBar != null)
        {
            healthBar.value = (float)currentHealth / maxHealth;
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Show UI when player approaches
        var player = other.GetComponent<NetworkIdentity>();
        if (player != null && player.isLocalPlayer && dummyUI != null)
        {
            dummyUI.gameObject.SetActive(true);
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        // Hide UI when player leaves
        var player = other.GetComponent<NetworkIdentity>();
        if (player != null && player.isLocalPlayer && dummyUI != null)
        {
            dummyUI.gameObject.SetActive(false);
        }
    }
    
    // Manual reset button
    [ContextMenu("Reset Dummy")]
    public void ManualReset()
    {
        if (isServer)
        {
            ResetDummy();
        }
    }
}