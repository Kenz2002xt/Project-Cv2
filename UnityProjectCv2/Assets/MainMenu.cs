using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // This is attached to your "Play" or "Join Game" button
    public void OnPlayButtonClicked()
    {
        // Load the game scene when the player clicks "Join Game"
        SceneManager.LoadScene("GameScene");
    }
}
