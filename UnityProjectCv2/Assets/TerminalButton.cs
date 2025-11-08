using UnityEngine;

//simple script for the player to be able to open the canvas holding the game's main UI 


public class TerminalButton : MonoBehaviour
{
    //references the main GameManager script that controls the game's flow
    private GameManager gameManager;

    //Called from the GameManager
    //Assigns the GameManager reference to the button
    public void AssignGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    //this function will run whenever the player clicks the terminal button in the scene
    //it tells the GameManager to open the player's UI panel
    public void OnTerminalClicked()
    {
        //makes sure to check if the reference exists before trying to use it
        if (gameManager != null)
        {
            Debug.Log("[TerminalButton] Showing local UI");
            gameManager.ToggleGameCanvas(true); //opens the game canvas
        }
        else
        {
            Debug.LogWarning("[TerminalButton] No GameManager assigned");
        }
    }
}