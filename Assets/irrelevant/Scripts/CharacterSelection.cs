using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelection : MonoBehaviour
{
    // Called when the player clicks on a character button
    public void SelectCharacter(int index)
    {
        // Save the selected character's index in PlayerPrefs
        PlayerPrefs.SetInt("SelectedCharacter", index);

        // Optionally, load the game scene after selection
        LoadGame();
    }

    // Method to load the game scene
    public void LoadGame()
    {
        SceneManager.LoadScene("select 1");  // Replace "GameScene" with the name of your actual game scene
    }
}
