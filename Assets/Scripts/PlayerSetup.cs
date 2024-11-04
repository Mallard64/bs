using UnityEngine;
using Mirror;

public class PlayerSetup : NetworkBehaviour
{
    public Camera playerCamera;  // Reference to the player's camera

    void Start()
    {
        if (!isLocalPlayer) return;

        // Manually find the player's camera by tag
        if (playerCamera == null)
        {
            playerCamera = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
            if (playerCamera == null)
            {
                Debug.LogError("MainCamera not found in the scene.");
                return;
            }
        }

        // Enable the local player's camera only
        playerCamera.enabled = true;
        Debug.Log("Camera enabled for player: " + gameObject.name);

        // Set the camera to follow the player
        CameraFollow cameraFollow = playerCamera.GetComponent<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.target = transform;
            Debug.Log("Camera is now following the player: " + gameObject.name);
        }
        else
        {
            Debug.LogError("CameraFollow component missing from the camera.");
        }
    }

}
