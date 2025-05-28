
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Telepathy;
using UnityEngine.SceneManagement;

public class MatchmakerNetworkManager : NetworkManager
{
    [Header("Matchmaker Settings (Server A)")]
    public ushort matchmakerPort = 7777;

    [Header("Game Servers (Server B)")]
    public string gameServerAddress = "localhost";
    public ushort[] gameServerPorts = { 8888};
    public float gameDuration = 300f;  // seconds until port returns

    public struct RedirectMessage : NetworkMessage
    {
        public string address;
        public ushort port;
    }

    private Queue<ushort> freePorts;
    private TelepathyTransport tp;

    public override void Awake()
    {
        base.Awake();
        tp = GetComponent<TelepathyTransport>();
        tp.port = matchmakerPort;
        freePorts = new Queue<ushort>(gameServerPorts);

        NetworkClient.RegisterHandler<RedirectMessage>(OnRedirectMessage, false);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log($"[Matchmaker] Listening on {networkAddress}:{tp.port}");
    }

    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        base.OnServerConnect(conn);

        // ignore host
        if (conn.connectionId == 0)
            return;

        if (freePorts.Count == 0)
        {
            conn.Send(new RedirectMessage { address = "", port = 0 });
            StartCoroutine(DelayedDisconnect(conn));
            return;
        }

        ushort assigned = freePorts.Dequeue();
        StartCoroutine(FreePortLater(assigned, gameDuration));

        conn.Send(new RedirectMessage { address = gameServerAddress, port = assigned });
        StartCoroutine(DelayedDisconnect(conn));
    }

    private IEnumerator DelayedDisconnect(NetworkConnectionToClient conn)
    {
        yield return new WaitForSeconds(0.05f);
        conn.Disconnect();
    }

    private IEnumerator FreePortLater(ushort port, float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log($"[Matchmaker] Port {port} returned to pool");
        freePorts.Enqueue(port);
    }

    private void OnRedirectMessage(RedirectMessage msg)
    {
        if (string.IsNullOrEmpty(msg.address) || msg.port == 0)
        {
            Debug.LogWarning("[Matchmaker] No servers available.");
            return;
        }

        Debug.Log($"[Client] Redirect → {msg.address}:{msg.port}");
        StartCoroutine(DoRedirect(msg));
    }

    private IEnumerator DoRedirect(RedirectMessage msg)
    {
        // wait one frame
        yield return null;

        // Swap to the game scene

        PlayerPrefs.SetInt("port", msg.port);
        PlayerPrefs.SetString("address", msg.address);
        // disconnect from matchmaker
        StopClient();


        // reconfigure transport
        SceneManager.LoadScene("Knockout");
        yield return null;
        tp = GetComponent<TelepathyTransport>();
        tp.port = msg.port;
        networkAddress = msg.address;

        
        // connect to game server
        //StartClient();
    }
}
//```

//**Usage:**
//1.Create a** Matchmaker** scene with this `NetworkManager`.
//2. Attach **TelepathyTransport** and set its port via `matchmakerPort`.
//3. Configure **gameServerPorts** and **gameServerAddress**.
//4. Clients initially connect to this scene; upon receiving a `RedirectMessage`, they load the **"Knockout"** scene and reconnect to the assigned port.
//5. Run your dedicated game server builds separately listening on the specified ports.
