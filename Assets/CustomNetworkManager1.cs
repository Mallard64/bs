using UnityEngine;
using Mirror;

public class CustomNetworkManager1 : NetworkManager
{
    public GameObject warriorPrefab;
    public GameObject magePrefab;
    public GameObject archerPrefab;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // Get the selected character index from PlayerPrefs
        int selectedCharacter = PlayerPrefs.GetInt("SelectedCharacter", 0);

        // Choose the prefab based on the selected character
        GameObject playerPrefab = new GameObject();

        switch (selectedCharacter)
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
        }

        // Instantiate the chosen player prefab
        GameObject playerInstance = Instantiate(playerPrefab, new Vector3(0,0,0), Quaternion.identity);

        // Add the player to the server
        NetworkServer.AddPlayerForConnection(conn, playerInstance);
    }
}
