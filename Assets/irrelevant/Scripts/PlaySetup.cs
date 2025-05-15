using UnityEngine;
using Mirror;

public class PlaySetup : NetworkBehaviour
{
    [SyncVar]
    public int selectedCharacter;

    public GameObject warriorPrefab;
    public GameObject magePrefab;
    public GameObject archerPrefab;

    public override void OnStartLocalPlayer()
    {
        // Send the selected character to the server once this client becomes the local player
        int selectedCharacter = PlayerPrefs.GetInt("SelectedCharacter", 0);
        CmdSendCharacterSelection(selectedCharacter);
    }

    [Command]
    private void CmdSendCharacterSelection(int characterIndex)
    {
        // Store the selected character on the server
        selectedCharacter = characterIndex;

        // Now that the server knows the selection, spawn the character
        SpawnCharacter();
    }

    private void SpawnCharacter()
    {
        // Determine which prefab to use based on the selected character
        GameObject chosenPrefab = warriorPrefab; // Default to warrior

        switch (selectedCharacter)
        {
            case 1:
                chosenPrefab = magePrefab;
                break;
            case 2:
                chosenPrefab = archerPrefab;
                break;
            case 3:
                chosenPrefab = warriorPrefab;
                break;
            default:
                Debug.LogWarning("SelectedCharacter index is out of range or not set correctly, defaulting to Warrior.");
                break;
        }

        // Instantiate and replace the player instance
        GameObject newPlayer = Instantiate(chosenPrefab, Vector3.zero, Quaternion.identity);

        // Replace the existing player object for this connection with the new prefab
        NetworkServer.ReplacePlayerForConnection(connectionToClient, newPlayer, ReplacePlayerOptions.Destroy);
    }
}
