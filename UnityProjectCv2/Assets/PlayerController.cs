using Fusion;
using UnityEngine;

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