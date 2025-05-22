using UnityEngine;
using Mirror;
using TMPro;

public class ScoreManager : NetworkBehaviour
{
    public static ScoreManager Instance;

    [Header("UI Slots")]
    public TextMeshProUGUI leftText;   // assign in inspector: the left‐side UI
    public TextMeshProUGUI rightText;  // assign in inspector: the right‐side UI

    [SyncVar(hook = nameof(OnP1ScoreChanged))]
    private int player1Kills;

    [SyncVar(hook = nameof(OnP2ScoreChanged))]
    private int player2Kills;

    // Filled in on each client once their local Enemy spawns:
    private int localPlayerNum = 0;
    private bool localPlayerFound = false;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void OnStartServer()
    {
        // initialize scores
        player1Kills = 0;
        player2Kills = 0;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        TryFindLocalPlayer();
    }

    void Update()
    {
        TryFindLocalPlayer();
    }

    void TryFindLocalPlayer()
    {
        foreach (var e in FindObjectsOfType<Enemy>())
        {
            if (e.isLocalPlayer)
            {
                localPlayerNum = e.playerNum;  // needs public or internal access
                localPlayerFound = true;
                UpdateTexts();
                break;
            }
        }
    }

    [Server]
    public void AddKill(int playerId)
    {
        if (playerId == 1) player1Kills++;
        else if (playerId == 2) player2Kills++;
    }

    // Hooks fire on **all clients** when the SyncVar changes:
    void OnP1ScoreChanged(int _, int newVal)
    {
        player1Kills = newVal;
        UpdateTexts();
    }

    void OnP2ScoreChanged(int _, int newVal)
    {
        player2Kills = newVal;
        UpdateTexts();
    }

    // Puts local player's kills on the leftText, other player's on rightText
    void UpdateTexts()
    {
        if (!localPlayerFound) return;

        int localKills = (localPlayerNum == 1) ? player1Kills : player2Kills;
        int otherKills = (localPlayerNum == 1) ? player2Kills : player1Kills;

        leftText.text = localKills.ToString();
        rightText.text = otherKills.ToString();
    }
}
