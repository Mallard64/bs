using UnityEngine;
using TMPro;  // Required if you're using TextMeshPro, remove if you're using standard Text

public class EnemyHealthUI : MonoBehaviour
{
    public Enemy enemy;             // Reference to the Enemy script (attach in Inspector)
    public TextMeshProUGUI healthText;  // Reference to the TextMeshProUGUI component (or Text if using basic UI Text)
    public Vector3 offset = new Vector3(0, 1, 0);  // Offset from the enemy (e.g., display text above the enemy)

    void Update()
    {
        // Follow the enemy's position
        transform.position = enemy.transform.position + offset;

        // Update the text to show the enemy's current health
        healthText.text = "HP: " + enemy.health.ToString();
    }
}
