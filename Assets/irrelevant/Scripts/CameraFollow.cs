using UnityEngine;
using Mirror;

public class CameraFollow : NetworkBehaviour
{
    public Transform target; // This will be the player that the camera follows
    public GameObject player;
    public Vector3 offset = new Vector3(0, 0, 0); // Default offset for 2D/3D games
    public Canvas c;
    public SpriteRenderer s;

    private Camera playerCamera;

    void Start()
    {
        // Find the player's camera and disable it for non-local players
        playerCamera = GetComponent<Camera>();

        // Make sure the camera is only enabled for the local player
        if (!isLocalPlayer)
        {
            playerCamera.enabled = false;
            c.enabled = false;
            s.enabled = false;
        }
        else
        {
            playerCamera.enabled = true;  // Enable the camera for the local player
            target = transform;  // Set the player to this GameObject's transform
        }
    }

    void LateUpdate()
    {
        if (!isLocalPlayer) return;

        // Update camera position to follow the player
        if (target != null)
        {
            playerCamera.transform.position = target.position + offset;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        if (player.GetComponent<MouseShooting>().isFlipped)
        {
            transform.rotation = Quaternion.Euler(0, 0, 180);
        }
        else
        {
            transform.rotation = Quaternion.Euler(0, 0, 0);
        }
        
    }
}
