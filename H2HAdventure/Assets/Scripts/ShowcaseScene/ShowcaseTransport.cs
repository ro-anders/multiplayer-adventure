using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ShowcaseTransport : MonoBehaviour
{
    public ShowcaseNetworkController networkController;
    public ShowcaseLobbyController lobbyController;
    public ShowcasePrestartController prestartController;
    public ShowcaseGameController gameController;

    private ShowcasePlayer thisClient;
    private List<ShowcasePlayer> allClients = new List<ShowcasePlayer>();
    private LobbyServer lobbyServer;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AddClient(ShowcasePlayer newClient)
    {
        allClients.Add(newClient);
        if (newClient.isLocalPlayer)
        {
            thisClient = newClient;
            if (thisClient.isServer)
            {
                lobbyServer = new LobbyServer(this);
            }
        }
        networkController.PlayerStarted();
    }


    // ---- Reqs and Ffls ---------------------------------------------------------------

    public void ReqProposeGame(ProposedGame newGame)
    {
        newGame.players = new int[] { thisClient.GetId() };
        string newGameJson = JsonUtility.ToJson(newGame);
        thisClient.CmdProposeGame(newGameJson);
    }

    public void FflProposeGame(ShowcasePlayer player, 
        string gameJson)
    {
        ProposedGame newGame = JsonUtility.FromJson<ProposedGame>(gameJson);
        lobbyServer.HandleProposeGame(newGame);
    }

    public void ReqAcceptGame()
    {
        thisClient.CmdAcceptGame(thisClient.GetId());
    }

    public void FflAcceptGame(int player)
    {
        lobbyServer.HandleAcceptGame(player);
    }

    public void ReqAbortGame()
    {
        thisClient.CmdAbortGame(thisClient.GetId());
    }

    public void FflAbortGame(int abortingPlayerId)
    {
        lobbyServer.HandleAbortGame(abortingPlayerId);
    }

    public void ReqReadyToStart()
    {
        thisClient.CmdReadyToStart(thisClient.GetId());
    }

    public void FflReadyToStart(int abortingPlayerId)
    {
        lobbyServer.HandleReadyToStart(abortingPlayerId);
    }


    // ---- Bcasts and Hdls ---------------------------------------------------------------

    public void BcstNewProposedGame(ProposedGame proposedGame)
    {
        string proposedGameJson = JsonUtility.ToJson(proposedGame);
        thisClient.RpcNewProposedGame(proposedGameJson);

    }

    public void HdlNewProposedGame(string serializedProposedGame)
    {
        ProposedGame proposal = JsonUtility.FromJson<ProposedGame>(serializedProposedGame);
        lobbyController.OnProposalReceived(proposal, proposal.ContainsPlayer(thisClient.GetId()));
    }

    public void BcstNoGame()
    {
        thisClient.RpcClearGame();

    }

    public void HdlNoGame()
    {
        lobbyController.OnClearProposalReceived();
    }

    public void BcstStartGame()
    {
        thisClient.RpcStartGame();

    }

    public void HdlStartGame()
    {
        lobbyController.OnStartGame();
        prestartController.OnStartGame();
    }

}
