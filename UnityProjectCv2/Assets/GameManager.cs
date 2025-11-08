using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// GameManager
/// This is the largest script and it controls the entire flow of the multiplayer moral decision game
/// It handles player roles, round logic, UI display, sound/particle effects,
/// and scene transitions (Win/Game Over). 
/// Networking is managed through:
///   -Synchronizing shared variables between host/client (via [Networked] properties)
///  - Calling remote actions (via [Rpc] functions)
///  - Managing turn-based flow through StateAuthority ownership
/// Since it is so large, I've done my best to split up each section clearly
/// 
/// Resources used to make code:
/// "How to make a Quiz Game in Unity (E02. CODE)- tutorial"- Brackeys on Youtube
/// "How to create a Multiplayer Card Game in Unity" 12-14- Humble Toymaker on Youtube (used to visualize turn system)
/// "Remote Procedure Calls"- Photon Fusion Manual/Data Transfer/Remote Procedure Calls
///  Unity UI Referencs Canvas Components- Unity Manual
///  Coroutines- Unity Learn Tutorials
///  "netcode-sample-photon-fusion-2" menu section- edgemap on GitHub
///  
/// </summary>

public class GameManager : NetworkBehaviour
{
    //stores one question and its 3 answer choices
    //storing it this way helps with organization and makes syncing questions simpler.
    [System.Serializable]
    public class QuestionData
    {
        public string question;
        public string[] answers;
    }

    //WHY: UI Prefabs and References
    //these hold references to the game’s UI canvases
    //in multiplayer, each client will instantiate their own local UI (not shared across the network)
    //while game state is shared via networked variables and RPCs
    [Header("UI References")]
    [SerializeField] private GameObject gameCanvasPrefab; //main game UI for choices
    [SerializeField] private GameObject decisionCanvasPrefab; //UI that gives feedback on match/mismatch results
    [SerializeField] private GameObject mainCanvasUI; //main player UI showing trial round and health score

    private SceneLoader SceneLoader;
    private GameObject gameCanvasInstance;
    private GameObject decisionCanvasInstance;
    private CanvasGroup gameCanvasGroup;

    //UI element referenes
    private TMP_Text questionText;
    private Button[] answerButtons;
    private List<int> usedQuestionIndices = new List<int>(); //tracks which questions have been used


    //WHY: Triggered through RPCs to ensure both clients hear/see the same response at the same time
    //Audio and particle system feedback references 
    private AudioSource AudioCorrect;
    private AudioSource AudioIncorrect;
    private ParticleSystem CorrectParticle;
    private ParticleSystem IncorrectParticle;

    //Decision Canvas elements to show match/mismatch and which player chose what
    private TMP_Text decisionMatchText;
    private TMP_Text chooserChoiceText;
    private TMP_Text deciderChoiceText;
    private Button nextQuestionButton;
    private TMP_Text roleText;

    //Main player canvas elements
    private TMP_Text trialRoundText;
    private TMP_Text healthTrackerText;

    //WHY: Networked Variables
    //[Networked] properties are automatically synchronized between host and clients
    //only the host (StateAuthority) can modify them so clients receive the updated values
    //these variables define the shared state of the game for all players
    //the networked variables that are synced across all players
    [Networked] private int currentQuestionIndex { get; set; }
    [Networked] private int currentRound { get; set; } = 1; //starts the trial round at 1
    [Networked] private bool isPlayer1Chooser { get; set; }
    [Networked] private PlayerRef player1 { get; set; }
    [Networked] private PlayerRef player2 { get; set; }

    //WHY: Local Variables
    //these only store temporary local info that doesn’t need to be synced 
    //ex. individual player inputs (like which button was clicked) are local until
    //submitted via RPC to the host
    private string chooserChoice = "";
    private string deciderChoice = "";
    private int deciderHealth = 100;
    private QuestionData[] questions;


    //------------------------------------ GAME SETUP---------------------------------------------//
    private void Awake()
    {
        //this defines all of the morality questions and their answers
        //WHY: This initializes all morality questions before the game begins
        //these are stored locally since they never change
        //only the index of the current question gets synced
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
            new QuestionData { question = "You can erase someone's painful memories. Do you?", answers = new string[] { "Yes", "No", "Depends" } },
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
        //Game Canvas setup where the questions are shwon
        if (gameCanvasPrefab != null)
        {
            gameCanvasInstance = Instantiate(gameCanvasPrefab);
            Debug.Log("[GameManager] Spawned local GameCanvas prefab");
            gameCanvasGroup = gameCanvasInstance.GetComponent<CanvasGroup>();

            // Make UI invisible but still running so player can instead open with terminal button
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
            decisionCanvasInstance.SetActive(false); //Hide until a match/mismatch occurs
        }

        //Find and hook GameCanvas elements
        //WHY: UI references are linked this way instead of in the Inspector
        //because this script runs on both clients at runtime and needs to find its own copies
        questionText = gameCanvasInstance.transform.Find("GamePanel/QuestionText")?.GetComponent<TMP_Text>();
        roleText = gameCanvasInstance.transform.Find("GamePanel/RoleText")?.GetComponent<TMP_Text>();

        //Setup for the three answer buttons
        answerButtons = new Button[3];
        answerButtons[0] = gameCanvasInstance.transform.Find("GamePanel/AnswerButton1")?.GetComponent<Button>();
        answerButtons[1] = gameCanvasInstance.transform.Find("GamePanel/AnswerButton2")?.GetComponent<Button>();
        answerButtons[2] = gameCanvasInstance.transform.Find("GamePanel/AnswerButton3")?.GetComponent<Button>();

        //Hook DecisionCanvas UI elements
        decisionMatchText = decisionCanvasInstance.transform.Find("Panel/DecisionText")?.GetComponent<TMP_Text>();
        chooserChoiceText = decisionCanvasInstance.transform.Find("Panel/ChooserChoice")?.GetComponent<TMP_Text>();
        deciderChoiceText = decisionCanvasInstance.transform.Find("Panel/DeciderChoice")?.GetComponent<TMP_Text>();
        nextQuestionButton = decisionCanvasInstance.transform.Find("Panel/NextQuestion")?.GetComponent<Button>();

        //Button for continuing to the next question
        if (nextQuestionButton != null)
            nextQuestionButton.onClick.AddListener(OnNextQuestionClicked);

        //Hook Terminal Button
        //linking it to GameManager allows it to open and close the right canvas
        var terminalButton = FindFirstObjectByType<TerminalButton>();
        if (terminalButton != null)
        {
            terminalButton.AssignGameManager(this);
            Debug.Log("[GameManager] Linked TerminalButton to GameManager");
        }

        //Hook Exit Button that closes the game canvas if players choose but doesnt stop or break the game
        var exitButton = gameCanvasInstance.transform.Find("GamePanel/ExitButton")?.GetComponent<Button>();
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(() => ToggleGameCanvas(false));
            Debug.Log("[GameManager] ExitButton linked to hide GameCanvas");
        }

        //Find and hook MainCanvasUI elements (health and round count)
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


        //find the audio and particle effects if null
        //these play through RPC calls so both players hear and see the same effects 
        if (AudioCorrect == null)
            AudioCorrect = GameObject.Find("AudioCorrect")?.GetComponent<AudioSource>();

        if (AudioIncorrect == null)
            AudioIncorrect = GameObject.Find("AudioIncorrect")?.GetComponent<AudioSource>();

        if (CorrectParticle == null)
            CorrectParticle = GameObject.Find("CorrectParticle")?.GetComponent<ParticleSystem>();

        if (IncorrectParticle == null)
            IncorrectParticle = GameObject.Find("IncorrectParticle")?.GetComponent<ParticleSystem>();

        SceneLoader = FindAnyObjectByType<SceneLoader>();
    }


    //-------------------------------- GAME FLOW --------------------------------------//


    //controls showing/hiding the player's terminal UI
    public void ToggleGameCanvas(bool show)
    {
        if (gameCanvasGroup == null) return;

        gameCanvasGroup.alpha = show ? 1f : 0f;
        gameCanvasGroup.interactable = show;
        gameCanvasGroup.blocksRaycasts = show;
    }

    //this is called when the GameManager spawns in Fusion
    public override void Spawned()
    {
        //only the host starts the countdown and manages the game state
        if (Object.HasStateAuthority)
            StartCoroutine(WaitForPlayersAndStart());
    }

    //Wait until bith players are connected before starting
    //the intro sequence of instructions hides this occurring in the scene
    private IEnumerator WaitForPlayersAndStart()
    {
        while (Runner.ActivePlayers.Count() < 2)
            yield return null;

        //assigns the first two players and starts Round 1
        var players = Runner.ActivePlayers.ToList();
        player1 = players[0];
        player2 = players[1];
        isPlayer1Chooser = true; //player 1 starts as the chooser
        currentRound = 1;

        Debug.Log("[GameManager] Both players joined. Starting game...");
        StartNewRound(); //begin the first question trial round
        UpdateMainUI(); //update the shared display
    }

    //---------------------------------- QUESTION HANDLING --------------------------------//

    //Starts a new morality question trial round
    private void StartNewRound(int forcedQuestionIndex = -1)
    {
        if (currentRound > 10)
        {
            Debug.Log("[GameManager] Game over after 10 rounds!");
            return;
        }

        int newQuestionIndex = forcedQuestionIndex;

        //Host will pick a random unused question
        if (Object.HasStateAuthority && forcedQuestionIndex == -1)
        {
            //builds a list of unused question indices to ensure variety each round
            var availableIndices = Enumerable.Range(0, questions.Length)
                .Where(i => !usedQuestionIndices.Contains(i))
                .ToList();

            if (availableIndices.Count == 0)
            {
                usedQuestionIndices.Clear(); //reset if all are used
                availableIndices = Enumerable.Range(0, questions.Length).ToList();
            }

            //pick one random question and record it as used.
            newQuestionIndex = availableIndices[Random.Range(0, availableIndices.Count)];
            usedQuestionIndices.Add(newQuestionIndex);

            currentQuestionIndex = newQuestionIndex;
            chooserChoice = "";
            deciderChoice = "";

            //tell all clients to update UI with a new question
            RPC_UpdateUI(currentQuestionIndex, isPlayer1Chooser);
        }
        else if (forcedQuestionIndex != -1)
        {
            //if there is already a question index, just update local UI
            //only for those who aren't the host so the questions remain in sync
            currentQuestionIndex = forcedQuestionIndex;
            chooserChoice = "";
            deciderChoice = "";
            UpdateUI(currentQuestionIndex);
        }
    }


    //called by the host, this runs on all clients to sync questions and roles
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateUI(int questionIndex, bool player1Chooser)
    {
        isPlayer1Chooser = player1Chooser;
        UpdateUI(questionIndex);
    }

    //updates the local UI text and answer buttons
    private void UpdateUI(int questionIndex)
    {
        if (questionText == null) return;

        questionText.text = questions[questionIndex].question;

        //assign answer text and button behavior
        for (int i = 0; i < answerButtons.Length; i++)
        {
            int choiceIndex = i;
            var text = answerButtons[i].GetComponentInChildren<TMP_Text>();
            text.text = questions[questionIndex].answers[i];
            answerButtons[i].onClick.RemoveAllListeners();

            //assign the button click logic
            answerButtons[i].onClick.AddListener(() =>
            {
                var localPlayer = Runner.LocalPlayer;
                bool isChooser = (isPlayer1Chooser && localPlayer == player1) || (!isPlayer1Chooser && localPlayer == player2);

                if (isChooser)
                    SubmitChooserChoice(localPlayer, choiceIndex);
                else
                    SubmitDeciderChoice(localPlayer, choiceIndex);
            });

            answerButtons[i].interactable = false; //disable until ready
        }

        StartCoroutine(EnableButtonsWhenReady());
    }


    //waits until player info is loaded before enabling buttons
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


    //------------------------------------- PLAYER CHOICE LOGIC ----------------------------------//

    //chooser selects their answer
    public void SubmitChooserChoice(PlayerRef player, int answerIndex)
    {
        //only accepts the first chouce to avoid possible duplicate clicks
        if (chooserChoice != "") return;
        chooserChoice = questions[currentQuestionIndex].answers[answerIndex];
        Debug.Log($"[Chooser] {player} chose: {chooserChoice}");
        RPC_SubmitChooserChoiceToHost(player, chooserChoice); //send the choice to host
    }

    //send's the chooser's choice to host for sync
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitChooserChoiceToHost(PlayerRef player, string choice)
    {
        chooserChoice = choice;
        RPC_EnableDeciderButtons(); //lets the decider make their decision next
    }


    //Host tells all clients that the decider can now respond
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


    //decider makes their choice
    public void SubmitDeciderChoice(PlayerRef player, int answerIndex)
    {
        if (deciderChoice != "") return;
        deciderChoice = questions[currentQuestionIndex].answers[answerIndex];
        Debug.Log($"[Decider] {player} chose: {deciderChoice}");
        RPC_SubmitDeciderChoiceToHost(player, deciderChoice);
    }

    //sends the decider choice to host
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SubmitDeciderChoiceToHost(PlayerRef player, string choice)
    {
        deciderChoice = choice;
        CheckAnswers(); //compares both of the choices
    }


    //comapres the player answers and adjusts health
    private void CheckAnswers()
    {
        bool isMatch = chooserChoice == deciderChoice; //compares both choices

        //adjusts health and triggers feedback
        if (!isMatch)
        {
            RPC_PlayIncorrectParticle(); //calls particle system on decision action
            RPC_PlayIncorrectSound();    //calls sound effects on decision action
            deciderHealth -= 25;
            Debug.Log($" Mismatch! Decider loses 10 health. Remaining: {deciderHealth}");
        }
        else
        {
            RPC_PlayCorrectParticle();
            RPC_PlayCorrectSound();
            deciderHealth += 10;
            Debug.Log(" Match!");
        }

        //shows results to both of the players 
        RPC_ShowDecision(isMatch, chooserChoice, deciderChoice, deciderHealth);

        // Notify all clients to update main UI
        if (Object.HasStateAuthority)
        {
            RPC_UpdateHealthUI(deciderHealth);
        }


        //check the win/lose conditions
        if (Object.HasStateAuthority)
        {
            if (deciderHealth <= 0)
                RPC_TriggerGameOver();
            else if (currentRound >= 10)
                RPC_TriggerWin();
        }
    }


    //------------------------------------ UI AND FEEDBACK --------------------------------------------//

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

            //picks a random unused question
            var availableIndices = Enumerable.Range(0, questions.Length)
                .Where(i => !usedQuestionIndices.Contains(i))
                .ToList();

            if (availableIndices.Count == 0)
            {
                usedQuestionIndices.Clear();
                availableIndices = Enumerable.Range(0, questions.Length).ToList();
            }

            int newIndex = availableIndices[Random.Range(0, availableIndices.Count)];
            usedQuestionIndices.Add(newIndex);

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

    //Sound effect and particle system RPCS//
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayCorrectSound()
    {
        AudioCorrect?.Play();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayIncorrectSound()
    {
        AudioIncorrect?.Play();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayCorrectParticle()
    {
        CorrectParticle?.Play();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayIncorrectParticle()
    {
        IncorrectParticle?.Play();
    }


    //Win/Loss trigger RPCS
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerWin()
    {
        Debug.Log("[GameManager] RPC_TriggerWin called on client");
        StartCoroutine(DelayedSceneLoad("Win"));
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_TriggerGameOver()
    {
        Debug.Log("[GameManager] RPC_TriggerGameOver called on client");
        StartCoroutine(DelayedSceneLoad("GameOver"));
    }

    private IEnumerator DelayedSceneLoad(string type)
    {
        //wait to let audio/particles play before transitioning
        yield return new WaitForSeconds(0.2f);

        var loader = FindAnyObjectByType<SceneLoader>();
        if (loader != null)
        {
            //helper functions instead of direct scene manager calls 
            if (type == "Win")
                loader.OpenWin();
            else if (type == "GameOver")
                loader.OpenGameOver();
        }
        else
        {
            Debug.LogWarning("[GameManager] SceneLoader not found on this client.");
        }
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