using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using Mirror;

[Serializable]
public class ServerInfo
{
    public string ip;
    public int port;
    public int playerCount;
    public bool online;
    public DateTime lastUpdate;
}



// Firestore response classes
[Serializable]
public class FirestoreDocumentsResponse
{
    public FirestoreDocument[] documents;
}

[Serializable]
public class FirestoreDocument
{
    public string name;
    public Dictionary<string, object> fields;
    public string createTime;
    public string updateTime;
}

// Usage remains the same
public class QuickJoinManager : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private RESTServerFinder serverFinder; // Changed to RESTServerFinder

    private void Start()
    {
        RESTServerFinder.OnServerFound += OnServerFound;
        RESTServerFinder.OnServerSearchFailed += OnServerSearchFailed;
        QuickJoin();
    }

    private void OnDestroy()
    {
        RESTServerFinder.OnServerFound -= OnServerFound;
        RESTServerFinder.OnServerSearchFailed -= OnServerSearchFailed;
    }

    public void QuickJoin()
    {
        Debug.Log("Searching for available server...");
        serverFinder.FindAvailableServer();
    }

    private void OnServerFound(string ip, int port)
    {
        Debug.Log($"Connecting to server: {ip}:{port}");

        networkManager.networkAddress = ip;

        var telepathy = networkManager.GetComponent<TelepathyTransport>();
        if (telepathy != null)
        {
            telepathy.port = (ushort)port;
        }

        networkManager.StartClient();
    }

    private void OnServerSearchFailed(string error)
    {
        Debug.LogError($"Failed to find server: {error}");
        // Show error UI to player
    }
}