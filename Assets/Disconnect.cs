using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using UnityEngine.UI;

public class SceneResetWithDisconnect : MonoBehaviour
{
    public string sceneToLoad = "New Scene"; // Change this to your target scene name
    public Button resetButton; // Assign this in the Unity Inspector

    void Start()
    {
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetScene);
        }
    }

    public void ResetScene()
    {
        // If the player is connected to a Mirror server, disconnect them
        if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }

        // If the player is the host, stop the server as well
        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
        }

        // Load the specified scene
        SceneManager.LoadScene(sceneToLoad);
    }
}
