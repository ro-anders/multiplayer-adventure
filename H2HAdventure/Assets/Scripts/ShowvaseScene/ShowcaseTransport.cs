using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class ShowcaseTransport : MonoBehaviour
{
    public ShowcaseNetworkController networkController;
    public ShowcaseLobbyController lobbyController;

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
}
