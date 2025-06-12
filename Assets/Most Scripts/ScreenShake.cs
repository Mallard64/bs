using UnityEngine;
using System.Collections;

public class ScreenShake : MonoBehaviour
{
    public static ScreenShake Instance;
    
    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            originalPosition = transform.localPosition;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        originalPosition = transform.localPosition;
    }
    
    public static void Shake(float duration, float magnitude)
    {
        if (Instance != null)
        {
            Instance.DoShake(duration, magnitude);
        }
    }
    
    public static void ShakeExplosion(float magnitude = 0.3f)
    {
        Shake(0.5f, magnitude);
    }
    
    public static void ShakeGunshot(float magnitude = 0.1f)
    {
        Shake(0.2f, magnitude);
    }
    
    public static void ShakeLightning(float magnitude = 0.4f)
    {
        Shake(0.3f, magnitude);
    }
    
    public static void ShakeHeavyWeapon(float magnitude = 0.25f)
    {
        Shake(0.4f, magnitude);
    }
    
    void DoShake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }
        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }
    
    IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            
            // Fade out the shake over time
            float fadeOut = 1f - (elapsed / duration);
            transform.localPosition = originalPosition + new Vector3(x * fadeOut, y * fadeOut, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Return to original position
        transform.localPosition = originalPosition;
        shakeCoroutine = null;
    }
}