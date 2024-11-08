using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelection1 : MonoBehaviour
{
    // Called when the player clicks on a character button
    public void SelectCharacter(int index)
    {
        // Save the selected character's index in PlayerPrefs
        PlayerPrefs.SetInt("Gamemode", index);

        // Optionally, load the game scene after selection
        LoadGame(index);
    }

    // Method to load the game scene
    public void LoadGame(int index)
    {
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
            SceneManager.LoadScene("SampleScene2");
        }
        if (index == 4)
        {
            SceneManager.LoadScene("SampleScene3");
        }
        // Replace "GameScene" with the name of your actual game scene
    }
}
