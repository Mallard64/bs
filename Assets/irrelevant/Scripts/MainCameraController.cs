using UnityEngine;
using Mirror;

public class MainCameraManager : MonoBehaviour
{
    void Start()
    {
        // Check if there's a player spawned by listening for the player spawn event
        NetworkClient.RegisterPrefab(NetworkManager.singleton.playerPrefab);
    }

    void Update()
    {
        // The player has spawned, destroy the Main Camera
        GameObject mainCamera = GameObject.FindWithTag("MainCamera");
        if (mainCamera != null)
        {
            Debug.Log("Player spawned, destroying Main Camera.");
            Destroy(mainCamera);
        }

        // Once the camera is destroyed, we can disable this script
        this.enabled = false;
    }
}
