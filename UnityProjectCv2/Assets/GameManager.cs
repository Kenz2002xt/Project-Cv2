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
            new QuestionData { question = "You see someone stealing food to survive. What do you do?", answers = new string[] { "Report them", "Help them", "Ignore it" } },
            new QuestionData { question = "Is revenge ever justified?", answers = new string[] { "Yes", "No", "Rarely" } },
            new QuestionData { question = "Is killing in self-defense moral?", answers = new string[] { "Yes", "No", "Last Resort" } },
            new QuestionData { question = "Should people be judged for their past mistakes", answers = new string[] { "Always", "Rarely", "Mostly" } },
            new QuestionData { question = "What matters most: truth, loyalty, kindness", answers = new string[] { "Truth", "Loyalty", "Kindness" } },
            new QuestionData { question = "What defines a good person?", answers = new string[] { "Intentions", "Actions", "Impact" } },
            new QuestionData { question = "What is worse: greed, cruelty, cowardice", answers = new string[] { "Greed", "Cruelty", "Cowardice" } },
            new QuestionData { question = "Is it better to be feared or respected?", answers = new string[] { "Feared", "Respected", "Neither" } },
            new QuestionData { question = "You can erase someone's painful memories. Do you?", answers = new string[] { "Yesr", "No", "Depends" } },
            new QuestionData { question = "You can save yourself or a child from drowning. Who lives?", answers = new string[] { "Myself", "The Child", "Neither" } },
            new QuestionData { question = "Would you tell a painful truth or a comforting lie?", answers = new string[] { "Truth", "Lie", "Neither" } },
            new QuestionData { question = "Your commander hides a radiation leak to avoid panic", answers = new string[] { "Expose it", "Stay Silent", "Confront" } },
            new QuestionData { question = "An unknown signal could be a distress call or a trap", answers = new string[] { "Investigate", "Ignore", "Scan" } },
            new QuestionData { question = "You can send data home or save power for life support", answers = new string[] { "Send", "Save", "Ask Crew" } },
            new QuestionData { question = "Purge 10% of the population to preserve oxygen?", answers = new string[] { "Approve", "Deny", "5%" } },
            new QuestionData { question = "You can donate a kidney to save a stranger, but you may die", answers = new string[] { "Donate", "Refuse", "Wait" } },
            new QuestionData { question = "You can save a drowning stranger or a group of pets", answers = new string[] { "Stranger", "Pets", "Try Both" } },
            new QuestionData { question = "You can manipulate someone to achieve a good outcome", answers = new string[] { "Do it", "Don't", "Depends" } },
            new QuestionData { question = "How do you respond to betrayal?", answers = new string[] { "Forgive", "Revenge", "Ignore" } },
            new QuestionData { question = "What is your approach to failure in others?", answers = new string[] { "Guide", "Abandon", "Observe" } },
            new QuestionData { question = "Which emotion drives your response to jealousy?", answers = new string[] { "Anger", "Envy", "Motivation" } },
            new QuestionData { question = "What is your response to personal failure?", answers = new string[] { "Reflect", "Blame", "Ignore" } },
            new QuestionData { question = "What is your response to fear?", answers = new string[] { "Fight", "Flight", "Freeze" } },
            new QuestionData { question = "What is your response to authority you disagree with?", answers = new string[] { "Challenge", "Obey", "Subvert" } }
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