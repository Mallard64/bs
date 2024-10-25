using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class CustomNetworkManager1 : NetworkManager
{
    public GameObject warriorPrefab;
    public GameObject magePrefab;
    public GameObject archerPrefab;
    public Transform t;
    private List<Transform> spawnPoints = new List<Transform>(); // All spawn points
    private Queue<Transform> availableSpawnPoints = new Queue<Transform>(); // Queue to cycle through spawn points
    private Dictionary<NetworkConnectionToClient, Transform> playerSpawnPoints = new Dictionary<NetworkConnectionToClient, Transform>();
    private int nextSpawnIndex = 0;

    public override void Start()
    {
        base.Start();
        // Find all SpawnPoint objects in the scene and add their Transform to the spawnPoints list
        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
        spawnPoints = new List<Transform>();

        foreach (GameObject obj in spawnPointObjects)
        {
            spawnPoints.Add(obj.transform);
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
    public Transform GetNextSpawnPoint()
    {
        if (availableSpawnPoints.Count == 0)
        {
            Debug.Log("All spawn points used up, re-adding all spawn points to the queue.");
            foreach (var spawnPoint in spawnPoints)
            {
                availableSpawnPoints.Enqueue(spawnPoint);
            }
        }

        Transform nextSpawnPoint = availableSpawnPoints.Dequeue();
        availableSpawnPoints.Enqueue(nextSpawnPoint); // Recycle the used spawn point
        Debug.Log("Assigning spawn point: " + nextSpawnPoint.position);
        return nextSpawnPoint;
    }

    // Function to get a unique spawn point for each player
    public Transform AssignSpawnPoint(NetworkConnectionToClient conn)
    {
        // Check if the player already has an assigned spawn point
        if (playerSpawnPoints.ContainsKey(conn))
        {
            return playerSpawnPoints[conn]; // Return the already assigned spawn point
        }

        // Assign a new spawn point if available
        if (spawnPoints.Count > 0)
        {
            Transform spawnPoint = spawnPoints[0]; // Pick the first available spawn point
            playerSpawnPoints.Add(conn, spawnPoint); // Assign it to the player

            // Remove the spawn point from the list to ensure it's not reused
            spawnPoints.RemoveAt(0);

            Debug.Log("Assigned spawn point: " + spawnPoint.position + " to player with connection ID: " + conn.connectionId);
            return spawnPoint;
        }

        Debug.LogError("No available spawn points!");
        return null;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // Register the handler to receive the message
        NetworkServer.RegisterHandler<PlayerMessage>(OnCreatePlayer);
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
            default:
                playerPrefab = warriorPrefab;
                break;
        }

        // Instantiate the chosen player prefab
        Transform spawnPoint = AssignSpawnPoint(conn);
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);

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

        // Choose the next spawn point for the player
        Transform spawnPoint = AssignSpawnPoint(conn);

        // Instantiate the chosen player prefab at the selected spawn point
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, Quaternion.identity);

        // Add the player to the server
        NetworkServer.AddPlayerForConnection(conn, playerInstance);
        
    }
}
