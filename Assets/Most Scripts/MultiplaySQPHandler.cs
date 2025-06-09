using System.Collections;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using Mirror;

public class MultiplaySQPHandler : MonoBehaviour
{
    [Header("SQP Settings")]
    [SerializeField] private ushort sqpPort = 7778; // Different from game port!
    [SerializeField] private ushort maxPlayers = 10;

    private IServerQueryHandler sqpHandler;
    private NetworkManager networkManager;

    async void Start()
    {
        if (!Application.isBatchMode) return;

        networkManager = GetComponent<NetworkManager>();

        try
        {
            // Initialize Unity Services
            await UnityServices.InitializeAsync();

            // Get server config (no waiting needed - Multiplay allocates before starting)
            var serverConfig = MultiplayService.Instance.ServerConfig;

            Debug.Log($"[Multiplay] Server ID: {serverConfig.ServerId}");
            Debug.Log($"[Multiplay] Allocated Port: {serverConfig.Port}");
            Debug.Log($"[Multiplay] Query Port: {serverConfig.QueryPort}");
            Debug.Log($"[Multiplay] Allocation ID: {serverConfig.AllocationId}");

            // Start SQP handler for health checks
            sqpHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(
                maxPlayers,
                "Robot Rush",
                "Deathmatch",
                "1.0",
                "Arena"
            );

            Debug.Log("[SQP] Handler started");

            // Configure port from Multiplay allocation
            GetComponent<TelepathyTransport>().port = (ushort)serverConfig.Port;

            // Start Mirror server
            networkManager.StartServer();

            // Mark server as ready
            await MultiplayService.Instance.ReadyServerForPlayersAsync();

            Debug.Log("[Multiplay] Server ready!");

            // Start updating SQP
            InvokeRepeating(nameof(UpdateSQP), 1f, 5f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Multiplay] Failed to initialize: {e}");
            // Just start server on default port as fallback
            networkManager.StartServer();
        }
    }

    async System.Threading.Tasks.Task StartSQP()
    {
        try
        {
            // Start the SQP handler - THIS IS CRITICAL!
            sqpHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(
                maxPlayers,              // Max players
                "Robot Rush",           // Server name
                "Deathmatch",          // Game type
                "1.0",                 // Version
                "Arena"                // Map name
            );

            Debug.Log($"[SQP] Handler started on port {sqpPort}");

            // Update SQP data periodically
            InvokeRepeating(nameof(UpdateSQP), 1f, 5f);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[SQP] Failed to start handler: {e}");
        }
    }

    void UpdateSQP()
    {
        if (sqpHandler != null && NetworkServer.active)
        {
            // Update current player count
            sqpHandler.CurrentPlayers = (ushort)NetworkServer.connections.Count;

            // Update server info
            sqpHandler.ServerName = $"Robot Rush - {NetworkServer.connections.Count}/{maxPlayers}";
        }
    }

    void OnDestroy()
    {
        sqpHandler?.Dispose();
    }
}