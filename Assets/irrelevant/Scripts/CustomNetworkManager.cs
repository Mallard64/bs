using UnityEngine;
using Mirror;

public class CustomNetworkManager : NetworkManager
{
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Call the base method to add the player to the game
        base.OnServerAddPlayer(conn);

        // Find and destroy the Main Camera once a player spawns
        GameObject mainCamera = GameObject.FindWithTag("MainCamera");
        if (mainCamera != null)
        {
            Debug.Log("Player spawned, destroying Main Camera.");
            Destroy(mainCamera);
        }
    }
}
