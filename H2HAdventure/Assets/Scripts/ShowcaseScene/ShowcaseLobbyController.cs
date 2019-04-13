using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowcaseLobbyController : MonoBehaviour
{
    private const string PROPOSAL_TITLE_NO_OTHER = "Propose a game";
    private const string PROPOSAL_TITLE_OTHER = "offer a counter-proposal";

    private const string ACCEPT_TITLE_NO_OTHER = "The following game has been proposed: ";
    private const string ACCEPT_TITLE_OTHER = "Your game was rejected and a new one proposed:";

    private const string ACCEPT_SUPPLEMENT_NO_LIMIT = "or...";
    private const string ACCEPT_SUPPLEMENT_LIMIT = "you have {} seconds to accept";

    private const float TIME_TO_JOIN_2P_GAME = 10;
    private const float SCREENSAVER_TIMEOUT = 120;

    public ShowcaseController parent;
    public ShowcaseTransport xport;

    private GameObject proposalPanel;
    private Text proposalTitleText;
    private ToggleGroup gameBoardToggleGrp;
    private ToggleGroup numPlayersToggleGrp;
    private ToggleGroup difficulty1ToggleGrp;
    private ToggleGroup difficulty2ToggleGrp;
    private GameObject acceptPanel;
    private Text acceptTitleText;
    private Text acceptGameDescText;
    private Text acceptSupplementText;
    private GameObject waitPanel;
    private Text waitGameDescText;
    private GameObject leftOutPanel;

    private float timeToAccept = -1;
    public float idleTime = -1;

    // Start is called before the first frame update
    void Start()
    {
        proposalPanel = transform.Find("ProposalPanel").gameObject;
        proposalTitleText = transform.Find("ProposalPanel/DescText").gameObject.GetComponent<Text>();
        gameBoardToggleGrp = transform.Find("ProposalPanel/GameBoardToggleGroup").gameObject.GetComponent<ToggleGroup>();
        numPlayersToggleGrp = transform.Find("ProposalPanel/NumPlayersToggleGroup").gameObject.GetComponent<ToggleGroup>();
        difficulty1ToggleGrp = transform.Find("ProposalPanel/Difficulty1ToggleGroup").gameObject.GetComponent<ToggleGroup>();
        difficulty2ToggleGrp = transform.Find("ProposalPanel/Difficulty2ToggleGroup").gameObject.GetComponent<ToggleGroup>();
        acceptPanel = transform.Find("AcceptPanel").gameObject;
        acceptTitleText = transform.Find("AcceptPanel/DescText").gameObject.GetComponent<Text>();
        acceptGameDescText = transform.Find("AcceptPanel/GameDescriptionText").gameObject.GetComponent<Text>();
        acceptSupplementText = transform.Find("AcceptPanel/SupplementText").gameObject.GetComponent<Text>();
        waitPanel = transform.Find("WaitPanel").gameObject;
        waitGameDescText = transform.Find("WaitPanel/GameDescriptionText").gameObject.GetComponent<Text>();
        leftOutPanel = transform.Find("LeftOutPanel").gameObject;

        Reset();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.anyKeyDown)
        {
            idleTime = 0;
        }
        if (idleTime >= 0)
        {
            idleTime += Time.deltaTime;
            if (idleTime > SCREENSAVER_TIMEOUT)
            {
                parent.SetupHasBeenIdle();
            }
        }
        if (timeToAccept > 0)
        {
            int currentSeconds = (int)Mathf.Ceil(timeToAccept);
            timeToAccept -= Time.deltaTime;
            int newSeconds = (int)Mathf.Ceil(timeToAccept);
            if ((newSeconds < currentSeconds) && (currentSeconds > 1))
            {
                if (acceptSupplementText.text != ACCEPT_SUPPLEMENT_NO_LIMIT)
                {
                    acceptSupplementText.text = ACCEPT_SUPPLEMENT_LIMIT.Replace("{}", newSeconds.ToString());
                }
            }
        }
    }

    public void Reset()
    {
        proposalPanel.SetActive(true);
        proposalTitleText.text = PROPOSAL_TITLE_NO_OTHER;
        acceptPanel.SetActive(false);
        waitPanel.SetActive(false);
        leftOutPanel.SetActive(false);
        timeToAccept = -1;
        idleTime = 0;
    }

    // ----- Button and Other UI Handlers -----------------------------------------------------

    public void OnProposePressed()
    {
        IEnumerator<Toggle> enumerator = gameBoardToggleGrp.ActiveToggles().GetEnumerator();
        enumerator.MoveNext();
        Toggle selected = enumerator.Current;
        int gameBoard = (selected.gameObject.name == "GameBoard1Toggle" ? 0 :
            (selected.gameObject.name == "GameBoard2Toggle" ? 1 : 2));
        enumerator = numPlayersToggleGrp.ActiveToggles().GetEnumerator();
        enumerator.MoveNext();
        selected = enumerator.Current;
        int numPlayers = (selected.gameObject.name == "NumPlayers2Toggle" ? 2 : 3);
        enumerator = difficulty1ToggleGrp.ActiveToggles().GetEnumerator();
        enumerator.MoveNext();
        selected = enumerator.Current;
        int diff1 = (selected.gameObject.name == "Diff1AToggle" ? 0 : 1);
        enumerator = difficulty2ToggleGrp.ActiveToggles().GetEnumerator();
        enumerator.MoveNext();
        selected = enumerator.Current;
        int diff2 = (selected.gameObject.name == "Diff2AToggle" ? 0 : 1);
        ProposedGame newGame = new ProposedGame
        {
            gameNumber = gameBoard,
            numPlayers = numPlayers,
            diff1 = diff1,
            diff2 = diff2
        };
        xport.ReqProposeGame(newGame);
    }

    public void OnAbortPressed()
    {
        xport.ReqAbortGame();
    }

    public void OnAcceptPressed()
    {
        xport.ReqAcceptGame();
    }

    // --- Network Action Handlers -----------------------------------------------------------------------

    public void OnProposalReceived(ProposedGame game, bool inGame) {
        if (inGame)
        {
            // If the game has me and still needs another player
            // display the waiting panel
            if (game.players.Length < game.numPlayers)
            {
                waitGameDescText.text = GameDisplayString(game);
                waitPanel.SetActive(true);
                proposalPanel.SetActive(false);
                acceptPanel.SetActive(false);
                leftOutPanel.SetActive(false);
            }
            // If the game has me and is ready to play.  Move to next phase
            else
            {
                parent.GameHasBeenAgreed();
            }
            timeToAccept = -1;
            idleTime = -1;
        }
        else
        {
            // If the game doesn't have me and and still needs another player
            // display the accept panel and the counter-proposal panel
            if (game.players.Length < game.numPlayers)
            {
                bool gameWasRejected = waitPanel.activeInHierarchy;
                waitPanel.SetActive(false);
                proposalPanel.SetActive(true);
                proposalTitleText.text = PROPOSAL_TITLE_OTHER;
                acceptPanel.SetActive(true);
                acceptTitleText.text = (gameWasRejected ? ACCEPT_TITLE_OTHER : ACCEPT_TITLE_NO_OTHER);
                acceptGameDescText.text = GameDisplayString(game);
                acceptSupplementText.text = ACCEPT_SUPPLEMENT_NO_LIMIT;
                leftOutPanel.SetActive(false);
                timeToAccept = -1;
                idleTime = (idleTime < 0 ? 0 : idleTime);
            }
            // If the game doesn't have me and is a 2 player game ready to go
            // display the accept panel only and put in the countdown
            else
            {
                waitPanel.SetActive(false);
                proposalPanel.SetActive(false);
                acceptPanel.SetActive(true);
                acceptTitleText.text = ACCEPT_TITLE_NO_OTHER;
                acceptGameDescText.text = GameDisplayString(game);
                acceptSupplementText.text = ACCEPT_SUPPLEMENT_LIMIT;
                leftOutPanel.SetActive(false);
                timeToAccept = TIME_TO_JOIN_2P_GAME;
            }
        }
    }

    public void OnClearProposalReceived()
    {
        proposalPanel.SetActive(true);
        proposalTitleText.text = PROPOSAL_TITLE_NO_OTHER;
        acceptPanel.SetActive(false);
        waitPanel.SetActive(false);
        leftOutPanel.SetActive(false);
        timeToAccept = -1;
        idleTime = (idleTime < 0 ? 0 : idleTime);
    }

    public void OnStartGame()
    {
        // Game has started.  Show the left out screen.
        proposalPanel.SetActive(false);
        acceptPanel.SetActive(false);
        waitPanel.SetActive(false);
        leftOutPanel.SetActive(true);
        idleTime = -1;
    }

    public void OnGameOver()
    {
        Reset();
    }

    private string GameDisplayString(ProposedGame game)
    {
        return "Game " + (game.gameNumber + 1) + ", " + (game.numPlayers == 2 ? "2-3" : "3") + " players, " +
            (game.diff1 == 0 ? "Faster dragons" : "Slower dragons") + ", " + (game.diff2 == 0 ? "Dragons avoid sword" : "Dragons charge sword");
        
    }
}
