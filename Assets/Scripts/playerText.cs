using UnityEngine;
using Mirror;
using TMPro;
using UnityEditor;

using UnityEngine.SceneManagement;

public class GameStatsManager : NetworkBehaviour
{
    [SyncVar(hook = nameof(OnTimerUpdated))]
    public float gameTime;

    [SyncVar(hook = nameof(OnKillsUpdated))]
    public int kills;

    [SyncVar(hook = nameof(OnKillsUpdated1))]
    public int kills1;

    public string h;

    GameObject[] texts;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI killsText;
    public TextMeshProUGUI killsText1;

    public float updateInterval = 1f;
    private float timer;

    void Start()
    {
        texts = Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[];
        for (int i = 0; i < texts.Length; i++)
        {
            Debug.Log(texts[i].name);
            if (texts[i].name == "Timer")
            {
                timerText = texts[i].GetComponent<TextMeshProUGUI>();
            }
            if (texts[i].name == "Kills (player 1)")
            {
                killsText = texts[i].GetComponent<TextMeshProUGUI>();
            }
            if (texts[i].name == "Kills (player 2)")
            {
                killsText1 = texts[i].GetComponent<TextMeshProUGUI>();
            }
        }
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
        if (timerText != null)
        {
            timerText.text = "Time: " + Mathf.FloorToInt(gameTime) + "s";
        }
        
        killsText.text = h + ": " + kills;
        killsText1.text = h + ": " + kills1;
    }
}
