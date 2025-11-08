using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class PlayerAssessmentManager : NetworkBehaviour
{
    [Header("Player 1 Sliders")]
    public Slider[] player1Sliders = new Slider[4];

    [Header("Player 2 Sliders")]
    public Slider[] player2Sliders = new Slider[4];

    [Header("Buttons")]
    public Button sendButton;
    public Button closeButton;

    // Networked variables for sliders (these are synchronized across clients)
    [Networked] private float p1Slider1 { get; set; }
    [Networked] private float p1Slider2 { get; set; }
    [Networked] private float p1Slider3 { get; set; }
    [Networked] private float p1Slider4 { get; set; }

    [Networked] private float p2Slider1 { get; set; }
    [Networked] private float p2Slider2 { get; set; }
    [Networked] private float p2Slider3 { get; set; }
    [Networked] private float p2Slider4 { get; set; }

    private void Awake()
    {
        sendButton.onClick.AddListener(OnSendPressed);
        closeButton.onClick.AddListener(OnClosePressed);

        // Assign slider change listeners
        for (int i = 0; i < player1Sliders.Length; i++)
        {
            int index = i;
            player1Sliders[i].onValueChanged.AddListener(val => OnSliderChanged(true, index, val));
        }

        for (int i = 0; i < player2Sliders.Length; i++)
        {
            int index = i;
            player2Sliders[i].onValueChanged.AddListener(val => OnSliderChanged(false, index, val));
        }
    }

    // This is called when a slider is changed. We update the corresponding networked variable.
    private void OnSliderChanged(bool isPlayer1, int index, float value)
    {
        if (!Object.HasInputAuthority) return; // Only allow the player with authority to move sliders.

        // Update networked value based on which player owns this slider
        if (isPlayer1)
        {
            switch (index)
            {
                case 0: p1Slider1 = value; break;
                case 1: p1Slider2 = value; break;
                case 2: p1Slider3 = value; break;
                case 3: p1Slider4 = value; break;
            }
        }
        else
        {
            switch (index)
            {
                case 0: p2Slider1 = value; break;
                case 1: p2Slider2 = value; break;
                case 2: p2Slider3 = value; break;
                case 3: p2Slider4 = value; break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Update visual slider positions from networked values.
        if (!Object.HasInputAuthority)
        {
            // Update Player 1 sliders
            player1Sliders[0].value = p1Slider1;
            player1Sliders[1].value = p1Slider2;
            player1Sliders[2].value = p1Slider3;
            player1Sliders[3].value = p1Slider4;

            // Update Player 2 sliders
            player2Sliders[0].value = p2Slider1;
            player2Sliders[1].value = p2Slider2;
            player2Sliders[2].value = p2Slider3;
            player2Sliders[3].value = p2Slider4;
        }
    }

    private void OnSendPressed()
    {
        Debug.Log("Send pressed! Values sent across network.");
        // You can trigger any server-side events or logic here
    }

    private void OnClosePressed()
    {
        Debug.Log("Close pressed! Hiding assessment panel.");
        gameObject.SetActive(false); // Close the assessment panel
    }

    private void OnDestroy()
    {
        sendButton.onClick.RemoveListener(OnSendPressed);
        closeButton.onClick.RemoveListener(OnClosePressed);
    }
}
