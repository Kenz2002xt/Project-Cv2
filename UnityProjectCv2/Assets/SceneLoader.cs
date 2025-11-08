using UnityEngine;
using UnityEngine.SceneManagement;

//system to load in scenes
//Taken from Project B's scene managment script 
//used to attach to buttons throughout gameplay


//PlayGame()- Loads the LobbyScene when the player starts or joins the game
//OpenControls()- Loads the ControlsScene for showing game instructions
//OpenCredits()- Loads the CreditsScene to display game credits
//OpenMainMenu()- Loads the MainMenuScene for returning to the main menu
// OpenWin()- Loads the WinScene id the players succeeds in the game
//OpenGameOver()- Loads the GameOverScene if the players fails

public class SceneLoader : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    public void OpenControls()
    {
        SceneManager.LoadScene("ControlsScene");
    }

    public void OpenCredits()
    {
        SceneManager.LoadScene("CreditsScene");
    }

    public void OpenMainMenu()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    public void OpenWin()
    {
        SceneManager.LoadScene("WinScene");
    }

    public void OpenGameOver()
    {
        SceneManager.LoadScene("GameOverScene");
    }
}
