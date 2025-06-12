using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LeaderboardEntry : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI rankText;
    public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI winRateText;
    public TextMeshProUGUI gamesPlayedText;
    public Image backgroundImage;
    public Image rankIcon;
    
    [Header("Rank Colors")]
    public Color[] rankColors = new Color[]
    {
        new Color(1f, 0.84f, 0f),    // Gold
        new Color(0.75f, 0.75f, 0.75f), // Silver
        new Color(0.8f, 0.5f, 0.2f),    // Bronze
        new Color(0.9f, 0.9f, 0.9f)     // Default
    };
    
    [Header("Rank Icons")]
    public Sprite[] rankSprites; // Crown, medal, etc.
    
    public void SetupEntry(int rank, LobbyManager.PlayerStats stats)
    {
        // Set rank
        if (rankText != null)
        {
            rankText.text = $"#{rank}";
            
            // Color based on rank
            Color rankColor = rank <= rankColors.Length ? rankColors[rank - 1] : rankColors[rankColors.Length - 1];
            rankText.color = rankColor;
        }
        
        // Set rank icon
        if (rankIcon != null && rankSprites != null && rank <= rankSprites.Length)
        {
            rankIcon.sprite = rankSprites[rank - 1];
            rankIcon.gameObject.SetActive(true);
        }
        else if (rankIcon != null)
        {
            rankIcon.gameObject.SetActive(false);
        }
        
        // Set player name
        if (playerNameText != null)
        {
            playerNameText.text = stats.playerName;
        }
        
        // Set kills
        if (killsText != null)
        {
            killsText.text = $"{stats.totalKills} kills";
        }
        
        // Set win rate
        if (winRateText != null)
        {
            winRateText.text = $"{stats.winRate:F1}%";
            
            // Color based on win rate
            if (stats.winRate >= 80f)
                winRateText.color = Color.green;
            else if (stats.winRate >= 60f)
                winRateText.color = Color.yellow;
            else if (stats.winRate >= 40f)
                winRateText.color = Color.white;
            else
                winRateText.color = Color.red;
        }
        
        // Set games played
        if (gamesPlayedText != null)
        {
            gamesPlayedText.text = $"{stats.gamesPlayed} games";
        }
        
        // Set background highlight for top 3
        if (backgroundImage != null)
        {
            if (rank <= 3)
            {
                Color bgColor = rank <= rankColors.Length ? rankColors[rank - 1] : rankColors[rankColors.Length - 1];
                bgColor.a = 0.2f; // Semi-transparent
                backgroundImage.color = bgColor;
            }
            else
            {
                backgroundImage.color = new Color(1f, 1f, 1f, 0.1f);
            }
        }
    }
    
    public void OnEntryClicked()
    {
        // Add click animation or show detailed stats
        StartCoroutine(PulseAnimation());
    }
    
    System.Collections.IEnumerator PulseAnimation()
    {
        Vector3 originalScale = transform.localScale;
        
        // Scale up
        float timer = 0f;
        while (timer < 0.1f)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 1.05f, timer / 0.1f);
            transform.localScale = originalScale * scale;
            yield return null;
        }
        
        // Scale back down
        timer = 0f;
        while (timer < 0.1f)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Lerp(1.05f, 1f, timer / 0.1f);
            transform.localScale = originalScale * scale;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
}