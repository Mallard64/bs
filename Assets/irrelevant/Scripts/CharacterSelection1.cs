using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelection1 : MonoBehaviour
{
    public void Start()
    {
        PlayerPrefs.SetInt("team", -11);
    }
    // Called when the player clicks on a character button
    public void SelectCharacter(int index)
    {
        // Save the selected character's index in PlayerPrefs
        PlayerPrefs.SetInt("Gamemode", index);

        // Optionally, load the game scene after selection
        LoadGame(index);
    }

    public void SelectTeam(int index)
    {
        PlayerPrefs.SetInt("team", index);
    }

    // Method to load the game scene
    public void LoadGame(int index)
    {
        if (PlayerPrefs.GetInt("team") == -11)
        {
            return;
        }
        if (index == 1)
        {
            SceneManager.LoadScene("Knockout");
        }
        if (index == 2)
        {
            SceneManager.LoadScene("SampleScene1");
        }
        if (index == 3)
        {
            SceneManager.LoadScene("Knockout 1");
        }
        if (index == 4)
        {
            SceneManager.LoadScene("SampleScene3");
        }
        // Replace "GameScene" with the name of your actual game scene
    }
}
