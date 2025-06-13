using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.SceneManagement;

public class Portal : NetworkBehaviour, IInteractable
{
    [Header("Portal Settings")]
    public string targetScene = "Boss";
    public string portalName = "Boss Arena";
    public string description = "Face the ultimate challenge!";
    public int minPlayersRequired = 1;
    public int maxPlayersAllowed = 4;
    
    [Header("Visual Effects")]
    public ParticleSystem portalEffect;
    public Animator portalAnimator;
    public SpriteRenderer portalGlow;
    public AudioClip activationSound;
    public AudioClip teleportSound;
    
    [Header("UI")]
    public GameObject interactionPrompt;
    public TextMeshProUGUI promptText;
    public TextMeshProUGUI playerCountText;
    public Canvas portalUI;
    
    [SyncVar(hook = nameof(OnActiveChanged))]
    public bool isPortalActive = true;
    
    [SyncVar(hook = nameof(OnPlayerCountChanged))]
    public int playersNearPortal = 0;
    
    private HashSet<uint> playersInRange = new HashSet<uint>();
    private bool playerInRange = false;
    private AudioSource audioSource;
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        SetupPortalEffects();
        UpdateUI();
    }
    
    void SetupPortalEffects()
    {
        if (portalEffect != null)
        {
            var main = portalEffect.main;
            main.startColor = GetPortalColor();
            portalEffect.Play();
        }
        
        if (portalGlow != null)
        {
            portalGlow.color = GetPortalColor();
            StartCoroutine(PulseGlow());
        }
        
        if (portalAnimator != null)
        {
            portalAnimator.SetBool("IsActive", isPortalActive);
        }
    }
    
    Color GetPortalColor()
    {
        switch (targetScene.ToLower())
        {
            case "boss": return new Color(1f, 0.3f, 0.3f); // Red for boss
            case "knockout": return new Color(0.3f, 0.3f, 1f); // Blue for PvP
            default: return new Color(0.5f, 1f, 0.5f); // Green for others
        }
    }
    
    IEnumerator PulseGlow()
    {
        while (gameObject != null)
        {
            if (portalGlow != null && isPortalActive)
            {
                float alpha = Mathf.PingPong(Time.time * 2f, 0.5f) + 0.5f;
                Color color = portalGlow.color;
                color.a = alpha;
                portalGlow.color = color;
            }
            yield return null;
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        var networkIdentity = other.GetComponent<NetworkIdentity>();
        var mouseShooting = other.GetComponent<MouseShooting>();
        
        // Only count objects with MouseShooting (actual players)
        if (networkIdentity != null && mouseShooting != null)
        {
            var playerId = networkIdentity.netId;
            
            if (isServer)
            {
                playersInRange.Add(playerId);
                playersNearPortal = playersInRange.Count;
            }
            
            if (networkIdentity.isLocalPlayer)
            {
                playerInRange = true;
                ShowInteractionPrompt(true);
                PlayActivationSound();
            }
        }
    }
    
    void OnTriggerExit2D(Collider2D other)
    {
        var networkIdentity = other.GetComponent<NetworkIdentity>();
        var mouseShooting = other.GetComponent<MouseShooting>();
        
        // Only count objects with MouseShooting (actual players)
        if (networkIdentity != null && mouseShooting != null)
        {
            var playerId = networkIdentity.netId;
            
            if (isServer)
            {
                playersInRange.Remove(playerId);
                playersNearPortal = playersInRange.Count;
            }
            
            if (networkIdentity.isLocalPlayer)
            {
                playerInRange = false;
                ShowInteractionPrompt(false);
            }
        }
    }
    
    void Update()
    {
        // Update UI
        UpdateUI();
    }
    
    // IInteractable implementation
    public void Interact(GameObject player)
    {
        var networkIdentity = player.GetComponent<NetworkIdentity>();
        if (networkIdentity != null && networkIdentity.isLocalPlayer)
        {
            CmdAttemptTeleport();
        }
    }
    
    public string GetInteractionText()
    {
        if (playersNearPortal >= minPlayersRequired)
        {
            return $"enter {portalName}";
        }
        else
        {
            return $"wait for {minPlayersRequired - playersNearPortal} more players";
        }
    }
    
    public bool CanInteract(GameObject player)
    {
        return isPortalActive && playersNearPortal >= minPlayersRequired;
    }
    
    void ShowInteractionPrompt(bool show)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(show && isPortalActive);
        }
    }
    
    void UpdateUI()
    {
        if (promptText != null)
        {
            if (playersNearPortal >= minPlayersRequired)
            {
                promptText.text = $"Press E to enter {portalName}";
                promptText.color = Color.green;
            }
            else
            {
                promptText.text = $"Need {minPlayersRequired - playersNearPortal} more players";
                promptText.color = Color.yellow;
            }
        }
        
        if (playerCountText != null)
        {
            playerCountText.text = $"{playersNearPortal}/{maxPlayersAllowed} Players";
        }
    }
    
    [Command(requiresAuthority = false)]
    void CmdAttemptTeleport()
    {
        if (!isPortalActive)
        {
            RpcShowMessage("Portal is currently inactive!");
            return;
        }
        
        if (playersNearPortal < minPlayersRequired)
        {
            RpcShowMessage($"Need at least {minPlayersRequired} players to enter!");
            return;
        }
        
        if (playersNearPortal > maxPlayersAllowed)
        {
            RpcShowMessage($"Too many players! Maximum {maxPlayersAllowed} allowed.");
            return;
        }
        
        // Teleport all players in range
        StartCoroutine(TeleportSequence());
    }
    
    IEnumerator TeleportSequence()
    {
        // Play teleport effects
        RpcPlayTeleportEffects();
        
        // Wait for effects
        yield return new WaitForSeconds(1.5f);
        
        // Change scene for all players in range
        foreach (var playerId in playersInRange)
        {
            if (NetworkServer.spawned.TryGetValue(playerId, out var playerObj))
            {
                var connection = playerObj.connectionToClient;
                if (connection != null)
                {
                    connection.Send(new SceneMessage { sceneName = targetScene });
                }
            }
        }
        
        Debug.Log($"🌀 Portal teleported {playersInRange.Count} players to {targetScene}");
    }
    
    [ClientRpc]
    void RpcPlayTeleportEffects()
    {
        StartCoroutine(TeleportEffectsSequence());
    }
    
    IEnumerator TeleportEffectsSequence()
    {
        // Intense portal effects
        if (portalEffect != null)
        {
            var emission = portalEffect.emission;
            emission.rateOverTime = 100f;
        }
        
        // Play teleport sound
        if (teleportSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }
        
        // Screen flash effect
        var flash = GameObject.Find("ScreenFlash");
        if (flash != null)
        {
            var flashRenderer = flash.GetComponent<SpriteRenderer>();
            if (flashRenderer != null)
            {
                flashRenderer.color = new Color(1, 1, 1, 0.8f);
                
                float timer = 0f;
                while (timer < 1f)
                {
                    timer += Time.deltaTime;
                    float alpha = Mathf.Lerp(0.8f, 0f, timer);
                    flashRenderer.color = new Color(1, 1, 1, alpha);
                    yield return null;
                }
            }
        }
    }
    
    [ClientRpc]
    void RpcShowMessage(string message)
    {
        // Show message to player
        Debug.Log($"🌀 Portal: {message}");
        
        // You could display this in a UI notification system
        var notification = FindObjectOfType<NotificationSystem>();
        if (notification != null)
        {
            notification.ShowNotification(message, 3f);
        }
    }
    
    void PlayActivationSound()
    {
        if (activationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
    }
    
    void OnActiveChanged(bool oldValue, bool newValue)
    {
        isPortalActive = newValue;
        
        if (portalAnimator != null)
        {
            portalAnimator.SetBool("IsActive", isPortalActive);
        }
        
        if (portalEffect != null)
        {
            if (isPortalActive)
                portalEffect.Play();
            else
                portalEffect.Stop();
        }
    }
    
    void OnPlayerCountChanged(int oldValue, int newValue)
    {
        playersNearPortal = newValue;
        UpdateUI();
    }
    
    [Server]
    public void SetPortalActive(bool active)
    {
        isPortalActive = active;
    }
}

// Message for scene changes
public struct SceneMessage : NetworkMessage
{
    public string sceneName;
}