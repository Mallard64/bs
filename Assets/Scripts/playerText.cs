using UnityEngine;
using Mirror;
using TMPro;
using UnityEngine.SceneManagement;

public class GameStatsManager : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnTimerUpdated))]
    public float gameTime;

    [SyncVar(hook = nameof(OnKillsUpdated))]
    public int kills;

    [SyncVar(hook = nameof(OnKillsUpdated1))]
    public int kills1;

    public TextMeshProUGUI timerText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI killsText1;

    public float updateInterval = 1f;
    private float timer;

    void Start()
    {
        if (isServer)
        {
            gameTime = 0f;
            kills = 0;
            StartCoroutine(UpdateTime());
        }
        UpdateUI();
    }

    [Server]
    private System.Collections.IEnumerator UpdateTime()
    {
        while (true)
        {
            yield return new WaitForSeconds(updateInterval);
            gameTime += updateInterval;
        }
    }

    [Server]
    public void AddKill()
    {
        kills += 1;
    }

    [Server]
    public void AddKill1()
    {
        kills1 += 1;
    }

    private void OnTimerUpdated(float oldTime, float newTime)
    {
        UpdateUI();
    }

    private void OnKillsUpdated(int oldKills, int newKills)
    {
        UpdateUI();
    }

    private void OnKillsUpdated1(int oldKills, int newKills)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        timerText.text = "Time: " + Mathf.FloorToInt(gameTime) + "s";
        killsText.text = "Kills: " + kills;
        killsText1.text = "Kills: " + kills1;
    }
}
