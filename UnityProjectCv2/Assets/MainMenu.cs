using UnityEngine;
using UnityEngine.SceneManagement;

//simple script inside of the LobbyScene scene
//this is attached to the Join buttons in the canvas
//allows the player to start the game in the main GameScene

public class MainMenu : MonoBehaviour
{
    //Called when the player clicks the Join button
    //transitions from the LobbyScene to the GameScene
    public void OnPlayButtonClicked()
    {
        //Loads the main gameplay scene
        //this scene name matches exactly as it appears in the build profile
        SceneManager.LoadScene("GameScene");
    }
}
