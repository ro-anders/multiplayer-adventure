using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShowcaseLobbyController : MonoBehaviour
{
    private const string PROPOSAL_TITLE_NO_OTHER = "Propose a game";
    private const string PROPOSAL_TITLE_OTHER = "offer a counter-proposal";

    public ShowcaseController parent;

    private ShowcaseTransport transport;
    private GameObject proposalPanel;
    private Text proposalTitleText;
    private ToggleGroup gameBoardToggleGrp;
    private ToggleGroup numPlayersToggleGrp;
    private ToggleGroup difficulty1ToggleGrp;
    private ToggleGroup difficulty2ToggleGrp;
    private GameObject acceptPanel;
    private GameObject waitPanel;
    private Text waitGameDescText;
    private GameObject leftOutPanel;

    // Start is called before the first frame update
    void Start()
    {
        proposalPanel = parent.transform.Find("ShowcseTransport").gameObject;
        proposalPanel = transform.Find("ProposalPanel").gameObject;
        proposalTitleText = transform.Find("ProposalPanel/TitleText").gameObject.GetComponent<Text>();
        gameBoardToggleGrp = transform.Find("ProposalPanel/GameBoardToggleGroup").gameObject.GetComponent<ToggleGroup>();
        numPlayersToggleGrp = transform.Find("ProposalPanel/NumPlayersToggleGroup").gameObject.GetComponent<ToggleGroup>();
        difficulty1ToggleGrp = transform.Find("ProposalPanel/Difficulty1ToggleGroup").gameObject.GetComponent<ToggleGroup>();
        difficulty2ToggleGrp = transform.Find("ProposalPanel/Difficulty2ToggleGroup").gameObject.GetComponent<ToggleGroup>();
        acceptPanel = transform.Find("AcceptPanel").gameObject;
        waitPanel = transform.Find("WaitPanel").gameObject;
        leftOutPanel = transform.Find("LeftOutPanel").gameObject;
        waitGameDescText = transform.Find("WaitPanel/GameDescriptionText").gameObject.GetComponent<Text>();

        proposalPanel.SetActive(true);
        proposalTitleText.text = PROPOSAL_TITLE_NO_OTHER;
        acceptPanel.SetActive(false);
        waitPanel.SetActive(false);
        leftOutPanel.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnProposePressed()
    {
        IEnumerator<Toggle> enumerator = gameBoardToggleGrp.ActiveToggles().GetEnumerator();
        enumerator.MoveNext();
        Toggle selected = enumerator.Current;
        int gameBoard = (selected.gameObject.name == "GameBoard1Toggle" ? 0 :
            (selected.gameObject.name == "GameBoard2Toggle" ? 1 : 2));
        enumerator = numPlayersToggleGrp.ActiveToggles().GetEnumerator();
        selected = enumerator.Current;
        int numPlayers = (selected.gameObject.name == "NumPlayers2Toggle" ? 2 : 3);
        enumerator = difficulty1ToggleGrp.ActiveToggles().GetEnumerator();
        selected = enumerator.Current;
        int diff1 = (selected.gameObject.name == "Diff1AToggle" ? 0 : 1);
        enumerator = difficulty2ToggleGrp.ActiveToggles().GetEnumerator();
        selected = enumerator.Current;
        int diff2 = (selected.gameObject.name == "Diff2AToggle" ? 0 : 1);
        ProposedGame newGame = new ProposedGame
        {
            gameNumber = gameBoard,
            numPlayers = numPlayers,
            diff1 = diff1,
            diff2 = diff2
        };
        transport.ReqProposeGame(newGame);
    }

    public void OnProposalReceived(ProposedGame game, bool inGame) {
        if (inGame)
        {
            // If the game has me and still needs another player
            // display the waiting panel
            if (game.players.Length >= game.numPlayers)
            {
                waitGameDescText.text = GameDisplayString(game);
                waitPanel.SetActive(true);
                proposalPanel.SetActive(false);
                acceptPanel.SetActive(false);
                leftOutPanel.SetActive(false);

            }

            // If the game has me and is ready to play.  Move to next phase
            // TBD

        }
        else
        {
            // If the game doesn't have me and and still needs another player
            // display the accept panel and the counter-proposal panel
            // TBD

            // If the game doesn't have me and is a 2 player game ready to go
            // display the accept panel only and put in the countdown
            // TBD
        }
    }

    private string GameDisplayString(ProposedGame game)
    {
        return "snuf";
    }
}
