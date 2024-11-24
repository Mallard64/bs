using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;

public class CustomNetworkManager1 : NetworkManager
{
    public GameObject warriorPrefab;
    public GameObject magePrefab;
    public GameObject archerPrefab;
    public GameObject throwerPrefab;
    public GameObject background;
    public GameObject goal;
    public GameObject goal1;
    public Vector3 t;
    public List<Vector3> spawnPoints; // All spawn points
    public List<TextMeshProUGUI> texts; // All spawn points
    public Queue<Vector3> availableSpawnPoints = new Queue<Vector3>(); // Queue to cycle through spawn points
    public Dictionary<int, Vector3> playerSpawnPoints = new Dictionary<int, Vector3>();
    public Queue<TextMeshProUGUI> availableText = new Queue<TextMeshProUGUI>(); // Queue to cycle through spawn points
    public Dictionary<int, TextMeshProUGUI> playerText = new Dictionary<int, TextMeshProUGUI>();
    string oldScene = "";
    private int nextSpawnIndex = 0;

    public override void Start()
    {
        base.Start();
        // Find all SpawnPoint objects in the scene and add their Vector3 to the spawnPoints list
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");

        foreach (GameObject obj in spawnPointObjects)
        {
            spawnPoints.Add(obj.transform.position);
        }
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("No spawn points found! Make sure spawn points are tagged correctly.");
        }
        else
        {
            // Initialize the queue with all spawn points
            foreach (var spawnPoint in spawnPoints)
            {
                availableSpawnPoints.Enqueue(spawnPoint);
            }
        }
    }

    // Function to get the next available spawn point
    public Vector3 GetNextSpawnPoint()
    {
        if (availableSpawnPoints.Count == 0)
        {
            Debug.Log("All spawn points used up, re-adding all spawn points to the queue.");
            foreach (var spawnPoint in spawnPoints)
            {
                availableSpawnPoints.Enqueue(spawnPoint);
            }
        }

        Vector3 nextSpawnPoint = availableSpawnPoints.Dequeue();
        availableSpawnPoints.Enqueue(nextSpawnPoint); // Recycle the used spawn point
        return nextSpawnPoint;
    }

    // Function to get a unique spawn point for each player
    public Vector3 AssignSpawnPoint(int conn)
    {
        // Check if the player already has an assigned spawn point
        if (playerSpawnPoints.ContainsKey(conn))
        {
            return playerSpawnPoints[conn]; // Return the already assigned spawn point
        }
        else if (spawnPoints.Count > 0)
        {
            Debug.Log("SPAWN POINT" + spawnPoints.Count);
            Vector3 spawnPoint = spawnPoints[0]; // Pick the first available spawn point
            playerSpawnPoints.Add(conn, spawnPoint); // Assign it to the player

            // Remove the spawn point from the list to ensure it's not reused
            spawnPoints.RemoveAt(0);

            Debug.Log(spawnPoints.Count);
            return spawnPoint;
        }

        Debug.LogError("No available spawn points!");
        return Vector3.zero;
    }

    // Function to get the next available spawn point
    public TextMeshProUGUI GetNextText()
    {
        if (availableText.Count == 0)
        {
            Debug.Log("All spawn points used up, re-adding all spawn points to the queue.");
            foreach (var spawnPoint in spawnPoints)
            {
                availableSpawnPoints.Enqueue(spawnPoint);
            }
        }

        TextMeshProUGUI nextSpawnPoint = availableText.Dequeue();
        availableText.Enqueue(nextSpawnPoint); // Recycle the used spawn point
        return nextSpawnPoint;
    }

    // Function to get a unique spawn point for each player
    public TextMeshProUGUI AssignText(int conn)
    {
        // Check if the player already has an assigned spawn point
        if (playerSpawnPoints.ContainsKey(conn))
        {
            return playerText[conn]; // Return the already assigned spawn point
        }
        else if (spawnPoints.Count > 0)
        {
            Debug.Log("SPAWN POINT" + spawnPoints.Count);
            TextMeshProUGUI spawnPoint = texts[0]; // Pick the first available spawn point
            playerText.Add(conn, spawnPoint); // Assign it to the player

            // Remove the spawn point from the list to ensure it's not reused
            texts.RemoveAt(0);

            return spawnPoint;
        }

        Debug.LogError("No available spawn points!");
        return null;
    }

    public override void Update()
    {
        base.Update();
        if ((SceneManager.GetActiveScene().name == "Knockout" || SceneManager.GetActiveScene().name == "Knockout 1") && oldScene != SceneManager.GetActiveScene().name)
        {
            if (SceneManager.GetActiveScene().name == "Knockout 1")
            {
                Instantiate(goal, new Vector3(-10,-24,0), Quaternion.identity);
                Instantiate(goal1, new Vector3(-10, 24, 0), Quaternion.identity);
            }
            Instantiate(background);
            NetworkServer.RegisterHandler<PlayerMessage>(OnCreatePlayer);
        }
        oldScene = SceneManager.GetActiveScene().name;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Register the handler to receive the message
       
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();

        // Send the selected character to the server when the client connects
        int selectedCharacter = PlayerPrefs.GetInt("SelectedCharacter", 0);
        PlayerMessage message = new PlayerMessage
        {
            selectedCharacter = selectedCharacter
        };
        NetworkClient.Send(message);
    }

    void OnCreatePlayer(NetworkConnectionToClient conn, PlayerMessage message)
    {
        // Choose the prefab based on the selected character
        GameObject playerPrefab = null;

        switch (message.selectedCharacter)
        {
            case 1:
                playerPrefab = magePrefab;
                break;
            case 2:
                playerPrefab = archerPrefab;
                break;
            case 3:
                playerPrefab = warriorPrefab;
                break;
            case 4:
                playerPrefab = throwerPrefab;
                break;
            default:
                playerPrefab = warriorPrefab;
                break;
        }
        // Instantiate the chosen player prefab
        GameObject playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        // Add the player to the server
        NetworkServer.AddPlayerForConnection(conn, playerInstance);
        
        
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Get the selected character index from PlayerPrefs
        int selectedCharacter = PlayerPrefs.GetInt("SelectedCharacter", 0);

        // Choose the prefab based on the selected character
        GameObject playerPrefab = null;

        switch (selectedCharacter)
        {
            case 1:
                playerPrefab = magePrefab;
                break;
            case 2:
                playerPrefab = archerPrefab;
                break;
            default:
                playerPrefab = warriorPrefab;
                break;
        }
        // Instantiate the chosen player prefab at the selected spawn point
        GameObject playerInstance = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);

        // Add the player to the server
        NetworkServer.AddPlayerForConnection(conn, playerInstance);
        
    }
}
