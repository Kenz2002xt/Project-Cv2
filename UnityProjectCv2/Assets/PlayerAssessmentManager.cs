using UnityEngine;
using UnityEngine.UI;
using Fusion;

public class PlayerAssessmentManager : NetworkBehaviour
{
    // Networked slider values for two players
    [Networked] public float player1Rational { get; set; }
    [Networked] public float player1Confident { get; set; }
    [Networked] public float player1Honest { get; set; }
    [Networked] public float player1Empathetic { get; set; }

    [Networked] public float player2Rational { get; set; }
    [Networked] public float player2Confident { get; set; }
    [Networked] public float player2Honest { get; set; }
    [Networked] public float player2Empathetic { get; set; }

    // UI references
    private GameObject selfAssessmentPanel;

    private Slider p1RationalSlider;
    private Slider p1ConfidentSlider;
    private Slider p1HonestSlider;
    private Slider p1EmpatheticSlider;

    private Slider p2RationalSlider;
    private Slider p2ConfidentSlider;
    private Slider p2HonestSlider;
    private Slider p2EmpatheticSlider;

    private Button closeButton;
    private Button terminalButton;

    public override void Spawned()
    {
        // Only the local player sets up UI
        if (!Object.HasInputAuthority) return;

        selfAssessmentPanel = GameObject.Find("selfAssessment/selfAssessmentPanel");
        if (selfAssessmentPanel == null)
        {
            Debug.LogError("selfAssessmentPanel not found in scene!");
            return;
        }

        SetupSliders();
        SetupButtons();

        // Show the panel by default
        selfAssessmentPanel.SetActive(true);
    }

    private void SetupSliders()
    {
        // Player 1 sliders
        p1RationalSlider = selfAssessmentPanel.transform.Find("Player1Rational")?.GetComponent<Slider>();
        p1ConfidentSlider = selfAssessmentPanel.transform.Find("Player1Confident")?.GetComponent<Slider>();
        p1HonestSlider = selfAssessmentPanel.transform.Find("Player1Honest")?.GetComponent<Slider>();
        p1EmpatheticSlider = selfAssessmentPanel.transform.Find("Player1Empathetic")?.GetComponent<Slider>();

        // Player 2 sliders
        p2RationalSlider = selfAssessmentPanel.transform.Find("Player2Rational")?.GetComponent<Slider>();
        p2ConfidentSlider = selfAssessmentPanel.transform.Find("Player2Confident")?.GetComponent<Slider>();
        p2HonestSlider = selfAssessmentPanel.transform.Find("Player2Honest")?.GetComponent<Slider>();
        p2EmpatheticSlider = selfAssessmentPanel.transform.Find("Player2Empathetic")?.GetComponent<Slider>();

        // Add listeners to update networked variables when sliders move
        if (p1RationalSlider != null) p1RationalSlider.onValueChanged.AddListener(v => player1Rational = v);
        if (p1ConfidentSlider != null) p1ConfidentSlider.onValueChanged.AddListener(v => player1Confident = v);
        if (p1HonestSlider != null) p1HonestSlider.onValueChanged.AddListener(v => player1Honest = v);
        if (p1EmpatheticSlider != null) p1EmpatheticSlider.onValueChanged.AddListener(v => player1Empathetic = v);

        if (p2RationalSlider != null) p2RationalSlider.onValueChanged.AddListener(v => player2Rational = v);
        if (p2ConfidentSlider != null) p2ConfidentSlider.onValueChanged.AddListener(v => player2Confident = v);
        if (p2HonestSlider != null) p2HonestSlider.onValueChanged.AddListener(v => player2Honest = v);
        if (p2EmpatheticSlider != null) p2EmpatheticSlider.onValueChanged.AddListener(v => player2Empathetic = v);
    }

    private void SetupButtons()
    {
        // Close button inside the panel
        closeButton = selfAssessmentPanel.transform.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton != null)
            closeButton.onClick.AddListener(() => selfAssessmentPanel.SetActive(false));

        // Terminal button somewhere else in scene
        terminalButton = GameObject.Find("TerminalButton")?.GetComponent<Button>();
        if (terminalButton != null)
            terminalButton.onClick.AddListener(() => selfAssessmentPanel.SetActive(true));
    }

    private void Update()
    {

        // Keep sliders synced with networked values
        if (p1RationalSlider != null) p1RationalSlider.value = player1Rational;
        if (p1ConfidentSlider != null) p1ConfidentSlider.value = player1Confident;
        if (p1HonestSlider != null) p1HonestSlider.value = player1Honest;
        if (p1EmpatheticSlider != null) p1EmpatheticSlider.value = player1Empathetic;

        if (p2RationalSlider != null) p2RationalSlider.value = player2Rational;
        if (p2ConfidentSlider != null) p2ConfidentSlider.value = player2Confident;
        if (p2HonestSlider != null) p2HonestSlider.value = player2Honest;
        if (p2EmpatheticSlider != null) p2EmpatheticSlider.value = player2Empathetic;
    }
}