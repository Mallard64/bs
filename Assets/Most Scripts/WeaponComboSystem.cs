using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;

public class WeaponComboSystem : NetworkBehaviour
{
    [Header("Combo Settings")]
    public float comboWindow = 3f; // Time window to maintain combo
    public float comboDecayTime = 1.5f; // Time before combo starts decaying
    public int maxComboCount = 10;
    
    [Header("UI References")]
    public TextMeshProUGUI comboCountText;
    public TextMeshProUGUI comboMultiplierText;
    public GameObject comboPopup;
    public Animator comboAnimator;
    
    [SyncVar(hook = nameof(OnComboChanged))]
    public int currentCombo = 0;
    
    [SyncVar(hook = nameof(OnMultiplierChanged))]
    public float damageMultiplier = 1f;
    
    private Queue<ComboAction> recentActions = new Queue<ComboAction>();
    private float lastActionTime;
    private Coroutine comboDecayCoroutine;
    
    // Combo tracking
    private int lastWeaponId = -1;
    private int lastModeUsed = -1;
    private float lastShotTime;
    
    [System.Serializable]
    public struct ComboAction
    {
        public int weaponId;
        public int mode;
        public float timestamp;
        public Vector3 direction;
    }
    
    // Special combo patterns that give bonus effects
    private Dictionary<string, ComboBonus> comboBonuses = new Dictionary<string, ComboBonus>()
    {
        // Elemental Staff combos
        {"4-0,4-1,4-2", new ComboBonus("Elemental Trinity", 2.5f, "All elements unleashed!")},
        {"4-2,4-2,4-2", new ComboBonus("Lightning Storm", 2.0f, "Chain lightning intensifies!")},
        
        // War Hammer combos
        {"7-0,7-1,7-2", new ComboBonus("Berserker Rage", 2.2f, "Unstoppable force!")},
        {"7-2,7-2,7-0", new ComboBonus("Earthquake", 2.0f, "Ground trembles!")},
        
        // Spirit Bow combos
        {"6-0,6-1,6-2", new ComboBonus("Hunter's Mark", 1.8f, "Perfect accuracy!")},
        {"6-2,6-2,6-1", new ComboBonus("Arrow Rain", 2.1f, "Sky darkens with arrows!")},
        
        // Ninja Kunai combos
        {"9-0,9-2,9-1", new ComboBonus("Shadow Strike", 2.3f, "Death from shadows!")},
        {"9-2,9-0,9-2", new ComboBonus("Phantom Assault", 2.0f, "Untraceable attack!")},
        
        // Cross-weapon combos
        {"4-2,9-2,10-1", new ComboBonus("Dimensional Rift", 3.0f, "Reality bends!")},
        {"7-1,6-1,5-2", new ComboBonus("Destruction Wave", 2.8f, "Overwhelming power!")},
        {"8-1,4-0,9-1", new ComboBonus("Tech-Magic Fusion", 2.5f, "Science meets magic!")}
    };
    
    [System.Serializable]
    public struct ComboBonus
    {
        public string name;
        public float multiplier;
        public string description;
        
        public ComboBonus(string n, float m, string d)
        {
            name = n;
            multiplier = m;
            description = d;
        }
    }

    void Start()
    {
        if (!isLocalPlayer)
        {
            if (comboCountText != null) comboCountText.gameObject.SetActive(false);
            if (comboMultiplierText != null) comboMultiplierText.gameObject.SetActive(false);
            if (comboPopup != null) comboPopup.SetActive(false);
        }
    }

    [Server]
    public void RegisterWeaponFire(int weaponId, int mode, Vector3 direction)
    {
        float currentTime = Time.time;
        
        // Check if this continues a combo or starts a new one
        if (currentTime - lastActionTime <= comboWindow)
        {
            // Continue combo
            currentCombo++;
            
            // Check for mode switching bonus
            if (weaponId == lastWeaponId && mode != lastModeUsed)
            {
                currentCombo++; // Bonus for switching modes on same weapon
                RpcShowComboText("Mode Switch!");
            }
            
            // Check for weapon switching bonus
            if (weaponId != lastWeaponId && currentTime - lastShotTime <= 1f)
            {
                currentCombo++; // Bonus for quick weapon switching
                RpcShowComboText("Weapon Switch!");
            }
        }
        else
        {
            // Reset combo
            currentCombo = 1;
            recentActions.Clear();
        }
        
        // Clamp combo
        currentCombo = Mathf.Min(currentCombo, maxComboCount);
        
        // Add action to recent actions
        ComboAction action = new ComboAction
        {
            weaponId = weaponId,
            mode = mode,
            timestamp = currentTime,
            direction = direction
        };
        
        recentActions.Enqueue(action);
        
        // Keep only last 5 actions for pattern matching
        while (recentActions.Count > 5)
        {
            recentActions.Dequeue();
        }
        
        // Update damage multiplier based on combo
        UpdateDamageMultiplier();
        
        // Check for special combo patterns
        CheckComboPatterns();
        
        // Update tracking variables
        lastWeaponId = weaponId;
        lastModeUsed = mode;
        lastActionTime = currentTime;
        lastShotTime = currentTime;
        
        // Reset decay coroutine
        if (comboDecayCoroutine != null)
        {
            StopCoroutine(comboDecayCoroutine);
        }
        comboDecayCoroutine = StartCoroutine(ComboDecayRoutine());
    }
    
    [Server]
    void UpdateDamageMultiplier()
    {
        // Base multiplier increases with combo count
        float baseMultiplier = 1f + (currentCombo - 1) * 0.15f;
        damageMultiplier = Mathf.Min(baseMultiplier, 3f); // Cap at 3x damage
    }
    
    [Server]
    void CheckComboPatterns()
    {
        if (recentActions.Count < 3) return;
        
        // Build pattern string from recent actions
        List<ComboAction> actionList = new List<ComboAction>(recentActions);
        
        // Check 3-action patterns
        for (int i = 0; i <= actionList.Count - 3; i++)
        {
            string pattern = $"{actionList[i].weaponId}-{actionList[i].mode}," +
                           $"{actionList[i + 1].weaponId}-{actionList[i + 1].mode}," +
                           $"{actionList[i + 2].weaponId}-{actionList[i + 2].mode}";
            
            if (comboBonuses.ContainsKey(pattern))
            {
                ComboBonus bonus = comboBonuses[pattern];
                damageMultiplier = bonus.multiplier;
                currentCombo += 3; // Bonus combo points
                RpcShowSpecialCombo(bonus.name, bonus.description);
                return;
            }
        }
    }
    
    [ClientRpc]
    void RpcShowComboText(string text)
    {
        if (!isLocalPlayer) return;
        StartCoroutine(ShowComboTextRoutine(text));
    }
    
    [ClientRpc]
    void RpcShowSpecialCombo(string comboName, string description)
    {
        if (!isLocalPlayer) return;
        StartCoroutine(ShowSpecialComboRoutine(comboName, description));
    }
    
    IEnumerator ShowComboTextRoutine(string text)
    {
        if (comboPopup == null) yield break;
        
        var popupText = comboPopup.GetComponent<TextMeshProUGUI>();
        if (popupText != null)
        {
            popupText.text = text;
            popupText.color = Color.yellow;
        }
        
        comboPopup.SetActive(true);
        if (comboAnimator != null)
        {
            comboAnimator.Play("ComboPopup");
        }
        
        yield return new WaitForSeconds(1f);
        comboPopup.SetActive(false);
    }
    
    IEnumerator ShowSpecialComboRoutine(string comboName, string description)
    {
        if (comboPopup == null) yield break;
        
        var popupText = comboPopup.GetComponent<TextMeshProUGUI>();
        if (popupText != null)
        {
            popupText.text = $"{comboName}\n{description}";
            popupText.color = Color.cyan;
            popupText.fontSize = 24f;
        }
        
        comboPopup.SetActive(true);
        if (comboAnimator != null)
        {
            comboAnimator.Play("SpecialCombo");
        }
        
        yield return new WaitForSeconds(2f);
        
        if (popupText != null)
        {
            popupText.fontSize = 18f;
        }
        
        comboPopup.SetActive(false);
    }
    
    IEnumerator ComboDecayRoutine()
    {
        yield return new WaitForSeconds(comboDecayTime);
        
        // Start decaying combo
        while (currentCombo > 0)
        {
            yield return new WaitForSeconds(0.5f);
            currentCombo = Mathf.Max(0, currentCombo - 1);
            UpdateDamageMultiplier();
            
            if (currentCombo == 0)
            {
                recentActions.Clear();
                damageMultiplier = 1f;
            }
        }
    }
    
    void OnComboChanged(int oldCombo, int newCombo)
    {
        if (!isLocalPlayer) return;
        
        if (comboCountText != null)
        {
            comboCountText.text = newCombo > 0 ? $"Combo: {newCombo}" : "";
            comboCountText.gameObject.SetActive(newCombo > 0);
        }
    }
    
    void OnMultiplierChanged(float oldMultiplier, float newMultiplier)
    {
        if (!isLocalPlayer) return;
        
        if (comboMultiplierText != null)
        {
            if (newMultiplier > 1f)
            {
                comboMultiplierText.text = $"{newMultiplier:F1}x Damage";
                comboMultiplierText.gameObject.SetActive(true);
                
                // Color coding for multiplier
                if (newMultiplier >= 2.5f)
                    comboMultiplierText.color = Color.red;
                else if (newMultiplier >= 2f)
                    comboMultiplierText.color = Color.magenta;
                else if (newMultiplier >= 1.5f)
                    comboMultiplierText.color = Color.yellow;
                else
                    comboMultiplierText.color = Color.white;
            }
            else
            {
                comboMultiplierText.gameObject.SetActive(false);
            }
        }
    }
    
    // Public method to get current damage multiplier for other scripts
    public float GetDamageMultiplier()
    {
        return damageMultiplier;
    }
    
    // Public method to reset combo (for deaths, etc.)
    [Server]
    public void ResetCombo()
    {
        currentCombo = 0;
        damageMultiplier = 1f;
        recentActions.Clear();
        
        if (comboDecayCoroutine != null)
        {
            StopCoroutine(comboDecayCoroutine);
        }
    }
}