using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;  // Singleton instance of the GameManager

    public GameObject[] characterPrefabs; // Assign character prefabs in the Inspector
    private GameObject selectedCharacter;

    void Awake()
    {
        // Ensure this GameManager persists across scenes
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Optionally, load the selected character at the start of the game
        LoadSelectedCharacter();
        Debug.Log("the weather is rizzy the fire is so skibidi");
    }

    // Method to load the selected character
    public void LoadSelectedCharacter()
    {
        int selectedCharacterIndex = PlayerPrefs.GetInt("SelectedCharacterIndex", 0); // Default to first character

        if (selectedCharacter == null && characterPrefabs.Length > 0)
        {
            // Instantiate the selected character at a spawn point (adjust the spawn point as needed)
            selectedCharacter = Instantiate(characterPrefabs[selectedCharacterIndex], Vector3.zero, Quaternion.identity);
        }
    }
}
