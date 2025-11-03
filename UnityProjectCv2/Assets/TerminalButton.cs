using UnityEngine;

public class TerminalButton : MonoBehaviour
{
    private GameManager gameManager;

    public void AssignGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    public void OnTerminalClicked()
    {
        if (gameManager != null)
        {
            Debug.Log("[TerminalButton] Showing local UI");
            gameManager.ToggleGameCanvas(true);
        }
        else
        {
            Debug.LogWarning("[TerminalButton] No GameManager assigned");
        }
    }
}