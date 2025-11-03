using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

//
//Will add comments later and tutortials used to create
//
//

public class GameManager : NetworkBehaviour
{
    [System.Serializable]
    public class QuestionData
    {
        public string question;
        public string[] answers;
    }

    [Header("UI References")]
    [SerializeField] private GameObject gameCanvasPrefab;
    [SerializeField] private GameObject decisionCanvasPrefab;
    [SerializeField] private GameObject mainCanvasUI; 

    private GameObject gameCanvasInstance;
    private GameObject decisionCanvasInstance;
    private CanvasGroup gameCanvasGroup;

    private TMP_Text questionText;
    private Button[] answerButtons;

    // Decision Canvas elements
    private TMP_Text decisionMatchText;
    private TMP_Text chooserChoiceText;
    private TMP_Text deciderChoiceText;
    private Button nextQuestionButton;
    private TMP_Text roleText;

    // Main Canvas elements
    private TMP_Text trialRoundText;
    private TMP_Text healthTrackerText;

    [Networked] private int currentQuestionIndex { get; set; }
    [Networked] private int currentRound { get; set; } = 1;
    [Networked] private bool isPlayer1Chooser { get; set; }
    [Networked] private PlayerRef player1 { get; set; }
    [Networked] private PlayerRef player2 { get; set; }

    private string chooserChoice = "";
    private string deciderChoice = "";
    private int deciderHealth = 100;

    private QuestionData[] questions;

    private void Awake()
    {
        questions = new QuestionData[]
        {
            new QuestionData { question = "Would you sacrifice one person to save five?", answers = new string[] { "Yes", "No", "Depends" } },
            new QuestionData { question = "Would you tell a painful truth or a comforting lie?", answers = new string[] { "Truth", "Lie", "Neither" } },
            new QuestionData { question = "You see someone stealing food to survive. What do you do?", answers = new string[] { "Report them", "Help them", "Ignore it" } }
        };
    }

    private void Start()
    {
        //Game Canvas setup
        if (gameCanvasPrefab != null)
        {
            gameCanvasInstance = Instantiate(gameCanvasPrefab);
            Debug.Log("[GameManager] Spawned local GameCanvas prefab");
            gameCanvasGroup = gameCanvasInstance.GetComponent<CanvasGroup>();

            // Make UI invisible but still running
            if (gameCanvasGroup != null)
            {
                gameCanvasGroup.alpha = 0f;
                gameCanvasGroup.interactable = false;
                gameCanvasGroup.blocksRaycasts = false;
            }
        }

        //Decision Canvas setup
        if (decisionCanvasPrefab != null)
        {
            decisionCanvasInstance = Instantiate(decisionCanvasPrefab);
            decisionCanvasInstance.SetActive(false);
        }

        //Hook GameCanvas elements
        questionText = gameCanvasInstance.transform.Find("GamePanel/QuestionText")?.GetComponent<TMP_Text>();
        roleText = gameCanvasInstance.transform.Find("GamePanel/RoleText")?.GetComponent<TMP_Text>();

        answerButtons = new Button[3];
        answerButtons[0] = gameCanvasInstance.transform.Find("GamePanel/AnswerButton1")?.GetComponent<Button>();
        answerButtons[1] = gameCanvasInstance.transform.Find("GamePanel/AnswerButton2")?.GetComponent<Button>();
        answerButtons[2] = gameCanvasInstance.transform.Find("GamePanel/AnswerButton3")?.GetComponent<Button>();

        //Hook DecisionCanvas elements
        decisionMatchText = decisionCanvasInstance.transform.Find("Panel/DecisionText")?.GetComponent<TMP_Text>();
        chooserChoiceText = decisionCanvasInstance.transform.Find("Panel/ChooserChoice")?.GetComponent<TMP_Text>();
        deciderChoiceText = decisionCanvasInstance.transform.Find("Panel/DeciderChoice")?.GetComponent<TMP_Text>();
        nextQuestionButton = decisionCanvasInstance.transform.Find("Panel/NextQuestion")?.GetComponent<Button>();

        if (nextQuestionButton != null)
            nextQuestionButton.onClick.AddListener(OnNextQuestionClicked);

        //Hook Terminal Button
        var terminalButton = FindFirstObjectByType<TerminalButton>();
        if (terminalButton != null)
        {
            terminalButton.AssignGameManager(this);
            Debug.Log("[GameManager] Linked TerminalButton to GameManager");
        }

        //Hook Exit Button
        var exitButton = gameCanvasInstance.transform.Find("GamePanel/ExitButton")?.GetComponent<Button>();
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() => ToggleGameCanvas(false));
            Debug.Log("[GameManager] ExitButton linked to hide GameCanvas");
        }

        //Hook MainCanvasUI elements
        var mainCanvas = GameObject.Find("MainCanvasUI");
        if (mainCanvas != null)
        {
            trialRoundText = mainCanvas.transform.Find("TrialRound")?.GetComponent<TMP_Text>();
            healthTrackerText = mainCanvas.transform.Find("HealthTracker")?.GetComponent<TMP_Text>();

            Debug.Log("[GameManager] Found MainCanvasUI elements in scene");
            UpdateMainUI(); // Initialize display
        }
        else
        {
            Debug.LogWarning("[GameManager] Could not find MainCanvasUI in scene");
        }
    }

    // Show or hide the player's UI
    public void ToggleGameCanvas(bool show)
    {
        if (gameCanvasGroup == null) return;

        gameCanvasGroup.alpha = show ? 1f : 0f;
        gameCanvasGroup.interactable = show;
        gameCanvasGroup.blocksRaycasts = show;
    }

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
            StartCoroutine(WaitForPlayersAndStart());
    }

    private IEnumerator WaitForPlayersAndStart()
    {
        while (Runner.ActivePlayers.Count() < 2)
            yield return null;

        var players = Runner.ActivePlayers.ToList();
        player1 = players[0];
        player2 = players[1];
        isPlayer1Chooser = true;
        currentRound = 1;

        Debug.Log("[GameManager] Both players joined. Starting game...");
        StartNewRound();
        UpdateMainUI();
    }

    // --- Host-only random question logic ---
    private void StartNewRound(int forcedQuestionIndex = -1)
    {
        if (currentRound > 10)
        {
            Debug.Log("[GameManager] Game over after 10 rounds!");
            return;
        }

        int newQuestionIndex = forcedQuestionIndex;

        if (Object.HasStateAuthority && forcedQuestionIndex == -1)
        {
            do
            {
                newQuestionIndex = Random.Range(0, questions.Length);
            }
            while (questions.Length > 1 && newQuestionIndex == currentQuestionIndex);

            currentQuestionIndex = newQuestionIndex;
            chooserChoice = "";
            deciderChoice = "";

            RPC_UpdateUI(currentQuestionIndex, isPlayer1Chooser);
        }
        else if (forcedQuestionIndex != -1)
        {
            currentQuestionIndex = forcedQuestionIndex;
            chooserChoice = "";
            deciderChoice = "";
            UpdateUI(currentQuestionIndex);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateUI(int questionIndex, bool player1Chooser)
    {
        isPlayer1Chooser = player1Chooser;
        UpdateUI(questionIndex);
    }

    private void UpdateUI(int questionIndex)
    {
        if (questionText == null) return;

        questionText.text = questions[questionIndex].question;

        for (int i = 0; i < answerButtons.Length; i++)
        {
            int choiceIndex = i;
            var text = answerButtons[i].GetComponentInChildren<TMP_Text>();
            text.text = questions[questionIndex].answers[i];
            answerButtons[i].onClick.RemoveAllListeners();

            answerButtons[i].onClick.AddListener(() =>
            {
                var localPlayer = Runner.LocalPlayer;
                bool isChooser = (isPlayer1Chooser && localPlayer == player1) || (!isPlayer1Chooser && localPlayer == player2);

                if (isChooser)
                    SubmitChooserChoice(localPlayer, choiceIndex);
                else
                    SubmitDeciderChoice(localPlayer, choiceIndex);
            });

            answerButtons[i].interactable = false;
        }

        StartCoroutine(EnableButtonsWhenReady());
    }

    private IEnumerator EnableButtonsWhenReady()
    {
        while (Runner == null || Runner.LocalPlayer.IsNone)
            yield return null;

        var localPlayer = Runner.LocalPlayer;
        bool isChooser = (isPlayer1Chooser && localPlayer == player1) || (!isPlayer1Chooser && localPlayer == player2);

        foreach (var btn in answerButtons)
            btn.interactable = isChooser;

        if (roleText != null)
            roleText.text = isChooser ? "You are the Chooser" : "You are the Decider";
    }

    public void SubmitChooserChoice(PlayerRef player, int answerIndex)
    {
        if (chooserChoice != "") return;
        chooserChoice = questions[currentQuestionIndex].answers[answerIndex];
        Debug.Log($"[Chooser] {player} chose: {chooserChoice}");
        RPC_SubmitChooserChoiceToHost(player, chooserChoice);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitChooserChoiceToHost(PlayerRef player, string choice)
    {
        chooserChoice = choice;
        RPC_EnableDeciderButtons();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_EnableDeciderButtons()
    {
        var localPlayer = Runner.LocalPlayer;
        bool isDecider = (isPlayer1Chooser && localPlayer == player2) || (!isPlayer1Chooser && localPlayer == player1);

        foreach (var btn in answerButtons)
            btn.interactable = isDecider;

        if (roleText != null)
            roleText.text = isDecider ? "You are the Decider" : "You are the Chooser";
    }

    public void SubmitDeciderChoice(PlayerRef player, int answerIndex)
    {
        if (deciderChoice != "") return;
        deciderChoice = questions[currentQuestionIndex].answers[answerIndex];
        Debug.Log($"[Decider] {player} chose: {deciderChoice}");
        RPC_SubmitDeciderChoiceToHost(player, deciderChoice);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitDeciderChoiceToHost(PlayerRef player, string choice)
    {
        deciderChoice = choice;
        CheckAnswers();
    }

    private void CheckAnswers()
    {
        bool isMatch = chooserChoice == deciderChoice;
        if (!isMatch)
        {
            deciderHealth -= 10;
            Debug.Log($" Mismatch! Decider loses 10 health. Remaining: {deciderHealth}");
        }
        else
        {
            Debug.Log(" Match!");
        }

        RPC_ShowDecision(isMatch, chooserChoice, deciderChoice, deciderHealth);

        // Notify all clients to update main UI
        if (Object.HasStateAuthority)
        {
            RPC_UpdateHealthUI(deciderHealth);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateHealthUI(int newHealth)
    {
        deciderHealth = newHealth;  // Update local copy
        UpdateMainUI();             // Refresh UI on this client
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowDecision(bool isMatch, string chooserAns, string deciderAns, int health)
    {
        gameCanvasInstance.SetActive(false);
        decisionCanvasInstance.SetActive(true);

        decisionMatchText.text = isMatch ? "MATCH" : "MISMATCH";
        chooserChoiceText.text = $"The Chooser: {chooserAns}";
        deciderChoiceText.text = $"The Decider: {deciderAns}";

        Debug.Log($"[Decision] {(isMatch ? "Match" : "Mismatch")} | Decider health: {health}");
    }

    private void OnNextQuestionClicked()
    {
        if (Object.HasStateAuthority)
        {
            currentRound++;
            isPlayer1Chooser = !isPlayer1Chooser;

            int newIndex;
            do
            {
                newIndex = Random.Range(0, questions.Length);
            }
            while (questions.Length > 1 && newIndex == currentQuestionIndex);

            currentQuestionIndex = newIndex;
            RPC_StartNextRound(currentRound, isPlayer1Chooser, newIndex);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_StartNextRound(int round, bool player1Chooser, int newQuestionIndex)
    {
        currentRound = round;
        isPlayer1Chooser = player1Chooser;

        decisionCanvasInstance.SetActive(false);
        gameCanvasInstance.SetActive(true);

        UpdateMainUI();
        StartNewRound(newQuestionIndex);
    }

    //Helper to update the shared UI
    private void UpdateMainUI()
    {
        if (trialRoundText != null)
            trialRoundText.text = $"Trial: {currentRound}/10";

        if (healthTrackerText != null)
            healthTrackerText.text = $"Health: {deciderHealth}";
    }
}