using UnityEngine;
using Mirror;
using TMPro;
using UnityEditor;

using UnityEngine.SceneManagement;

public class GameStatsManager : NetworkBehaviour
{
    public float gameTime;

    public int kills;

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
        gameTime = 0f;
        kills = 0;
        kills1 = 0;
        UpdateUI();
    }

    public void AddKill()
    {
        kills += 1;
        Debug.Log("Add kill1+");
        if ((kills >= 2 || kills1 >= 2) && SceneManager.GetActiveScene().name == "Knockout 1")
        {
            gameTime = 120f;
        }
    }

    public void AddKill1()
    {
        kills1 += 1;
        Debug.Log("Add kill+");
        if ((kills >= 2 || kills1 >= 2) && SceneManager.GetActiveScene().name == "Knockout 1")
        {
            gameTime = 120f;
        }
    }

    private void OnTimerUpdated(float oldTime, float newTime)
    {
    }

    private void OnKillsUpdated(int oldKills, int newKills)
    {
    }

    private void OnKillsUpdated1(int oldKills, int newKills)
    {
    }

    private void Update()
    {
        if (FindObjectsOfType<Enemy>().Length < 2)
        {
            gameTime = 0;
        }
        else
        {
            gameTime += Time.deltaTime;
        }
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
