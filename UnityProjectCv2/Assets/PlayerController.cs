using Fusion;
using UnityEngine;



//original version of PlayerController that used number key inputs (1, 2, 3) for testing
//was replaced by UI button callbacks in game manager
//keeping this script for documentation and reference only


public class PlayerController : NetworkBehaviour
{
    public struct PlayerInputData : INetworkInput
    {
        public int choiceIndex;
        public bool submitPressed;
    }

    private GameManager gameManager;
    private PlayerInputData currentInput;

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (!Object.HasInputAuthority)
            return;

        currentInput.submitPressed = false;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentInput.choiceIndex = 0;
            currentInput.submitPressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentInput.choiceIndex = 1;
            currentInput.submitPressed = true;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentInput.choiceIndex = 2;
            currentInput.submitPressed = true;
        }

        input.Set(currentInput);
    }
}